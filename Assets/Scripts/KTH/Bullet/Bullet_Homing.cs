using UnityEngine;
using Runeweaver;

/// <summary>
/// 역할: 원소별 고유 궤적(오른쪽/왼쪽 퍼짐, 머리 위 장전) 후 적을 추적하는 시스템
/// 특징: 
/// 1. Fire: 오른쪽 팔 생성 -> 크게 휘어 들어오는 궤적
/// 2. Volt: 왼쪽 팔 생성 -> 날카롭게 파고드는 궤적
/// 3. Ice: 머리 위 생성 -> 뒤로 후퇴하며 장전 연출 후 직격
/// </summary>
public class Bullet_Homing : MonoBehaviour
{
    public enum HomingStyle { Fire, Ice, Volt }
    public HomingStyle style;

    [Header("Basic Movement")]
    public float baseHomingSpeed = 10f;    // 이동 속도
    public float baseRotateSpeed = 14f;    // 회전(선회) 강도
    public float detectRadius = 25f;       // 적 감지 범위
    public float maxLifeTime = 5f;         // 최대 수명

    [Header("Trajectory Settings")]
    public float homingStartDelay = 1f;  // [핵심] 이 시간 동안은 궤적을 그리며 밖으로 퍼짐
    public float maxHomingAngle = 110f;    // 유도 가능한 최대 각도 (등 뒤 방지)
    public float groundOffset = 0.5f;      // [지면 박힘 방지] 최소 높이 유지

    private BulletBase bulletBase;
    private TrailRenderer trail;
    private Transform target;
    private EnemyHealth targetHealth;

    private float lifeTimer = 0f;
    private Vector3 initialDirection;      // 발사 시 정면
    private Vector3 sideOffsetDir;         // 발사 시 우측

    void Awake()
    {
        bulletBase = GetComponent<BulletBase>();
        trail = GetComponentInChildren<TrailRenderer>();
    }

    void OnEnable()
    {
        // 초기화
        lifeTimer = 0f;
        target = null;
        targetHealth = null;
        initialDirection = transform.forward;
        sideOffsetDir = transform.right;

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        SetupInitialPosition();
        FindNewTarget();
    }

    /// <summary>
    /// 캐릭터 발사 포인트 기준 초기 위치 세팅
    /// </summary>
    private void SetupInitialPosition()
    {
        // 생성 위치에 줄 랜덤 값 (0.1f ~ 0.3f 정도의 미세한 차이)
        float randomSide = Random.Range(-0.2f, 0.2f);
        float randomUp = Random.Range(-0.1f, 0.2f);
        float randomForward = Random.Range(-0.2f, 0.2f);

        switch (style)
        {
            case HomingStyle.Fire:
                // 오른쪽 팔 근처 + 랜덤성
                transform.position += (sideOffsetDir * (1.2f + randomSide)) + (Vector3.up * (0.2f + randomUp));
                break;

            case HomingStyle.Ice:
                // 머리 위 중앙 (요청하신 대로 Vector3.up 보정 제거 버전)
                // 대신 약간의 앞뒤/좌우 랜덤값만 주어 뭉치지 않게 함
                transform.position += (sideOffsetDir * randomSide) + (initialDirection * (-0.3f + randomForward));
                break;

            case HomingStyle.Volt:
                // 왼쪽 팔 근처 + 랜덤성
                transform.position += (sideOffsetDir * (-1.2f + randomSide)) + (Vector3.up * (0.2f + randomUp));
                break;
        }
    }

    void Update()
    {
        if (bulletBase == null || !bulletBase.IsActive) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer > maxLifeTime) { bulletBase.Deactivate(); return; }

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
        if (lifeTimer < homingStartDelay)
        {
            // [화염: 후방 선회 궤적] 
            // 1. 초기에는 뒤쪽(-initialDirection)과 오른쪽(sideOffset)으로 강하게 힘을 줍니다.
            // 2. 시간이 지날수록(progress) 서서히 앞쪽(initialDirection)으로 힘을 전환합니다.

            float progress = lifeTimer / homingStartDelay;

            // 뒤로 빠졌다가 앞으로 돌아오는 힘 (Lerp로 부드럽게 전환)
            Vector3 forwardBackward = Vector3.Lerp(-initialDirection * 1.2f, initialDirection, progress);
            // 오른쪽으로 크게 도는 힘
            Vector3 sideArc = sideOffsetDir * 3.0f;
            // 위로 살짝 띄움
            Vector3 upArc = Vector3.up * 0.5f;

            Vector3 moveDir = (forwardBackward + sideArc + upArc).normalized;
            float speedBoost = Mathf.Lerp(0.8f, 1.3f, progress);

            transform.position += moveDir * baseHomingSpeed * speedBoost * Time.deltaTime;

            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
            }
        }
        else
        {
            // [수정] 적이 없을 때 정면을 향하도록 보정
            if (target != null)
            {
                RotateTowards(GetTargetCenter(), 1.0f);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(initialDirection), Time.deltaTime * 3f);
            }
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