using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TeleportSlash", menuName = "Boss/Patterns/TeleportSlash")]
public class TeleportSlashPattern : BossPattern
{
    public float walkTime = 1.2f;
    public float strafeDistance = 3.5f;
    public float teleportDist = 2.0f;
    public float waitBeforeSlash = 0.2f;

    public override IEnumerator Execute(BossBrain brain)
    {
        // 0. 초기화 및 내비게이션 주도권 회수
        brain.ResetAttackResult();
        brain.mover.SetAgentActive(false);

        // --- 1단계: 옆으로 걷기 (직접 좌표 이동) ---
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
            // 에이전트 없이 직접 위치를 옮깁니다 (발돋움 방지)
            brain.transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (strafeDir != Vector3.zero)
                brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(strafeDir), Time.deltaTime * 10f);

            yield return null;
        }

        // --- 2단계: 순간이동 전 '애니메이션 리셋' ---
        brain.anim.SetFloat("MoveSpeed", 0f);
        // 블렌드 트리를 강제로 Idle 시점으로 되돌립니다 (0번 레이어의 0프레임)
        brain.anim.Play("Locomotion", 0, 0f);
        yield return new WaitForSeconds(0.05f);

        // --- 3단계: [핵심] 실제 순간이동 실행 ---
        brain.PlayTeleportEffect();
        yield return new WaitForSeconds(0.1f); // 이펙트 연출 대기

        // [복구된 코드] 플레이어 뒤쪽 좌표 계산
        Vector3 rawBehindPos = brain.player.position - (brain.player.forward * teleportDist);
        Vector3 validPos = brain.GetValidNavMeshPos(rawBehindPos);

        // [복구된 코드] 보스의 위치를 실제로 옮깁니다! (이게 없어서 안 움직였던 거예요)
        brain.transform.position = validPos;

        // 플레이어를 바라보게 회전 고정
        Vector3 lookDir = (brain.player.position - validPos).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            brain.transform.rotation = Quaternion.LookRotation(lookDir);

        // 물리 엔진이 새 좌표를 인지할 시간을 줍니다.
        yield return new WaitForFixedUpdate();
        yield return null;

        // --- 4단계: 기습 공격 실행 ---
        yield return new WaitForSeconds(waitBeforeSlash);

        brain.ToggleSword(true);
        brain.anim.SetTrigger("Boss_Attack_Slash");

        yield return new WaitForSeconds(1.2f); // 공격 애니메이션 시간
        brain.ToggleSword(false);

        // --- 5단계: 결과 반응 및 마무리 ---
        yield return new WaitForSeconds(0.5f);
        if (brain.LastAttackHitSuccess) brain.anim.SetTrigger("Gesture_HeadNo");
        else brain.anim.SetTrigger("Gesture_HeadYes");

        yield return new WaitForSeconds(1.0f); // 제스처 감상

        // --- 6단계: 권한 반납 및 에이전트 복구 ---
        brain.anim.SetFloat("MoveSpeed", 0f);
        brain.mover.SetAgentActive(true);
        brain.SetRotationUpdate(true);
    }
}