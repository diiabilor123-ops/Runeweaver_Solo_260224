using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 추가

namespace Runeweaver.Augment
{
    [System.Serializable]
    public class AugmentLeftClick
    {
        [Header("Fire (Crit Based)")]
        public float fireHomingChance = 1.0f; // 치명타 시 100% 발사

        [Header("Ice (Luck Based)")]
        public float iceSpawnChance = 0.4f; // 40% 확률 발사
        public float iceSlowAmount = 0.01f; // 1% 슬로우

        [Header("Volt (AS Based)")]
        public float voltBaseChance = 0.1f; // 기본 10%
        public float voltASWeight = 1.0f;   // 추가 공속 반영 가중치

        [Header("Common Settings")]
        public int conversionStack = 4;     // 4스택 시 속성 전환
        public int explosionStack = 10;     // 10스택 시 폭발

        private Dictionary<ElementType, int> _elementStacks = new Dictionary<ElementType, int>();

        // --- [추가: BulletManager 에러 해결용] ---
        public int GetTotalStackCount()
        {
            // 모든 원소의 스택 합계를 반환
            return _elementStacks.Values.Sum();
        }

        // --- [추가: 유도탄 발사 확률 계산 로직] ---
        /// <summary>
        /// 원소별 특성에 맞는 유도탄 발사 확률을 계산합니다.
        /// </summary>
        public float GetHomingChance(ElementType type, bool isCritical, float currentAttackSpeed)
        {
            int stack = GetStack(type);
            if (stack < 2) return 0f; // 최소 2스택 필요

            switch (type)
            {
                case ElementType.Fire:
                    // 화염: 치명타 시 확정(또는 설정된 확률) 발사
                    return isCritical ? fireHomingChance : 0f;

                case ElementType.Ice:
                    // 얼음: 단순 확률 기반
                    return iceSpawnChance;

                case ElementType.Volt:
                    // 번개: 기본 확률 + 공속 가중치 (공속이 빠를수록 잘 터짐)
                    return voltBaseChance + (currentAttackSpeed * voltASWeight * 0.05f);

                default:
                    return 0f;
            }
        }

        // --- [추가: 현재 스택이 쌓인 모든 원소 종류 가져오기] ---
        public List<ElementType> GetOwnedElementTypes()
        {
            return _elementStacks.Keys.ToList();
        }

        public void AddStack(ElementType element)
        {
            if (!_elementStacks.ContainsKey(element)) _elementStacks[element] = 0;
            _elementStacks[element]++;
        }

        public int GetStack(ElementType element) => _elementStacks.ContainsKey(element) ? _elementStacks[element] : 0;

        // 특정 원소가 전환 단계(4스택) 이상인지 확인
        public bool IsConverted(ElementType type) => GetStack(type) >= conversionStack;


        public bool IsAnyElementConverted() => GetConvertedElements().Count > 0;

        // 메인 화살이 어떤 원소들의 힘을 가졌는지 리스트 반환 (4스택 이상인 것만)
        public List<ElementType> GetConvertedElements()
        {
            List<ElementType> converted = new List<ElementType>();
            foreach (var pair in _elementStacks)
            {
                if (pair.Value >= conversionStack)
                    converted.Add(pair.Key);
            }
            return converted;
        }

        // DamageCalculator 에러 해결용 (기존 함수 복구)
        public float GetStatModifier(ElementType type)
        {
            int stack = GetStack(type);
            if (stack >= 5) return 0.40f;
            if (stack >= 3) return 0.22f;
            if (stack >= 1) return 0.08f;
            return 0f;
        }

        // PlayerAugment에서 사용하던 함수
        public List<ElementType> GetElementList()
        {
            List<ElementType> list = new List<ElementType>();
            foreach (var pair in _elementStacks)
            {
                for (int i = 0; i < pair.Value; i++) list.Add(pair.Key);
            }
            return list;
        }

    }
}