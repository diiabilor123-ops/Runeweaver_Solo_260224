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
            else _hasBufferedAttack = true; // 공격 중이면 선입력 저장
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
            _anim.ResetTrigger("Attack");

            _aimHandler.UpdateAim(); // 1. 공격 시작 시 즉시 조준
            if (_anim) _anim.SetTrigger("Attack");

            // 애니메이션 배속(AttackSpeed)에 따른 대기 시간 계산
            float currentAttackSpeed = _anim ? _anim.GetFloat("AttackSpeed") : 1f;
            yield return new WaitForSecondsRealtime(attackPostDelay / Mathf.Max(0.1f, currentAttackSpeed));

            _controller.IsAttacking = false;

            // 선입력 처리 (공격 끝날 때 예약된 입력이 있으면 다시 공격)
            if (_hasBufferedAttack)
            {
                _hasBufferedAttack = false;
                yield return null;
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
            // (나중에 실제 플레이어 치명타 확률 변수를 여기에 연결하세요)
            float playerCritChance = 0.15f; // 예: 기본 15%
            bool isCrit = UnityEngine.Random.value < playerCritChance;

            // 2. 현재 공격 속도를 계산합니다. (번개 유도 화살 확률에 영향)
            // 애니메이터의 AttackSpeed 파라미터가 1.5라면 extraAS는 0.5가 됩니다.
            float currentAS = _anim ? _anim.GetFloat("AttackSpeed") : 1.0f;
            float extraAS = Mathf.Max(0, currentAS - 1.0f);

            // 3. 이제 3개의 인자를 모두 넣어서 호출합니다!
            _weaponHandler.ExecuteAttack(_currentAttackSlot, isCrit, extraAS);

            // 전진 연출 (DOTween)
            transform.DOMove(transform.position + transform.forward * stepDistance, 0.1f).SetEase(Ease.OutQuad);

        }
    }
}