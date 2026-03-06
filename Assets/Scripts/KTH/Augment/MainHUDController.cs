using Runeweaver;
using Runeweaver.Augment;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainHUDController : MonoBehaviour
{
    [System.Serializable]
    public struct SkillSlotUI
    {
        public SkillSlotType slotType;
        public Transform elementIconParent; // 원소 아이콘들이 담길 부모 (Horizontal Layout Group)
    }

    public SkillSlotUI[] slotUIs;
    public GameObject elementIconPrefab; // 원소 아이콘 이미지 프리팹
    public Sprite[] elementSprites;      // 화염, 얼음, 번개 등 순서대로 저장

    public static MainHUDController Instance; // 싱글톤 선언

    private void Awake()
    {
        // 인스턴스 할당
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RefreshAllSlots();
    }

    // 증강을 선택할 때마다 호출하여 하단 UI 갱신
    public void RefreshAllSlots()
    {
        foreach (var slotUI in slotUIs)
        {
            // 기존 아이콘들 삭제
            foreach (Transform child in slotUI.elementIconParent) Destroy(child.gameObject);

            // PlayerAugment에서 현재 슬롯에 쌓인 원소 리스트를 가져옴
            List<ElementType> elements = PlayerAugment.Instance.GetSortedElements(slotUI.slotType);

            // 순서대로 아이콘 생성 (얼음 -> 화염 순서 등)
            foreach (ElementType et in elements)
            {
                GameObject icon = Instantiate(elementIconPrefab, slotUI.elementIconParent);
                icon.GetComponent<Image>().sprite = GetSprite(et);
            }
        }
    }

    private Sprite GetSprite(ElementType type)
    {
        // Enum 순서나 이름에 맞게 Sprite 반환
        return elementSprites[(int)type - 1];
    }
}