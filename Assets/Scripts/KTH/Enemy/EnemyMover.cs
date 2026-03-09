using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMover : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyData data;
    private float originalStoppingDistance;

    public void Init(EnemyData data)
    {
        this.data = data;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;
        originalStoppingDistance = data.attackRange;
        agent.acceleration = 120f; // 더 즉각적인 반응을 위해 상향
        agent.autoBraking = true;
    }

    public void MoveTo(Vector3 targetPos, bool usePrecise = false)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.stoppingDistance = usePrecise ? 0.1f : originalStoppingDistance;
        agent.SetDestination(targetPos);
    }

    public void Stop()
    {
        if (agent == null || !agent.enabled) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    // [수정] Teleport 기능을 더 안전하게 관리
    public void Teleport(Vector3 targetPos)
    {
        if (agent == null) return;

        agent.enabled = false;
        transform.position = targetPos;
        agent.enabled = true;

        // 켜진 직후 즉시 정지 상태로 만들어 이전 경로의 간섭을 차단
        if (agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
            agent.isStopped = true;
        }
    }

    public void SetAgentActive(bool active)
    {
        if (agent == null) return;

        if (active)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                // 이전의 모든 관성과 경로를 지우고 현재 위치를 '진실'로 고정
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.ResetPath();
                agent.Warp(transform.position);
            }
        }
        else
        {
            // 끌 때도 확실히 멈추고 경로를 지움
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
    }

    public void SetRotationUpdate(bool update)
    {
        if (agent != null) agent.updateRotation = update;
    }
}