using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMover : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyData data;

    public void Init(EnemyData data) // 에러 해결: Init 추가
    {
        this.data = data;
        agent = GetComponent<NavMeshAgent>();
        if (agent != null && data != null)
        {
            agent.speed = data.moveSpeed;
            agent.acceleration = 120f;
        }
    }

    public void MoveTo(Vector3 targetPos) // 에러 해결: MoveTo 추가
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.SetDestination(targetPos);
    }

    public void Stop()
    {
        if (agent == null || !agent.enabled) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    public void SetAgentActive(bool active)
    {
        if (agent == null) return;
        agent.enabled = active;
        if (active && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
    }

    public void Teleport(Vector3 targetPos)
    {
        if (agent == null) return;

        // 현재 에이전트가 켜져 있었는지 기억합니다.
        bool wasEnabled = agent.enabled;

        agent.enabled = false;
        transform.position = targetPos;

        // 원래 켜져 있었을 때만 다시 켭니다. 
        // 패턴 중(False 상태)에 호출하면 계속 꺼진 상태를 유지하게 됩니다.
        if (wasEnabled)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(targetPos);
        }
    }

    public void SetRotationUpdate(bool update) // 에러 해결: SetRotationUpdate 추가
    {
        if (agent != null) agent.updateRotation = update;
    }
}