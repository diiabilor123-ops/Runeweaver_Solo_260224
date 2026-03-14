using Runeweaver;
using Runeweaver.Augment; // PlayerAugment 네임스페이스 추가
using UnityEngine;
using System.Collections.Generic;

public struct DamageResult
{
    public float finalDamage;
    public bool isCritical;
}

public static class DamageCalculator
{
    public static DamageResult Calculate(float baseAmount, ElementType singleElement, Team attackerTeam, EnemyData targetData = null)
    {
        List<ElementType> tempElements = new List<ElementType>();
        if (singleElement != ElementType.None) tempElements.Add(singleElement);

        return Calculate(baseAmount, tempElements, attackerTeam, targetData);
    }

    /// <summary>
    /// [수정] attackElement를 ElementEnemyType으로 받아 상성 계산을 수행하고,
    /// 플레이어의 경우 PlayerAugment의 실시간 스탯을 반영합니다.
    /// </summary>
    public static DamageResult Calculate(float baseAmount, List<ElementType> playerElements, Team attackerTeam, EnemyData targetData = null)
    {
        DamageResult result = new DamageResult();

        // playerElements가 null이면 빈 리스트로 초기화해서 에러 방지
        if (playerElements == null) playerElements = new List<ElementType>();

        var left = PlayerAugment.Instance.leftClick;
        var pass = PlayerAugment.Instance.passive;

        // 1. 기본 보너스 (1, 3, 5단계 스탯 증가)
        float critChance = 10f; // 기본 치명타 확률
        float damageModifier = 1.0f;
        float critDamageMultiplier = 2.0f; // 기본 치명타 배율

        if (attackerTeam == Team.Player)
        {
            // [SO 연동] 화염 왼클릭 스택에 따른 치명타 확률 증가 (예: 1, 3, 5스택 수치 적용)
            float fireBonus = AugmentManager.Instance.GetAugmentValue(SkillSlotType.LeftClick, ElementType.Fire, left.GetStack(ElementType.Fire));
            critChance += fireBonus * 100f;

            // [SO 연동] 얼음 왼클릭 스택에 따른 데미지 증가 (예: 1, 3, 5스택 수치 적용)
            float iceBonus = AugmentManager.Instance.GetAugmentValue(SkillSlotType.LeftClick, ElementType.Ice, left.GetStack(ElementType.Ice));
            damageModifier += iceBonus;
        }

        // 2. [신규] 4스택 보너스 적용 (LeftClick 기준)
        foreach (var type in playerElements)
        {
            if (left.GetStack(type) >= 4)
            {
                if (type == ElementType.Fire) critDamageMultiplier += 0.5f; // 화염 4스택: 치명타 피해 +50%
                if (type == ElementType.Ice) damageModifier += 0.25f;       // 얼음 4스택: 공격력 +25%
                // 번개 4스택은 공속이므로 계산기 밖(PlayerCombat 등)에서 처리됨
            }
        }

        // 3. 상성 계산 (몬스터 원소 vs 플레이어 발사체 원소)
        float elementalMultiplier = 1.0f;
        if (targetData != null && playerElements.Count > 0)
        {
            MonsterElement targetType = targetData.mainElement;
            bool hasResistanceIgnore = false;

            // 각 원소별 상성 체크
            foreach (var pElem in playerElements)
            {
                if (pass.IsMastered(pElem))
                {
                    // 6스택일 때 SO의 stepValues[5]에 설정된 추가 데미지 배율 적용
                    damageModifier += AugmentManager.Instance.GetAugmentValue(SkillSlotType.Passive, pElem, 6);
                }

                float currentTypeMultiplier = 1.0f;

                // 번개 > 얼음 / 얼음 > 불 / 불 > 번개 (유리)
                if ((pElem == ElementType.Volt && targetType == MonsterElement.M_Ice) ||
                    (pElem == ElementType.Ice && targetType == MonsterElement.M_Fire) ||
                    (pElem == ElementType.Fire && targetType == MonsterElement.M_Volt))
                {
                    currentTypeMultiplier = 1.5f;
                }
                // 불 < 얼음 / 얼음 < 번개 / 번개 < 불 (불리)
                else if ((pElem == ElementType.Fire && targetType == MonsterElement.M_Ice) ||
                         (pElem == ElementType.Ice && targetType == MonsterElement.M_Volt) ||
                         (pElem == ElementType.Volt && targetType == MonsterElement.M_Fire))
                {
                    // 6스택 패시브가 있다면 0.75배 저항을 무시하고 1.0배로 계산
                    currentTypeMultiplier = hasResistanceIgnore ? 1.0f : 0.75f;
                }

                // 여러 원소가 섞여있을 경우 가장 유리한 상성 적용 (또는 평균값, 기획에 따라 조절)
                elementalMultiplier = Mathf.Max(elementalMultiplier, currentTypeMultiplier);
            }


        }

        // 4. [신규] 6스택 패시브 데미지 보너스
        foreach (var pElem in playerElements)
        {
            if (pass.GetStack(pElem) >= 6)
            {
                damageModifier += 0.5f; // 속성 데미지 50% 증가
            }
        }

        // 5. 최종 계산
        float totalBaseDamage = baseAmount * damageModifier * elementalMultiplier;
        result.isCritical = Random.Range(0f, 100f) < critChance;
        result.finalDamage = result.isCritical ? totalBaseDamage * critDamageMultiplier : totalBaseDamage;

        return result;
    }
}