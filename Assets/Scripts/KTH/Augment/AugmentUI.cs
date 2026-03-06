using UnityEngine;
using UnityEngine.UI; // 버튼 제어를 위해 추가
using System.Collections.Generic;
using Runeweaver;
using Runeweaver.Augment; // PlayerAugment 참조를 위해 추가
using System.Linq;
using TMPro; // 텍스트 매시 프로 사용 시


public class AugmentUI : MonoBehaviour
{
    [Header("Timer Settings")]
    public TextMeshProUGUI timerText; // 30초 표시용 텍스트 연결
    private float selectionTimer = 30f;
    private bool isTimerActive = false;

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
        selectionTimer = 30f; // 타이머 리셋
        isTimerActive = true;

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
            cards[i].selectButton.onClick.AddListener(() =>
            {
                // 매니저를 통해 데이터를 플레이어에게 저장하고 UI를 닫음
                AugmentManager.Instance.ApplySelect(slot, element);
            });


        }
    }

    void Update()
    {
        if (!isTimerActive) return;

        // Time.timeScale이 0일 때도 흐르는 시간(unscaledDeltaTime) 사용
        selectionTimer -= Time.unscaledDeltaTime;

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(selectionTimer).ToString();

        if (selectionTimer <= 0)
        {
            isTimerActive = false;
            // 시간 초과 시 첫 번째 카드 강제 선택
            cards[0].selectButton.onClick.Invoke();
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

    // AugmentUI.cs 내의 함수 수정 예시
    private string GetPreviewEffect(SkillSlotType slot, ElementType element, int count)
    {
        // 1. Manager를 통해 해당 슬롯/원소에 맞는 SO 데이터를 가져옴
        AugmentDataSO data = AugmentManager.Instance.GetAugmentData(slot, element);

        // 2. 데이터가 있고, 스택이 1~6단계 사이라면 SO의 설명을 반환
        if (data != null && count >= 1 && count <= 6)
        {
            return data.stepDescriptions[count - 1];
        }

        // 3. 데이터가 없거나 6단계를 초과한 경우 기본 메시지
        return "<color=white>위력 추가 강화</color>";
    }

    // 원소별로 강화되는 스탯 이름을 반환하는 헬퍼 함수
    private string GetStatNameByElement(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire:
                return "치명타 확률";
            case ElementType.Ice:
                return "공격력";
            case ElementType.Volt:
                return "공격 속도";
            case ElementType.Nature:
                return "체력 재생";
            case ElementType.Light:
                return "사거리";
            case ElementType.Dark:
                return "피해 흡수";
            default:
                return "기본 스탯";
        }
    }
}