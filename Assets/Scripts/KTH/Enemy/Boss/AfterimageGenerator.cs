using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AfterimageGenerator : MonoBehaviour
{
    [System.Serializable]
    public class GhostData
    {
        public Mesh mesh;
        public Vector3 position;
        public Quaternion rotation;
        public float timeCreated;
    }

    [Header("설정")]
    [SerializeField] private Material ghostMaterial; // 잔상 전용 반투명 쉐이더 머티리얼
    [SerializeField] private float ghostDuration = 0.5f; // 잔상 유지 시간
    [SerializeField] private float spawnInterval = 0.05f; // 잔상 생성 간격

    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private List<GhostData> activeGhosts = new List<GhostData>();
    private float lastSpawnTime;
    private bool isGenerating = false;

    private void Awake()
    {
        // 보스의 모든 부위(SkinnedMeshRenderer)를 가져옵니다.
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    public void StartAfterimage() => isGenerating = true;
    public void StopAfterimage() => isGenerating = false;

    private void Update()
    {
        // 1. 잔상 데이터 생성 (Bake)
        if (isGenerating && Time.time >= lastSpawnTime + spawnInterval)
        {
            RecordGhost();
            lastSpawnTime = Time.time;
        }

        // 2. 저장된 메쉬들을 화면에 그리기
        DrawGhosts();
    }

    private void RecordGhost()
    {
        foreach (var smr in skinnedMeshRenderers)
        {
            if (smr == null) continue;

            // 현재 애니메이션이 적용된 메쉬 상태를 구워냅니다 (Bake)
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);

            activeGhosts.Add(new GhostData
            {
                mesh = bakedMesh,
                position = smr.transform.position,
                rotation = smr.transform.rotation,
                timeCreated = Time.time
            });
        }
    }

    private void DrawGhosts()
    {
        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            float elapsed = Time.time - activeGhosts[i].timeCreated;

            // 지속 시간이 지난 잔상은 제거 (메쉬 메모리 해제 필수)
            if (elapsed >= ghostDuration)
            {
                Destroy(activeGhosts[i].mesh);
                activeGhosts.RemoveAt(i);
                continue;
            }

            // [핵심] 그래픽스 명령으로 메쉬를 직접 그립니다.
            // _Alpha 등의 변수를 가진 쉐이더를 사용하여 서서히 사라지게 할 수 있습니다.
            float alpha = 1f - (elapsed / ghostDuration);
            ghostMaterial.SetFloat("_Alpha", alpha); // 쉐이더에 투명도 전달

            Graphics.DrawMesh(
                activeGhosts[i].mesh,
                activeGhosts[i].position,
                activeGhosts[i].rotation,
                ghostMaterial,
                0
            );
        }
    }
    /// <summary>
    /// 특정 위치와 회전값으로 즉시 잔상을 하나 남깁니다. (순간이동 예고용)
    /// </summary>
    public void RecordGhostAt(Vector3 pos, Quaternion rot)
    {
        foreach (var smr in skinnedMeshRenderers)
        {
            if (smr == null) continue;

            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);

            activeGhosts.Add(new GhostData
            {
                mesh = bakedMesh,
                position = pos, // 입력받은 위치 사용
                rotation = rot, // 입력받은 회전 사용
                timeCreated = Time.time
            });
        }
    }
}