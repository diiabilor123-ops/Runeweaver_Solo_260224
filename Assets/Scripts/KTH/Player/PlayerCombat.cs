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
        [SerializeField] private float stepDistance = 0.05f;    // 공격 시 미세 전진 거리

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

        /// <summary>
        /// 대시 등으로 공격을 강제 취소할 때 호출됩니다.
        /// </summary>
        public void CancelAttack()
        {
            StopAllCoroutines();
            _controller.IsAttacking = false;
            _hasBufferedAttack = false;
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
            _aimHandler.UpdateAim(); // 2. 발사 직전 다시 조준 (정밀도 향상)

            // 1. 이번 공격의 데미지 판정을 미리 계산합니다.
            float playerCritChance = PlayerStats.Instance.critRate;
            bool isCrit = UnityEngine.Random.value < playerCritChance;

            // 2. 현재 공격 속도를 계산합니다. (번개 유도 화살 확률에 영향)
            // 애니메이터의 AttackSpeed 파라미터가 1.5라면 extraAS는 0.5가 됩니다.
            float currentAS = _anim ? _anim.GetFloat("AttackSpeed") : 1.0f;
            float extraAS = Mathf.Max(0, currentAS - 1.0f);

            // 3. 발사기 호출
            if (_weaponHandler != null)
            {
                _weaponHandler.ExecuteAttack(_currentAttackSlot, isCrit, extraAS);
            }

            // 4. 전진 연출 (성능을 위해 이전 연출 중복 방지 추가)
            transform.DOKill(true);
            transform.DOMove(transform.position + transform.forward * stepDistance, 0.1f)
                     .SetEase(Ease.OutQuad);
        }
    }
}