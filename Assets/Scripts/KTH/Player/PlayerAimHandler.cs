using UnityEngine;

namespace Runeweaver.Player
{
    /// <summary>
    /// [조준 담당] 마우스 위치를 기반으로 캐릭터와 발사 지점의 회전을 관리합니다.
    /// </summary>
    public class PlayerAimHandler : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;

        /// <summary>
        /// 마우스 커서 방향으로 몸체(Y축)와 발사구(정확한 좌표)를 즉시 회전시킵니다.
        /// 기존 PlayerCombat의 InstantLookAtMouse 로직을 그대로 계승합니다.
        /// </summary>
        public void UpdateAim()
        {
            if (UnityEngine.Camera.main == null) return;

            // 1. 카메라 레이캐스트로 지면(수평 평면) 충돌 계산
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0, firePoint.position.y, 0));

            if (horizontalPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                // 2. 캐릭터 몸체 회전 (Y축 고정하여 눕는 현상 방지)
                Vector3 lookDir = hitPoint - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);

                // 3. 발사구(firePoint) 회전 (상하 각도까지 포함하여 정확한 조준)
                Vector3 fireDir = hitPoint - firePoint.position;
                if (fireDir != Vector3.zero)
                    firePoint.rotation = Quaternion.LookRotation(fireDir.normalized);
            }
        }
    }
}