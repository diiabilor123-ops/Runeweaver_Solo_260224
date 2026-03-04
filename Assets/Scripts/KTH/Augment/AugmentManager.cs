using Runeweaver;
using Runeweaver.Augment;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance;

    [Header("UI Reference")]
    public GameObject augmentUIPanel; // 에러 해결: 변수 선언

    [Header("Database")]
    // 모든 증강 정보가 담긴 SO 리스트 (불릿매니저처럼 사용)
    public List<AugmentDataSO> allAugments;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 원소 아이템을 먹었을 때 호출
    /// </summary>
    public void OnElementPicked(ElementType pickedType)
    {
        // 유효성 검사: None 타입은 증강을 발생시키지 않음
        if (pickedType == ElementType.None) return;

        // 1. 시간 정지 (하데스 스타일)
        Time.timeScale = 0f;

        // 2. 랜덤하게 3개의 슬롯 선택지 추출
        // [수정] Enums.cs의 SkillSlotType을 기준으로 하되, Passive는 제외
        var selectedSlots = System.Enum.GetValues(typeof(SkillSlotType))
            .Cast<SkillSlotType>()
            .Where(s => s != SkillSlotType.Passive)
            .OrderBy(x => Random.value)
            .Take(3)
            .ToList();

        // 3. UI 활성화 및 데이터 전달
        if (augmentUIPanel != null)
        {
            augmentUIPanel.SetActive(true);

            if (augmentUIPanel.TryGetComponent<AugmentUI>(out var ui))
            {
                // UI에 '플레이어 원소'와 '슬롯 후보군' 전달
                ui.SetupSlotChoices(pickedType, selectedSlots);
            }
        }
    }

    /// <summary>
    /// UI에서 최종 선택 버튼을 눌렀을 때 호출
    /// </summary>
    public void ApplySelect(SkillSlotType slot, ElementType element)
    {
        // 1. 데이터 저장 (PlayerAugment는 플레이어 전용이므로 ElementType 사용)
        if (PlayerAugment.Instance != null)
        {
            PlayerAugment.Instance.AddElementStack(slot, element);
        }

        // 2. 시간 재개 및 UI 닫기
        Time.timeScale = 1f;
        if (augmentUIPanel != null)
        {
            augmentUIPanel.SetActive(false);
        }

        Debug.Log($"[Augment] {slot} 슬롯에 {element} 속성 강화 적용!");
    }
}