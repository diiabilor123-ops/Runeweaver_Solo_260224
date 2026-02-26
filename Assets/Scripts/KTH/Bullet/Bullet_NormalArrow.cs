using UnityEngine;

/// <summary>
/// 소제목: 투사체 전투 로직 (Combat Logic)
/// 역할: 적 및 환경 충돌을 감지하고, 데미지/피드백/관통 로직을 통합 처리합니다.
/// </summary>
public class Bullet_NormalArrow : MonoBehaviour
{
    private BulletBase bulletbase;
    private EffectVisuals visuals;

    void Awake()
    {
        bulletbase = GetComponent<BulletBase>();
        visuals = GetComponent<EffectVisuals>();
    }

    /// <summary>
    /// 실제 물리적 충돌이 일어났을 때의 로직을 처리합니다.
    /// </summary>
    // Bullet_BasicAttack.cs (수정 제안본)
    private void OnTriggerEnter(Collider other)
    {
        // 0. 유효성 검사: 이미 꺼진 탄이면 계산하지 않음
        if (bulletbase == null || !bulletbase.IsActive) return;

        // 1. [환경 충돌] 벽이나 장애물에 부딪혔을 때
        if (other.CompareTag("Wall"))
        {
            visuals.PlayHitVisual(transform.position); // 시각/사운드 통합 처리 요청
            bulletbase.Deactivate();
            return;
        }

        // 2. [전투 충돌] 데미지를 입을 수 있는 대상(IDamageableTest)인지 확인
        // 전투 충돌
        if (other.TryGetComponent(out IDamageable target))
        {
            // 세 번째 인자로 Team.Player를 전달합니다.
            float damage = 10f * bulletbase.Data.damageMultiplier;
            target.TakeDamage(damage, ElementType.None, Team.Player);

            // [변경] 투사체는 visuals에게 "나 맞았으니까 피드백 다 해줘"라고 한 줄만 보냅니다.
            if (visuals != null)
            {
                visuals.PlayHitVisual(transform.position);
            }

            if (!bulletbase.Data.isPenetrating)
            {
                bulletbase.Deactivate();
            }
        }
    }

    /// <summary>
    /// 적중 시 시각 연출(VFX)을 통합 관리합니다.
    /// </summary>
    private void HandleImpactEffect()
    {
        if (visuals != null)
        {
            visuals.PlayHitVisual(transform.position);
        }
    }
}
