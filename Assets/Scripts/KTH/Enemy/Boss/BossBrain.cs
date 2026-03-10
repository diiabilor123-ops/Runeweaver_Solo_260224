using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private EnemyTeleport teleport; // [추가] 순간이동 모듈 참조
    private bool isPatternRunning = false;
    private bool lastAttackHitSuccess = false;

    public Animator anim => animator;
    public bool LastAttackHitSuccess => lastAttackHitSuccess;
    public new Transform player => base.player;
    public new EnemyMover mover => base.mover;
    public EnemyTeleport Teleport => teleport; // [추가] 외부에서 접근 가능하도록 프로퍼티 제공

    public EnemyVisuals enemyVisuals => base.visuals;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        teleport = GetComponent<EnemyTeleport>(); // [추가]
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

            if (!isPatternRunning)
            {
                if (distance < 10f && randomPatterns.Count > 0)
                {
                    yield return StartCoroutine(ExecuteNextPattern());
                }
                else
                {
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

        yield return StartCoroutine(selectedPattern.Execute(this));

        mover.Stop();
        animator.SetFloat(HashMoveSpeed, 0f);

        float cooldown = data.attackCooldown > 0 ? data.attackCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);

        animator.CrossFadeInFixedTime("Locomotion", 0.2f, 0, 0f);

        isPatternRunning = false;
        currentState = State.Chase;
    }

    // [위치 계산 헬퍼 함수] 플레이어 뒤쪽 위치 계산
    public Vector3 GetBehindPlayerPos(float dist)
    {
        if (player == null) return transform.position;
        return player.position - (player.forward * dist);
    }

    // 인터페이스 동기화
    public void Warp(Vector3 pos) => mover.Teleport(pos);
    public void SetRotationUpdate(bool update) => mover.SetRotationUpdate(update);

    // BossBrain.cs 내부의 해당 함수만 교체하세요
    public Vector3 GetValidNavMeshPos(Vector3 targetPos)
    {
        UnityEngine.AI.NavMeshHit hit;
        // 처음 잘 되던 3.0f 반경으로 복구
        if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }
        return targetPos;
    }

    public void ReportAttackHit(bool success) => lastAttackHitSuccess = success;
    public void ResetAttackResult() => lastAttackHitSuccess = false;
    public void ToggleSword(bool active) => sword?.ToggleCollider(active);
}