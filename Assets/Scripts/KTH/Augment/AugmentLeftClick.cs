using System.Collections.Generic;
using UnityEngine;
using Runeweaver;

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

        public void AddStack(ElementType element)
        {
            if (!_elementStacks.ContainsKey(element)) _elementStacks[element] = 0;
            _elementStacks[element]++;
        }

        public int GetStack(ElementType element) => _elementStacks.ContainsKey(element) ? _elementStacks[element] : 0;

        // 특정 원소가 전환 단계(4스택) 이상인지 확인
        public bool IsConverted(ElementType type) => GetStack(type) >= conversionStack;

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

        public bool IsAnyElementConverted() => GetConvertedElements().Count > 0;
    }
}