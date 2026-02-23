using UnityEngine;

/// <summary>
/// 역할: 투사체 오브젝트에 붙어 실제 '외형'을 생성하고 관리합니다.
/// 설계 의도: 물리 로직(BulletBase)과 시각 연출(EffectControl) 사이의 통로 역할을 수행합니다.
/// </summary>
public class EffectVisuals : MonoBehaviour
{
    private BulletBase bulletbase;          // 물리/데이터 정보 참조
    private EffectControl currentmainEffect; // 생성된 실제 이펙트 컨트롤러

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
        // 1. 시각 효과 (VFX) 실행
        if (currentmainEffect != null)
        {
            currentmainEffect.TriggerHit(bulletbase.Data.isPenetrating, hitPosition);
        }

        // 2. 사운드 재생
        if (bulletbase.Data.hitSound != null && SoundManager.Instance != null)
        {
            // SoundManager가 이제 위치값을 받으므로 hitPosition을 넘겨줍니다.
            SoundManager.Instance.Play(bulletbase.Data.hitSound, hitPosition);
        }

        // 3. 역경직 실행
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayHitStop(0.05f); // 0.05초간 하데스식 멈춤
        }
    }
}