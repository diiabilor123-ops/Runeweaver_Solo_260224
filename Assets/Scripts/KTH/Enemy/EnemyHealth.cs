using UnityEngine;

/// <summary>
/// [몬스터의 생명 및 데미지 관리]
/// IDamageable 인터페이스를 구현하여 플레이어의 공격을 받을 수 있게 합니다.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    public bool IsDead { get; private set; }
    private float currentHp;
    private EnemyData data;
    private EnemyVisuals visuals;

    [Header("Damage UI")]
    [SerializeField] private GameObject damageTextPrefab; // 몬스터용 팝업 프리팹 할당

    public void Init(EnemyData data)
    {
        this.data = data;
        this.currentHp = data.maxHp;
        this.visuals = GetComponent<EnemyVisuals>();
    }

    /// <summary>
    /// 외부(플레이어 화살 등)에서 데미지를 줄 때 호출하는 함수
    /// </summary>
    public void TakeDamage(float amount, ElementType attackElement, Team attackerTeam)
    {
        if (IsDead) return;

        // [핵심 추가] 공격자가 같은 적군(Enemy) 팀이면 데미지를 입지 않음
        if (attackerTeam == Team.Enemy) return;

        // [수정] 보호막 컴포넌트 확인
        EnemyShield shield = GetComponent<EnemyShield>();
        float remainingDamage = amount;

        // 1. 보호막 데미지 처리
        if (shield != null)
        {
            remainingDamage = shield.AbsorbDamage(amount);

            // [추가] 쉴드가 흡수한 데미지 텍스트 팝업 (쉴드 색상인 파란색 사용 예시)
            float shieldedDamage = amount - remainingDamage;
            if (shieldedDamage > 0)
            {
                // [수정] 쉴드 데미지도 계산기를 통해 크리티컬 판정 포함
                DamageResult shieldResult = DamageCalculator.Calculate(shieldedDamage, attackElement, attackerTeam, data);

                // [추가] 파란색 팝업 + 크리티컬 판정 반영
                DamagePopup.SpawnPopup(damageTextPrefab, transform.position, shieldResult.finalDamage, shieldResult.isCritical, Color.blue);
            }
        }

        // 2. 실제 체력 데미지 처리
        if (remainingDamage > 0)
        {
            DamageResult result = DamageCalculator.Calculate(remainingDamage, attackElement, attackerTeam, data);
            currentHp -= result.finalDamage;

            // [수정] 통합된 static 함수 호출 (일반: 흰색)
            DamagePopup.SpawnPopup(damageTextPrefab, transform.position, result.finalDamage, result.isCritical, Color.white);

            if (visuals != null) visuals.PlayHitFlash();
        }

        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"{gameObject.name} 사망!");
        Destroy(gameObject);
    }
}