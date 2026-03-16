using UnityEngine;
using System.Linq;

namespace Runeweaver.Player
{
    /// <summary>
    /// [조준 담당] 마우스 위치를 기반으로 캐릭터와 발사 지점의 회전을 관리합니다.
    /// </summary>
    public class PlayerAimHandler : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;

        [Header("Magnetic Aim Settings")]
        [SerializeField] private float assistRadius = 2.5f;
        [SerializeField] private float maxAssistAngle = 35f;
        [SerializeField] private LayerMask enemyLayer;

        /// <summary>
        /// 마우스 커서가 가리키는 지점을 '바닥 평면' 기준으로 찾아줍니다.
        /// </summary>
        public Vector3 GetMouseWorldPosition()
        {
            if (UnityEngine.Camera.main == null) return transform.position + transform.forward;

            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            // 발사 지점(firePoint)의 높이를 기준으로 가상의 바닥 평면을 만듭니다.
            Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0, firePoint.position.y, 0));

            if (horizontalPlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return transform.position + transform.forward * 10f;
        }

        /// <summary>
        /// 마우스 주변의 적을 찾아 보정된 방향을 반환합니다.
        /// </summary>
        public Vector3 GetMagneticAimDirection()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3 currentForward = transform.forward;

            // 1. 마우스 주변 적 탐색
            Collider[] hits = Physics.OverlapSphere(mouseWorldPos, assistRadius, enemyLayer);
            if (hits.Length == 0) return currentForward;

            Collider bestTarget = null;
            float minMouseDist = float.MaxValue;

            foreach (var hit in hits)
            {
                Vector3 enemyGroundPos = hit.transform.position;
                enemyGroundPos.y = transform.position.y; // 높이 평면화

                Vector3 dirToTarget = (enemyGroundPos - transform.position).normalized;

                // [각도 필터] 내 정면 기준 일정 각도 이내만
                float angle = Vector3.Angle(currentForward, dirToTarget);
                if (angle > maxAssistAngle) continue;

                // [거리 필터] 마우스 조준점과 가장 가까운 적
                float distToMouse = Vector3.Distance(mouseWorldPos, enemyGroundPos);
                if (distToMouse < minMouseDist)
                {
                    minMouseDist = distToMouse;
                    bestTarget = hit;
                }
            }

            if (bestTarget != null)
            {
                Vector3 finalDir = (bestTarget.transform.position - transform.position).normalized;
                finalDir.y = 0;
                return finalDir;
            }

            return currentForward;
        }

        public void UpdateAim()
        {
            Vector3 hitPoint = GetMouseWorldPosition();

            // 캐릭터 몸체 회전
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);

            // 발사구 회전
            Vector3 fireDir = hitPoint - firePoint.position;
            if (fireDir != Vector3.zero)
                firePoint.rotation = Quaternion.LookRotation(fireDir.normalized);
        }
    }
}