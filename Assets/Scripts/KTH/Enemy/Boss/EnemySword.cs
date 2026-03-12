using UnityEngine;
using Runeweaver;

public class EnemySword : MonoBehaviour
{
    private BossBrain brain;
    private EnemyData data;
    private Collider swordCollider;
    private bool hasHitThisSwing = false;

    [Header("VFX Anchor 설정")]
    // 보스 자식으로 만든 빈 오브젝트들을 각각 할당하세요.
    [SerializeField] private Transform normalSlashAnchor;
    [SerializeField] private Transform slamSlashAnchor;
    [SerializeField] private Transform slamImpactAnchor;

    [Header("VFX 프리팹")]
    [SerializeField] private GameObject normalSlashPrefab;
    [SerializeField] private GameObject slamSlashPrefab;
    [SerializeField] private GameObject teleportSlashPrefab;
    [SerializeField] private GameObject impactVFXPrefab;

    [Header("사운드 설정")]
    [SerializeField] private SoundDataSO swingSound;
    [SerializeField] private SoundDataSO hitSound;

    public Transform SlamSlashAnchor => slamSlashAnchor;
    public Transform SlamImpactAnchor => slamImpactAnchor; // [이 줄 추가]
    public void Init(BossBrain brain, EnemyData data)
    {
        this.brain = brain;
        this.data = data;
        swordCollider = GetComponent<Collider>();

        if (swordCollider != null)
        {
            swordCollider.isTrigger = true;
            swordCollider.enabled = false;
        }
    }



    public void ToggleCollider(bool active)
    {
        if (active)
        {
            hasHitThisSwing = false;
            if (swordCollider != null) swordCollider.enabled = true;
        }
        else
        {
            AE_DisableAll();
        }
    }

    // --- 애니메이션 이벤트 호출 함수 ---

    public void AE_StartNormalSlash() => SpawnAtAnchor(normalSlashPrefab, normalSlashAnchor);
    public void AE_StartSlamSlash() => SpawnAtAnchor(slamSlashPrefab, slamSlashAnchor);

    public void AE_SlamImpact()
    {
        // 텔레포트 슬래시 전용 프리팹(혹은 슬램 프리팹)을 임팩트 앵커 위치에 소환
        SpawnAtAnchor(teleportSlashPrefab, slamImpactAnchor);
    }

    private void SpawnAtAnchor(GameObject prefab, Transform anchor)
    {
        if (prefab == null || anchor == null) return;

        // [핵심] 사용자가 미리 잡아둔 Anchor의 위치와 회전값을 그대로 사용하여 생성
        // 부모를 지정하지 않아야(null) 하데스식 '잔상' 효과가 납니다.
        GameObject vfx = Instantiate(prefab, anchor.position, anchor.rotation);

        var ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpeed = brain.anim.speed; // 애니메이션 속도 동기화
            ps.Play();
        }

        Destroy(vfx, 2f);

        if (swingSound != null) SoundManager.Instance.Play(swingSound, transform.position);
    }

    public void AE_EnableCollider()
    {
        if (swordCollider != null)
        {
            hasHitThisSwing = false;
            swordCollider.enabled = true;
        }
    }

    public void AE_DisableAll()
    {
        if (swordCollider != null) swordCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitThisSwing || !other.CompareTag("Player")) return;
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            HitData hit = new HitData
            {
                damage = data.attackDamage,
                element = data.mainElement,
                attackerTeam = Team.Enemy,
                hitPoint = other.ClosestPoint(transform.position),
                attackerPos = brain.transform.position
            };
            target.TakeDamage(hit);
            hasHitThisSwing = true;
            brain.ReportAttackHit(true);
            SpawnImpactVFX(hit.hitPoint);
        }
    }

    private void SpawnImpactVFX(Vector3 point)
    {
        if (impactVFXPrefab != null) Destroy(Instantiate(impactVFXPrefab, point, Quaternion.identity), 2f);
        if (hitSound != null) SoundManager.Instance.Play(hitSound, point);
    }

}