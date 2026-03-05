using UnityEngine;

/// <summary>
/// 역할: 투사체 오브젝트에 붙어 실제 '외형'을 생성하고 관리합니다.
/// 설계 의도: 물리 로직(BulletBase)과 시각 연출(EffectControl) 사이의 통로 역할을 수행합니다.
/// </summary>
public class EffectVisuals : MonoBehaviour
{
    private BulletBase bulletbase;          // 물리/데이터 정보 참조
    private EffectControl currentmainEffect; // 생성된 실제 이펙트 컨트롤러

    [Header("Fallbacks")]
    [Tooltip("데이터에 적중 이펙트가 없을 경우 사용할 기본 프리팹")]
    public GameObject defaultHitEffectPrefab;

    void Awake()
    {
        // 동일한 오브젝트에 붙은 물리 기반 스크립트 참조
        bulletbase = GetComponent<BulletBase>();
    }

    /// <summary>
    /// 투사체가 발사될 때 호출되어 외형을 초기화합니다.
    /// </summary>
    public void InitializeVisuals()
    {
        if (bulletbase == null || bulletbase.Data == null) return;

        // 1. [풀링 대응] 이전 발사의 흔적 완벽 정리
        ClearVisuals();

        // --- [핵심 추가] 이전 발사의 흔적 정리 ---
        // 재사용 시 기존에 붙어있던 자식 이펙트가 있다면 정리합니다.
        if (currentmainEffect != null)
        {
            // 이펙트 컨트롤러가 풀링을 지원한다면 반납 로직을 넣고,
            // 아니면 그냥 파괴하거나 비활성화합니다.
            Destroy(currentmainEffect.gameObject);
            currentmainEffect = null;
        }

        // 2. 메인 이펙트(화살 본체) 생성
        if (bulletbase.Data.mainEffect != null && bulletbase.Data.mainEffect.prefab != null)
        {
            GameObject effGo = Instantiate(bulletbase.Data.mainEffect.prefab, transform.position, transform.rotation);
            effGo.transform.SetParent(this.transform);

            currentmainEffect = effGo.GetComponent<EffectControl>();
            if (currentmainEffect != null)
            {
                currentmainEffect.Init(bulletbase.Data.mainEffect);
                currentmainEffect.Play();
            }
        }

        // 3. 비행 사운드 재생
        // (발사 사운드는 BulletBase.Setup에서 재생하므로, 여기서는 루프되는 비행음만 담당하는 것이 좋습니다)
        if (bulletbase.Data.flySound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(bulletbase.Data.flySound, transform.position);
        }
    }

    /// <summary>
    /// [추가] 화살이 풀로 돌아갈 때 호출되어야 할 정리 함수
    /// </summary>
    public void ClearVisuals()
    {
        // 1. 트레일 초기화 (가장 중요: 잔상 방지)
        var trails = GetComponentsInChildren<TrailRenderer>();
        foreach (var trail in trails)
        {
            trail.Clear();
        }

        // 2. 파티클 초기화
        var particles = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Clear();
            ps.Stop();
        }

        // 3. 생성되었던 메인 모델(EffectControl) 제거
        if (currentmainEffect != null)
        {
            Destroy(currentmainEffect.gameObject);
            currentmainEffect = null;
        }

        // 4. 혹시 모를 자식 오브젝트들 정리 (프리팹이 남는 경우 대비)
        foreach (Transform child in transform)
        {
            // TrailRenderer가 붙은 핵심 축(가시적 외형)이 아니라면 삭제
            if (child.GetComponent<TrailRenderer>() == null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 적중 시 호출되어 피격 연출을 실행합니다.
    /// </summary>
    public void PlayHitVisual(Vector3 hitPosition, GameObject specificHitEffect = null)
    {
        if (bulletbase == null || bulletbase.Data == null) return;

        // 1. 적중 사운드
        if (bulletbase.Data.hitSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(bulletbase.Data.hitSound, hitPosition);
        }

        // 2. 적중 이펙트 결정
        GameObject effectToSpawn = null;

        if (specificHitEffect != null)
        {
            effectToSpawn = specificHitEffect;
        }
        else if (bulletbase.Data.hitEffect != null)
        {
            // 구조에 따른 방어적 참조
            effectToSpawn = bulletbase.Data.hitEffect.prefab;

            // 만약 prefab이 null이고 hitEffectPrefabs 배열이 있다면 첫 번째 사용
            if (effectToSpawn == null && bulletbase.Data.hitEffect.hitEffectPrefabs != null && bulletbase.Data.hitEffect.hitEffectPrefabs.Length > 0)
            {
                effectToSpawn = bulletbase.Data.hitEffect.hitEffectPrefabs[0];
            }
        }

        if (effectToSpawn == null) effectToSpawn = defaultHitEffectPrefab;

        // 3. 이펙트 생성 및 자동 파괴(또는 풀링)
        if (effectToSpawn != null)
        {
            GameObject go = Instantiate(effectToSpawn, hitPosition, Quaternion.identity);
            // 피격 이펙트는 보통 1~2초 뒤에 사라지게 설정 (풀링 매니저가 있다면 PoolManager.Instance.Release 권장)
            Destroy(go, 2f);
        }
    }

    // [추가] 몬스터 적중 시 연출 처리
    public void PlayMonsterHitVisual(Vector3 hitPosition, GameObject specificHitEffect = null)
    {
        // 1. HitData에 전달된 전용 이펙트가 있다면 우선 생성
        if (specificHitEffect != null)
        {
            Instantiate(specificHitEffect, hitPosition, Quaternion.identity);
        }
        // 2. 없다면 데이터에 설정된 기본 적중 이펙트 생성
        else if (bulletbase.Data != null && bulletbase.Data.hitEffect != null)
        {
            Instantiate(bulletbase.Data.hitEffect, hitPosition, Quaternion.identity);
        }

        // 3. 사운드 재생 (HitData에서 받아오면 더 좋음)
        if (bulletbase.Data != null && bulletbase.Data.hitSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(bulletbase.Data.hitSound, hitPosition);
        }
    }
}