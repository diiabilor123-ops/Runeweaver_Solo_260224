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
            Instance = this;
            currentHp = maxHp;
        }
    }
}