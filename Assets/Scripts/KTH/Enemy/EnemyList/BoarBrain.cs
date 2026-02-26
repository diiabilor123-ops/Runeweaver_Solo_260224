using UnityEngine;
using System.Collections;

/// <summary>
/// [불꽃 멧돼지 AI]
/// 직선 돌진 공격을 하며 바닥에 불길(원소 장판)을 남깁니다.
/// </summary>
public class BoarBrain : EnemyBrain
{
    // 멧돼지의 상태 정의
    private enum State { Idle, Trace, Ready, Charge, Cooldown }
    [SerializeField] private State currentState = State.Idle;

    private float timer = 0f;
    private Vector3 chargeDirection;

    // 애니메이터 참조 추가
    private Animator anim;

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponent<Animator>(); // 애니메이터 컴포넌트 가져오기
    }

    protected override void LogicUpdate()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // [애니메이션 업데이트] 현재 속도에 맞춰 MoveSpeed 파라미터 전달
        // NavMeshAgent의 현재 속도 크기를 전달합니다.
        if (anim != null)
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
                mover.MoveTo(player.position);
                // 공격 사거리 안에 들어오면 공격 준비
                if (distance <= data.attackRange) ChangeState(State.Ready);
                break;

            case State.Ready:
                // 공격 전조: 플레이어를 바라보며 멈춤
                LookAtPlayer();
                timer -= Time.deltaTime;
                if (timer <= 0) ChangeState(State.Charge);
                break;

            case State.Charge:
                // 돌진 중 (물리적 이동이나 특정 거리만큼 이동 로직 필요)
                // 돌진이 끝나면 쿨다운으로 이동
                break;

            case State.Cooldown:
                timer -= Time.deltaTime;
                if (timer <= 0) ChangeState(State.Idle);
                break;
        }
    }

    private void ChangeState(State newState)
    {
        currentState = newState;

        // 상태가 바뀔 때 실행할 1회성 로직들
        switch (newState)
        {
            case State.Ready:
                mover.Stop();
                timer = data.attackWarningTime;
                visuals.PlayHitFlash(); // 전조 현상으로 살짝 번쩍이게 재활용
                break;

            case State.Charge:
                // [애니메이션 실행] 공격 트리거 발동
                if (anim != null) anim.SetTrigger("AttackTrigger");

                chargeDirection = (player.position - transform.position).normalized;
                // 돌진 코루틴이나 물리 힘 가하기 실행
                StartCoroutine(ChargeRoutine());
                break;

            case State.Cooldown:
                timer = data.attackCooldown;
                break;
        }
    }

    private IEnumerator ChargeRoutine()
    {
        float chargeTime = 0.5f; // 0.5초 동안 돌진
        float chargeSpeed = data.moveSpeed * 3f; // 평소보다 3배 빠름

        float elapsed = 0f;
        while (elapsed < chargeTime)
        {
            // 돌진 방향으로 이동
            transform.Translate(chargeDirection * chargeSpeed * Time.deltaTime, Space.World);

            // [원소 상호작용 지점] 여기서 바닥에 불길 오브젝트를 소환하면 됩니다!
            // CreateFireTrail(); 

            elapsed += Time.deltaTime;
            yield return null;
        }
        ChangeState(State.Cooldown);
    }

    private void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }
}