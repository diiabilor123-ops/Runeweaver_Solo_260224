// Enums.cs
namespace Runeweaver
{
    // 스킬 슬롯의 종류를 정의합니다.
    public enum SkillSlotType
    {
        Passive,
        Q,
        LeftClick,
        RightClick,
        Space
    }

    // (나중을 위해 원소 타입도 같이 정의해두면 편합니다)
    public enum ElementType
    {
        None, Fire, Ice, Volt, Nature, Light, Dark
    }

    // [수정] 몬스터용 원소 (명확한 구분을 위해 MonsterElement로 명명)
    public enum MonsterElement
    {
        M_None,
        M_Fire,   // 불
        M_Ice,   // 얼음/물
        M_Volt,   // 번개
        M_Nature,
        M_Light,
        M_Dark
    }
}