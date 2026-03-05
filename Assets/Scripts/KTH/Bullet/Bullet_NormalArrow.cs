using System.Collections.Generic;
using UnityEngine;
using Runeweaver; // 추가

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
            hitTargets.Add(target);

            // [핵심 수정] 1. DamageCalculator를 통해 데미지와 크리티컬 여부 계산
            // 기본 데미지(10)에 SO의 배율을 곱해서 전달합니다.
            float baseDamage = 10f * bulletbase.Data.damageMultiplier;
            // [참고] AugmentManager에 GetAddedDamage가 없다면 0f를 넣거나 해당 함수를 구현해야 합니다.
            float extraDamage = 0f;

            // 2. [핵심 수정] 단일 원소가 아닌 리스트 전체를 전달
            // 이제 DamageCalculator가 리스트 내부의 원소들을 순회하며 4스택/6스택 보너스를 계산합니다.
            DamageResult result = DamageCalculator.Calculate(
                baseDamage + extraDamage,
                bulletbase.AppliedElements, // List<ElementType>을 그대로 전달
                Team.Player,
                target.enemyData
            );

            // 1. [수정] 플레이어 원소를 몬스터 상성용 원소(MonsterElement)로 변환
            ElementType pElem = (bulletbase.AppliedElements.Count > 0) ? bulletbase.AppliedElements[0] : ElementType.None;
            MonsterElement mElem = ConvertToMonsterElement(pElem);


            // [수정] 화살이 데이터를 가져와서 보따리를 싸서 몬스터에게 전달!
            HitData hit = new HitData
            {
                damage = result.finalDamage,       // 계산된 최종 데미지 적용
                isCritical = result.isCritical,    // 크리티컬 여부 전달
                element = mElem,               // [수정] MonsterElement 할당
                attackElement = pElem,         // [수정] 원본 ElementType 할당
                attackerTeam = Team.Player,
                hitPoint = transform.position,
                attackerPos = transform.position, // 화살 위치에서 넉백 발생
                hitEffectPrefab = visuals.monsterHitEffectPrefab // 화살 데이터에 설정된 몬스터 피격 파티클
            };

            // 보따리 전달
            target.TakeDamage(hit);

            if (visuals != null) visuals.PlayHitVisual(transform.position);
            if (!bulletbase.Data.isPenetrating) bulletbase.Deactivate();
        }
    }

    /// <summary>
    /// 플레이어의 원소 타입을 몬스터 상성 계산용 타입으로 변환합니다.
    /// </summary>
    private MonsterElement ConvertToMonsterElement(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return MonsterElement.M_Fire;
            case ElementType.Ice: return MonsterElement.M_Ice;
            case ElementType.Volt: return MonsterElement.M_Volt;
            default: return MonsterElement.M_None;
        }

    }
}
