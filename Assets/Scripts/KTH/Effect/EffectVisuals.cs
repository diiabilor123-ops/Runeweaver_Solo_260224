using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 역할: 불릿 프리팹의 자식으로 붙어있는 시각적 요소(Mesh, Particle, VFX Graph)를 제어합니다.
/// 수정 사항: 
/// 1. Instantiate 로직 제거 (이미 자식으로 존재하므로)
/// 2. VFX Graph 이벤트 ("create", "Hit") 연동
/// 3. 풀링 시스템 대응을 위한 Clear 로직 최적화
/// </summary>
public class EffectVisuals : MonoBehaviour
{
    private BulletBase bulletbase;

    [Header("Fallbacks")]
    [Tooltip("데이터에 적중 이펙트가 없을 경우 사용할 기본 프리팹")]
    public GameObject defaultHitEffectPrefab;

    void Awake()
    {
        bulletbase = GetComponent<BulletBase>();
    }

    /// <summary>
    /// 투사체가 발사될 때 호출됩니다. 자식으로 붙어있는 비주얼들을 재생합니다.
    /// </summary>
    public void InitializeVisuals()
    {
        if (bulletbase == null || bulletbase.Data == null) return;

        // 1. [풀링 대응] 이전 발사 흔적(트레일 등) 완벽 정리
        ClearVisuals();

        // 2. 발사 시 캐릭터 위치 VFX (바람/파동 등 - 이건 새로 생성하는 게 맞습니다)
        if (bulletbase.Data.shootVFX != null)
        {
            GameObject vfx = Instantiate(bulletbase.Data.shootVFX, transform.position, transform.rotation);
            Destroy(vfx, 1f);
        }

        // 3. 비행 사운드 재생
        if (bulletbase.Data.flySound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(bulletbase.Data.flySound, transform.position);
        }

        // 4. [핵심] 자식 오브젝트의 컴포넌트들 찾아 실행

        // 파티클 시스템 실행
        var particles = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Clear(); // 찌꺼기 제거
            ps.Play();
        }

        // VFX Graph 실행 (유저님 요청대로 "create" 이벤트 사용)
        var vfxGraphs = GetComponentsInChildren<VisualEffect>();
        foreach (var vfx in vfxGraphs)
        {
            if (bulletbase.Data.isVFXGraph)
            {
                // SO에 설정이 없으면 기본값 "create" 사용
                string evName = string.IsNullOrEmpty(bulletbase.Data.startEvent) ? "create" : bulletbase.Data.startEvent;
                vfx.SendEvent(evName);
            }
            else vfx.Play();
        }

        // 트레일 렌더러 초기화 (순간이동 시 선 꼬임 방지)
        var trails = GetComponentsInChildren<TrailRenderer>();
        foreach (var trail in trails)
        {
            trail.Clear();
        }
    }

    /// <summary>
    /// 화살이 사라질 때 호출되어 모든 시각 효과를 멈춥니다. (Destroy 하지 않음)
    /// </summary>
    public void ClearVisuals()
    {
        var trails = GetComponentsInChildren<TrailRenderer>();
        foreach (var trail in trails) trail.Clear();

        var particles = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Clear();
            ps.Stop();
        }

        var vfxGraphs = GetComponentsInChildren<VisualEffect>();
        foreach (var vfx in vfxGraphs) vfx.Stop();
    }

    /// <summary>
    /// 적중 시 호출되어 피격 연출을 실행하고 VFX Graph에 "Hit" 이벤트를 보냅니다.
    /// </summary>
    public void PlayHitVisual(Vector3 hitPosition, GameObject specificHitEffect = null)
    {
        if (bulletbase == null || bulletbase.Data == null) return;

        // 1. 사운드 재생
        if (bulletbase.Data.hitSound != null && SoundManager.Instance != null)
            SoundManager.Instance.Play(bulletbase.Data.hitSound, hitPosition);

        // 2. 외부 히트 이펙트 생성 (피격 위치에 터지는 이펙트)
        GameObject effectToSpawn = specificHitEffect;
        if (effectToSpawn == null && bulletbase.Data.hitEffectPrefabs != null && bulletbase.Data.hitEffectPrefabs.Length > 0)
            effectToSpawn = bulletbase.Data.hitEffectPrefabs[0];

        if (effectToSpawn == null) effectToSpawn = defaultHitEffectPrefab;

        if (effectToSpawn != null)
        {
            GameObject go = Instantiate(effectToSpawn, hitPosition, Quaternion.identity);
            Destroy(go, 2f);
        }

        // 3. 자식에 있는 VFX Graph에 "Hit" 이벤트 전송
        if (bulletbase.Data.isVFXGraph)
        {
            var vfxGraphs = GetComponentsInChildren<VisualEffect>();
            foreach (var vfx in vfxGraphs)
            {
                string evName = string.IsNullOrEmpty(bulletbase.Data.hitEvent) ? "Hit" : bulletbase.Data.hitEvent;
                vfx.SendEvent(evName);
            }
        }
    }

    public void PlayMonsterHitVisual(Vector3 hitPosition, GameObject specificHitEffect = null)
    {
        PlayHitVisual(hitPosition, specificHitEffect);
    }
}