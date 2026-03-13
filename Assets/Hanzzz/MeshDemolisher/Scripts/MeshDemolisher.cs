using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hanzzz.MeshDemolisher
{
    public class MeshDemolisher
    {
        private static VertexAttribute[] VERTEX_TEXTURE_ATTRIBUTES = new VertexAttribute[] {
            VertexAttribute.TexCoord0, VertexAttribute.TexCoord1, VertexAttribute.TexCoord2, VertexAttribute.TexCoord3,
            VertexAttribute.TexCoord4, VertexAttribute.TexCoord5, VertexAttribute.TexCoord6, VertexAttribute.TexCoord7
        };

        private ClippedVoronoi cv;

        public MeshDemolisher()
        {
            cv = new ClippedVoronoi();
        }

        public bool VerifyDemolishInput(GameObject targetGameObject, List<Transform> demolishPoints)
        {
            List<Vector3> voronoiPoints = demolishPoints.Select(x => x.position).ToList();
            if (!DelaunayTetrahedralization.VerifyDelaunayTetrahedralizeInput(voronoiPoints)) return false;

            Transform targetTransform = targetGameObject.transform;
            if (targetTransform.localScale.x < 0f || targetTransform.localScale.y < 0f || targetTransform.localScale.z < 0f) return false;

            Mesh targetMesh = targetGameObject.GetComponent<MeshFilter>().sharedMesh;
            if (targetMesh.subMeshCount > 1) return false;

            return true;
        }

        // 1. 일반 파괴 함수 (동기 방식)
        public List<GameObject> Demolish(GameObject targetGameObject, List<Transform> demolishPoints, Material interiorMaterial)
        {
            Mesh targetMesh = targetGameObject.GetComponent<MeshFilter>().sharedMesh;
            List<Vector3> breakPoints = demolishPoints.Select(x => x.position).ToList();
            List<Vector3> meshVertices = targetMesh.vertices.Select(x => targetGameObject.transform.TransformPoint(x)).ToList();
            List<int> meshTriangles = targetMesh.triangles.ToList();

            cv.CalculateClippedVoronoi(breakPoints, meshVertices, meshTriangles);
            Material targetMeshMaterial = targetGameObject.GetComponent<MeshRenderer>().sharedMaterial;

            return ConstructGameObjects(targetMesh, targetMeshMaterial, interiorMaterial);
        }

        // 2. 비동기 파괴 함수 (에러 해결 지점!)
        public async Task<List<GameObject>> DemolishAsync(GameObject targetGameObject, List<Transform> demolishPoints, Material interiorMaterial)
        {
            Mesh targetMesh = targetGameObject.GetComponent<MeshFilter>().sharedMesh;
            List<Vector3> breakPoints = demolishPoints.Select(x => x.position).ToList();
            List<Vector3> meshVertices = targetMesh.vertices.Select(x => targetGameObject.transform.TransformPoint(x)).ToList();
            List<int> meshTriangles = targetMesh.triangles.ToList();

            // 계산 부분만 별도 쓰레드에서 실행
            await Task.Run(() => cv.CalculateClippedVoronoi(breakPoints, meshVertices, meshTriangles));

            Material targetMeshMaterial = targetGameObject.GetComponent<MeshRenderer>().sharedMaterial;
            return ConstructGameObjects(targetMesh, targetMeshMaterial, interiorMaterial);
        }

        private List<GameObject> ConstructGameObjects(Mesh targetMesh, Material targetMeshMaterial, Material interiorMaterial)
        {
            List<GameObject> res = new List<GameObject>();
            Dictionary<VertexAttribute, List<FloatStruct>> originalVerticesAttributes = GetOriginalVerticesAttributes(targetMesh);

            List<IPointLocation> clipPoints = cv.clipPoints;
            Dictionary<int, HashSet<List<int>>> clipVoronoiCellsExterior = cv.clipVoronoiCellsExterior;
            Dictionary<int, HashSet<List<int>>> clipVoronoiCellsInterior = cv.clipVoronoiCellsInterior;
            Dictionary<int, List<(List<(int, Point3D)>, double)>> exteriorPointsMappings = cv.exteriorPointsMappings;

            foreach (int cellIndex in clipVoronoiCellsExterior.Keys)
            {
                GameObject g = new GameObject($"{cellIndex}");
                MeshFilter meshFilter = g.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();
                Mesh mesh = new Mesh();
                List<Vector3> vertices = new List<Vector3>();
                Dictionary<VertexAttribute, List<FloatStruct>> newVerticesAttributes = CreateEmptyVerticesAttributes(originalVerticesAttributes);

                // (중략: 조각 생성 로직)
                int index = 0;
                List<int> trianglesExterior = new List<int>();
                List<int> trianglesInterior = new List<int>();

                foreach (var bound in clipVoronoiCellsExterior[cellIndex])
                {
                    int n = bound.Count;
                    Point3D center = bound.Aggregate(new Point3D(0d, 0d, 0d), (sum, next) => sum + clipPoints[next].ToPoint3D()) / n;
                    vertices.AddRange(bound.Select(x => clipPoints[x].ToPoint3D().ToVector3()));
                    vertices.Add(center.ToVector3());
                    InterpolateOriginalVerticesAttributes(clipPoints, bound, exteriorPointsMappings, newVerticesAttributes, originalVerticesAttributes);
                    for (int i = 0; i < n; i++) { trianglesExterior.Add(index + n); trianglesExterior.Add(index + (i + 1) % n); trianglesExterior.Add(index + i); }
                    index += n + 1;
                }

                foreach (var bound in clipVoronoiCellsInterior[cellIndex])
                {
                    int n = bound.Count;
                    Point3D center = bound.Aggregate(new Point3D(0d, 0d, 0d), (sum, next) => sum + clipPoints[next].ToPoint3D()) / n;
                    vertices.AddRange(bound.Select(x => clipPoints[x].ToPoint3D().ToVector3()));
                    vertices.Add(center.ToVector3());
                    AddDefaultVerticesAttributes(bound, newVerticesAttributes);
                    for (int i = 0; i < n; i++) { trianglesInterior.Add(index + n); trianglesInterior.Add(index + (i + 1) % n); trianglesInterior.Add(index + i); }
                    index += n + 1;
                }

                mesh.vertices = vertices.ToArray();
                Vector3 oldCenter = mesh.bounds.center;
                mesh.vertices = vertices.Select(v => v - oldCenter).ToArray();
                mesh.RecalculateBounds();
                g.transform.position = oldCenter;

                // [철벽 방어] UV & Color 데이터 입히기
                for (int j = 0; j < VERTEX_TEXTURE_ATTRIBUTES.Length; j++)
                {
                    var attr = VERTEX_TEXTURE_ATTRIBUTES[j];
                    if (originalVerticesAttributes.TryGetValue(attr, out var origList) &&
                        newVerticesAttributes.TryGetValue(attr, out var newList) && newList.Count > 0)
                    {
                        int dimension = origList[0].dimension;
                        switch (dimension)
                        {
                            case 2: mesh.SetUVs(j, newList.Select(x => x.ToVector2()).ToList()); break;
                            case 3: mesh.SetUVs(j, newList.Select(x => x.ToVector3()).ToList()); break;
                            case 4: mesh.SetUVs(j, newList.Select(x => x.ToVector4()).ToList()); break;
                        }
                    }
                }

                if (originalVerticesAttributes.ContainsKey(VertexAttribute.Color) &&
                    newVerticesAttributes.TryGetValue(VertexAttribute.Color, out var colorList) && colorList.Count > 0)
                {
                    mesh.SetColors(colorList.Select(x => x.ToColor()).ToList());
                }

                mesh.subMeshCount = 2;
                mesh.SetTriangles(trianglesExterior, 0);
                mesh.SetTriangles(trianglesInterior, 1);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                meshFilter.mesh = mesh;
                meshRenderer.materials = new Material[] { targetMeshMaterial, interiorMaterial };
                res.Add(g);
            }
            return res;
        }

        private Dictionary<VertexAttribute, List<FloatStruct>> GetOriginalVerticesAttributes(Mesh targetMesh)
        {
            Dictionary<VertexAttribute, List<FloatStruct>> res = new Dictionary<VertexAttribute, List<FloatStruct>>();
            for (int i = 0; i < VERTEX_TEXTURE_ATTRIBUTES.Length; i++)
            {
                var attr = VERTEX_TEXTURE_ATTRIBUTES[i];
                if (!targetMesh.HasVertexAttribute(attr)) continue;
                List<Vector4> temp = new List<Vector4>();
                targetMesh.GetUVs(i, temp);
                if (temp.Count > 0) res[attr] = temp.Select(x => new FloatStruct(x)).ToList();
            }
            if (targetMesh.HasVertexAttribute(VertexAttribute.Color))
            {
                List<Color> colors = new List<Color>();
                targetMesh.GetColors(colors);
                if (colors.Count > 0) res[VertexAttribute.Color] = colors.Select(x => new FloatStruct(x)).ToList();
            }
            return res;
        }

        private Dictionary<VertexAttribute, List<FloatStruct>> CreateEmptyVerticesAttributes(Dictionary<VertexAttribute, List<FloatStruct>> originalData)
        {
            Dictionary<VertexAttribute, List<FloatStruct>> res = new Dictionary<VertexAttribute, List<FloatStruct>>();
            foreach (var key in originalData.Keys) res[key] = new List<FloatStruct>();
            return res;
        }

        private void InterpolateOriginalVerticesAttributes(List<IPointLocation> clipPoints, List<int> bound, Dictionary<int, List<(List<(int, Point3D)>, double)>> mapping, Dictionary<VertexAttribute, List<FloatStruct>> newData, Dictionary<VertexAttribute, List<FloatStruct>> oldData)
        {
            Point3D normal = Point3D.Cross(clipPoints[bound[1]].ToPoint3D() - clipPoints[bound[0]].ToPoint3D(), clipPoints[bound[2]].ToPoint3D() - clipPoints[bound[1]].ToPoint3D());
            normal.Normalize(); normal *= -1d;

            Dictionary<VertexAttribute, FloatStruct> initialValues = new Dictionary<VertexAttribute, FloatStruct>();
            foreach (var kvp in oldData) if (kvp.Value.Count > 0) initialValues[kvp.Key] = kvp.Value[0].DefaultValue();

            Dictionary<VertexAttribute, FloatStruct> centerValue = new Dictionary<VertexAttribute, FloatStruct>(initialValues);

            foreach (int ptIdx in bound)
            {
                Dictionary<VertexAttribute, FloatStruct> current = new Dictionary<VertexAttribute, FloatStruct>(initialValues);
                if (mapping.TryGetValue(ptIdx, out var originalPoints))
                {
                    foreach (var op in originalPoints)
                    {
                        int closest = op.Item1[0].Item1;
                        double bestDot = Point3D.Dot(op.Item1[0].Item2, normal);
                        for (int i = 1; i < op.Item1.Count; i++)
                        {
                            double d = Point3D.Dot(op.Item1[i].Item2, normal);
                            if (d > bestDot) { bestDot = d; closest = op.Item1[i].Item1; }
                        }
                        foreach (var key in oldData.Keys) if (current.ContainsKey(key)) current[key] += (float)op.Item2 * oldData[key][closest];
                    }
                }
                foreach (var key in oldData.Keys) { newData[key].Add(current[key]); centerValue[key] += current[key]; }
            }
            float count = (float)bound.Count;
            foreach (var key in oldData.Keys) newData[key].Add(centerValue[key] / count);
        }

        private void AddDefaultVerticesAttributes(List<int> bound, Dictionary<VertexAttribute, List<FloatStruct>> newData)
        {
            int n = bound.Count + 1;
            foreach (var key in newData.Keys)
            {
                var def = newData[key].Count > 0 ? newData[key][0].DefaultValue() : new FloatStruct();
                for (int i = 0; i < n; i++) newData[key].Add(def);
            }
        }
    }
}