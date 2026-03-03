using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.0 네임스페이스

namespace Runeweaver.Camera
{
    // [소제목: 시네머신 연동 및 파이프라인 확장]
    // CinemachineExtension을 상속받아 시네머신의 가상 카메라 위치 계산 과정에 직접 개입합니다.
    [ExecuteAlways]
    [AddComponentMenu("Runeweaver/CameraFollow")]
    public class CameraFollow : CinemachineComponentBase
    {
        public Transform target;      // 플레이어 오브젝트
        public Vector3 offset = new Vector3(-9, 12, -9); // 카메라의 기본 상대 위치 (골든 수치)
        public float smoothTime = 0.4f; // 카메라가 목표 위치로 이동하는 부드러움 정도

        [Header("Hades Dynamics")]
        public float mouseInfluence = 3.0f;  // 마우스 위치에 따라 카메라를 밀어주는 힘
        public float moveInfluence = 1.0f;   // 플레이어 이동 방향으로 시야를 확보하는 힘

        // 마우스 오프셋이 급격하게 변하지 않도록 저장하는 변수
        private Vector3 _smoothMouseOffset;
        private Vector3 _mouseVelocity; // SmoothDamp용 내부 변수
        private UnityEngine.Camera _mainCamera;

        // [소제목: 파이프라인 스테이지 정의]
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

        protected void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
        }

        // [소제목: 카메라 위치 계산 및 연출 적용]
        // 시네머신이 기본 위치를 계산한 후, 이 함수가 호출되어 위치를 최종 보정합니다.
        // 2. PostPipelineStageCallback 대신 OnPostPipelineStage를 사용합니다.
        public override void MutateCameraState(ref CameraState state, float deltaTime)
        {
            if (target == null || _mainCamera == null) return;

            // 1. 플레이어 기준 기본 위치
            Vector3 baseTargetPos = target.position + offset;

            // 2. 마우스 영향력 계산 (뷰포트 0.5가 중앙)
            Vector3 mouseViewportPos = _mainCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 rawMouseDir = new Vector3(mouseViewportPos.x - 0.5f, 0, mouseViewportPos.y - 0.5f);

            // [핵심] 마우스의 이동량 자체를 SmoothDamp로 부드럽게 깎아줍니다.
            // 이렇게 하면 마우스를 휙 움직여도 카메라는 '스윽'하고 따라옵니다.
            _smoothMouseOffset = Vector3.SmoothDamp(_smoothMouseOffset, rawMouseDir * mouseInfluence, ref _mouseVelocity, smoothTime);

            // 3. 최종 목표 위치 계산
            Vector3 finalTargetPos = baseTargetPos + _smoothMouseOffset;

            // 캐릭터 이동 방향 시야 확보 (이것도 너무 튀면 값을 낮추거나 SmoothDamp 적용 대상에 포함)
            finalTargetPos += target.forward * moveInfluence;

            // 4. 시네머신 상태에 할당 (deltaTime이 0일 때 에디터 보정용 예외처리)
            if (deltaTime > 0)
            {
                state.RawPosition = Vector3.Lerp(state.RawPosition, finalTargetPos, 1f - Mathf.Pow(0.001f, deltaTime / smoothTime));
            }
            else
            {
                state.RawPosition = finalTargetPos;
            }
        }

        public override bool IsValid => target != null;
    }
}