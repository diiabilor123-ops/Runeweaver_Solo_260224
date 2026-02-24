using System.Collections;
using UnityEngine;
using DG.Tweening; // 쫀득한 연출을 위한 DOTween

namespace Runeweaver.Player
{
    /// <summary>
    /// [대시 전용 스크립트]
    /// 지정된 거리만큼 빠르게 이동하고 쿨타임을 관리합니다.
    /// </summary>
    public class PlayerDash : MonoBehaviour
    {
        [SerializeField] private float dashDistance = 5f;
        [SerializeField] private float dashDuration = 0.15f; // 대시 속도 (낮을수록 빠름)
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
            StartCoroutine(DashRoutine(inputDir));
        }

        private IEnumerator DashRoutine(Vector3 inputDir)
        {
            _controller.IsDashing = true; // 전체 상태를 대시 중으로 변경
            CanDash = false;

            if (_anim) _anim.SetTrigger("Dash");

            // 입력 방향이 있으면 그쪽으로, 없으면 현재 앞방향으로 대시
            Vector3 dashTarget = inputDir != Vector3.zero ? inputDir : transform.forward;

            // DOMove: DOTween을 이용해 물리적 충돌은 무시하고 목표 지점까지 부드럽게 쏩니다.
            transform.DOMove(transform.position + dashTarget * dashDistance, dashDuration).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(dashDuration);
            _controller.IsDashing = false; // 대시 종료

            yield return new WaitForSeconds(dashCooldown);
            CanDash = true; // 쿨타임 종료
        }
    }
}