using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace Runeweaver.Player
{
    /// <summary>
    /// [전투 담당 스크립트]
    /// 공격 애니메이션, 선입력(Buffer), 미세 전진, 탄환 생성을 관리합니다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Bullet & FirePoint")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;

        [Header("Attack Settings")]
        [SerializeField] private float attackPostDelay = 0.35f; // 공격 후 딜레이
        [SerializeField] private float stepDistance = 0.6f;     // 공격 시 전진 거리

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
            if (!_controller.IsAttacking) StartCoroutine(AttackRoutine());
            else _hasBufferedAttack = true;
        }

        // 대시 등에 의해 공격이 끊길 때 호출
        public void CancelAttack()
        {
            StopCoroutine(nameof(AttackRoutine));
            _controller.IsAttacking = false;
            _hasBufferedAttack = false;
        }

        private IEnumerator AttackRoutine()
        {
            _controller.IsAttacking = true;

            // [하데스 조작감 핵심] 공격 시작하는 0.001초에 즉시 마우스 방향 바라보기
            InstantLookAtMouse();

            if (_anim) _anim.SetTrigger("Attack");

            // 활시위를 당기는 시간만큼 대기 (예: 0.15초)
            // 이 수치를 애니메이션 모션과 눈으로 맞추면 "손맛"이 완성됩니다!
            yield return new WaitForSeconds(0.15f);

            // [미세 전진] 공격 방향(앞)으로 살짝 몸을 던집니다.
            transform.DOMove(transform.position + transform.forward * stepDistance, 0.1f);

            // [김태훈 님 로직 적용] 탄 생성 및 데이터 주입
            SpawnBulletWithData();

            yield return new WaitForSeconds(attackPostDelay);
            _controller.IsAttacking = false;

            // [선입력 확인] 공격이 끝난 시점에 예약된 클릭이 있었다면 다시 공격!
            if (_hasBufferedAttack)
            {
                _hasBufferedAttack = false;
                TryAttack();
            }
        }

        private void SpawnBulletWithData()
        {
            if (bulletPrefab == null || firePoint == null) return;

            GameObject go = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            if (go.TryGetComponent(out BulletBase bullet))
            {
                // Manager를 통해 현재 장착된 탄환 데이터를 가져옵니다.
                BulletDataSO data = BulletManager.Instance.GetCurrentEquippedData();
                if (data != null)
                {
                    bullet.Setup(data, firePoint.forward);
                    // 사운드 매니저 연동
                    if (data.shootSound != null && SoundManager.Instance != null)
                        SoundManager.Instance.Play(data.shootSound, firePoint.position);
                }
            }
        }

        private void InstantLookAtMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 dir = new Vector3(hit.point.x, transform.position.y, hit.point.z) - transform.position;
                if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}