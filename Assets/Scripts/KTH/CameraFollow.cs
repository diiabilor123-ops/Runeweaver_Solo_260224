using UnityEngine;

namespace Runeweaver.Camera
{
    public class HadesCamera : MonoBehaviour
    {
        public Transform target;      // 플레이어
        public Vector3 offset = new Vector3(0, 12, -9); // 골든 수치
        public float smoothTime = 0.2f; // 카메라 반응 속도 (낮을수록 빠름)

        [Header("Hades Dynamics")]
        public float mouseInfluence = 3.0f;  // 마우스 방향으로 밀어주는 힘
        public float moveInfluence = 2.0f;   // 이동 방향으로 밀어주는 힘

        private Vector3 _currentVelocity;
        private Vector3 _targetPos;

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. 기본 위치 (플레이어 머리 위)
            _targetPos = target.position + offset;

            // 2. 마우스 영향력 계산 (화면 중앙으로부터 마우스 위치의 오프셋)
            Vector3 mousePos = Input.mousePosition;
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            Vector3 mouseDir = (mousePos - screenCenter);

            // 화면 크기에 상관없게 정규화 시킨 후 영향력 곱하기
            Vector3 mouseOffset = new Vector3(mouseDir.x / screenCenter.x, 0, mouseDir.y / screenCenter.y);
            _targetPos += mouseOffset * mouseInfluence;

            // 3. 이동 방향 영향력 (플레이어가 보고 있는 방향으로 시야 확보)
            _targetPos += target.forward * moveInfluence;

            // 4. 최종 부드러운 이동 (SmoothDamp가 Lerp보다 훨씬 쫀득합니다)
            transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _currentVelocity, smoothTime);

            // 5. 회전 고정 (하데스 시점 X:55~60도)
            // 에디터에서 맞춘 회전값이 유지되도록 둡니다.
        }
    }
}