using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Runeweaver.Augment
{
    [System.Serializable]
    public class AugmentLeftClick
    {
        // 이제 인스펙터의 수치보다 SO의 수치가 우선됩니다.
        [Header("Homing Chance Settings")]
        public float fireHomingChance = 1.0f;
        public float iceSpawnChance = 0.4f;
        public float voltBaseChance = 0.1f;
        public float voltASWeight = 1.0f;

        [Header("System Settings")]
        public int conversionStack = 4;

        private Dictionary<ElementType, int> _elementStacks = new Dictionary<ElementType, int>();

        public int GetTotalStackCount() => _elementStacks.Values.Sum();

        /// <summary>
        /// 원소별 특성에 맞는 유도탄 발사 확률을 계산합니다. (짝수 2스택 시 기능 해금)
        /// </summary>
        public float GetHomingChance(ElementType type, bool isCritical, float currentAttackSpeed)
        {
            int stack = GetStack(type);
            // [기획 반영] 짝수 스택(2개 이상)일 때만 유도 기능 활성화
            if (stack < 2) return 0f;

            switch (type)
            {
                case ElementType.Fire:
                    return isCritical ? fireHomingChance : 0f;
                case ElementType.Ice:
                    return iceSpawnChance;
                case ElementType.Volt:
                    return voltBaseChance + (currentAttackSpeed * voltASWeight * 0.05f);
                default:
                    return 0f;
            }
        }

        // --- [핵심 수정: SO 연동] ---
        /// <summary>
        /// 홀수 스택(1, 3, 5)에서 강화되는 스탯 수치를 SO에서 가져옵니다.
        /// </summary>
        public float GetStatModifier(ElementType type)
        {
            int stack = GetStack(type);
            if (stack <= 0) return 0f;

            // AugmentManager를 통해 SO에 적힌 Step Values 값을 가져옴
            return AugmentManager.Instance.GetAugmentValue(SkillSlotType.LeftClick, type, stack);
        }

        public void AddStack(ElementType element)
        {
            if (!_elementStacks.ContainsKey(element)) _elementStacks[element] = 0;
            _elementStacks[element]++;
        }

        public int GetStack(ElementType element) => _elementStacks.ContainsKey(element) ? _elementStacks[element] : 0;

        public List<ElementType> GetOwnedElementTypes() => _elementStacks.Keys.ToList();

        public List<ElementType> GetConvertedElements()
        {
            List<ElementType> converted = new List<ElementType>();
            foreach (var pair in _elementStacks)
            {
                if (pair.Value >= conversionStack) converted.Add(pair.Key);
            }
            return converted;
        }

        public List<ElementType> GetElementList()
        {
            List<ElementType> list = new List<ElementType>();
            foreach (var pair in _elementStacks)
            {
                for (int i = 0; i < pair.Value; i++) list.Add(pair.Key);
            }
            return list;
        }

        public void Clear() => _elementStacks.Clear();
    }
}