using UnityEngine;
using System.Collections;
using Runeweaver; // 네임스페이스 확인

[CreateAssetMenu(fileName = "TeleportTrackSlam", menuName = "Boss/Patterns/TeleportTrackSlam")]
public class TeleportSlamPattern : BossPattern
{
    public float leapDistance = 4.5f;
    public GameObject smashVFX;
    public float slamDamageRadius = 3.0f; // 광역 피해 범위 추가

    public void OnSlamImpact(BossBrain brain)
    {
        Vector3 impactPoint = brain.transform.position;
        EnemySword sword = brain.GetComponentInChildren<EnemySword>();

        if (sword != null && sword.SlamImpactAnchor != null)
        {
            impactPoint = sword.SlamImpactAnchor.position;
        }

        // 1. 비주얼 생성
        if (smashVFX != null) Instantiate(smashVFX, impactPoint, Quaternion.identity);

        // 2. 광역 데미지 판정
        HandleSlamAOE(brain, impactPoint);

        // 3. (선택사항) 여기서 카메라 흔들기 등 추가 가능
    }

    public override IEnumerator Execute(BossBrain brain)
    {
        brain.mover.SetAgentActive(false);
        float distBefore = Vector3.Distance(brain.transform.position, brain.player.position);

        // 1. 전조 로직 (기존과 동일)
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

        // 2. 텔레포트
        Vector3 spawnPos = brain.GetValidNavMeshPos(brain.player.position + brain.player.forward * leapDistance);
        yield return brain.StartCoroutine(brain.Teleport.TeleportRoutine(spawnPos, 0.3f));

        brain.FaceTarget(brain.player.position, 100f);
        brain.anim.applyRootMotion = true;

        // 이제 애니메이션 이벤트(AE_StartSlamSlash)가 이펙트를 켤 것입니다.
        brain.anim.SetTrigger("Boss_Attack_Slam");

        yield return new WaitForSeconds(3f); // 애니메이션 수행 완료 대기

        // 위치 보정 및 리커버리 로직 (기존과 동일)
        Vector3 landingPos = brain.transform.position;
        brain.transform.position = brain.GetValidNavMeshPos(landingPos);
        brain.anim.applyRootMotion = false;
        brain.mover.Teleport(brain.transform.position);
        brain.anim.SetBool("IsTired", true);

        // 후속 연계 로직...
        float distAfter = Vector3.Distance(brain.transform.position, brain.player.position);
        if (distAfter < 3.5f)
        {
            brain.anim.SetBool("IsTired", false);
            yield return brain.StartCoroutine(brain.ExecuteCircularSlash());
        }
        else
        {
            float recoveryTimer = 0;
            while (recoveryTimer < 0.8f)
            {
                recoveryTimer += Time.deltaTime;
                brain.FaceTarget(brain.player.position, 15f);
                yield return null;
            }
            brain.anim.SetBool("IsTired", false);
        }
    }

    private void HandleSlamAOE(BossBrain brain, Vector3 center)
    {
        // 보스 주변의 모든 Collider를 찾음
        Collider[] hitColliders = Physics.OverlapSphere(center, 3.0f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player") && hitCollider.TryGetComponent<IDamageable>(out var target))
            {
                HitData hit = new HitData
                {
                    damage = brain.data.attackDamage * 1.5f,
                    element = brain.data.mainElement,
                    attackerTeam = Team.Enemy,
                    hitPoint = hitCollider.ClosestPoint(center),
                    attackerPos = center // 공격의 중심을 충격 지점으로 설정
                };
                target.TakeDamage(hit);
            }
        }
    }
}