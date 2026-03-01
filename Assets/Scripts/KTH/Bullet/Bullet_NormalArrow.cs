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
        if (other.CompareTag("Wall")) { /* 기존 벽 충돌 로직 */ return; }

        // 2. [전투 충돌] 데미지를 입을 수 있는 대상(IDamageableTest)인지 확인
        // 전투 충돌
        if (other.TryGetComponent(out EnemyHealth target))
        {
            if (hitTargets.Contains(target)) return;

            // [수정] 화살이 데이터를 가져와서 보따리를 싸서 몬스터에게 전달!
            HitData hit = new HitData
            {
                damage = 10f * bulletbase.Data.damageMultiplier,
                element = ElementType.None, // 화살 SO에서 가져오면 더 좋음
                attackerTeam = Team.Player,
                hitPoint = transform.position,
                attackerPos = transform.position, // 화살 위치에서 넉백 발생
                hitEffectPrefab = visuals.monsterHitEffectPrefab // 화살 데이터에 설정된 몬스터 피격 파티클
            };

            // 보따리 전달
            target.TakeDamage(hit);

            hitTargets.Add(target);
            // 투사체가 적중했다는 사실만 EffectVisuals에 알림
            if (visuals != null) visuals.PlayHitVisual(transform.position);
            if (!bulletbase.Data.isPenetrating) bulletbase.Deactivate();
        }
    }

}
