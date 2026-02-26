using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// [몬스터의 이동 제어]
/// NavMesh를 이용해 경로를 찾고, 부드럽게 회전하는 기능을 담당합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMover : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyData data;

    public void Init(EnemyData data)
    {
        this.data = data;
        agent = GetComponent<NavMeshAgent>();

        // SO 데이터에 기반한 초기 세팅
        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;
    }

    /// <summary>
    /// 특정 목표 지점으로 이동 명령을 내립니다.
    /// </summary>
    public void MoveTo(Vector3 targetPos)
    {
        if (agent.isStopped) agent.isStopped = false;
        agent.SetDestination(targetPos);
    }

    public void Stop()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }
}