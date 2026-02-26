using UnityEngine;
using System.Collections;

/// <summary>
/// [불꽃 멧돼지 AI 판단부]
/// 플레이어와의 거리를 체크하여 Trace, Ready, Charge, Attack 상태를 결정합니다.
/// </summary>
public class BoarBrain : EnemyBrain
{
    // 멧돼지의 상태 정의
    private enum State { Idle, Trace, Ready, Charge, Attack, Cooldown }
    [SerializeField] private State currentState = State.Idle;

    private EnemyCombat combat;
    private float stateTimer = 0f;
    private Animator anim;

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponent<Animator>();

        // 전투 컴포넌트 추가 및 초기화
        combat = GetComponent<EnemyCombat>();
        if (combat == null) combat = gameObject.AddComponent<EnemyCombat>();
        combat.Init(data, player);
    }

    protected override void LogicUpdate()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // 공격 중이 아닐 때만 이동 애니메이션 업데이트
        if (anim != null && !combat.IsAttacking && currentState != State.Cooldown)
        {
            float speed = mover.GetComponent<UnityEngine.AI.NavMeshAgent>().velocity.magnitude;
            anim.SetFloat("MoveSpeed", speed);
        }

        switch (currentState)
        {
            case State.Idle:
                if (distance <= data.detectionRange) ChangeState(State.Trace);
                break;

            case State.Trace:
                // [판단 로직]
                // 1. 아주 가까우면 평타
                if (distance <= 2.2f) ChangeState(State.Attack);
                // 2. 적당히 멀면 돌진 준비 (하데스처럼 기습!)
                else if (distance > 5.0f && distance <= data.detectionRange) ChangeState(State.Ready);
                // 3. 그 외엔 추격
                else mover.MoveTo(player.position);
                break;

            case State.Ready:
                // 공격 전조: 플레이어를 바라보며 멈춤
                LookAtPlayer(); // 돌진 전 조준
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) ChangeState(State.Charge);
                break;

            case State.Cooldown:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) ChangeState(State.Idle);
                break;
        }
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState) return; // 같은 상태 중복 방지
        currentState = newState;

        // 상태가 바뀔 때 실행할 1회성 로직들
        switch (newState)
        {
            case State.Ready:
                mover.Stop();
                stateTimer = data.attackWarningTime;
                visuals.PlayHitFlash(); // 기 모으는 연출
                break;

            case State.Charge:
                Vector3 dir = (player.position - transform.position).normalized;
                // Combat에게 돌진 명령 후, 끝나면 Cooldown으로 상태 변경 콜백 전달
                combat.StartCharge(dir, () => ChangeState(State.Cooldown));
                break;

            case State.Attack:
                mover.Stop();
                FaceTarget(player.position); // 즉시 조준
                combat.StartMeleeAttack(() => ChangeState(State.Cooldown));
                break;

            case State.Cooldown:
                if (anim != null) anim.SetFloat("MoveSpeed", 0);
                stateTimer = data.attackCooldown;
                break;
        }
    }


    // 즉시 대상을 바라보게 하는 함수
    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }
}