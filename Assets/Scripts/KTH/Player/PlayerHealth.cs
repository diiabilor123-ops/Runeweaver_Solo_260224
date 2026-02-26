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

    void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float amount, ElementType element, Team attackerTeam)
    {
        // [팀킬 방지] 공격자가 같은 Player 팀이면 데미지를 무시합니다.
        if (attackerTeam == Team.Player) return;

        if (isInvincible) return;

        currentHp -= amount;

        ShowDamagePopup(amount);

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

    [Header("Damage UI")]
    [SerializeField] private GameObject damageTextPrefab; // 아까 만든 프리팹을 여기에 넣을 거예요

    private void ShowDamagePopup(float damage)
    {
        if (damageTextPrefab == null) return;

        // 1. 소환 위치를 현재 내 머리 위로 고정
        Vector3 spawnPos = transform.position + Vector3.up * 2f;

        // [수정] Quaternion.Euler(X축, Y축, Z축)를 사용해 45도 회전값을 줍니다.
        // 카메라 각도에 맞춰 X값도 조절해야 할 수 있습니다 (예: 45, 45, 0)
        Quaternion rotation = Quaternion.Euler(0, 45f, 0);

        // 2. 소환! (Quaternion.identity는 회전값 0을 의미함)
        GameObject popup = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);

        // [수정 포인트] 3. 스케일을 0.01로 고정하여 거대해지는 것 방지!
        popup.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // 4. 숫자 입력 (아까 만든 Setup 함수가 있다면 그걸 호출)
        if (popup.TryGetComponent<DamagePopup>(out var popupScript))
        {
            popupScript.Setup(damage);
        }
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // 게임 오버 팝업이나 씬 재시작 로직
    }
}