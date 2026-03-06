using UnityEngine;
using Runeweaver;
using Runeweaver.Augment;

[CreateAssetMenu(fileName = "AugmentDataSO", menuName = "AugmentDataSO/Data/AugmentData")]
public class AugmentDataSO : ScriptableObject
{
    [Header("Target Settings")]
    public SkillSlotType targetSlot; // 어떤 스킬용인가? (LeftClick, Q 등)
    public ElementType elementType;  // 어떤 원소인가? (Fire, Ice 등)

    [Header("Step Descriptions (1~6)")]
    [TextArea(3, 5)]
    public string[] stepDescriptions = new string[6]; // 1단계부터 6단계까지의 설명 텍스트

    // 필요하다면 여기에 수치값(float[] statValues) 등을 추가해서 
    // 실제 로직(AugmentLeftClick 등)에서 참조하게 할 수도 있습니다.
}