using Hanzzz.MeshDemolisher;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public List<GameObject> tiles;
    public Material interiorMaterial;
    public NavMeshSurface navMeshSurface;
    private HashSet<int> destroyingIndices = new HashSet<int>();
    private static MeshDemolisher _meshDemolisher = new MeshDemolisher();

    private bool _isNavMeshUpdatePending = false;

    // 상하좌우 인접 데이터 (기존 유지)
    private readonly int[][] neighbors = new int[][] {
        new int[] { 1, 3 }, new int[] { 0, 2, 4 }, new int[] { 1, 5 },
        new int[] { 0, 4, 6 }, new int[] { 1, 3, 5, 7 }, new int[] { 2, 4, 8 },
        new int[] { 3, 7 }, new int[] { 4, 6, 8 }, new int[] { 5, 7 }
    };

    // 패턴 매니저가 호출할 "안전한 타일 찾기"
    public int GetSafeTileToDestroy()
    {
        List<int> currentActive = new List<int>();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (i != 4 && tiles[i].activeSelf && !destroyingIndices.Contains(i))
                currentActive.Add(i);
        }

        currentActive = currentActive.OrderBy(x => Random.value).ToList();
        foreach (int testIndex in currentActive)
        {
            if (CanPathToCenterAfterDestroy(testIndex))
            {
                destroyingIndices.Add(testIndex); // 미리 예약
                return testIndex;
            }
        }
        return -1;
    }

    public void RequestNavMeshRebuild()
    {
        if (_isNavMeshUpdatePending) return;
        StartCoroutine(RebuildNavMeshDelayed());
    }

    private IEnumerator RebuildNavMeshDelayed()
    {
        _isNavMeshUpdatePending = true;
        yield return new WaitForEndOfFrame(); // 모든 타일 파괴 처리가 끝난 후 실행
        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
        _isNavMeshUpdatePending = false;
    }

    // 스킬 적중 시점에 맞춰 호출될 파괴 함수
    public void DestroyTileWithDelay(int index, float delay)
    {
        StartCoroutine(ExecuteDestroy(index, delay));
    }

    private IEnumerator ExecuteDestroy(int index, float delay)
    {
        yield return new WaitForSeconds(delay); // 스킬 낙하 시간 대기

        GameObject tile = tiles[index];
        if (!tile.activeSelf) yield break;

        PointGenerator pg = tile.GetComponent<PointGenerator>();

        if (pg != null && pg.pointsParent != null)
        {
            List<Transform> breakPoints = new List<Transform>();
            foreach (Transform child in pg.pointsParent) breakPoints.Add(child);

            List<GameObject> fragments = _meshDemolisher.Demolish(tile, breakPoints, interiorMaterial);

            int count = 0;
            foreach (var frag in fragments)
            {
                Rigidbody rb = frag.AddComponent<Rigidbody>();
                MeshCollider mc = frag.AddComponent<MeshCollider>();
                mc.convex = true; // 이 작업이 매우 무겁습니다.

                rb.AddExplosionForce(400f, tile.transform.position, 7f);
                Destroy(frag, 3f);

                // 파편 5개마다 한 프레임씩 쉬어줌 (렉 분산)
                count++;
                if (count % 5 == 0) yield return null;
            }
        }

        tile.SetActive(false);
        destroyingIndices.Remove(index);

        // [핵심] 타일마다 빌드하는 게 아니라 예약만 함
        RequestNavMeshRebuild();
    }

    private bool CanPathToCenterAfterDestroy(int indexToDestroy)
    {
        HashSet<int> activeTiles = new HashSet<int>();
        for (int i = 0; i < tiles.Count; i++)
        {
            // 핵심 수정: 실제로 꺼져있는 타일 뿐만 아니라, 
            // 이미 파괴 예약된 타일(destroyingIndices)과 현재 테스트 중인 타일도 제외하고 계산합니다.
            if (tiles[i].activeSelf && i != indexToDestroy && !destroyingIndices.Contains(i))
            {
                activeTiles.Add(i);
            }
        }

        // 만약 중앙 타일(4번) 외에 남은 타일이 없다면 경로를 확인할 필요가 없습니다.
        if (activeTiles.Count == 0) return true;

        Queue<int> queue = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();

        // 중앙 타일이 활성화되어 있는지 확인 (항상 4번이 기준)
        if (tiles[4].activeSelf && !destroyingIndices.Contains(4))
        {
            queue.Enqueue(4);
            visited.Add(4);
        }
        else
        {
            // 만약 중앙 타일마저 부서질 예정이라면 이 로직은 성립하지 않습니다.
            return false;
        }

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int n in neighbors[current])
            {
                if (activeTiles.Contains(n) && !visited.Contains(n))
                {
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
        }

        // 살아남아야 하는 모든 타일이 중앙(4번)으로 연결되는지 확인
        return visited.Count == activeTiles.Count;
    }

    // TileManager.cs에 추가할 함수
    public void PerformDemolish(int index)
    {
        if (index == -1 || !tiles[index].activeSelf) return;

        GameObject tile = tiles[index];
        PointGenerator pg = tile.GetComponent<PointGenerator>();

        if (pg != null && pg.pointsParent != null)
        {
            List<Transform> breakPoints = new List<Transform>();
            foreach (Transform child in pg.pointsParent) breakPoints.Add(child);

            // [핵심] 기존의 _meshDemolisher.Demolish 호출 (Static 객체 사용)
            var fragments = _meshDemolisher.Demolish(tile, breakPoints, interiorMaterial);

            foreach (var frag in fragments)
            {
                Rigidbody rb = frag.AddComponent<Rigidbody>();
                frag.AddComponent<MeshCollider>().convex = true;
                rb.AddExplosionForce(500f, tile.transform.position, 10f);
                Destroy(frag, 3f);
            }
        }

        tile.SetActive(false);
        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
        // 파괴 리스트에서 제거하여 다음 랜덤 선정에 영향 안 주게 함
        destroyingIndices.Remove(index);
    }

    public List<int> GetAllActiveTilesExceptCenter()
    {
        List<int> activeList = new List<int>();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (i != 4 && tiles[i].activeSelf)
            {
                activeList.Add(i);
                // 한 번에 여러 개가 파괴되므로 미리 예약 리스트에 넣어 중복 방지
                if (!destroyingIndices.Contains(i)) destroyingIndices.Add(i);
            }
        }
        return activeList;
    }
}