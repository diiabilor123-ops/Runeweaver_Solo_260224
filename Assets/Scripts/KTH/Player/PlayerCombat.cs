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

            // [추가] 혹시 남아있을지 모르는 공격 트리거를 강제로 꺼버림 (중복 방지 핵심)
            _anim.ResetTrigger("Attack");

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
            if (currentAttackSpeed <= 0) currentAttackSpeed = 1f; // 0으로 나누기 방지

            yield return new WaitForSecondsRealtime(attackPostDelay / currentAttackSpeed);

            _controller.IsAttacking = false;

            // 5. 선입력 확인: 공격이 끝날 때 클릭 예약이 있었다면 즉시 다음 공격!
            if (_hasBufferedAttack)
            {
                _hasBufferedAttack = false;
                yield return null; // 한 프레임 대기
                TryAttack();
            }
        }

        /// <summary>
        /// [중요] 애니메이션 이벤트에서 호출할 함수입니다.
        /// 애니메이션 파일의 Events 탭에서 'Function' 이름을 ShootArrow로 적어주세요.
        /// </summary>
        public void ShootArrow()
        {
            // [수정] 발사 직전에도 마우스 위치를 다시 갱신하여 조작감을 높임
            InstantLookAtMouse();

            // 1. 전진 연출
            transform.DOMove(transform.position + transform.forward * stepDistance, 0.1f).SetEase(Ease.OutQuad);

            // 2. [원래 코드 복구] 각도 보정 없이 firePoint의 세팅된 로테이션을 그대로 사용
            if (bulletPrefab == null || firePoint == null) return;

            // [핵심 수정] 화살의 발사 회전값을 firePoint.forward 기준으로 잡되, 
            // 수평(Y=0)을 강제하여 위/아래로 쏘는 현상을 방지합니다.
            Vector3 shootDir = firePoint.forward;
            shootDir.y = 0f;
            shootDir.Normalize();

            // 원래 코드 방식: Instantiate(prefab, position, rotation)
            GameObject go = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDir));

            if (go.TryGetComponent(out BulletBase bullet))
            {
                BulletDataSO data = BulletManager.Instance.GetCurrentEquippedData();
                if (data != null)
                {
                    bullet.Setup(data, shootDir);

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
            // 1. 카메라에서 마우스 위치로 레이 생성
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);

            // 2. firePoint의 높이(Y값)를 기준으로 수평 평면 생성
            // Plane(법선 방향, 평면 위의 한 점)
            Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0, firePoint.position.y, 0));

            // 3. 레이가 이 수평 평면과 만나는 지점(distance) 계산
            if (horizontalPlane.Raycast(ray, out float enter))
            {
                // 실제 충돌 지점 계산
                Vector3 hitPoint = ray.GetPoint(enter);

                // 캐릭터 몸체는 Y축으로만 회전 (수평 방향 벡터)
                Vector3 lookDir = hitPoint - transform.position;
                lookDir.y = 0;

                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }

                // firePoint는 정확한 hitPoint를 바라보게 함
                Vector3 fireDir = hitPoint - firePoint.position;
                if (fireDir != Vector3.zero)
                {
                    firePoint.rotation = Quaternion.LookRotation(fireDir.normalized);
                }
            }
        }


    }
}