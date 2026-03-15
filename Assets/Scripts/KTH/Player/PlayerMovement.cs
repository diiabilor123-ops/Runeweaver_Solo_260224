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
            if (isAttacking)
            {
                StopMovementAnimation();
                _anim.speed = 1.0f;
                return;
            }

            if (dir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * PlayerStats.Instance.rotateSpeed);

                if (IsGroundAhead(dir))
                {
                    transform.position += dir * PlayerStats.Instance.moveSpeed * Time.deltaTime;

                    // [수정] 애니메이션 재생 속도를 이동 속도에 맞춥니다.
                    _anim.speed = 1.0f;

                    // [핵심] 목표값을 1.0(Run)으로 전달합니다. 
                    // (걷기/뛰기 키가 따로 있다면 여기서 값을 분기하면 됩니다)
                    UpdateAnimationParameter(1.0f);
                }
                else
                {
                    StopMovementAnimation();
                }
            }
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