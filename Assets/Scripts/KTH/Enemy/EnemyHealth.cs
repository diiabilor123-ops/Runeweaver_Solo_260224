using Runeweaver;
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
    
    private EnemyVisuals visuals;
    private NavMeshAgent agent; // 에이전트 참조 추가
    public EnemyData enemyData;

    // EnemyHealth.cs 상단 변수 선언부에 추가
    private int lastHitFrame = -1;

    [Header("Damage UI")]
    [SerializeField] private GameObject damageTextPrefab; // 몬스터용 팝업 프리팹 할당

    public void Init(EnemyData data)
    {
        this.enemyData = data;
        this.currentHp = data.maxHp;
        this.visuals = GetComponent<EnemyVisuals>();
        this.agent = GetComponent<NavMeshAgent>(); // 초기화
    }

    public event System.Action<float, float> OnHealthChanged; // (현재 체력, 최대 체력)을 전달

    /// <summary>
    /// 외부(플레이어 화살 등)에서 데미지를 줄 때 호출하는 함수
    /// </summary>
    public void TakeDamage(HitData hitData)
    {
        // [추가] 같은 프레임에 이미 데미지를 입었다면 무시
        if (Time.frameCount == lastHitFrame) return;

        if (IsDead || hitData.attackerTeam == Team.Enemy) return;

        EnemyShield shield = GetComponent<EnemyShield>();


        // [중요] 화살에서 이미 계산된 정보를 가져옵니다.
        // 다시 DamageCalculator를 두드리지 마세요!
        float finalDamage = hitData.damage;
        bool finalIsCritical = hitData.isCritical;

        // 1. 쉴드 처리 (쉴드가 있다면 먼저 깎고 남은 데미지를 반환)
        if (shield != null)
        {
            // 쉴드가 데미지를 흡수하는 로직 (기존 hitData.damage 활용)
            float remainingDamage = shield.AbsorbDamage(finalDamage, hitData.hitPoint);

            // 쉴드에 막힌 데미지 팝업 (파란색)
            float shieldedAmount = hitData.damage - remainingDamage;
            if (shieldedAmount > 0)
            {
                // 쉴드 팝업 (파란색) - 여기서는 단순히 연출용으로만 표시
                DamagePopup.SpawnPopup(damageTextPrefab, transform.position, shieldedAmount, false, Color.blue);
            }
            finalDamage = remainingDamage;
        }

        // 2. 실제 체력 데미지 처리
        if (finalDamage > 0)
        {
            // [중요] 이미 화살에서 최종 데미지가 계산되어 왔으므로 비율만 계산합니다.
            float damageRatio = finalDamage / enemyData.maxHp;
            currentHp -= finalDamage;

            // [추가] 체력이 변했음을 구독자들에게 알림
            OnHealthChanged?.Invoke(currentHp, enemyData.maxHp);

            // [해결 1] 크리티컬 여부에 따라 색상 결정 (노란색/흰색)
            Color textColor = finalIsCritical ? Color.yellow : Color.white;
            DamagePopup.SpawnPopup(damageTextPrefab, transform.position, finalDamage, finalIsCritical, textColor);

            // 타격 피드백 결정
            if (currentHp <= 0 && damageRatio >= 0.95f) // 한 방에 처치 (약 100%)
            {
                FeedbackManager.Instance.PlayOneShotFeedback();
            }
            else if (damageRatio >= 0.5f) // 최대 체력의 50% 이상
            {
                FeedbackManager.Instance.PlayMassiveFeedback();
            }
            else if (finalIsCritical) // 크리티컬 (나머지 상황 중)
            {
                FeedbackManager.Instance.PlayCritFeedback();
            }

            // 피격 피드백 (넉백 로직 제거됨)
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
    public void TakeDamage(float amount, MonsterElement element, Team team)
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