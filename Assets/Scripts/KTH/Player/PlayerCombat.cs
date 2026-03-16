using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace Runeweaver.Player
{
    /// <summary>
    /// [전투 지휘관] 공격 애니메이션 흐름, 선입력(Buffer), 상태 제어를 담당합니다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float attackPostDelay = 0.35f; // 공격 후 딜레이
        [SerializeField] private float recoilDistance = 0.15f; // [변경] 미세 후진(반동) 거리
        [SerializeField] private float recoilDuration = 0.1f;  // 반동이 일어나는 시간

        [Header("Safe Recoil Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer; // [추가] 벽 체크용
        [SerializeField] private float groundCheckRadius = 0.3f; // [추가] 체크할 부피 범위
        [SerializeField] private float groundCheckDistance = 2.0f;

        private PlayerController _controller;
        private Animator _anim;
        private PlayerAimHandler _aimHandler;
        private WeaponHandler _weaponHandler;

        private SkillSlotType _currentAttackSlot;
        private bool _hasBufferedAttack;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _anim = GetComponent<Animator>();
            _aimHandler = GetComponent<PlayerAimHandler>();
            _weaponHandler = GetComponent<WeaponHandler>();
        }

        /// <summary>
        /// 외부(Controller)에서 공격 버튼 입력 시 호출됩니다.
        /// </summary>
        public void TryAttack(SkillSlotType slot)
        {
            if (!_controller.IsAttacking)
            {
                _currentAttackSlot = slot;
                StartCoroutine(AttackRoutine());
            }
            else
            {
                // [수정] 선입력 시 마지막에 누른 슬롯을 기억하도록 업데이트
                _currentAttackSlot = slot;
                _hasBufferedAttack = true;
            }
        }

        // [추가] 애니메이션 이벤트: 시위를 당기기 시작할 때 (응축)
        public void OnChargeStart()
        {
            // 현재 장착된 화살의 발사 사운드 데이터를 가져와서 재생 (끼익- 하는 느낌)
            var data = BulletManager.Instance.GetCurrentEquippedData();
            if (data != null && data.shootSound != null)
            {
                // 소리가 너무 일찍 나오면 playDelay를 SO에서 조절하거나 여기서 조절합니다.
                SoundManager.Instance.Play(data.shootSound, transform.position);
            }
        }

        /// <summary>
        /// 대시 등으로 공격을 강제 취소할 때 호출됩니다.
        /// </summary>
        public void CancelAttack()
        {
            StopAllCoroutines();
            _controller.IsAttacking = false;
            _hasBufferedAttack = false;

            if (_anim != null) _anim.ResetTrigger("Attack");

            transform.DOKill(); // 미세 전진 중단
        }

        /// <summary>
        /// 공격 애니메이션 실행 및 후딜레이를 관리하는 핵심 루틴입니다.
        /// </summary>
        private IEnumerator AttackRoutine()
        {
            _controller.IsAttacking = true;

            // [중요] 공격 시작 시점의 슬롯을 확정 지어 놓습니다.
            SkillSlotType attackStartSlot = _currentAttackSlot;

            // [추가] 애니메이션 파라미터 안전성 체크
            if (_anim != null)
            {
                _anim.ResetTrigger("Attack");
                _anim.SetTrigger("Attack");
            }

            _aimHandler.UpdateAim(); // 1. 공격 시작 시 즉시 조준

            // 애니메이션 배속(AttackSpeed)에 따른 대기 시간 계산
            float currentAttackSpeed = _anim ? _anim.GetFloat("AttackSpeed") : 1f;
            yield return new WaitForSeconds(attackPostDelay / Mathf.Max(0.1f, currentAttackSpeed));

            _controller.IsAttacking = false;

            // 선입력 처리 (공격 끝날 때 예약된 입력이 있으면 다시 공격)
            if (_hasBufferedAttack)
            {
                _hasBufferedAttack = false;
                // [핵심] 다음 프레임에 바로 공격을 이어가도록 처리
                TryAttack(_currentAttackSlot);
            }
        }

        /// <summary>
        /// [애니메이션 이벤트] 애니메이션의 '발사' 프레임에서 호출됩니다.
        /// </summary>
        public void ShootArrow()
        {
            if (_controller.IsDashing) return;

            // 1. [핵심 추가] 발사 직전, 에임 핸들러에게 보정된 방향을 물어봅니다.
            // 마우스 근처에 적이 있으면 그쪽으로, 없으면 원래 보던 방향을 반환합니다.
            Vector3 correctedDir = _aimHandler.GetMagneticAimDirection();
            transform.forward = correctedDir;

            // 1. 크리티컬 여부 계산 (피드백과 발사 로직에서 공통 사용)
            float playerCritChance = PlayerStats.Instance.critRate;
            bool isCrit = UnityEngine.Random.value < playerCritChance;

            // 2. [수정] 판정에 따른 피드백 호출
            if (FeedbackManager.Instance != null)
            {
                if (isCrit)
                    FeedbackManager.Instance.PlayCritFeedback(); // 묵직한 크리티컬
                else
                    FeedbackManager.Instance.PlayNormalFeedback(); // 가벼운 일반 타격
            }

            // 2. [핵심] 갈 수 있는 최선의 위치 계산
            Vector3 recoilDir = -transform.forward;
            Vector3 safeTargetPos = transform.position;
            float stepSize = 0.05f; // 체크할 세부 단위

            // 현재 위치에서 목표 거리까지 조금씩 가보면서 바닥이 있는지 확인합니다.
            for (float d = stepSize; d <= recoilDistance; d += stepSize)
            {
                Vector3 nextCheckPos = transform.position + (recoilDir * d);

                if (IsSafeToRecoil(nextCheckPos))
                {
                    safeTargetPos = nextCheckPos; // 안전하면 이 지점까지는 갈 수 있다고 저장
                }
                else
                {
                    break; // 한 번이라도 위험하면 거기서 멈춤
                }
            }

            // 3. 이동 및 연출 처리
            transform.DOKill(true);

            if (Vector3.Distance(transform.position, safeTargetPos) > 0.01f)
            {
                // 조금이라도 이동할 공간이 있다면 그곳까지 부드럽게 이동
                transform.DOMove(safeTargetPos, recoilDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                // [수정] 이동할 공간이 아예 없는 낭떠러지 끝이라면? 
                // 캐릭터의 '모델'만 살짝 흔들어주거나 제자리 진동(Shake)을 강하게 줍니다.
                // 이렇게 해야 "공격은 나갔는데 벽 때문에 못 밀려나는구나"라는 느낌이 납니다.
                transform.DOShakePosition(0.15f, 0.1f, 10, 90, false, true);
            }

            float currentAS = _anim ? _anim.GetFloat("AttackSpeed") : 1.0f;
            float extraAS = Mathf.Max(0, currentAS - 1.0f);

            if (_weaponHandler != null)
            {
                _weaponHandler.ExecuteAttack(_currentAttackSlot, isCrit, extraAS);
            }
        }

        private bool IsSafeToRecoil(Vector3 targetPos)
        {
            // 1. 벽 체크 (머리 높이에서 수행)
            if (Physics.CheckSphere(targetPos + Vector3.up * 1f, 0.4f, wallLayer))
                return false;

            // 2. 바닥 체크 (높이와 거리를 플레이어 좌표에 맞춰 보정)
            // 플레이어 좌표가 0.5이므로, 발바닥보다 살짝 위인 0.6 지점에서 쏩니다.
            // targetPos.y가 0.5라고 가정하면 오프셋을 0.1만 줘도 충분합니다.
            Vector3 rayStart = new Vector3(targetPos.x, targetPos.y + 0.1f, targetPos.z);

            // [중요] 거리를 넉넉하게 1.0f로 설정하여 바닥(y=0)을 확실히 뚫고 지나가게 합니다.
            float checkDist = 1.0f;
            Debug.DrawRay(rayStart, Vector3.down * checkDist, Color.red, 1.0f);

            // 3. 레이캐스트 실행 (자기 자신 무시 설정 추가)
            // QueryTriggerInteraction.Ignore를 넣어 트리거 콜라이더에 맞는 것을 방지합니다.
            if (Physics.Raycast(rayStart, Vector3.down, checkDist, groundLayer, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return false; // 바닥 없음 (낭떠러지)
        }
    }
}