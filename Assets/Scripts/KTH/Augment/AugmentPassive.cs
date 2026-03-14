using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Runeweaver.Augment
{
    [System.Serializable]
    public class AugmentPassive
    {
        private Dictionary<ElementType, int> _elementStacks = new Dictionary<ElementType, int>();

        public void AddStack(ElementType element)
        {
            if (!_elementStacks.ContainsKey(element)) _elementStacks[element] = 0;
            _elementStacks[element]++;
        }

        public int GetStack(ElementType element)
            => _elementStacks.ContainsKey(element) ? _elementStacks[element] : 0;

        public int GetTotalStackCount() => _elementStacks.Values.Sum();

        public void Clear() => _elementStacks.Clear();

        public List<ElementType> GetElementList()
        {
            List<ElementType> list = new List<ElementType>();
            foreach (var pair in _elementStacks)
            {
                for (int i = 0; i < pair.Value; i++) list.Add(pair.Key);
            }
            return list;
        }

        public void ClearStack(ElementType element)
        {
            if (_elementStacks.ContainsKey(element))
            {
                _elementStacks[element] = 0;
            }
        }

        // 짝수 스택 기획 반영 (SO 데이터와 별개로 코드에서 시점을 제어)
        public bool CanExplode(ElementType type) => GetStack(type) >= 2; // 2스택부터 폭발
        public bool IsMastered(ElementType type) => GetStack(type) >= 6; // 6스택 마스터
    }
}