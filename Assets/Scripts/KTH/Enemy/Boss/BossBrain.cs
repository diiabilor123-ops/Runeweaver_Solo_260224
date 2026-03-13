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

    private BossPattern currentActivePattern; // 현재 실행 중인 패턴 저장

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

        currentActivePattern = selectedPattern; // [추가] 현재 패턴 기억
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

    // BossBrain.cs 내부의 ToggleSword 함수

    public void StopAllAttackCollision()
    {
        sword?.AE_DisableAll();
    }

    // [보완] ToggleSword는 이제 '수동 강제 제어'용으로만 남겨둡니다.
    public void ToggleSword(bool active)
    {
        if (sword != null) sword.ToggleCollider(active);
    }

    public void ReportAttackHit(bool success) { }
    public void ResetAttackResult() { }
    public Vector3 GetBehindPlayerPos(float dist) => player.position - (player.forward * dist);
    public Vector3 GetValidNavMeshPos(Vector3 pos)
    {
        UnityEngine.AI.NavMeshHit hit;
        return UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas) ? hit.position : pos;
    }

    #region Animation Event Bridge
    // 애니메이션 창에서 선택할 함수들
    public void AE_StartNormalSlash() => sword?.AE_StartNormalSlash();
    public void AE_StartSlamSlash() => sword?.AE_StartSlamSlash();
    public void AE_EnableCollider() => sword?.AE_EnableCollider();
    public void AE_DisableAll() => sword?.AE_DisableAll();
    public void AE_SlamImpact()
    {
        // 1. 비주얼 및 사운드 (EnemySword 측)
        sword?.AE_SlamImpact();

        // 2. 실제 데미지 판정 (현재 실행 중인 패턴 측)
        if (currentActivePattern is TeleportSlamPattern slamPattern)
        {
            slamPattern.OnSlamImpact(this);
        }
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // 실제 게임에 설정된 slamDamageRadius 값을 시각화
        Gizmos.DrawWireSphere(transform.position, 3.0f);
    }

    // BossBrain.cs 에 아래 내용 추가 및 수정
    public void PauseAI()
    {
        // 1. 현재 실행 중인 모든 코루틴(패턴, 루프 등) 중단
        StopAllCoroutines();
        isPatternRunning = false;

        // 2. 이동 중지 및 상태 초기화
        if (mover != null) mover.SetAgentActive(false);
        anim.SetFloat("MoveSpeed", 0);
        ToggleSword(false); // 칼 콜라이더 끄기
    }

    public void ResumeAI()
    {
        // AI 루프 다시 시작
        StartCoroutine(FullBossLoop());
    }

}