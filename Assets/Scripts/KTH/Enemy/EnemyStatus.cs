using Runeweaver;
using System.Collections.Generic;
using UnityEngine;

// EnemyStatus는 이제 데미지 처리를 하지 않고 스택 관리만 합니다.
public class EnemyStatus : MonoBehaviour
{
    private Dictionary<ElementType, int> elementStacks = new Dictionary<ElementType, int>();

    public void AddElementStack(ElementType type, int amount = 1)
    {
        if (!elementStacks.ContainsKey(type)) elementStacks[type] = 0;
        elementStacks[type] += amount;

        if (elementStacks[type] >= 10)
        {
            ExecuteExplosion(type);
            elementStacks[type] = 0; // 초기화
        }
    }

    private void ExecuteExplosion(ElementType type)
    {
        // 1. 폭발 이펙트 소환 (원소별로 다른 이펙트가 있다면 더 좋습니다)
        Debug.Log($"{type} 폭발 발생!");
        // Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        // 2. 주변 적 감지 (범위 공격)
        float explosionRadius = 5f;
        float explosionDamage = 50f; // 기획에 따라 조절
        Collider[] targets = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (var col in targets)
        {
            // 자기 자신을 포함한 주변 적들의 Health 시스템을 찾음
            if (col.TryGetComponent<EnemyHealth>(out var health))
            {
                // 폭발용 HitData 생성
                HitData explosionHit = new HitData
                {
                    damage = explosionDamage,
                    element = MonsterElement.M_None, // 폭발은 무상성 혹은 특정 속성
                    attackerTeam = Team.Player,
                    hitPoint = col.transform.position,
                    attackerPos = transform.position,
                    isCritical = false // 폭발은 고정 데미지 혹은 확률 적용
                };

                // 데미지 적용!
                health.TakeDamage(explosionHit);
            }
        }
    }
}