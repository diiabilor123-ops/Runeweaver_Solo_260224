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

        ShowDamagePopup(finalDamage);


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

    [Header("Damage UI")]
    [SerializeField] private GameObject damageTextPrefab; // 아까 만든 프리팹을 여기에 넣을 거예요

    private void ShowDamagePopup(float damage)
    {
        if (damageTextPrefab == null) return;

        // 1. 소환 위치를 현재 내 머리 위로 고정
        Vector3 spawnPos = transform.position + Vector3.up * 2.5f;

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
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"{gameObject.name} 사망!");
        Destroy(gameObject);
    }
}