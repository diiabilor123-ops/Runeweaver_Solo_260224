using Runeweaver;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 역할: 원소별 고유 궤적(오른쪽/왼쪽 퍼짐, 머리 위 장전) 후 적을 추적하는 시스템
/// 특징: 
/// 1. Fire: 오른쪽 팔 생성 -> 크게 휘어 들어오는 궤적
/// 2. Volt: 왼쪽 팔 생성 -> 날카롭게 파고드는 궤적
/// 3. Ice: 머리 위 생성 -> 뒤로 후퇴하며 장전 연출 후 직격
/// </summary>
public class Bullet_Homing : BulletBase
{
    public enum HomingStyle { Fire, Ice, Volt }
    public HomingStyle style;

    [Header("Basic Movement")]
    public float baseHomingSpeed = 10f;    // 이동 속도
    public float baseRotateSpeed = 18f;    // 회전(선회) 강도
    public float detectRadius = 25f;       // 적 감지 범위
    public float maxLifeTime = 5f;         // 최대 수명

    [Header("Trajectory Settings")]
    public float homingStartDelay = 0.5f;  // [핵심] 이 시간 동안은 궤적을 그리며 밖으로 퍼짐
    public float maxHomingAngle = 110f;    // 유도 가능한 최대 각도 (등 뒤 방지)
    public float groundOffset = 0.5f;      // [지면 박힘 방지] 최소 높이 유지

    private TrailRenderer trail;
    private Rigidbody rb; // 물리 리셋용
    private Transform target;
    private EnemyHealth targetHealth;

    private float lifeTimer = 0f;
    private Vector3 initialDirection;      // 발사 시 정면
    private Vector3 sideOffsetDir;         // 발사 시 우측
    private float sideSign = 1f; // 1이면 오른쪽, -1이면 왼쪽

    // [추가] Setup이 완전히 끝났는지 확인하는 플래그
    private bool isInitialized = false;

    void Awake()
    {
        // 부모(BulletBase) 필드는 이미 있으므로 자식에서 필요한 것만 참조
        trail = GetComponentInChildren<TrailRenderer>();
        rb = GetComponent<Rigidbody>();
    }

    // 오브젝트 풀에서 꺼내질 때 초기화 (OnEnable은 SetActive(true) 즉시 실행됨)
    void OnEnable()
    {
        isInitialized = false; // 꺼내지자마자 초기화 완료 전까지 Update를 막음
        lifeTimer = 0f;
    }

    // [핵심 수정] Setup을 오버라이드하여 데이터 주입 직후 초기화를 수행합니다.
    public override void Setup(BulletDataSO data, Vector3 direction, List<ElementType> elements, SkillSlotType slot, GameObject originPrefab = null)
    {
        // 1. 물리 및 타이머 완전 초기화 (풀링 재사용 핵심)
        if (rb != null)
        {
            rb.isKinematic = true; // 위치 세팅 동안 물리 연산 중지
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. 부모 Setup 호출 (여기서 IsActive = true, SetActive(true) 됨)
        base.Setup(data, direction, elements, slot, originPrefab);

        // 3. 데이터 확립
        initialDirection = direction.normalized;
        if (initialDirection.sqrMagnitude < 0.001f) initialDirection = transform.forward;

        lifeTimer = 0f;
        target = null;
        targetHealth = null;

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        // 3. 방향 데이터 확립 (딱 한 번만 실행)
        initialDirection = direction.normalized;
        if (initialDirection.sqrMagnitude < 0.001f) initialDirection = transform.forward;

        transform.forward = initialDirection;
        sideOffsetDir = Vector3.Cross(Vector3.up, initialDirection).normalized;
        sideSign = (Random.value > 0.5f) ? 1f : -1f;


        // 4. 위치 보정 및 타겟 탐색
        SetupInitialPosition();
        FindNewTarget();

        // [수정] 세팅이 끝난 후 다시 물리 연산 허용 (단, 이동은 transform으로 하므로 kinematic 유지도 방법)
        if (rb != null) rb.isKinematic = false;

        // 4. [핵심] 모든 설정이 끝났음을 알림
        isInitialized = true;
        this.IsActive = true;

        Debug.Log($"[Homing Setup 완료] {gameObject.name} / IsActive: {IsActive}");
    }

    /// <summary>
    /// 캐릭터 발사 포인트 기준 초기 위치 세팅
    /// </summary>
    private void SetupInitialPosition()
    {
        // PoolManager에서 이미 설정해준 위치를 기준점으로 잡습니다.
        Vector3 spawnOrigin = transform.position;
        float randomSide = Random.Range(-0.2f, 0.2f);
        float finalOffset = 1.2f * sideSign;

        Vector3 offset = Vector3.zero;
        switch (style)
        {
            case HomingStyle.Fire:
                offset = (sideOffsetDir * (finalOffset + randomSide)) + (Vector3.up * 0.2f);
                break;
            case HomingStyle.Ice:
                offset = (sideOffsetDir * randomSide) + (initialDirection * -0.3f);
                break;
            case HomingStyle.Volt:
                offset = (sideOffsetDir * (-finalOffset + randomSide)) + (Vector3.up * 0.2f);
                break;
        }

        // 누적되지 않도록 위치를 '재설정' 합니다.
        transform.position = spawnOrigin + offset;
    }

    void Update()
    {
        // [수정] Setup이 끝나기 전이거나 비활성 상태면 로직을 실행하지 않음
        if (!isInitialized || !IsActive) return;

        // 2단계: 핵심 변수 값 확인 (움직이지 않는 직접적 원인)
        // 속도가 0이거나, deltaTime이 이상하거나, 프레임이 멈췄는지 확인
        if (Time.frameCount % 30 == 0) // 너무 많이 찍히지 않게 30프레임마다 출력
        {
            Debug.Log($"[Debug] {style} 화살 체크 - 속도: {baseHomingSpeed}, 수명: {lifeTimer}/{maxLifeTime}, 위치: {transform.position}");
        }

        lifeTimer += Time.deltaTime;

        // [수정] Destroy 대신 Deactivate 호출 (풀 반납)
        if (lifeTimer > maxLifeTime)
        {
            Deactivate();
            return;
        }

        ValidateTarget();
        PreventGroundCollision(); // 지면 박힘 방지 로직 상시 가동

        // 스타일별 비행 처리
        switch (style)
        {
            case HomingStyle.Fire: UpdateFireTrajectory(); break; 
            case HomingStyle.Ice: UpdateIceTrajectory(); break;
            case HomingStyle.Volt: UpdateVoltTrajectory(); break;
        }
    }

    // ---------------------------------------------------------
    // [원소별 궤적 로직]
    // ---------------------------------------------------------

    private void UpdateFireTrajectory()
    {
        // [수정] 중첩된 if문 정리 및 유도 로직 복구
        if (lifeTimer < homingStartDelay)
        {
            float progress = lifeTimer / homingStartDelay;
            Vector3 forwardBackward = Vector3.Lerp(-initialDirection * 1.2f, initialDirection, progress);
            Vector3 sideArc = sideOffsetDir * (3.0f * sideSign) * (1.0f - progress);
            Vector3 upArc = Vector3.up * 0.5f;

            Vector3 moveDir = (forwardBackward + sideArc + upArc).normalized;
            transform.position += moveDir * baseHomingSpeed * Time.deltaTime;

            if (moveDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
        }
        else
        {
            // 실제 유도 구간
            if (target != null)
                RotateTowards(GetTargetCenter(), 1.0f);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(initialDirection), Time.deltaTime * 3f);

            MoveForward(baseHomingSpeed * 1.4f);
        }
    }

    private void UpdateVoltTrajectory()
    {
        // 번개 유도 시작 시점을 약간 늦추거나 부드럽게 연결
        float voltDelay = homingStartDelay * 0.5f;

        if (lifeTimer < voltDelay)
        {
            // [번개: 정면 지향성 추가] initialDirection 비중을 높여 90도 꺾임을 방지
            // 왼쪽 대각선 앞으로 자연스럽게 전진
            Vector3 arcVec = (initialDirection * 1.2f - sideOffsetDir * 0.8f + Vector3.up * 0.3f).normalized;

            transform.position += arcVec * baseHomingSpeed * 1.2f * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(arcVec), Time.deltaTime * 10f);
        }
        else
        {
            // 적이 없을 때도 90도로 꺾여있지 않도록 정면을 유지하게 함
            if (target != null)
            {
                RotateTowards(GetTargetCenter(), 2.0f);
            }
            else
            {
                // 타겟이 없으면 다시 정면 방향으로 서서히 회전 복귀
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(initialDirection), Time.deltaTime * 2f);
            }
            MoveForward(baseHomingSpeed * 1.7f);
        }
    }

    private void UpdateIceTrajectory()
    {
        if (lifeTimer < 0.7f)
        {
            // [장전] 뒤로 살짝 밀리며 회전 (트레일 잠시 끔)
            transform.position -= initialDirection * 1.5f * Time.deltaTime;
            transform.Rotate(Vector3.forward, 500f * Time.deltaTime);
            if (trail != null) trail.emitting = false;
        }
        else
        {
            // [발사] 직선 위주로 빠르게 타격
            if (trail != null) trail.emitting = true;
            if (target != null) RotateTowards(target.position, 1.2f);
            MoveForward(baseHomingSpeed * 1.5f);
        }
    }

    // ---------------------------------------------------------
    // [공용 유도 로직]
    // ---------------------------------------------------------

    private void RotateTowards(Vector3 targetPos, float speedMultiplier)
    {
        Vector3 dir = (targetPos - transform.position).normalized;

        // 발사 시점 정면 기준 각도 제한 (U턴 방지)
        if (Vector3.Angle(initialDirection, dir) > maxHomingAngle) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, baseRotateSpeed * speedMultiplier * Time.deltaTime);
    }

    private void PreventGroundCollision()
    {
        // 바닥에서 일정 높이 이하로 내려가려 하면 강제로 위를 보게 함
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundOffset))
        {
            // 1. 태그가 Ground거나 
            // 2. 혹은 레이어가 Environment(환경)인 경우에만 반응 (태그 에러 방지용 체크 추가 가능)
            if (hit.collider.CompareTag("Ground"))
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(transform.forward + Vector3.up * 0.5f), Time.deltaTime * 10f);
            }
        }
    }

    private Vector3 GetTargetCenter()
    {
        if (target == null) return transform.position + transform.forward;

        // 적의 발 위치가 아닌 중심점(CapsuleCollider 등)을 조준하도록 보정
        if (target.TryGetComponent<Collider>(out var col))
        {
            return col.bounds.center;
        }
        return target.position + Vector3.up * 1.0f; // 콜라이더 없으면 임의로 높임
    }

    private void MoveForward(float speed) => transform.position += transform.forward * speed * Time.deltaTime;

    private void ValidateTarget()
    {
        if (target != null && (!target.gameObject.activeInHierarchy || (targetHealth != null && targetHealth.IsDead)))
        {
            target = null;
            targetHealth = null;
        }

        if (target == null) FindNewTarget();
    }

    private void FindNewTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, detectRadius);
        float minTargetDist = detectRadius;

        foreach (var col in cols)
        {
            if (col.CompareTag("Monster") && col.TryGetComponent<EnemyHealth>(out var h))
            {
                if (h.IsDead) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                Vector3 dir = (col.transform.position - transform.position).normalized;

                // 시야 각도 내에 있는 적만 타겟팅
                if (Vector3.Angle(initialDirection, dir) < maxHomingAngle && dist < minTargetDist)
                {
                    minTargetDist = dist;
                    target = col.transform;
                    targetHealth = h;
                }
            }
        }
    }
}