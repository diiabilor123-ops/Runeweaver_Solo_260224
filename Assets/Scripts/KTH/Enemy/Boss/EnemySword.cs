using UnityEngine;
using Runeweaver;

public class EnemySword : MonoBehaviour
{
    private BossBrain brain;
    private EnemyData data;
    private Collider swordCollider;
    private bool hasHitThisSwing = false;

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
        if (swordCollider == null) return;
        swordCollider.enabled = active;
        if (active) hasHitThisSwing = false; // 켤 때마다 히트 여부 초기화
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
            brain.ReportAttackHit(true); // 브레인에게 명중 보고
        }
    }
}