using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 보스 AI 지휘관 클래스: 전체적인 상태 제어와 패턴 실행을 관리합니다.
/// </summary>
public class BossBrain : EnemyBrain
{
    // [ 1. 상태 및 상수 정의 ]
    public enum State { Intro, Chase, Pattern, Die }
    [SerializeField] private State currentState = State.Intro;

    private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");

    // [ 2. 패턴 슬롯 및 데이터 ]
    [Header("패턴 설정")]
    [SerializeField] private BossPattern introPattern;
    [SerializeField] private List<BossPattern> randomPatterns;

    private List<BossPattern> patternPool = new List<BossPattern>();

    // [ 3. 컴포넌트 참조 ]
    private Animator animator;
    private EnemySword sword;
    private bool isPatternRunning = false;
    private bool lastAttackHitSuccess = false;

    public Animator anim => animator;
    public bool LastAttackHitSuccess => lastAttackHitSuccess;
    public new Transform player => base.player;
    public new EnemyMover mover => base.mover;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        sword = GetComponentInChildren<EnemySword>();
        if (sword != null) sword.Init(this, data);
    }

    private void Start()
    {
        StartCoroutine(FullBossLoop());
    }

    protected override void LogicUpdate() { }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    // [ 5. 보스 메인 로직 루프 ]
    private IEnumerator FullBossLoop()
    {
        if (introPattern != null)
        {
            currentState = State.Intro;
            yield return StartCoroutine(introPattern.Execute(this));
        }

        currentState = State.Chase;
        while (!health.IsDead)
        {
            if (player == null) yield break;

            float distance = Vector3.Distance(transform.position, player.position);

            // [핵심 수정] 패턴 실행 중에는 이 블록 자체가 실행되지 않아야 합니다.
            if (!isPatternRunning)
            {
                if (distance < 10f && randomPatterns.Count > 0)
                {
                    // 패턴 실행 명령
                    yield return StartCoroutine(ExecuteNextPattern());
                }
                else
                {
                    // 추격 상태
                    mover.MoveTo(player.position);
                    animator.SetFloat(HashMoveSpeed, 0.5f);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // [ 6. 패턴 실행 관리 ]
    private IEnumerator ExecuteNextPattern()
    {
        isPatternRunning = true;
        currentState = State.Pattern;

        if (patternPool.Count == 0) patternPool.AddRange(randomPatterns);
        int randomIndex = Random.Range(0, patternPool.Count);
        BossPattern selectedPattern = patternPool[randomIndex];
        patternPool.RemoveAt(randomIndex);

        // 1. 실제 패턴 실행
        yield return StartCoroutine(selectedPattern.Execute(this));

        // 2. 패턴이 끝났으니 강제로 정지 상태 유지
        mover.Stop();
        animator.SetFloat(HashMoveSpeed, 0f);

        // 3. 쿨타임 대기 (이 기간 동안은 보스가 가만히 서있음)
        float cooldown = data.attackCooldown > 0 ? data.attackCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);

        // [핵심 추가] 다시 걷기 직전, 블렌드 트리의 게이지를 0으로 리셋하며 부드럽게 전환
        // "Locomotion"은 블렌드 트리가 들어있는 상태의 이름입니다.
        animator.CrossFadeInFixedTime("Locomotion", 0.2f, 0, 0f);

        isPatternRunning = false; // 이제 추격 로직이 돌면서 MoveSpeed를 올립니다.
        currentState = State.Chase;
    }

    // 인터페이스 동기화 (EnemyMover의 함수와 매칭)
    public void Warp(Vector3 pos) => mover.Teleport(pos);

    public void SetRotationUpdate(bool update) => mover.SetRotationUpdate(update);

    public Vector3 GetValidNavMeshPos(Vector3 targetPos)
    {
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }
        return targetPos;
    }

    // [ 7. 패턴 SO를 위한 외부 인터페이스 ]
    public void ReportAttackHit(bool success) => lastAttackHitSuccess = success;
    public void ResetAttackResult() => lastAttackHitSuccess = false;
    public void ToggleSword(bool active) => sword?.ToggleCollider(active);
    public void PlayTeleportEffect() => visuals.PlayHitFlash();
}