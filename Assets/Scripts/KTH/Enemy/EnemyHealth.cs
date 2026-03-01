using UnityEngine;
using UnityEngine.AI;

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
    private NavMeshAgent agent; // 에이전트 참조 추가

    [Header("Damage UI")]
    [SerializeField] private GameObject damageTextPrefab; // 몬스터용 팝업 프리팹 할당

    public void Init(EnemyData data)
    {
        this.data = data;
        this.currentHp = data.maxHp;
        this.visuals = GetComponent<EnemyVisuals>();
        this.agent = GetComponent<NavMeshAgent>(); // 초기화
    }

    /// <summary>
    /// 외부(플레이어 화살 등)에서 데미지를 줄 때 호출하는 함수
    /// </summary>
    public void TakeDamage(HitData hitData)
    {
        if (IsDead || hitData.attackerTeam == Team.Enemy) return;

        EnemyShield shield = GetComponent<EnemyShield>();
        float remainingDamage = hitData.damage;

        // 1. 쉴드 처리
        if (shield != null)
        {
            remainingDamage = shield.AbsorbDamage(hitData.damage);

            // 쉴드에 막힌 데미지 팝업 (파란색)
            float shieldedAmount = hitData.damage - remainingDamage;
            if (shieldedAmount > 0)
            {
                DamageResult sResult = DamageCalculator.Calculate(shieldedAmount, hitData.element, hitData.attackerTeam, data);
                DamagePopup.SpawnPopup(damageTextPrefab, transform.position, sResult.finalDamage, sResult.isCritical, Color.blue);

                // 쉴드 이펙트 재생 (EnemyShield에서 이미 호출 중일 수 있으니 확인 필요)
                if (visuals != null) visuals.PlayShieldEffect(transform.position);
            }
        }

        // 2. 실제 체력 데미지 처리
        if (remainingDamage > 0)
        {
            DamageResult result = DamageCalculator.Calculate(remainingDamage, hitData.element, hitData.attackerTeam, data);
            currentHp -= result.finalDamage;

            // 체력 데미지 팝업 (흰색)
            DamagePopup.SpawnPopup(damageTextPrefab, transform.position, result.finalDamage, result.isCritical, Color.white);

            // 3. 피격 피드백 (넉백 로직 제거됨)
            PlayHitFeedback(hitData);
        }

        if (currentHp <= 0) Die();
    }

    // --- [정리] 피격 연출 피드백 함수 ---
    private void PlayHitFeedback(HitData hitData)
    {
        if (visuals != null)
        {
            // 1. 피격 파티클 생성
            if (hitData.hitEffectPrefab != null)
            {
                Instantiate(hitData.hitEffectPrefab, hitData.hitPoint, Quaternion.identity);
            }

            // 2. 피격 애니메이션 및 몬스터 머티리얼 번쩍임
            visuals.PlayHitAnimation();
            visuals.PlayHitFlash();
        }
    }



    // 인터페이스 호환용 (필요시)
    public void TakeDamage(float amount, ElementType element, Team team)
    {
        HitData defaultHit = new HitData { damage = amount, element = element, attackerTeam = team, attackerPos = transform.position };
        TakeDamage(defaultHit);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"{gameObject.name} 사망!");
        if (agent != null) agent.enabled = false;

        // 사망 연출 호출 (필요 시)
        // visuals.PlayDeathEffect();

        Destroy(gameObject);
    }
}