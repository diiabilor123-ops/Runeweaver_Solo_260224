using System.Collections;
using UnityEngine;

namespace Dev.jhs.Object
{
    public class Player : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 3f;
        [SerializeField] private float rotationSpeed = 15f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashColldown = 1f;

        [Header("Attack")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        private float attackCoolDown = 0.5f;
        private bool isAttack = false;

        private bool isDashing  = false;
        private bool canDash = true;

        private Animator anim;
        private void Awake()
        {
            anim = GetComponent<Animator>();
        }
        private void Update()
        {
            if (isDashing || isAttack) return;
            playerMove();
            LookAtMouse();

            if(Input.GetKeyDown(KeyCode.Space)&& canDash)
            {
                StartCoroutine(Dash());
            }

            if(Input.GetMouseButton(0) && !isAttack)
            {
                StartCoroutine(AttackRoutine());
            }

        }

        private void playerMove()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 moveDir = new Vector3(h, 0, v).normalized;

            if (moveDir.magnitude >= 0.1f)
            {
                // 위치 이동
                transform.position += moveDir * speed * Time.deltaTime;
            }

           
            UpdateAnimation(moveDir.magnitude);
        }
        private void UpdateAnimation(float moveSpeed)
        {
            if (anim == null) return; 

            anim.SetFloat("MoveSpeed", moveSpeed);
        }

        private void LookAtMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float rayDistance;

            if (groundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 lookPoint = ray.GetPoint(rayDistance);

                Vector3 lookDir = new Vector3(lookPoint.x, transform.position.y, lookPoint.z) - transform.position;

                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        private IEnumerator Dash()
        {
            canDash = false;
            isDashing = true;

            Vector3 dashDir = transform.forward;

            float startTime = Time.time;

            while (Time.time < startTime + dashDuration)
            {
                transform.position += dashDir * dashSpeed * Time.deltaTime;
                yield return null;
            }
            isDashing = false;

            yield return new WaitForSeconds(dashColldown);
            canDash = true;
        }
        private IEnumerator AttackRoutine()
        {
            isAttack = true;
            if (anim != null) anim.SetTrigger("Attack");

            if (bulletPrefab != null && firePoint != null)
            {
                // 시작
                // 김태훈 수정함
                // 대신 생성된 탄에 "지금 활성화된 데이터"를 넣어달라고 매니저에게 요청합니다.
                // 1. 탄 프리팹 생성
                GameObject go = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

                // 2. BulletBase 컴포넌트를 가져와서 데이터 주입
                if (go.TryGetComponent(out BulletBase bullet))
                {
                    // [변경점] 이제 매니저에게 현재 장착된 데이터를 물어봅니다.
                    BulletDataSO dataToUse = BulletManager.Instance.GetCurrentEquippedData();

                    if (dataToUse != null)
                    {
                        bullet.Setup(dataToUse, firePoint.forward);

                        // ★ [추가] 발사 사운드 재생
                        // BulletDataSO에 shootSound 슬롯을 만드셨다면 아래와 같이 호출합니다.
                        if (dataToUse.shootSound != null && SoundManager.Instance != null)
                        {
                            SoundManager.Instance.Play(dataToUse.shootSound, firePoint.position);
                        }
                    }
                }
                // 끝
            }

            yield return new WaitForSeconds(attackCoolDown);

            isAttack = false;
        }
    }
}
