using Runeweaver;
using Runeweaver.Augment;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
    // 원소별 현재 쌓인 스택 저장
    private Dictionary<ElementType, int> elementStacks = new Dictionary<ElementType, int>();

    // 원소별 최대 스택 설정 (의도하신 대로 원소마다 다르게 설정 가능)
    private int GetMaxStack(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return 10;
            case ElementType.Ice: return 8;
            case ElementType.Volt: return 12;
            default: return 10;
        }
    }

    public void AddElementStack(ElementType type, int amount = 1)
    {
        if (type == ElementType.None) return;

        if (!elementStacks.ContainsKey(type)) elementStacks[type] = 0;
        elementStacks[type] += amount;

        int maxStack = GetMaxStack(type);
        Debug.Log($"[Monster Stack] {type} : {elementStacks[type]} / {maxStack}");

        // 스택이 최대치에 도달했을 때
        if (elementStacks[type] >= maxStack)
        {
            // [의도 반영] 플레이어 패시브 슬롯에 해당 원소가 2개 이상 있는지 확인
            if (PlayerAugment.Instance.passive.CanExplode(type))
            {
                ExecuteExplosion(type);
            }
            else
            {
                Debug.Log($"{type} 스택이 최대지만 패시브 조건(2스택) 미달로 폭발하지 않음");
            }

            // 터지든 안 터지든 스택이 최대면 0으로 초기화
            elementStacks[type] = 0;
        }
    }

    private void ExecuteExplosion(ElementType type)
    {
        Debug.Log($"<color=orange>{type} 최종 폭발 발생!</color>");

        // BulletManager를 통해 해당 원소의 데이터를 가져와 이펙트/사운드 활용
        BulletDataSO data = BulletManager.Instance.GetHomingData(type);

        if (data != null)
        {
            // 1. 폭발 비주얼 재생 (BulletDataSO의 hitEffectPrefabs[1] 활용)
            if (data.hitEffectPrefabs != null && data.hitEffectPrefabs.Length > 1)
            {
                GameObject vfx = Instantiate(data.hitEffectPrefabs[1], transform.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            // 2. 폭발 사운드 재생
            if (data.explosionSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.Play(data.explosionSound, transform.position);
            }
        }

        // 3. 주변 범위 데미지 로직
        float explosionRadius = 5f;
        float explosionDamage = 100f; // 이 수치는 SO에서 가져오거나 기획에 맞게 조절하세요.

        Collider[] targets = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var col in targets)
        {
            if (col.TryGetComponent<EnemyHealth>(out var health))
            {
                HitData explosionHit = new HitData
                {
                    damage = explosionDamage,
                    element = MonsterElement.M_None, // 폭발 자체는 무상성 처리 (필요시 수정)
                    attackerTeam = Team.Player,
                    hitPoint = col.transform.position,
                    attackerPos = transform.position,
                    isCritical = false
                };
                health.TakeDamage(explosionHit);
            }
        }
    }
}