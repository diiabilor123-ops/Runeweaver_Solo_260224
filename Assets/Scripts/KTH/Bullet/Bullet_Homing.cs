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
    public float baseHomingSpeed = 8f;     // 시작 속도
    public float maxHomingSpeed = 22f;    // 가속 시 최대 속도
    public float accelerationTime = 1.2f; // 최대 속도까지 걸리는 시간
    public float baseRotateSpeed = 25f;    // 회전 강도
    public float detectRadius = 25f;       // 감지 범위
    public float maxLifeTime = 5f;

    [Header("Trajectory & Smart Homing")]
    public float homingStartDelay = 0.5f;  // 최대 궤적 유지 시간
    public float maxHomingAngle = 110f;    // 유도 가능 각도
    public float reHomingAngle = 100f;     // 추격 유지 각도
    public float arrivalDistance = 1.0f;   // 직격 판정 거리 (이기어검 방지)
    public float groundOffset = 0.5f;

    [Header("Adaptive Settings")]
    public float maxArcWidth = 3.0f;       // 멀리 있을 때 궤적 폭
    public float minArcWidth = 0.6f;       // 가까이 있을 때 궤적 폭
    public float minArcDistance = 5.0f;    // 이 거리 이하일 때 '근접 추격' 모드

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
    private float dynamicStartDelay;       // 거리 기반 가변 딜레이
    private float currentArcScale;         // 거리 기반 가변 폭

    private float voltNextSnapTime = 0f;
    private Vector3 voltNoiseOffset;

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

    public override void Setup(BulletDataSO data, Vector3 direction, List<ElementType> elements, SkillSlotType slot, GameObject originPrefab = null)
    {
        base.Setup(data, direction, elements, slot, originPrefab);

        // 3. [속도 조절] SO 데이터를 가져오되, 유도 화살 특유의 '체공 느낌'을 위해 배율을 낮춥니다.
        if (data != null)
            this.baseHomingSpeed = data.speed * 0.25f; // 원래 속도의 절반 정도로 시작 (예: 10f)
        else
            this.baseHomingSpeed = 5f;

        // 4~7. 기존 방향 및 초기화 로직 동일
        this.initialDirection = direction.normalized;
        if (this.initialDirection.sqrMagnitude < 0.001f) this.initialDirection = transform.forward;
        transform.forward = this.initialDirection;
        sideOffsetDir = Vector3.Cross(Vector3.up, initialDirection).normalized;
        sideSign = (Random.value > 0.5f) ? 1f : -1f;

        lifeTimer = 0f;
        target = null;
        targetHealth = null;

        if (trail != null) { trail.Clear(); trail.emitting = true; }
        SetupInitialPosition();
        FindNewTarget();

        this.isInitialized = true;
        this.IsActive = true;
    }

    /// <summary>
    /// 캐릭터 발사 포인트 기준 초기 위치 세팅
    /// </summary>
    private void SetupInitialPosition()
    {
        // PoolManager에서 이미 설정해준 위치를 기준점으로 잡습니다.
        Vector3 spawnOrigin = transform.position;
        float randomSide = Random.Range(-0.2f, 0.2f);
        float finalOffset = 1.0f * sideSign;

        Vector3 offset = Vector3.zero;
        switch (style)
        {
            case HomingStyle.Fire:
                offset = (sideOffsetDir * (finalOffset + randomSide)) + (initialDirection * -0.5f);
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
        float distToTarget = (target != null) ? Vector3.Distance(transform.position, GetTargetCenter()) : 20f;

        // [개선] 적이 초근접(예: 3m 이내) 상태라면 궤적 시간을 거의 0으로 만듦
        float finalMinDelay = (distToTarget < 3.0f) ? 0.05f : 0.15f;
        dynamicStartDelay = Mathf.Clamp(distToTarget / 10f, finalMinDelay, homingStartDelay);

        // [개선] 가까울수록 옆으로 벌어지는 힘(Arc)을 더 극단적으로 줄임
        float arcReduction = (distToTarget < 5.0f) ? 0.3f : 1.0f;
        currentArcScale = Mathf.Clamp(distToTarget / 5f, minArcWidth, maxArcWidth) * arcReduction * 0.5f;

        if (lifeTimer < dynamicStartDelay)
        {
            float progress = lifeTimer / dynamicStartDelay; // 0에서 1로 변함

            // [핵심 1] 뒤로 갔다가 앞으로 오는 반동 (Sin 곡선 활용)
            // -1(뒤)에서 시작해서 1(앞)로 끝나는 느낌
            float recoil = Mathf.Lerp(-1.5f, 1.0f, progress);
            Vector3 depthVec = initialDirection * recoil;

            // [핵심 2] 옆으로 벌어지는 폭 (Sin 곡선으로 중간에 가장 멀리 퍼지게 함)
            float sideAmount = Mathf.Sin(progress * Mathf.PI) * currentArcScale * sideSign;
            Vector3 sideVec = sideOffsetDir * sideAmount;

            // [핵심 3] 위로 솟구치는 높이
            float upAmount = Mathf.Sin(progress * Mathf.PI) * (currentArcScale * 0.1f);
            Vector3 upVec = Vector3.up * upAmount;

            // 모든 힘을 합쳐 현재 이동 방향 결정
            Vector3 moveDir = (initialDirection + sideVec + upVec + depthVec).normalized;

            // 초근접 시 회전 속도를 2배로 올려서 즉시 적을 향하게 함
            float instantTurnSlerp = (distToTarget < 5.0f) ? 25f : 12f;
            transform.position += moveDir * baseHomingSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * instantTurnSlerp);
        }
        else
        {
            // 2. 유도 및 가속 구간 (기존과 동일하되 회전력 보강)
            float accelProgress = (lifeTimer - dynamicStartDelay) / (accelerationTime * 0.7F);
            float currentSpeed = Mathf.Lerp(baseHomingSpeed, maxHomingSpeed, accelProgress);

            if (target != null)
            {
                // [추가] 번개의 개성: 지그재그 노이즈 생성 (0.1초마다 갱신)
                if (lifeTimer > voltNextSnapTime)
                {
                    voltNextSnapTime = lifeTimer + 0.1f;
                    voltNoiseOffset = Random.insideUnitSphere * 0.3f; // 방향에 30% 정도 무작위성 추가
                }

                Vector3 dirToTarget = (GetTargetCenter() - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

                if (angleToTarget < reHomingAngle)
                {
                    // 적이 가까울수록 회전 배율을 높임 (1.4f -> 최대 3.0f)
                    float distanceWeight = Mathf.Clamp(10f / distToTarget, 1.0f, 3.0f);
                    RotateTowards(GetTargetCenter(), 1.4f * distanceWeight);
                }
            }
            MoveForward(currentSpeed);
        }
    }

    private void UpdateVoltTrajectory()
    {
        float distToTarget = (target != null) ? Vector3.Distance(transform.position, GetTargetCenter()) : 20f;

        // 번개는 거리가 가까우면 궤적 구간을 아예 스킵하듯 아주 짧게 설정
        float voltMinDelay = (distToTarget < 4.0f) ? 0.02f : 0.1f;
        float voltDynamicDelay = Mathf.Clamp(distToTarget / 10f, voltMinDelay, homingStartDelay * 0.6f);

        if (lifeTimer < voltDynamicDelay)
        {
            // 초기 궤적 (생략 가능할 정도로 짧음)
            Vector3 arcVec = (initialDirection + sideOffsetDir * -0.5f).normalized;
            transform.position += arcVec * baseHomingSpeed * Time.deltaTime;
        }
        else
        {
            // 유도 구간: 번개는 "지나침"이 발생하려 할 때 더 날카로운 각도로 꺾임
            float accelProgress = (lifeTimer - voltDynamicDelay) / (accelerationTime * 0.7f);
            float currentSpeed = Mathf.Lerp(baseHomingSpeed * 1.3f, maxHomingSpeed * 1.2f, accelProgress);

            if (target != null)
            {
                // 가까우면 회전 속도를 어마어마하게 높여서 '직각'으로 꺾이는 느낌 전달
                float voltTurnWeight = (distToTarget < 5.0f) ? 4.0f : 2.0f;
                RotateTowards(GetTargetCenter(), voltTurnWeight);
            }
            MoveForward(currentSpeed);
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
        // 감지 거리를 약간 더 늘림 (groundOffset * 1.5f)
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundOffset * 1.5f))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                // [개선] 단순히 위를 보는 게 아니라, 지면의 법선(Normal) 방향을 참고하여 
                // 현재 진행 방향을 지면과 평행하게 꺾어버립니다.
                Vector3 reflectiveDir = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;

                // 땅에 가까워질수록 회전력을 극대화 (강제로 머리를 들어올림)
                float liftForce = Mathf.Clamp01(1.0f - (hit.distance / (groundOffset * 1.5f)));

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(reflectiveDir + Vector3.up * 0.2f),
                    Time.deltaTime * 30f * liftForce // 땅에 가까울수록 더 강하게!
                );
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