using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossBrain : EnemyBrain
{
    public enum State { Intro, Chase, Pattern, Die }
    [SerializeField] private State currentState = State.Intro;

    private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");

    [Header("패턴 설정")]
    [SerializeField] private BossPattern introPattern;
    [SerializeField] private List<BossPattern> randomPatterns;
    [SerializeField] private BossPattern circularSlashPattern;

    private List<BossPattern> patternPool = new List<BossPattern>();
    private Animator animator;
    private EnemySword sword;
    private EnemyTeleport teleport;
    private bool isPatternRunning = false;

    public Animator anim => animator;
    public new Transform player => base.player;
    public new EnemyMover mover => base.mover;
    public EnemyTeleport Teleport => teleport;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        teleport = GetComponent<EnemyTeleport>();
        sword = GetComponentInChildren<EnemySword>();
        if (sword != null) sword.Init(this, data);
    }

    private void Start() => StartCoroutine(FullBossLoop());

    protected override void LogicUpdate() { }

    private IEnumerator FullBossLoop()
    {
        if (introPattern != null)
        {
            currentState = State.Intro;
            yield return StartCoroutine(introPattern.Execute(this));
        }

        while (!health.IsDead)
        {
            if (player == null) yield break;

            yield return StartCoroutine(ExecuteNextPattern());
            yield return StartCoroutine(StrategicEngagementRoutine());
        }
    }

    private IEnumerator ExecuteNextPattern()
    {
        isPatternRunning = true;
        currentState = State.Pattern;

        if (patternPool.Count == 0) patternPool.AddRange(randomPatterns);
        int randomIndex = Random.Range(0, patternPool.Count);
        BossPattern selectedPattern = patternPool[randomIndex];
        patternPool.RemoveAt(randomIndex);

        yield return StartCoroutine(selectedPattern.Execute(this));
        isPatternRunning = false;
    }

    private IEnumerator StrategicEngagementRoutine()
    {
        currentState = State.Chase;
        float duration = Random.Range(0.8f, 1.2f);
        float timer = 0;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < 4.0f) // 가까우면 제자리 주시
        {
            anim.SetFloat(HashMoveSpeed, 0f);
            mover.SetAgentActive(false);
            while (timer < duration)
            {
                timer += Time.deltaTime;
                FaceTarget(player.position, 15f); // 제자리에선 유저 주시
                yield return null;
            }
        }
        else // 멀면 이동 방향을 바라보며 앞으로 걷기
        {
            mover.SetAgentActive(true);
            anim.SetFloat(HashMoveSpeed, 0.4f);
            while (timer < duration)
            {
                timer += Time.deltaTime;
                Vector3 targetDir = (player.position - transform.position).normalized;
                Vector3 sideDir = Vector3.Cross(Vector3.up, targetDir);
                Vector3 dest = player.position - targetDir * 4f + sideDir * 1.5f;
                mover.MoveTo(dest);

                // [수정] 이동 방향(목적지)을 바라보게 하여 자연스럽게 앞으로 걷게 함
                Vector3 moveDir = (dest - transform.position).normalized;
                if (moveDir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 6f);
                yield return null;
            }
        }
    }

    public IEnumerator LookAtPlayerRoutine(float duration)
    {
        anim.SetFloat(HashMoveSpeed, 0f);
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            FaceTarget(player.position, 20f);
            yield return null;
        }
    }

    public void FaceTarget(Vector3 target, float speed = 5f) // 기본 속도를 낮춰서 묵직하게
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            // Slerp의 수치를 조절하여 '획획' 돌아가는 것을 '스으윽' 돌아가게 변경
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * speed);
        }
    }

    public IEnumerator ExecuteCircularSlash()
    {
        if (circularSlashPattern != null) yield return StartCoroutine(circularSlashPattern.Execute(this));
    }

    public void ToggleSword(bool active) => sword?.ToggleCollider(active);
    public void ReportAttackHit(bool success) { }
    public void ResetAttackResult() { }
    public Vector3 GetBehindPlayerPos(float dist) => player.position - (player.forward * dist);
    public Vector3 GetValidNavMeshPos(Vector3 pos)
    {
        UnityEngine.AI.NavMeshHit hit;
        return UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas) ? hit.position : pos;
    }
}