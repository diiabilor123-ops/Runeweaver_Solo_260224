using UnityEngine;

public class DashGhostManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Material ghostMaterial; // lilToon 반투명 머티리얼
    [SerializeField] private float fadeSpeed = 3f;    // 잔상이 사라지는 속도

    public void CreateGhost(Transform playerTransform)
    {
        // 1. 빈 잔상 부모 오브젝트 생성
        GameObject ghostObj = new GameObject("DashGhost_P90");
        ghostObj.transform.position = playerTransform.position;
        ghostObj.transform.rotation = playerTransform.rotation;
        ghostObj.transform.localScale = playerTransform.localScale;

        // 2. 플레이어 자식에 있는 모든 SkinnedMeshRenderer를 찾아서 메쉬를 굽습니다.
        SkinnedMeshRenderer[] smrs = playerTransform.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var smr in smrs)
        {
            if (!smr.gameObject.activeInHierarchy) continue;

            // 자식 오브젝트 생성
            GameObject subGhost = new GameObject(smr.name + "_Mesh");
            subGhost.transform.SetParent(ghostObj.transform);
            subGhost.transform.localPosition = smr.transform.localPosition;
            subGhost.transform.localRotation = smr.transform.localRotation;
            subGhost.transform.localScale = smr.transform.localScale;

            // 메쉬 데이터 굽기 (애니메이션 포즈 고정)
            Mesh mesh = new Mesh();
            smr.BakeMesh(mesh);

            // 렌더링 컴포넌트 추가
            MeshFilter mf = subGhost.AddComponent<MeshFilter>();
            MeshRenderer mr = subGhost.AddComponent<MeshRenderer>();

            mf.mesh = mesh;
            mr.material = ghostMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // 3. 수명 관리 스크립트 추가 (부모에 달아줍니다)
        // [주의] FadeOutDestroy가 자식들의 렌더러도 제어할 수 있게 수정되어야 합니다.
        FadeOutDestroy fade = ghostObj.AddComponent<FadeOutDestroy>();
        fade.fadeSpeed = fadeSpeed;
    }
}