using UnityEngine;

public struct DamageResult
{
    public float finalDamage;
    public bool isCritical;
}

public static class DamageCalculator
{
    public static DamageResult Calculate(float baseAmount, ElementType attackElement, Team attackerTeam, EnemyData targetData = null)
    {
        DamageResult result = new DamageResult();

        // [하데스 스타일 설계] 공격자 팀에 따라 치명타 확률을 다르게 세팅
        float critChance = 0f;
        float critMult = 2.0f;

        if (attackerTeam == Team.Player)
        {
            // 플레이어 기본 치명타 확률 (나중에 룬 시스템에서 이 값을 참조하게 됩니다)
            critChance = 20f;
        }
        else
        {
            // 몬스터는 치명타가 없거나 아주 낮게 설정
            critChance = 0f;
        }

        // 1. 원소 상성 계산 (기존 EnemyHealth에 있던 로직을 여기로 가져왔습니다)
        float multiplier = 1.0f;
        if (targetData != null)
        {
            if (targetData.mainElement == ElementType.Pyro && attackElement == ElementType.Aqua) multiplier = 1.5f;
            if (targetData.mainElement == ElementType.Aqua && attackElement == ElementType.Volt) multiplier = 1.5f;
        }

        float baseElementalDamage = baseAmount * multiplier;

        // 2. 크리티컬 판정
        result.isCritical = Random.Range(0f, 100f) < critChance;

        // 3. 최종 데미지 합산
        result.finalDamage = baseElementalDamage;
        if (result.isCritical)
        {
            result.finalDamage *= critMult;
        }

        return result;
    }
}