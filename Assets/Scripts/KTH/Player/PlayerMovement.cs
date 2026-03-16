using UnityEngine;

namespace Runeweaver.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Animation Settings")]
        // 가속도를 4~5 정도로 낮추면 Idle->Walk->Run 전환이 훨씬 묵직하고 자연스러워집니다.
        [SerializeField] private float acceleration = 5f;

        // 멈출 때 발이 공중에 뜨는 걸 방지하기 위해 멈춤 가속도를 따로 둡니다.
        [SerializeField] private float stopSpeed = 8f;

        [Header("Ground Check")]
        [SerializeField] private float checkDistance = 0.5f;
        [SerializeField] private LayerMask groundLayer;

        private Animator _anim;
        private float _currentSpeedValue;

        private void Awake() => _anim = GetComponent<Animator>();

        public void Move(Vector3 dir, bool isAttacking)
        {
            // 1. 공격 중일 때는 물리 이동과 애니메이션 모두 정지
            if (isAttacking)
            {
                StopMovementAnimation();
                _anim.speed = 1.0f;
                return;
            }

            // 2. 방향 입력이 있을 때 (WASD를 누르고 있을 때)
            if (dir != Vector3.zero)
            {
                // [회전] 이동 가능 여부와 상관없이 입력 방향으로 캐릭터를 돌립니다.
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * PlayerStats.Instance.rotateSpeed);

                // [애니메이션] 이동 가능 여부와 상관없이 "달리기" 파라미터를 올립니다.
                // 이렇게 해야 낭떠러지에서 비비더라도 캐릭터가 달리는 모션을 취합니다.
                UpdateAnimationParameter(1.0f);
                _anim.speed = 1.0f;

                // [물리 이동] 오직 바닥이 있을 때만 실제로 위치를 옮깁니다.
                if (IsGroundAhead(dir))
                {
                    transform.position += dir * PlayerStats.Instance.moveSpeed * Time.deltaTime;
                }
                else
                {
                    // 바닥이 없으면 위치 이동만 안 할 뿐, 위에서 애니메이션은 이미 1.0으로 가고 있습니다.
                    // 만약 낭떠러지에서 비비는 속도를 늦추고 싶다면 여기서 _anim.speed = 0.5f; 정도로 낮출 수 있습니다.
                }
            }
            // 3. 입력이 없을 때 (키보드에서 손을 뗐을 때)
            else
            {
                _anim.speed = 1.0f;
                StopMovementAnimation();
            }
        }

        private bool IsGroundAhead(Vector3 dir)
        {
            Vector3 rayStart = transform.position + (dir * checkDistance) + (Vector3.up * 0.5f);
            return Physics.Raycast(rayStart, Vector3.down, 1.5f, groundLayer);
        }

        private void UpdateAnimationParameter(float targetValue)
        {
            if (_anim == null) return;

            // 가속도를 낮춰서 Walk 구간(0.5 근처)을 충분히 거쳐가게 만듭니다.
            _currentSpeedValue = Mathf.MoveTowards(_currentSpeedValue, targetValue, Time.deltaTime * acceleration);
            _anim.SetFloat("Speed", _currentSpeedValue);
        }

        private void StopMovementAnimation()
        {
            if (_anim == null) return;

            // 멈출 때는 조금 더 빠르게 멈춰서 미끄러짐을 방지합니다.
            _currentSpeedValue = Mathf.MoveTowards(_currentSpeedValue, 0f, Time.deltaTime * stopSpeed);
            _anim.SetFloat("Speed", _currentSpeedValue);
        }
    }
}