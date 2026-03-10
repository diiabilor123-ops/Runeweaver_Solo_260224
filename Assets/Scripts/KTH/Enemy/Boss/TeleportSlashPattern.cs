using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TeleportSlash", menuName = "Boss/Patterns/TeleportSlash")]
public class TeleportSlashPattern : BossPattern
{
    public float walkTime = 1.2f;
    public float strafeDistance = 3.5f;
    public float teleportDist = 2.5f;
    public float waitBeforeSlash = 0.05f; // 나타나서 휘두르기 전까지의 미세 대기

    [Header("공격 속도 설정")]
    [Range(0.5f, 3.0f)]
    public float attackSpeedMultiplier = 1.8f;

    public override IEnumerator Execute(BossBrain brain)
    {
        brain.ResetAttackResult();
        var afterimage = brain.GetComponent<AfterimageGenerator>();

        // 1. 옆으로 걷기
        brain.mover.SetAgentActive(false);
        yield return brain.StartCoroutine(StrafeRoutine(brain));

        // 2. 목적지 계산 및 예고 잔상
        brain.anim.SetFloat("MoveSpeed", 0f);
        Vector3 targetPos = brain.GetValidNavMeshPos(brain.GetBehindPlayerPos(teleportDist));

        if (afterimage != null)
        {
            // 사라지는 위치 잔상
            afterimage.RecordGhostAt(brain.transform.position, brain.transform.rotation);

            // 나타날 방향 미리 계산
            Vector3 lookDir = (brain.player.position - targetPos).normalized; lookDir.y = 0;
            Quaternion targetRot = lookDir != Vector3.zero ? Quaternion.LookRotation(lookDir) : brain.transform.rotation;

            // 3. 루라바다 스타일 순간이동 (0.5초 예고 대기 포함)
            // 이 루틴 안에서 0.5초 기다린 후 보스가 딱 나타납니다.
            yield return brain.StartCoroutine(brain.Teleport.TeleportRoutine(targetPos, 0.7f));
            brain.transform.rotation = targetRot;
        }

        // --- 4. 등장과 동시에 즉시 타격! ---
        yield return new WaitForSeconds(waitBeforeSlash);

        brain.anim.SetFloat("AttackSpeed", attackSpeedMultiplier);
        brain.ToggleSword(true);
        brain.enemyVisuals.ToggleAfterimage(true); // 공격 휘두를 때 잔상 활성화

        // 공격 애니메이션 실행!
        brain.anim.SetTrigger("Boss_Attack_Slash");
        FeedbackManager.Instance.ExecuteHitFeedback(0.05f, 1.2f);

        // 공격 전반부 (휘두르는 중)
        float slashDuration = 0.5f / attackSpeedMultiplier;
        yield return new WaitForSeconds(slashDuration);
        brain.enemyVisuals.ToggleAfterimage(false);

        // 공격 후반부 (정지 동작)
        yield return new WaitForSeconds(0.7f / attackSpeedMultiplier);
        brain.ToggleSword(false);
        brain.anim.SetFloat("AttackSpeed", 1.0f);

        // 5. 마무리
        yield return new WaitForSeconds(0.5f);
        brain.mover.SetAgentActive(true);
    }

    private IEnumerator StrafeRoutine(BossBrain brain)
    {
        Vector3 startPos = brain.transform.position;
        Vector3 dirToPlayer = (brain.player.position - startPos).normalized;
        float side = (Random.value > 0.5f) ? 1f : -1f;
        Vector3 strafeDir = (Vector3.Cross(Vector3.up, dirToPlayer) * side).normalized;
        Vector3 targetPos = startPos + strafeDir * strafeDistance;

        brain.anim.SetFloat("MoveSpeed", 0.4f);

        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / walkTime;
            brain.transform.position = Vector3.Lerp(startPos, targetPos, t);
            if (strafeDir != Vector3.zero)
                brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(strafeDir), Time.deltaTime * 10f);
            yield return null;
        }
    }
}