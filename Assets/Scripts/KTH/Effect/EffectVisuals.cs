using UnityEngine;

/// <summary>
/// 역할: 투사체 오브젝트에 붙어 실제 '외형'을 생성하고 관리합니다.
/// 설계 의도: 물리 로직(BulletBase)과 시각 연출(EffectControl) 사이의 통로 역할을 수행합니다.
/// </summary>
public class EffectVisuals : MonoBehaviour
{
    private BulletBase bulletbase;          // 물리/데이터 정보 참조
    private EffectControl currentmainEffect; // 생성된 실제 이펙트 컨트롤러

    [Header("Hit Effects")]
    public GameObject monsterHitEffectPrefab; // 할당 필요

    // [추가] 사운드 SO 할당
    [Header("Sounds")]
    public SoundDataSO hitSoundSO;

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
        if (bulletbase.Data == null || bulletbase.Data.mainEffect == null) return;

        // 1. 데이터에 등록된 화살 본체 프리팹 생성
        GameObject effGo = Instantiate(bulletbase.Data.mainEffect.prefab, transform.position, transform.rotation);

        // 2. 화살 본체 이펙트가 투사체를 따라다니도록 자식으로 설정
        effGo.transform.SetParent(this.transform);

        // 3. 컨트롤러 초기화 및 재생
        currentmainEffect = effGo.GetComponent<EffectControl>();
        if (currentmainEffect != null)
        {
            currentmainEffect.Init(bulletbase.Data.mainEffect);
            currentmainEffect.Play();
        }
    }

    /// <summary>
    /// 적중 시 호출되어 피격 연출을 실행합니다.
    /// </summary>
    // EffectVisuals.cs 수정본
    public void PlayHitVisual(Vector3 hitPosition) // Vector3 인자 추가
    {
        // 1. 파티클 생성
        if (monsterHitEffectPrefab != null)
        {
            Instantiate(monsterHitEffectPrefab, hitPosition, Quaternion.identity);
        }

        // 2. [핵심] 사운드 매니저를 통해 소리 재생 (랜덤 피치 포함)
        if (hitSoundSO != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(hitSoundSO, hitPosition);
        }

        // 3. 역경직 실행
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayHitStop(0.05f); // 0.05초간 하데스식 멈춤
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