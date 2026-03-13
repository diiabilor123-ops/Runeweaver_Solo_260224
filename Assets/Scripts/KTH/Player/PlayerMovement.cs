using UnityEngine;

namespace Runeweaver.Player
{
    /// <summary>
    /// [이동 및 회전 담당]
    /// 블렌드 트리를 활용하여 Idle-Walk-Run을 부드럽게 전환하고,
    /// 월드 좌표 기준 이동으로 축 왜곡을 방지합니다.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float acceleration = 10f; // 애니메이션 파라미터 변화 속도
        [SerializeField] private float animSpeedMultiplier = 0f;// 실제 이동 속도가 빠를수록 애니메이션 재생 속도도 높입니다.

        [Header("Ground Check")]
        [SerializeField] private float checkDistance = 0.5f; // 캐릭터 중심에서 얼마나 앞을 체크할지
        [SerializeField] private LayerMask groundLayer;      // 타일 레이어 (NavMesh 또는 Environment)

        private Animator _anim;
        private float _currentSpeedValue; // 현재 블렌드 트리 파라미터 값

        private void Awake() => _anim = GetComponent<Animator>();

        // Controller에서 호출하는 이동 함수
        public void Move(Vector3 dir, bool isAttacking)
        {
            // [1] 공격 중 처리
            if (isAttacking)
            {
                // 공격 중에는 이동을 멈추고 애니메이션 파라미터도 0(Idle)으로 부드럽게 낮춤
                StopMovementAnimation();
                _anim.speed = 1.0f; // 공격 중에는 애니메이션 속도 초기화
                return;
            }

            // [2] WASD 입력이 있을 때
            if (dir != Vector3.zero)
            {
                // 1. 회전: 입력 방향(dir)으로 즉각적인 회전 (Slerp)
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * PlayerStats.Instance.rotateSpeed);

                // [핵심: 바닥 체크] 
                // 이동하려는 지점(dir 방향으로 checkDistance만큼 앞)의 아래에 바닥이 있는지 확인
                if (IsGroundAhead(dir))
                {
                    transform.position += dir * PlayerStats.Instance.moveSpeed * Time.deltaTime;
                    _anim.speed = 1.0f + (PlayerStats.Instance.moveSpeed * animSpeedMultiplier);
                    UpdateAnimationParameter(dir.magnitude);
                }
                else
                {
                    // 바닥이 없으면 이동하지 않고 Idle로 전환
                    StopMovementAnimation();
                }
            }
            // [3] 입력이 없을 때 (Idle)
            else
            {
                _anim.speed = 1.0f; // 멈췄을 때 속도 초기화
                StopMovementAnimation();
            }
        }

        // 레이캐스트를 이용한 바닥 확인 로직
        private bool IsGroundAhead(Vector3 dir)
        {
            // 캐릭터 발밑보다 살짝 위에서 아래로 쏩니다.
            Vector3 rayStart = transform.position + (dir * checkDistance) + (Vector3.up * 0.5f);

            // 1.5m 아래까지 레이를 쏴서 groundLayer가 잡히는지 확인
            return Physics.Raycast(rayStart, Vector3.down, 1.5f, groundLayer);
        }

        /// <summary>
        /// 애니메이션 파라미터를 목표값까지 부드럽게 보정하여 전달 (블렌드 트리용)
        /// </summary>
        private void UpdateAnimationParameter(float targetValue)
        {
            if (_anim == null) return;

            // Mathf.Lerp를 사용하여 0 <-> 0.5 <-> 1 사이를 부드럽게 보간합니다.
            // 이렇게 해야 걷기에서 뛰기로 넘어갈 때 발동작이 자연스럽습니다.
            _currentSpeedValue = Mathf.Lerp(_currentSpeedValue, targetValue, Time.deltaTime * acceleration);
            _anim.SetFloat("Speed", _currentSpeedValue);
        }

        /// <summary>
        /// 이동 애니메이션을 0(Idle)으로 부드럽게 정지
        /// </summary>
        private void StopMovementAnimation()
        {
            if (_anim == null) return;

            _currentSpeedValue = Mathf.Lerp(_currentSpeedValue, 0f, Time.deltaTime * acceleration);
            _anim.SetFloat("Speed", _currentSpeedValue);
        }
    }


}
