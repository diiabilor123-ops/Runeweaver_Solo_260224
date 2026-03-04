using Runeweaver;
using System.Collections.Generic;
using UnityEngine;

namespace Runeweaver.Augment
{
    /// <summary>
    /// 플레이어의 모든 증강(원소 스택) 상태를 총괄하는 중앙 클래스입니다.
    /// </summary>
    public class PlayerAugment : MonoBehaviour
    {
        public static PlayerAugment Instance { get; private set; }

        [Header("Skill Slot Augments")]
        // 각 스킬 슬롯별 핸들러 (현재는 왼클릭만 구현, 나머지도 같은 방식으로 추가)
        public AugmentLeftClick leftClick = new AugmentLeftClick();
        public AugmentPassive passive = new AugmentPassive(); // 패시브 슬롯 활성화

        // TODO: 나중에 추가할 슬롯들
        // public AugmentQ qSkill = new AugmentQ();
        // public AugmentPassive passive = new AugmentPassive();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// AugmentManager에서 원소 선택 시 호출하는 메인 함수입니다.
        /// </summary>
        public void AddElementStack(SkillSlotType slot, ElementType element)
        {
            switch (slot)
            {
                case SkillSlotType.LeftClick:
                    leftClick.AddStack(element);
                    break;
                case SkillSlotType.Passive: // 패시브 슬롯 추가
                    passive.AddStack(element);
                    break;
                    // Q, RightClick 등 추가 가능
            }

            // 스택이 변했음을 알리는 알림 (UI나 이펙트 갱신용)
            Debug.Log($"[Augment] {slot} 슬롯에 {element} 원소 추가됨!");
        }

        // PlayerAugment.cs 내부에 추가
        public List<ElementType> GetSortedElements(SkillSlotType slot)
        {
            if (slot == SkillSlotType.LeftClick) return leftClick.GetElementList();
            // Q, RightClick 등 추가 시 확장
            return new List<ElementType>();
        }
    }
}