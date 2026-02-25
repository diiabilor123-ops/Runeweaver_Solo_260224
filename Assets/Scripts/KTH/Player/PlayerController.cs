using UnityEngine;

namespace Runeweaver.Player
{
    /// <summary>
    /// [지휘관 스크립트]
    /// 카메라의 회전값(Y:45도 등)을 고려하여 플레이어의 이동 방향을 보정하고,
    /// 대시 및 공격 입력을 조율합니다.
    public class PlayerController : MonoBehaviour
    {
        // 실시간 상태 확인용 프로퍼티 (다른 클래스에서 읽기 가능)
        public bool IsDashing { get; set; }
        public bool IsAttacking { get; set; }

        // 기능별 컴포넌트 참조
        private PlayerMovement _movement;
        private PlayerDash _dash;
        private PlayerCombat _combat;

        // 카메라 참조 (메인 카메라를 기준으로 방향을 잡기 위함)
        private Transform _mainCameraTransform;

        private void Awake()
        {
            // 각 기능 스크립트들을 미리 가져와서 연결해둡니다.
            _movement = GetComponent<PlayerMovement>();
            _dash = GetComponent<PlayerDash>();
            _combat = GetComponent<PlayerCombat>();

            // 매 프레임 찾지 않도록 미리 캐싱해둡니다.
            if (UnityEngine.Camera.main != null)
                _mainCameraTransform = UnityEngine.Camera.main.transform;
        }

        private void Update()
        {
            // [1] 입력값 계산: GetAxisRaw로 즉각적인 반응 유도
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // [2] 카메라 방향을 기준으로 이동 방향 계산 (핵심 로직)
            Vector3 finalMoveDir = CalculateCameraRelativeDir(h, v);

            // [3] 대시 입력: 하데스처럼 공격 중 대시하면 공격을 캔슬
            if (Input.GetKeyDown(KeyCode.Space) && _dash.CanDash)
            {
                if (IsAttacking) _combat.CancelAttack();

                // 대시 방향은 입력이 있으면 그 방향으로, 없으면 현재 캐릭터가 보는 방향으로!
                Vector3 dashDir = finalMoveDir.magnitude > 0.1f ? finalMoveDir : transform.forward;
                _dash.DoDash(dashDir);
            }

            // [4] 공격 입력
            if (Input.GetMouseButtonDown(0))
            {
                _combat.TryAttack();
            }

            // [5] 행동 제어: 대시 중이 아닐 때만 이동 로직 실행
            if (!IsDashing)
            {
                // 이제 보정된 finalMoveDir를 전달하여 쿼터뷰에서도 정방향 이동이 가능하게 합니다.
                _movement.Move(finalMoveDir, IsAttacking);
            }
        }

        /// <summary>
        /// 카메라가 회전된 각도(예: Y:45)를 계산하여, 입력한 방향이 화면상 정방향이 되게 만듭니다.
        /// </summary>
        private Vector3 CalculateCameraRelativeDir(float h, float v)
        {
            if (_mainCameraTransform == null) return new Vector3(h, 0, v).normalized;

            // 카메라의 정면(Forward)과 오른쪽(Right) 벡터를 가져오되, 하늘/바닥 방향(Y축)은 무시합니다.
            Vector3 camForward = _mainCameraTransform.forward;
            Vector3 camRight = _mainCameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            // 사용자의 입력값(h, v)에 카메라 방향성을 곱해 새로운 방향 벡터를 생성합니다.
            // W를 누르면(v=1) 카메라가 바라보는 앞쪽으로 가고, D를 누르면(h=1) 카메라의 오른쪽으로 갑니다.
            return (camForward * v + camRight * h).normalized;
        }
    }
}