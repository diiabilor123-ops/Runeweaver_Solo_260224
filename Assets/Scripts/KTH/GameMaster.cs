using UnityEngine;
using Runeweaver;
using Runeweaver.Augment;
using Runeweaver.Player;

public class GameMaster : MonoBehaviour
{
    [Header("--- Test Settings ---")]
    [Range(0f, 2.0f)] public float gameSpeed = 1.0f;

    void Update()
    {
        Time.timeScale = gameSpeed;
        HandleHotkeys();
    }

    private void HandleHotkeys()
    {
        // [F1] 유저 2,2,2 빌드 + 모든 유도탄 100% 확률 세팅
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetupFullHomingUser();
        }

        // [F2] 보스 페이즈 강제 전환 테스트 (75% -> 50% -> 25%)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceNextBossPhase();
        }

        if (Input.GetKeyDown(KeyCode.F9)) KillAllEnemies();
    }

    private void SetupFullHomingUser()
    {
        if (PlayerAugment.Instance == null) return;

        // 1. 기존 스택 청소 (에러 방지를 위해 먼저 Clear)
        PlayerAugment.Instance.ClearStacks(SkillSlotType.LeftClick);
        PlayerAugment.Instance.ClearStacks(SkillSlotType.Passive);

        // 2. 2, 2, 2 스택 주입 (유도탄 및 폭발 조건 충족)
        foreach (ElementType type in new[] { ElementType.Fire, ElementType.Ice, ElementType.Volt })
        {
            for (int i = 0; i < 2; i++)
            {
                PlayerAugment.Instance.AddElementStack(SkillSlotType.LeftClick, type);
                PlayerAugment.Instance.AddElementStack(SkillSlotType.Passive, type);
            }
        }

        // 3. 발사 확률 100% 강제 고정
        var aug = PlayerAugment.Instance.leftClick;
        aug.iceSpawnChance = 1.0f;
        aug.voltBaseChance = 1.0f;

        // 화염 유도탄은 치명타 시 발사이므로 치명타 100% 설정
        if (PlayerStats.Instance != null) PlayerStats.Instance.critRate = 1.0f;

        Debug.Log("<color=cyan>[GM] 풀호밍 모드: 모든 화살 유도 + 패시브 폭발 활성화!</color>");
    }

    private void ForceNextBossPhase()
    {
        // 씬에서 BossPhaseManager를 찾음
        BossPhaseManager phaseManager = FindFirstObjectByType<BossPhaseManager>();
        if (phaseManager == null) return;

        EnemyHealth bossHealth = phaseManager.GetComponent<EnemyHealth>();
        if (bossHealth == null) return;

        // 현재 체력 비율에 따라 다음 페이즈로 넘기기 위해 체력을 깎음
        // 26%씩 깎으면 75%, 50%, 25% 라인을 순차적으로 통과함
        float damageAmount = bossHealth.enemyData.maxHp * 0.26f;
        bossHealth.TakeDamage(new HitData
        {
            damage = damageAmount,
            attackerTeam = Team.Player,
            hitPoint = bossHealth.transform.position
        });

        Debug.Log("<color=red>[GM] 보스 페이즈 트리거 데미지 투하!</color>");
    }

    private void KillAllEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            // 보스는 죽이지 않고 잡몹만 처치 (보스 기믹 테스트 방해 금지)
            if (enemy.GetComponent<BossPhaseManager>() != null) continue;
            enemy.TakeDamage(new HitData { damage = 9999f });
        }
    }
}