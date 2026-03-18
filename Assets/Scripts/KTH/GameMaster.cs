using UnityEngine;
using Runeweaver;
using Runeweaver.Augment;
using Runeweaver.Player;
using System.Collections; // 코루틴을 위해 추가

public class GameMaster : MonoBehaviour
{
    [Header("--- Test Settings ---")]
    [Range(0f, 2.0f)] public float gameSpeed = 1.0f;
    [SerializeField] private bool autoSetupOnStart = true; // 자동으로 시작할지 여부

    void Start()
    {
        // [수정] 시작하자마자 자동으로 풀세팅 코루틴 실행
        if (autoSetupOnStart)
        {
            StartCoroutine(AutoSetupRoutine());
        }
    }

    private IEnumerator AutoSetupRoutine()
    {
        // [중요] 다른 매니저(PlayerAugment 등)들이 Awake에서 Instance를 잡을 때까지 한 프레임 대기
        yield return null;

        Debug.Log("<color=yellow>[GM] 자동 세팅을 시작합니다...</color>");
        SetupFullHomingUser();
    }

    void Update()
    {
        Time.timeScale = gameSpeed;
        HandleHotkeys();
    }

    private void HandleHotkeys()
    {
        // 여전히 F1을 눌러서 수동으로 초기화할 수도 있게 남겨둡니다.
        if (Input.GetKeyDown(KeyCode.F1)) SetupFullHomingUser();
        if (Input.GetKeyDown(KeyCode.F2)) ForceNextBossPhase();
        if (Input.GetKeyDown(KeyCode.F9)) KillAllEnemies();
    }

    // --- 이하 로직은 기존과 동일 ---
    private void SetupFullHomingUser()
    {
        if (PlayerAugment.Instance == null)
        {
            Debug.LogError("[GM] PlayerAugment Instance가 없습니다!");
            return;
        }

        PlayerAugment.Instance.ClearStacks(SkillSlotType.LeftClick);
        PlayerAugment.Instance.ClearStacks(SkillSlotType.Passive);

        foreach (ElementType type in new[] { ElementType.Fire, ElementType.Ice, ElementType.Volt })
        {
            for (int i = 0; i < 2; i++)
            {
                PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, type);
                PlayerAugment.Instance.AddElementStack(SkillSlotType.Passive, type);
            }
        }

        var aug = PlayerAugment.Instance.leftClick;
        aug.iceSpawnChance = 1.0f;
        aug.voltBaseChance = 1.0f;

        if (PlayerStats.Instance != null) PlayerStats.Instance.critRate = 1.0f;

        Debug.Log("<color=cyan>[GM] 풀호밍 모드 자동 활성화 완료!</color>");
    }

    private void ForceNextBossPhase()
    {
        BossPhaseManager phaseManager = FindFirstObjectByType<BossPhaseManager>();
        if (phaseManager == null) return;

        EnemyHealth bossHealth = phaseManager.GetComponent<EnemyHealth>();
        if (bossHealth == null) return;

        float damageAmount = bossHealth.enemyData.maxHp * 0.26f;
        bossHealth.TakeDamage(new HitData
        {
            damage = damageAmount,
            attackerTeam = Team.Player,
            hitPoint = bossHealth.transform.position
        });
    }

    private void KillAllEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.GetComponent<BossPhaseManager>() != null) continue;
            enemy.TakeDamage(new HitData { damage = 9999f });
        }
    }
}