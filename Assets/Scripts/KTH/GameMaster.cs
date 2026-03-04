using UnityEngine;
using Runeweaver;
using Runeweaver.Augment;
using Runeweaver.Player;

public class GameMaster : MonoBehaviour
{
    private WeaponHandler weaponHandler;

    void Start()
    {
        // 씬에 있는 WeaponHandler를 찾습니다.
        weaponHandler = FindFirstObjectByType<WeaponHandler>();
    }

    void Update()
    {
        // [F1] 모든 유도 화살 증강 스택 2개씩 부여 (발동 조건 충족)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // PlayerAugment의 AddElementStack 메서드 사용
            PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, ElementType.Fire);
            PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, ElementType.Fire);

            PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, ElementType.Ice);
            PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, ElementType.Ice);

            PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, ElementType.Volt);
            PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, ElementType.Volt);

            // 확률 변수는 AugmentLeftClick 내부 변수명에 맞춰 직접 조정하거나 
            // 100% 발동을 위해 해당 클래스에서 기본값을 1f로 잠시 수정해두는 것이 좋습니다.
            PlayerAugment.Instance.leftClick.iceSpawnChance = 1.0f;
            PlayerAugment.Instance.leftClick.voltBaseChance = 1.0f;

            Debug.Log("<color=green>[GM] 모든 유도 화살 증강 조건(2스택) 충족!</color>");
        }

        // [F2] 강제 크리티컬 공격 실행 (불 화살 유도 테스트)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("<color=red>[GM] 강제 크리티컬 공격!</color>");
            // SkillSlotType.LeftClick 사용
            weaponHandler.ExecuteAttack(SkillSlotType.LeftClick, true, 0f);
        }

        // [F3] 일반 공격 실행 (얼음/번개 화살 확률 테스트)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("<color=cyan>[GM] 일반 공격 실행!</color>");
            weaponHandler.ExecuteAttack(SkillSlotType.LeftClick, false, 0f);
        }
    }
}