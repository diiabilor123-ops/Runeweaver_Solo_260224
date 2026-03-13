using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace Runeweaver.Player
{
    public class PlayerDash : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private DashGhostManager ghostManager;
        [SerializeField] private float ghostInterval = 0.05f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer; // 벽 레이어 인스펙터에서 설정 필수!
        [SerializeField] private float stepSize = 0.2f;

        public bool CanDash { get; private set; } = true;
        private PlayerController _controller;
        private Animator _anim;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _anim = GetComponent<Animator>();
        }

        public void DoDash(Vector3 inputDir)
        {
            if (CanDash && !_controller.IsDashing)
            {
                StartCoroutine(DashRoutine(inputDir));
            }
        }

        private IEnumerator DashRoutine(Vector3 inputDir)
        {
            CanDash = false;
            _controller.IsDashing = true;

            if (_anim) _anim.SetTrigger("Dash");

            Vector3 dashDirection = inputDir != Vector3.zero ? inputDir : transform.forward;
            transform.rotation = Quaternion.LookRotation(dashDirection);

            float maxDist = PlayerStats.Instance.dashDistance;
            Vector3 finalTargetPos = transform.position;

            // [수정] 이동 경로 스캔 시 '벽'이 있으면 즉시 중단
            for (float d = stepSize; d <= maxDist; d += stepSize)
            {
                Vector3 checkPoint = transform.position + (dashDirection * d);

                // 1. 벽 체크 (캐릭터가 지나갈 수 있는지 부피 체크)
                if (Physics.CheckSphere(checkPoint + Vector3.up * 1f, 0.4f, wallLayer))
                    break;

                // 2. 바닥 체크
                if (Physics.Raycast(checkPoint + Vector3.up, Vector3.down, 2f, groundLayer))
                    finalTargetPos = checkPoint;
                else
                    break;
            }

            IEnumerator ghostCoroutine = CreateGhostDuringDash();
            StartCoroutine(ghostCoroutine);

            // 보정된 위치로 대시
            transform.DOMove(finalTargetPos, PlayerStats.Instance.dashDuration).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(PlayerStats.Instance.dashDuration);

            StopCoroutine(ghostCoroutine);
            _controller.IsDashing = false;

            if (_anim) _anim.ResetTrigger("Dash");
            yield return new WaitForSeconds(PlayerStats.Instance.dashCooldown);
            CanDash = true;
        }

        private IEnumerator CreateGhostDuringDash()
        {
            while (true)
            {
                // 플레이어가 실제로 있는 위치에 잔상 생성
                if (ghostManager != null) ghostManager.CreateGhost(transform);
                yield return new WaitForSeconds(ghostInterval);
            }
        }
    }
}