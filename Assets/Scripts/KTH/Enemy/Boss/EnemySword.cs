using UnityEngine;
using Runeweaver;

public class EnemySword : MonoBehaviour
{
    private BossBrain brain;
    private EnemyData data;
    private Collider swordCollider;
    private bool hasHitThisSwing = false;

    [Header("VFX 설정")]
    [SerializeField] private GameObject slashVFXObject;
    [SerializeField] private GameObject impactVFXPrefab;

    [Header("사운드 설정")]
    [SerializeField] private SoundDataSO swingSound;
    [SerializeField] private SoundDataSO hitSound;

    private ParticleSystem slashParticle;

    public void Init(BossBrain brain, EnemyData data)
    {
        this.brain = brain;
        this.data = data;
        swordCollider = GetComponent<Collider>();

        if (slashVFXObject != null)
        {
            slashParticle = slashVFXObject.GetComponent<ParticleSystem>();
            slashVFXObject.SetActive(false);
        }

        if (swordCollider != null)
        {
            swordCollider.isTrigger = true;
            swordCollider.enabled = false;
        }
    }

    // --- 애니메이션 이벤트에서 호출할 함수들 ---

    /// <summary>
    /// 검을 휘두르기 시작하는 시점에 호출 (VFX + 사운드)
    /// </summary>
    public void AE_StartSlash()
    {
        if (slashVFXObject != null)
        {
            // [현대적 기법] 애니메이터의 재생 속도에 맞춰 VFX 속도를 동기화
            if (slashParticle != null)
            {
                var main = slashParticle.main;
                // 보스 애니메이션이 1.5배속이면 VFX도 1.5배속으로 재생
                main.simulationSpeed = brain.anim.speed;

                slashVFXObject.SetActive(false);
                slashVFXObject.SetActive(true);
                slashParticle.Play();
            }
        }

        if (swingSound != null)
            SoundManager.Instance.Play(swingSound, transform.position);
    }

    /// <summary>
    /// 실제 공격 판정이 발생하는 시점에 호출 (Collider ON)
    /// </summary>
    public void AE_EnableCollider()
    {
        if (swordCollider != null)
        {
            hasHitThisSwing = false;
            swordCollider.enabled = true;
        }
    }

    /// <summary>
    /// 공격 판정이 끝나는 시점에 호출 (Collider OFF + VFX Stop)
    /// </summary>
    public void AE_DisableCollider()
    {
        if (swordCollider != null) swordCollider.enabled = false;

        if (slashParticle != null) slashParticle.Stop();
    }

    // 기존 패턴 로직과의 호환성을 위해 유지 (필요없으면 삭제 가능)
    public void ToggleCollider(bool active)
    {
        if (active) { AE_StartSlash(); AE_EnableCollider(); }
        else { AE_DisableCollider(); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitThisSwing || !other.CompareTag("Player")) return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);

            HitData hit = new HitData
            {
                damage = data.attackDamage,
                element = data.mainElement,
                attackerTeam = Team.Enemy,
                hitPoint = hitPoint,
                attackerPos = brain.transform.position
            };

            target.TakeDamage(hit);
            hasHitThisSwing = true;
            brain.ReportAttackHit(true);

            SpawnImpactVFX(hitPoint);
        }
    }

    private void SpawnImpactVFX(Vector3 point)
    {
        if (impactVFXPrefab != null)
        {
            GameObject impact = Instantiate(impactVFXPrefab, point, Quaternion.identity);
            Destroy(impact, 2f);
        }

        if (hitSound != null)
            SoundManager.Instance.Play(hitSound, point);
    }
}