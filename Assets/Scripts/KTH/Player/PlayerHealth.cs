using UnityEngine;


/// <summary>
/// [플레이어 체력 관리]
/// IDamageable 인터페이스를 상속받아 적이나 환경으로부터 데미지를 입습니다.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable // 1. 인터페이스 상속 추가
{
    [SerializeField] private float maxHp = 100f;
    private float currentHp;
    private bool isInvincible = false; // 무적 상태 (연속 데미지 방지)

    [Header("Damage UI")]
    [SerializeField] private GameObject damageTextPrefab; // 플레이어용 팝업 프리팹 할당

    void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(HitData hitData)
    {
        // [팀킬 방지] 공격자가 같은 Player 팀이면 데미지를 무시합니다.
        if (hitData.attackerTeam == Team.Player || isInvincible) return;

        if (isInvincible) return;

        // [수정] 계산기 사용 (플레이어는 targetData가 없으므로 null 전달)
        DamageResult result = DamageCalculator.Calculate(hitData.damage, hitData.element, hitData.attackerTeam, null);

        currentHp -= result.finalDamage;

        // [수정] 통합된 static 함수 호출 (플레이어: 빨간색)
        DamagePopup.SpawnPopup(damageTextPrefab, transform.position, result.finalDamage, result.isCritical, Color.red);

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            // 하데스처럼 피격 시 무적 시간 부여 (0.5초)
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private System.Collections.IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        // 여기서 피격 효과(깜빡임 등)를 넣으면 좋습니다.
        yield return new WaitForSeconds(0.5f);
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // 게임 오버 팝업이나 씬 재시작 로직
    }
}