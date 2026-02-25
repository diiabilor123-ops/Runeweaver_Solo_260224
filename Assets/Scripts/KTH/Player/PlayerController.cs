using UnityEngine;

namespace Runeweaver.Player
{
    /// <summary>
    /// [지휘관 스크립트]
    /// 플레이어의 전체적인 상태(State)를 관리하고, 다른 기능 스크립트들을 조율합니다.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // 실시간 상태 확인용 프로퍼티 (다른 클래스에서 읽기 가능)
        public bool IsDashing { get; set; }
        public bool IsAttacking { get; set; }

        // 기능별 컴포넌트 참조
        private PlayerMovement _movement;
        private PlayerDash _dash;
        private PlayerCombat _combat;

        private void Awake()
        {
            // 각 기능 스크립트들을 미리 가져와서 연결해둡니다.
            _movement = GetComponent<PlayerMovement>();
            _dash = GetComponent<PlayerDash>();
            _combat = GetComponent<PlayerCombat>();
        }

        private void Update()
        {
            // [1] 입력값 계산: GetAxisRaw로 즉각적인 반응 유도
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // [대각선 보정] normalized를 사용하여 상하좌우/대각선 어디든 이동 거리를 '1'로 고정합니다.
            Vector3 inputDir = new Vector3(h, 0, v).normalized;

            // [2] 대시 입력: 하데스처럼 공격 중 대시하면 공격을 캔슬합니다.
            if (Input.GetKeyDown(KeyCode.Space) && _dash.CanDash)
            {
                if (IsAttacking) _combat.CancelAttack();
                _dash.DoDash(inputDir);
            }

            // [3] 공격 입력
            if (Input.GetMouseButtonDown(0))
            {
                _combat.TryAttack();
            }

            // [4] 행동 제어: 대시 중이 아닐 때만 이동 로직 실행
            if (!IsDashing)
            {
                // 입력 방향(inputDir)과 현재 공격 중인지 여부를 전달합니다.
                _movement.Move(inputDir, IsAttacking);
            }
        }
    }
}