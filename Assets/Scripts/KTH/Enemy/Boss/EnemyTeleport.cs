using UnityEngine;
using System.Collections;

public class EnemyTeleport : MonoBehaviour
{
    private EnemyMover mover;
    private EnemyVisuals visuals;
    private Vector3 originalScale;

    private void Awake()
    {
        mover = GetComponent<EnemyMover>();
        visuals = GetComponent<EnemyVisuals>();
        originalScale = transform.localScale;
    }

    /// <summary>
    /// 루라바다 스타일: 사라짐 -> 목적지 이동(투명) -> 예고 이펙트(0.5초) -> 등장
    /// </summary>
    public IEnumerator TeleportRoutine(Vector3 targetPos, float telegraphDelay = 0.5f)
    {
        // 1. 현재 위치에서 사라짐 연출
        visuals.PlayTeleportStartVFX();
        visuals.PlayHitFlash();
        transform.localScale = Vector3.zero; // 캐릭터 숨기기

        // AI 끄기 (이동 중 방해 금지)
        if (mover != null) mover.SetAgentActive(false);

        yield return new WaitForSeconds(0.1f); // 아주 짧은 암전 시간

        // 2. [핵심] 목적지로 미리 이동 (보이지 않는 상태)
        // 캐릭터가 안 보이는 상태에서 미리 Warp를 해버려야 나중에 제자리로 돌아가지 않습니다.
        if (mover != null) mover.Teleport(targetPos);

        FacePlayer(); // 보이지 않는 상태에서 플레이어 미리 조준

        // 3. 목적지에서 '나타남 예고' 이펙트 실행
        // 보스는 이미 여기 와있지만 scale이 0이라 안 보이고 이펙트만 먼저 보입니다.
        visuals.PlayTeleportEndVFX();

        // 4. 예고 파티클이 나오는 시간 동안 대기 (루라바다 엇박자 타이밍)
        yield return new WaitForSeconds(telegraphDelay);

        // 5. 캐릭터 등장!
        transform.localScale = originalScale;

        // 등장 시 묵직한 카메라 진동
        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.ExecuteHitFeedback(0f, 0.7f);

        // 여기서 코루틴이 종료됩니다. (이 직후 패턴 스크립트에서 공격 명령을 내림)
    }

    private void FacePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }
}