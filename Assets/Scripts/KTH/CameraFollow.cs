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
        public float smoothTime = 0.2f; // 카메라가 목표 위치로 이동하는 부드러움 정도

        [Header("Hades Dynamics")]
        public float mouseInfluence = 1.0f;  // 마우스 위치에 따라 카메라를 밀어주는 힘
        public float moveInfluence = 1.0f;   // 플레이어 이동 방향으로 시야를 확보하는 힘

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
            if (target == null) return;

            // [소제목: 카메라 위치 계산]
            // 기본 타겟 위치 계산
            Vector3 targetPos = target.position + offset;

            // [소제목: 마우스 및 이동 영향력 적용]
            Vector3 mouseViewportPos = _mainCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 mouseDir = mouseViewportPos - new Vector3(0.5f, 0.5f, 0);
            Vector3 mouseOffset = new Vector3(mouseDir.x, 0, mouseDir.y);

            targetPos += mouseOffset * mouseInfluence;
            targetPos += target.forward * moveInfluence;

            // [소제목: 부드러운 위치 할당 (3.0)]
            // Position 컴포넌트가 계산한 위치를 덮어씁니다.
            state.RawPosition = Vector3.Lerp(state.RawPosition, targetPos, 1f - Mathf.Pow(0.001f, deltaTime / smoothTime));
        }

        // [소제목: 파이프라인 우선순위]
        // 위치 제어 컴포넌트가 먼저 계산되도록 우선순위를 높게 설정합니다.
        public override bool IsValid => target != null;
    }
}