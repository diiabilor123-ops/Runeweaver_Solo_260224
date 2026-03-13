using UnityEngine;
using System.Collections.Generic;

public class DashGhostManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private float fadeSpeed = 3f;

    public void CreateGhost(Transform playerTransform)
    {
        if (ghostPrefab == null || PoolManager.Instance == null) return;

        GameObject ghostObj = PoolManager.Instance.Get(ghostPrefab, playerTransform.position, playerTransform.rotation);

        // [중요] 기존에 혹시 남아있을지 모르는 자식 메쉬들을 즉시 제거 (풀링 재사용 버그 방지)
        foreach (Transform child in ghostObj.transform)
        {
            Destroy(child.gameObject); // 여기서는 일반 Destroy도 괜찮지만, 
                                       // 즉시 비우는 것이 가장 안전합니다.
        }

        ghostObj.transform.localScale = playerTransform.localScale;
        ghostObj.SetActive(true);

        SkinnedMeshRenderer[] smrs = playerTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
        List<MeshRenderer> renderers = new List<MeshRenderer>();
        List<Mesh> bakedMeshes = new List<Mesh>();

        foreach (var smr in smrs)
        {
            if (!smr.gameObject.activeInHierarchy) continue;

            GameObject subGhost = new GameObject(smr.name + "_Mesh");
            subGhost.transform.SetParent(ghostObj.transform);

            // [중요 수정] 로컬 좌표 대신 월드 좌표를 복사하여 뼈대 위치 오차 제거
            subGhost.transform.position = smr.transform.position;
            subGhost.transform.rotation = smr.transform.rotation;
            subGhost.transform.localScale = smr.transform.lossyScale;

            Mesh mesh = new Mesh();
            smr.BakeMesh(mesh);
            bakedMeshes.Add(mesh);

            MeshFilter mf = subGhost.AddComponent<MeshFilter>();
            MeshRenderer mr = subGhost.AddComponent<MeshRenderer>();

            mf.mesh = mesh;
            mr.material = ghostMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            renderers.Add(mr);
        }

        FadeOutDestroy fade = ghostObj.GetComponent<FadeOutDestroy>();
        if (fade != null) fade.Init(ghostPrefab, renderers, bakedMeshes, fadeSpeed);
    }
}