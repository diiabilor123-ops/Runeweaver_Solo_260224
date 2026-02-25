using System.Collections;
using UnityEngine;
using DG.Tweening; // 쫀득한 연출을 위한 DOTween

namespace Runeweaver.Player
{
    /// <summary>
    /// [대시 전용 스크립트]
    /// 지정된 거리만큼 빠르게 이동하고 쿨타임을 관리합니다.
    /// In-Place 애니메이션 환경에 맞춰 DOTween으로 위치를 직접 이동시킵니다.
    /// </summary>
    public class PlayerDash : MonoBehaviour
    {
        [SerializeField] private float dashDistance = 5f;
        [SerializeField] private float dashDuration = 0.2f; // 대시 속도 (낮을수록 빠름)
        [SerializeField] private float dashCooldown = 0.5f;

        public bool CanDash { get; private set; } = true; // Controller가 읽어갈 수 있도록 프로퍼티 사용
        private PlayerController _controller;
        private Animator _anim;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _anim = GetComponent<Animator>();
        }

        public void DoDash(Vector3 inputDir)
        {
            // 중복 대시 방지 및 쿨타임 체크
            if (CanDash && !_controller.IsDashing)
            {
                StartCoroutine(DashRoutine(inputDir));
            }
        }

        private IEnumerator DashRoutine(Vector3 inputDir)
        {
            CanDash = false;
            _controller.IsDashing = true;

            // 1. 대시 애니메이션 실행
            if (_anim) _anim.SetTrigger("Dash");

            // 2. 대시 방향 결정: 입력이 있으면 그쪽으로, 없으면 현재 캐릭터 전방으로
            Vector3 dashDirection = inputDir != Vector3.zero ? inputDir : transform.forward;

            // 3. 대시 시작 시 즉시 해당 방향을 바라보게 함
            transform.rotation = Quaternion.LookRotation(dashDirection);

            // 4. DOTween 이동: In-Place 애니메이션이므로 코드가 직접 좌표를 옮깁니다.
            // Ease.OutQuad는 처음에 빠르고 끝에 살짝 감속되어 타격감이 좋습니다.
            transform.DOMove(transform.position + dashDirection * dashDistance, dashDuration)
                     .SetEase(Ease.OutQuad);

            // 대시 이동 시간만큼 대기
            yield return new WaitForSeconds(dashDuration);

            // 5. 대시 종료 처리
            _controller.IsDashing = false;

            // [추가 디테일] 대시가 끝난 직후 애니메이터의 Speed가 이전 값을 기억할 수 있으므로 
            // 블렌드 트리가 즉시 반응하도록 애니메이터를 리셋하거나 파라미터를 갱신해주는 것이 좋습니다.
            if (_anim) _anim.ResetTrigger("Dash");

            // 6. 쿨타임 대기
            yield return new WaitForSeconds(dashCooldown);
            CanDash = true;
        }
    }
}