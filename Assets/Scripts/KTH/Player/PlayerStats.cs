using UnityEngine;

namespace Runeweaver.Player
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Health Stats")]
        public float maxHp = 100f;
        public float currentHp;

        [Header("Movement Stats")]
        public float moveSpeed = 6f;
        public float rotateSpeed = 25f;

        [Header("Dash Stats")]
        public float dashDistance = 5f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 0.5f;

        [Header("Offensive Stats")]
        public float critRate = 0.15f; // [핵심] 이 값이 1.0이 되면 불화살 100%!
        public float baseDamage = 10f;
        public float attackSpeedMultiplier = 1.0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            currentHp = maxHp;
        }

        public void UpdateHealth(float amount)
        {
            currentHp += amount;
            currentHp = Mathf.Clamp(currentHp, 0, maxHp); // 0 ~ 최대체력 사이로 고정
        }

        // [추가] 외부에서 체력을 깎을 때 호출하는 안전한 함수
        public void ApplyDamage(float amount)
        {
            currentHp -= amount;
            currentHp = Mathf.Clamp(currentHp, 0, maxHp); // 0 이하로 내려가지 않게 방지
        }

        // 어그먼트로 인해 MaxHP가 늘어났을 때 호출하세요.
        public void UpdateMaxHP(float extraHP)
        {
            float oldMax = maxHp;
            maxHp += extraHP;

            // 현재 체력도 비율에 맞춰 늘려주는 것이 버그가 적습니다.
            currentHp += extraHP;

            Debug.Log($"MaxHP 증가! {oldMax} -> {maxHp}");
        }

        // 힐링 어그먼트가 있다면
        public void Heal(float amount)
        {
            currentHp = Mathf.Min(currentHp + amount, maxHp);
        }
    }
}