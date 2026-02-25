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
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotateSpeed = 25f;
        [SerializeField] private float acceleration = 10f; // 애니메이션 파라미터 변화 속도


        // [추가] 애니메이션 동기화 배율 (발 미끄러짐 방지용)
        // 실제 이동 속도가 빠를수록 애니메이션 재생 속도도 높입니다.
        [SerializeField] private float animSpeedMultiplier = 0f;

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
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);

                // 2. 이동: 월드 좌표 기준 정직한 이동 (In-Place 애니메이션을 쓰므로 코드가 이동을 전담)
                transform.position += dir * moveSpeed * Time.deltaTime;

                // [핵심 해결책] 
                // 애니메이터 전체 재생 속도(speed)를 실제 moveSpeed에 비례하게 조절합니다.
                // 발이 너무 미끄러지면 animSpeedMultiplier 값을 인스펙터에서 조절해보세요.
                _anim.speed = 1.0f + (moveSpeed * animSpeedMultiplier);

                // 3. 애니메이션: 블렌드 트리용 Speed 값을 1(Run)로 서서히 올림
                UpdateAnimationParameter(dir.magnitude);
            }
            // [3] 입력이 없을 때 (Idle)
            else
            {
                _anim.speed = 1.0f; // 멈췄을 때 속도 초기화
                StopMovementAnimation();
            }
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
