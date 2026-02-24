using UnityEngine;

namespace Runeweaver.Player
{
    /// <summary>
    /// [이동 및 회전 담당]
    /// 단순 이동과 마우스 방향으로 캐릭터를 회전시키는 기능을 수행합니다.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotateSpeed = 15f;

        private Animator _anim;

        private void Awake() => _anim = GetComponent<Animator>();

        // Controller에서 호출하는 이동 함수
        public void Move(Vector3 dir, bool isAttacking)
        {
            // 공격 중이라면 이동하지 않고 애니메이션 파라미터만 0으로 만듭니다.
            if (isAttacking)
            {
                if (_anim) _anim.SetFloat("Speed", 0);
                return;
            }

            // 실제 이동 처리 (Space.World 기준)
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

            // 애니메이션 파라미터 전달 (dir.magnitude는 0 또는 1)
            if (_anim) _anim.SetFloat("Speed", dir.magnitude);
        }

        // 부드럽게 마우스를 바라보게 하는 함수
        public void LookAtMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 targetPos = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                Vector3 lookDir = targetPos - transform.position;

                if (lookDir != Vector3.zero)
                {
                    // Quaternion.Slerp: 각도를 부드럽게 보간하며 회전시킵니다.
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * rotateSpeed);
                }
            }
        }
    }
}