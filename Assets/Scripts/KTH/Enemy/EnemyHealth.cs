using UnityEngine;

/// <summary>
/// [몬스터의 생명 및 데미지 관리]
/// HP, 속성 저항, 사망 판정을 담당합니다.
/// </summary>
public class EnemyHealth : MonoBehaviour
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
    public void TakeDamage(float amount, ElementType attackElement)
    {
        if (IsDead) return;

        // [핵심] 여기서 원소 상성 계산 (팀장님 기획 반영 지점)
        // 예: 내 속성이 불인데 물 공격을 받으면 1.5배
        float finalDamage = CalculateElementalDamage(amount, attackElement);

        currentHp -= finalDamage;

        // 피격 연출 명령
        if (visuals != null) visuals.PlayHitFlash();

        if (currentHp <= 0) Die();
    }

    private float CalculateElementalDamage(float amount, ElementType attackElement)
    {
        // TODO: 여기서 6대 원소 상성 로직(Enum 비교)을 작성하면 됩니다.
        return amount;
    }

    private void Die()
    {
        IsDead = true;
        if (visuals != null) visuals.PlayDeathEffect();

        // [증강 시스템 연결] 사망 시 원소 오브 드랍 로직 호출 예정 지점
        Debug.Log($"{data.enemyName} 사망. 원소 드랍 처리 필요.");

        gameObject.SetActive(false); // 임시 비활성화
    }
}