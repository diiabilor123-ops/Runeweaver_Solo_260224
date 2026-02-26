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

        // [핵심] 여기서 원소 상성 계산 (팀장님 기획 반영 지점)
        // 예: 내 속성이 불인데 물 공격을 받으면 1.5배
        float finalDamage = CalculateElementalDamage(amount, attackElement);

        currentHp -= finalDamage;
        Debug.Log($"{gameObject.name} 피격! 데미지: {finalDamage} (원소: {attackElement})");

        // 피격 연출 명령
        if (visuals != null) visuals.PlayHitFlash();

        if (currentHp <= 0) Die();
    }

    private float CalculateElementalDamage(float amount, ElementType attackElement)
    {
        float multiplier = 1.0f;

        // [상성 로직 예시] 몬스터의 속성(data.mainElement)과 공격 속성 비교
        if (data != null)
        {
            // 예: 불 몬스터에게 물 공격 시 1.5배
            if (data.mainElement == ElementType.Pyro && attackElement == ElementType.Aqua) multiplier = 1.5f;
            // 예: 물 몬스터에게 번개 공격 시 1.5배
            if (data.mainElement == ElementType.Aqua && attackElement == ElementType.Volt) multiplier = 1.5f;
        }

        return amount * multiplier;
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"{gameObject.name} 사망!");
        Destroy(gameObject);
    }
}