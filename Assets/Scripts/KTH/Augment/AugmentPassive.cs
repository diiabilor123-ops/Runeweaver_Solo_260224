using System.Collections.Generic;
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

        /// <summary>
        /// [2단계] 해당 원소의 스택이 쌓일 때마다 폭발이 일어날 수 있는지 확인
        /// </summary>
        public bool CanExplode(ElementType type) => GetStack(type) >= 2;

        /// <summary>
        /// [6단계] 속성 반감 무시 및 데미지 증가 여부
        /// </summary>
        public bool IsMastered(ElementType type) => GetStack(type) >= 6;
    }
}