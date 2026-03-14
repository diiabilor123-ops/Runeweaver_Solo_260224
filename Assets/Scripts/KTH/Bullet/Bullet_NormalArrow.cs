using System.Collections.Generic;
using UnityEngine;
using Runeweaver;

/// <summary>
/// 일반 화살의 특수 로직(수명 관리, 관통 시 중복 히트 방지)을 처리합니다.
/// </summary>
public class Bullet_NormalArrow : BulletBase
{
    [Header("Life Time")]
    [SerializeField] private float maxLifeTime = 5f;
    private float lifeTimer;

    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    void OnEnable()
    {
        hitTargets.Clear();
        lifeTimer = 0f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // [수정] Kinematic이 아닐 때만 속도 초기화 (에러 방지)
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }
    }

    void Update()
    {
        if (!IsActive) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifeTime)
        {
            Deactivate();
        }
    }

    // 부모의 OnTriggerEnter에서 호출됨
    protected override bool CanHit(IDamageable target)
    {
        // 이미 맞은 적이면 false 리턴하여 부모 로직 중단
        if (hitTargets.Contains(target)) return false;

        hitTargets.Add(target);
        return true;
    }

    // [중요] 여기에 별도의 OnTriggerEnter를 만들지 않습니다. 부모 것을 사용합니다.
}