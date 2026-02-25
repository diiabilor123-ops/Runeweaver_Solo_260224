using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace Runeweaver.Player
{
    /// <summary>
    /// [전투 담당 스크립트]
    /// 애니메이션 이벤트를 통해 화살 발사 타이밍을 100% 일치시키고,
    /// 선입력(Buffer)과 공격 시 미세 전진을 관리합니다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Bullet & FirePoint")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;

        [Header("Attack Settings")]
        [SerializeField] private float attackPostDelay = 0.35f; // 공격 후 딜레이
        [SerializeField] private float stepDistance = 0.05f;     // 공격 시 전진 거리


        private bool _hasBufferedAttack; // 선입력 체크용 변수
        private PlayerController _controller;
        private Animator _anim;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _anim = GetComponent<Animator>();
        }

        public void TryAttack()
        {
            // 공격 중이 아니면 실행, 중이면 '다음에 공격하겠다'고 예약만 함
            if (!_controller.IsAttacking)
            {
                StartCoroutine(AttackRoutine());
            }
            else _hasBufferedAttack = true;
        }

        // 대시 등에 의해 공격이 끊길 때 호출
        public void CancelAttack()
        {
            StopAllCoroutines();
            _controller.IsAttacking = false;
            _hasBufferedAttack = false;

            // 만약 미세 전진(DOTween) 중에 캔슬된다면 이동도 멈춰줍니다.
            transform.DOKill();
        }

        private IEnumerator AttackRoutine()
        {
            _controller.IsAttacking = true;

            // 1. 공격 시작 즉시 마우스 방향 바라보기
            InstantLookAtMouse();

            // 2. 애니메이션 실행 (Trigger)
            if (_anim) _anim.SetTrigger("Attack");

            // 3. [변경] 이전처럼 yield로 시간을 기다리지 않습니다.
            // 애니메이션이 재생되다가 설정한 Event 마커에 도달하면 
            // 아래에 있는 'ShootArrow()' 함수를 자동으로 실행합니다.

            // 4. 후딜레이 관리
            // 애니메이터에 설정한 AttackSpeed(Multiplier)를 고려하여 대기 시간을 계산합니다.
            float currentAttackSpeed = _anim ? _anim.GetFloat("AttackSpeed") : 1f;
            yield return new WaitForSeconds(attackPostDelay / currentAttackSpeed);

            _controller.IsAttacking = false;

            // 5. 선입력 확인: 공격이 끝날 때 클릭 예약이 있었다면 즉시 다음 공격!
            if (_hasBufferedAttack)
            {
                _hasBufferedAttack = false;
                TryAttack();
            }
        }

        /// <summary>
        /// [중요] 애니메이션 이벤트에서 호출할 함수입니다.
        /// 애니메이션 파일의 Events 탭에서 'Function' 이름을 ShootArrow로 적어주세요.
        /// </summary>
        public void ShootArrow()
        {
            // 1. 전진 연출
            transform.DOMove(transform.position + transform.forward * stepDistance, 0.1f).SetEase(Ease.OutQuad);

            // 2. [원래 코드 복구] 각도 보정 없이 firePoint의 세팅된 로테이션을 그대로 사용
            if (bulletPrefab == null || firePoint == null) return;

            // 원래 코드 방식: Instantiate(prefab, position, rotation)
            GameObject go = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            if (go.TryGetComponent(out BulletBase bullet))
            {
                BulletDataSO data = BulletManager.Instance.GetCurrentEquippedData();
                if (data != null)
                {
                    // 원래 코드 방식: firePoint.forward 방향으로 날리기
                    bullet.Setup(data, firePoint.forward);

                    if (data.shootSound != null && SoundManager.Instance != null)
                        SoundManager.Instance.Play(data.shootSound, firePoint.position);
                }
            }
        }

        /// <summary>
        /// [중요] 애니메이션 이벤트에서 호출할 함수입니다.
        /// 애니메이션 파일의 Events 탭에서 'Function' 이름을 ShootArrow로 적어주세요.
        /// </summary>
        private void InstantLookAtMouse()
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            // 맵 레이어(예: Ground)만 체크하도록 설정하는 것이 성능상 좋습니다.
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 targetPoint = hit.point;
                targetPoint.y = transform.position.y; // 캐릭터가 위아래로 꺾이지 않게 방지

                Vector3 dir = targetPoint - transform.position;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }
            }
        }


    }
}