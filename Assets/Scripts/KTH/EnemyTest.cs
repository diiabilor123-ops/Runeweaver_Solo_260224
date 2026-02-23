using UnityEngine;

/// <summary>
/// 역할: 몬스터의 상태를 관리합니다.
/// 인터페이스 IDamageable을 상속받아 투사체로부터 데미지를 받을 수 있게 됩니다.
/// </summary>
public class EnemyTest : MonoBehaviour, IDamageableTest
{
    [SerializeField] private float hp = 100f;

    public void TakeDamage(float amount)
    {
        hp -= amount;
        Debug.Log($"{gameObject.name}이(가) {amount}의 데미지를 입음. 남은 HP: {hp}");

        if (hp <= 0)
        {
            // 사망 처리 로직
            gameObject.SetActive(false);
        }
    }
}