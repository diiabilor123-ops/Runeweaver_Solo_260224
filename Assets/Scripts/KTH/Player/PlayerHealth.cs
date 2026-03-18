using Runeweaver;
using Runeweaver.Player;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 100f;
    private bool isInvincible = false;
    private Animator anim;
    private Rigidbody rb;

    // --- [추가] 조작 스크립트 참조 ---
    // 플레이어의 이동/공격을 담당하는 스크립트 이름을 여기에 적으세요.
    // private PlayerController moveScript;
    private MonoBehaviour moveScript;

    [Header("Damage UI")]
    [SerializeField] private GameObject damageTextPrefab;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // 이동 스크립트 가져오기 (본인 프로젝트의 이동 스크립트 타입으로 변경 권장)
        // 예: moveScript = GetComponent<PlayerController>();
        moveScript = GetComponent<MonoBehaviour>(); // 임시로 MonoBehaviour로 설정

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.maxHp = maxHp;
            PlayerStats.Instance.currentHp = maxHp;
        }
    }

    public void TakeDamage(HitData hitData)
    {
        if (hitData.attackerTeam == Team.Player || isInvincible) return;

        List<ElementType> monsterElementList = new List<ElementType>();
        if (hitData.element == MonsterElement.M_Fire) monsterElementList.Add(ElementType.Fire);
        else if (hitData.element == MonsterElement.M_Ice) monsterElementList.Add(ElementType.Ice);
        else if (hitData.element == MonsterElement.M_Volt) monsterElementList.Add(ElementType.Volt);

        DamageResult result = DamageCalculator.Calculate(
            hitData.damage,
            monsterElementList,
            hitData.attackerTeam,
            null
        );

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyDamage(result.finalDamage);
            DamagePopup.SpawnPopup(damageTextPrefab, transform.position, result.finalDamage, result.isCritical, Color.red);

            if (PlayerStats.Instance.currentHp <= 0) Die();
            else StartCoroutine(InvincibilityRoutine());
        }
    }

    private System.Collections.IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(0.5f);
        isInvincible = false;
    }

    public void ResetHealth()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.currentHp = maxHp;

        isInvincible = false;

        // 1. 애니메이션 및 물리 상태 복구
        if (anim != null) anim.Play("Idle", 0, 0f);
        if (rb != null) rb.isKinematic = false;

        // 2. [추가] 조작 스크립트 다시 활성화
        if (moveScript != null) moveScript.enabled = true;

        transform.localScale = Vector3.one;
    }

    private void Die()
    {
        if (isInvincible) return;
        isInvincible = true;

        Debug.Log("플레이어 사망!");

        // 1. 애니메이션 실행
        if (anim != null) anim.SetTrigger("Die");

        // 2. 시체가 밀리지 않게 물리 끄기
        if (rb != null) rb.isKinematic = true;

        // 3. [추가] 조작 스크립트 비활성화 (이동/공격 불가)
        if (moveScript != null) moveScript.enabled = false;

        // 4. 레벨 매니저 호출
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnPlayerDied();
        }
    }
}