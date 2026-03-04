using UnityEngine;
using UnityEngine.UI; // 버튼 제어를 위해 추가
using System.Collections.Generic;
using Runeweaver;
using Runeweaver.Augment; // PlayerAugment 참조를 위해 추가
using System.Linq;
using TMPro; // 텍스트 매시 프로 사용 시


public class AugmentUI : MonoBehaviour
{
    [System.Serializable]
    public struct ChoiceCard
    {
        public GameObject cardObject;      // 카드 전체 오브젝트
        public TextMeshProUGUI titleText;  // "Q 슬롯", "왼클릭" 등
        public TextMeshProUGUI infoText;   // "현재 1개 -> 2개"
        public TextMeshProUGUI effectText; // "유도 기능 해금!"
        public Button selectButton;        // 클릭 버튼
        public Image elementIcon;          // 원소 아이콘 (불, 얼음 등)
    }

    [Header("UI References")]
    public ChoiceCard[] cards; // 인스펙터에서 3개의 카드를 연결

    // [기능: 슬롯 선택지 생성 및 강화 효과 미리보기]
    public void SetupSlotChoices(ElementType element, List<SkillSlotType> slots)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            if (i >= slots.Count)
            {
                cards[i].cardObject.SetActive(false); // 선택지가 적으면 카드 숨기기
                continue;
            }

            cards[i].cardObject.SetActive(true);
            SkillSlotType slot = slots[i];

            // 1. [수정] PlayerAugment에서 현재 해당 슬롯/원소의 스택을 가져옴
            int currentCount = 0;
            if (slot == SkillSlotType.LeftClick)
            {
                currentCount = PlayerAugment.Instance.leftClick.GetStack(element);
            }
            // TODO: 나중에 Q, Dash 등 슬롯 추가 시 여기서 분기하여 가져오기

            int nextCount = currentCount + 1;

            // 2. 텍스트 데이터 구성
            cards[i].titleText.text = GetSlotNameKorean(slot);
            cards[i].infoText.text = $"{element} <color=white>{currentCount}</color> → <color=yellow>{nextCount}</color>";

            // 3. [핵심] 1~6단계 기획 내용이 반영된 텍스트 표시
            cards[i].effectText.text = GetPreviewEffect(slot, element, nextCount);


            // 4. [수정] 버튼 이벤트 연결
            cards[i].selectButton.onClick.RemoveAllListeners();
            cards[i].selectButton.onClick.AddListener(() => {
                // 매니저를 통해 데이터를 플레이어에게 저장하고 UI를 닫음
                AugmentManager.Instance.ApplySelect(slot, element);
            });


        }
    }


    // 슬롯 이름을 한글로 변환해주는 헬퍼
    private string GetSlotNameKorean(SkillSlotType slot)
    {
        switch (slot)
        {
            case SkillSlotType.LeftClick: return "기본 공격";
            case SkillSlotType.Q: return "화살비 (Q)";
            case SkillSlotType.RightClick: return "오른쪽 클릭";
            case SkillSlotType.Space: return "대시 (Space)";
            case SkillSlotType.Passive: return "패시브";
            default: return slot.ToString();
        }
    }

    // [중요] 질문자님의 1~6단계 기획안을 UI에 반영
    private string GetPreviewEffect(SkillSlotType slot, ElementType element, int count)
    {
        // 일단은 기획하신 왼클릭(기본공격) 위주로 작성
        if (slot == SkillSlotType.LeftClick)
        {
            switch (count)
            {
                case 1:
                    string statName = element == ElementType.Fire ? "치확" : (element == ElementType.Ice ? "공격력" : "공속");
                    return $"<color=green>[1단계] {statName} +8% 및 패시브 활성화</color>";
                case 2:
                    return "<color=cyan>[2단계] 유도 화살 추가 해금</color>";
                case 3:
                    string statName3 = element == ElementType.Fire ? "치확" : (element == ElementType.Ice ? "공격력" : "공속");
                    return $"<color=green>[3단계] {statName3} +12%</color>";
                case 4:
                    return $"<color=orange>[4단계] 공격 속성 ({element}) 전환</color>";
                case 5:
                    string statName5 = element == ElementType.Fire ? "치확" : (element == ElementType.Ice ? "공격력" : "공속");
                    return $"<color=green>[5단계] {statName5} +20%</color>";
                case 6:
                    return "<color=red>[6단계] Q스킬 잠금 & 공격 시 자동 발동</color>";
                default:
                    return "해당 속성 위력 증가";
            }
        }

        return "스탯 강화";
    }
}