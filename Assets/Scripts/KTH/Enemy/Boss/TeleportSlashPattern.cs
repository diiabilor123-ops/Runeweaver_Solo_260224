using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TeleportSlash", menuName = "Boss/Patterns/TeleportSlash")]
public class TeleportSlashPattern : BossPattern
{
    public float teleportDist = 2.5f;

    public override IEnumerator Execute(BossBrain brain)
    {
        brain.mover.SetAgentActive(false);

        // 1. 유저 곁으로 순간이동
        Vector3 targetPos = brain.GetValidNavMeshPos(brain.GetBehindPlayerPos(teleportDist));
        yield return brain.StartCoroutine(brain.Teleport.TeleportRoutine(targetPos, 0.4f));

        // 2. 타격
        brain.FaceTarget(brain.player.position, 100f);
        brain.ToggleSword(true);
        brain.anim.SetTrigger("Boss_Attack_Slash");

        yield return new WaitForSeconds(0.8f);
        brain.ToggleSword(false);

        // 3. 후속 연계 분기
        float dist = Vector3.Distance(brain.transform.position, brain.player.position);
        if (dist < 3.0f)
        {
            // 근처에 유저가 있으면 쫓아내기 위해 반월 베기
            yield return brain.StartCoroutine(brain.ExecuteCircularSlash());
        }
        else
        {
            // 유저가 멀면 0.5초간 제자리 주시 (하데스 느낌)
            yield return brain.StartCoroutine(brain.LookAtPlayerRoutine(0.5f));
        }
    }
}