using UnityEngine;
using Runeweaver;
public enum AugmentType { StatBoost, SkillChange }

[CreateAssetMenu(fileName = "AugmentData", menuName = "ugmentDataSO/Data/AugmentData")]
public class AugmentDataSO : ScriptableObject
{
    public string augmentName;
    public ElementType elementType; // 불, 얼음 등
    public SkillSlotType targetSlot; // Q, LeftClick 등
    public AugmentType augmentType;

    [Header("Stat Boost (수치 강화용)")]
    public float damageAdd;       // 데미지 추가
    public float critChanceAdd;   // 치명타 확률 추가

    [Header("Skill Change (로직 변경용)")]
    public bool enableHoming;     // 유도 기능 활성화 여부
    public int extraProjectile;   // 화살 추가 발사 개수
}