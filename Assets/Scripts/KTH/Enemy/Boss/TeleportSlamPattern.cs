using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TeleportTrackSlam", menuName = "Boss/Patterns/TeleportTrackSlam")]
public class TeleportSlamPattern : BossPattern
{
    public float leapDistance = 4.5f;
    public GameObject smashVFX;

    public override IEnumerator Execute(BossBrain brain)
    {
        brain.mover.SetAgentActive(false);
        float distBefore = Vector3.Distance(brain.transform.position, brain.player.position);

        // 1. 전조: 멀면 뒤로 걷기(이때는 유저 주시), 가까우면 제자리 주시
        if (distBefore > 5.0f)
        {
            float t = 0;
            Vector3 startPos = brain.transform.position;
            brain.anim.SetFloat("MoveSpeed", -0.8f);
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                Vector3 retreatDir = (brain.transform.position - brain.player.position).normalized;
                brain.transform.position = Vector3.Lerp(startPos, startPos + retreatDir * 2f, t / 0.6f);
                brain.FaceTarget(brain.player.position, 15f);
                yield return null;
            }
        }
        else
        {
            yield return brain.StartCoroutine(brain.LookAtPlayerRoutine(0.4f));
        }

        // 2. 텔레포트 및 슬램
        Vector3 spawnPos = brain.GetValidNavMeshPos(brain.player.position + brain.player.forward * leapDistance);
        yield return brain.StartCoroutine(brain.Teleport.TeleportRoutine(spawnPos, 0.3f));

        brain.FaceTarget(brain.player.position, 100f);
        brain.anim.applyRootMotion = true;
        brain.ToggleSword(true); // 공중 전진 데미지
        brain.anim.SetTrigger("Boss_Attack_Slam");

        yield return new WaitForSeconds(0.7f); // 착지 시점
        brain.ToggleSword(false);
        if (smashVFX != null) Instantiate(smashVFX, brain.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(1.2f); // 애니메이션 수행 완료 대기

        Vector3 landingPos = brain.transform.position;
        brain.transform.position = brain.GetValidNavMeshPos(landingPos);

        brain.anim.applyRootMotion = false;
        brain.mover.Teleport(brain.transform.position);

        // [추가] 상체 리커버리 액션 시작
        brain.anim.SetBool("IsTired", true);

        // 3. 후속 연계 판정
        float distAfter = Vector3.Distance(brain.transform.position, brain.player.position);
        if (distAfter < 3.5f)
        {
            // 유저가 가까우면 상체 액션 중이라도 즉시 원형 베기 연계
            brain.anim.SetBool("IsTired", false);
            yield return brain.StartCoroutine(brain.ExecuteCircularSlash());
        }
        else
        {
            // 유저가 없으면 0.8초간 상체 액션(IsTired)을 보여주며 주시
            float recoveryTimer = 0;
            while (recoveryTimer < 0.8f)
            {
                recoveryTimer += Time.deltaTime;
                brain.FaceTarget(brain.player.position, 15f); // 제자리에서 유저 조준
                yield return null;
            }
            brain.anim.SetBool("IsTired", false);
        }
    }
}