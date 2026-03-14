using UnityEngine;
using Runeweaver;
using Runeweaver.Augment;

[CreateAssetMenu(fileName = "AugmentDataSO", menuName = "AugmentDataSO/Data/AugmentData")]
public class AugmentDataSO : ScriptableObject
{
    [Header("Target Settings")]
    public SkillSlotType targetSlot;
    public ElementType elementType;

    [Header("Step Descriptions (1~6)")]
    [TextArea(3, 5)]
    public string[] stepDescriptions = new string[6];

    [Header("Step Values (1~6)")]
    [Tooltip("홀수 스택은 스탯 수치, 짝수 스택은 고정 수치나 강화값을 넣으세요")]
    public float[] stepValues = new float[6];

    // 특정 스택의 수치를 가져오는 헬퍼 함수
    public float GetValue(int stack)
    {
        if (stack <= 0) return 0;
        int index = Mathf.Clamp(stack - 1, 0, 5);
        return stepValues[index];
    }
}