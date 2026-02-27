using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소제목: 투사체 전투 로직 (Combat Logic)
/// 역할: 적 및 환경 충돌을 감지하고, 데미지/피드백/관통 로직을 통합 처리합니다.
/// </summary>
public class Bullet_NormalArrow : MonoBehaviour
{
    private BulletBase bulletbase;
    private EffectVisuals visuals;

    // [핵심] 적중한 적들을 기록하는 HashSet입니다.
    // 같은 대상을 여러 번 때리는 것을 방지하기 위해 IDamageable 인터페이스를 기록합니다.
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    void Awake()
    {
        bulletbase = GetComponent<BulletBase>();
        visuals = GetComponent<EffectVisuals>();
    }

    /// <summary>
    /// 실제 물리적 충돌이 일어났을 때의 로직을 처리합니다.
    /// </summary>
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
            // [핵심] 쿨타임 체크: 이미 이 화살에 맞은 적이라면 다시 데미지를 주지 않고 통과합니다.
            if (hitTargets.Contains(target)) return;

            // 세 번째 인자로 Team.Player를 전달합니다.
            float damage = 10f * bulletbase.Data.damageMultiplier;
            target.TakeDamage(damage, ElementType.None, Team.Player);

            // [핵심] 처음 맞은 적이라면 목록에 추가하여 다음 번 충돌 시 무시되도록 합니다.
            hitTargets.Add(target);

            // [변경] 투사체는 visuals에게 "나 맞았으니까 피드백 다 해줘"라고 한 줄만 보냅니다.
            if (visuals != null)
            {
                visuals.PlayHitVisual(transform.position);
            }

            // 관통 기능이 없다면 즉시 비활성화
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
