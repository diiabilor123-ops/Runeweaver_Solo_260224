using UnityEngine;

public class EnemyShield : MonoBehaviour
{
    [SerializeField] private float maxShield = 50f;
    private float currentShield;

    // 보호막 UI (노란색 셰이더 등) 연동을 위해 필요
    private EnemyVisuals visuals;

    void Awake()
    {
        currentShield = maxShield;
        visuals = GetComponent<EnemyVisuals>();
    }

    /// <summary>
    /// 데미지를 보호막이 먼저 흡수합니다.
    /// </summary>
    /// <returns>보호막이 흡수한 후 남은 데미지</returns>
    public float AbsorbDamage(float damage)
    {
        if (currentShield <= 0)
        {
            if (visuals != null) visuals.HasShield = false; // [추가] 쉴드 없음 설정
            return damage;
        }

        // [추가] 쉴드 있음 설정
        if (visuals != null) visuals.HasShield = true;

        float damageToShield = Mathf.Min(currentShield, damage);
        currentShield -= damageToShield;

        Debug.Log($"{gameObject.name} 보호막 잔량: {currentShield}");

        // [수정] 쉴드 피격 파티클 연출 위치를 몬스터의 현재 위치로 전달
        if (visuals != null) visuals.PlayShieldEffect(Vector3.zero);

        if (currentShield <= 0)
        {
            // [연출] 보호막 파괴 효과
            Debug.Log($"{gameObject.name} 보호막 파괴!");
        }

        return damage - damageToShield;
    }
}