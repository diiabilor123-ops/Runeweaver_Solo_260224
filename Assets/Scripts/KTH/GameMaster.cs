using UnityEngine;
using Runeweaver;
using Runeweaver.Augment;
using Runeweaver.Player;

public class GameMaster : MonoBehaviour
{
    [Header("--- Test Settings ---")]
    [Range(0f, 2.0f)] public float gameSpeed = 1.0f;

    private WeaponHandler _weaponHandler;

    void Start()
    {
        _weaponHandler = FindFirstObjectByType<WeaponHandler>();
    }

    void Update()
    {
        Time.timeScale = gameSpeed;
        HandleHotkeys();
    }

    private void HandleHotkeys()
    {
        // [F1] 모든 원소를 "딱 2스택"으로 맞춤
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetElementStacks(ElementType.Fire, 2);
            SetElementStacks(ElementType.Ice, 2);
            SetElementStacks(ElementType.Volt, 2);
            ForceMaxProbability(); // 확률도 100%로 고정
            Debug.Log("<color=green>[GM] 모든 원소 2스택 세팅 완료!</color>");
        }

        // [숫자패드 1, 2, 3] 각 원소별로 개별 2스택 보충
        if (Input.GetKeyDown(KeyCode.Keypad1)) SetElementStacks(ElementType.Fire, 2);
        if (Input.GetKeyDown(KeyCode.Keypad2)) SetElementStacks(ElementType.Ice, 2);
        if (Input.GetKeyDown(KeyCode.Keypad3)) SetElementStacks(ElementType.Volt, 2);

        // [F2] 강제 크리티컬 (불 유도탄 테스트용)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _weaponHandler?.ExecuteAttack(SkillSlotType.LeftClick, true, 0f);
        }

        // [F9] 모든 몬스터 즉시 처치
        if (Input.GetKeyDown(KeyCode.F9)) KillAllEnemies();
    }

    /// <summary>
    /// 특정 원소의 스택을 원하는 개수(targetCount)만큼 맞춥니다.
    /// </summary>
    private void SetElementStacks(ElementType element, int targetCount)
    {
        if (PlayerAugment.Instance == null) return;

        // 현재 스택 확인 (PlayerAugment 내부에 스택 리스트가 있으므로 호출)
        // 주의: 현재 구조상 스택을 '깎는' 기능이 없다면 필요한 만큼만 Add해줍니다.
        int currentCount = GetCurrentStackCount(element);

        if (currentCount < targetCount)
        {
            int need = targetCount - currentCount;
            for (int i = 0; i < need; i++)
            {
                PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, element);
            }
            Debug.Log($"<color=white>[GM] {element} 스택 보충: {currentCount} -> {targetCount}</color>");
        }
        else
        {
            Debug.Log($"<color=yellow>[GM] {element}는 이미 {currentCount}스택 이상입니다.</color>");
        }
    }

    // 확률 100% 강제 고정 (테스트 편의성)
    private void ForceMaxProbability()
    {
        var aug = PlayerAugment.Instance.leftClick;
        aug.iceSpawnChance = 1.0f;
        aug.voltBaseChance = 1.0f;
    }

    // 현재 특정 원소가 몇 스택인지 체크하는 보조 메서드
    private int GetCurrentStackCount(ElementType element)
    {
        // [수정] .currentStacks 대신 .GetStack(element)를 직접 호출합니다.
        if (PlayerAugment.Instance != null && PlayerAugment.Instance.leftClick != null)
        {
            return PlayerAugment.Instance.leftClick.GetStack(element);
        }
        return 0;
    }

    private void KillAllEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies) enemy.TakeDamage(new HitData { damage = 9999f });
        Debug.Log("<color=yellow>[GM] 필드 클리어.</color>");
    }
}