using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TeleportSlash", menuName = "Boss/Patterns/TeleportSlash")]
public class TeleportSlashPattern : BossPattern
{
    public float teleportDist = 2.5f;

    public override IEnumerator Execute(BossBrain brain)
    {
        brain.mover.SetAgentActive(false);
        brain.canRotate = true; // 회전 허용

        // 1. 유저 곁으로 순간이동
        Vector3 targetPos = brain.GetValidNavMeshPos(brain.GetBehindPlayerPos(teleportDist));
        yield return brain.StartCoroutine(brain.Teleport.TeleportRoutine(targetPos, 0.4f));

        // 2. 공격 방향 고정
        // [수정] 순간이동 직후 유저를 즉시 바라보게 하고 회전을 잠급니다.
        brain.InstantLookAt(brain.player.position);
        brain.canRotate = false; // [핵심] 이제부터는 유저가 움직여도 보스가 돌아가지 않습니다.

        // 공격 전 살짝 멈춤 (유저가 피할 틈을 줌)
        yield return new WaitForSeconds(0.15f);

        // 3. 타격 (보스는 이제 고정된 방향으로만 휘두릅니다)
        brain.ToggleSword(true);
        brain.anim.SetTrigger("Boss_Attack_Slash");

        // 애니메이션 도중 유저가 대시로 뒤로 넘어가도 보스는 허공을 쳐야 합니다.
        yield return new WaitForSeconds(0.8f);

        brain.ToggleSword(false);
        brain.canRotate = true; // [해제] 공격 끝났으니 다시 회전 가능

        // 4. 후속 연계
        float dist = Vector3.Distance(brain.transform.position, brain.player.position);
        if (dist < 3.0f)
        {
            yield return brain.StartCoroutine(brain.ExecuteCircularSlash());
        }
        else
        {
            yield return brain.StartCoroutine(brain.LookAtPlayerRoutine(0.5f));
        }

        brain.mover.SetAgentActive(true);
    }
}