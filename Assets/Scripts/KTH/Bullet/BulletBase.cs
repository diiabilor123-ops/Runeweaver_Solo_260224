using Runeweaver;
using Runeweaver.Augment;
using Runeweaver.Player;
using System.Collections.Generic;
using UnityEngine;

public class BulletBase : MonoBehaviour
{
    private GameObject _originPrefab;

    [Header("Core Data")]
    [SerializeField] private BulletDataSO _data;
    public BulletDataSO Data => _data;

    [Header("State")]
    public Vector3 Direction;
    public bool IsActive;

    [Header("Augment Info")]
    public SkillSlotType firedFromSlot;
    public ElementType specificType = ElementType.None;
    public List<ElementType> AppliedElements { get; private set; } = new List<ElementType>();

    public bool IsExplosive { get; private set; }
    public bool IsHoming { get; private set; }

    public virtual void Setup(BulletDataSO data, Vector3 direction, List<ElementType> elements, SkillSlotType slot, GameObject originPrefab = null)
    {
        this._originPrefab = originPrefab;
        this._data = data;
        this.Direction = direction.normalized;
        this.firedFromSlot = slot;

        if (elements == null) elements = new List<ElementType>();
        this.AppliedElements = new List<ElementType>(elements);

        if (specificType != ElementType.None && !AppliedElements.Contains(specificType))
        {
            AppliedElements.Add(specificType);
        }

        this.IsActive = true;
        this.IsExplosive = (AppliedElements.Count >= 6);
        this.IsHoming = (this is Bullet_Homing) || (specificType != ElementType.None);

        if (_data != null && _data.shootSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(_data.shootSound, transform.position);
        }

        gameObject.SetActive(true);
        GetComponent<BulletMovement>()?.ResetStartPosition();

        var visuals = GetComponent<EffectVisuals>();
        if (visuals != null) visuals.InitializeVisuals();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        // 벽이나 땅에 닿으면 제거
        if (other.CompareTag("Wall") || other.CompareTag("Ground")) { Deactivate(); return; }

        if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            if (!CanHit(enemyHealth)) return;

            // 1. 기본 데미지 처리
            float finalBaseDamage = Data.damage * Data.damageMultiplier;
            DamageResult damageResult = DamageCalculator.Calculate(finalBaseDamage, AppliedElements, Team.Player, enemyHealth.enemyData);

            HitData hitData = new HitData
            {
                damage = damageResult.finalDamage,
                isCritical = damageResult.isCritical,
                attackerTeam = Team.Player,
                hitPoint = transform.position,
                attackerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position,
                attackElement = specificType,
                element = ConvertToMonsterElement(specificType),
                hitEffectPrefab = Data.hitEffectPrefabs.Length > 0 ? Data.hitEffectPrefabs[0] : null
            };

            enemyHealth.TakeDamage(hitData);

            // 2. 비주얼 및 사운드 (일반 적중 효과만)
            var visuals = GetComponent<EffectVisuals>();
            if (visuals != null) visuals.PlayHitVisual(transform.position);

            if (SoundManager.Instance != null && Data.hitSound != null)
                SoundManager.Instance.Play(Data.hitSound, transform.position);

            // 3. [핵심] 적에게 원소 스택 적립
            if (other.TryGetComponent<EnemyStatus>(out var enemyStatus))
            {
                // 적에게 원소 타입과 스택 1개를 전달
                enemyStatus.AddElementStack(specificType, 1);
            }

            if (!Data.isPenetrating) Deactivate();
        }
    }

    protected virtual bool CanHit(IDamageable target) => true;

    private MonsterElement ConvertToMonsterElement(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return MonsterElement.M_Fire;
            case ElementType.Ice: return MonsterElement.M_Ice;
            case ElementType.Volt: return MonsterElement.M_Volt;
            default: return MonsterElement.M_None;
        }
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        GetComponent<EffectVisuals>()?.ClearVisuals();

        if (_originPrefab != null && PoolManager.Instance != null)
            PoolManager.Instance.Release(_originPrefab, gameObject);
        else
        {
            gameObject.SetActive(false);
            if (transform.parent == null) Destroy(gameObject, 0.1f);
        }
    }
}