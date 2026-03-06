using Runeweaver;
using Runeweaver.Player;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체의 핵심 데이터와 공통 충돌 로직을 담당하는 부모 클래스입니다.
/// </summary>
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
        this.Direction = direction;
        this.firedFromSlot = slot;

        if (elements == null) elements = new List<ElementType>();
        this.AppliedElements = new List<ElementType>(elements);

        if (specificType != ElementType.None && !AppliedElements.Contains(specificType))
        {
            AppliedElements.Add(specificType);
        }

        // 2. [가장 중요] 무조건 IsActive를 true로 설정하여 Update 로직이 돌아가게 함
        this.IsActive = true;

        this.IsExplosive = (AppliedElements.Count >= 6);

        // [수정 2] 유도 여부 판정 로직 보완
        // 단순히 specificType만 체크하면, 일반 유도 화살 프리팹이 작동하지 않을 수 있습니다.
        // 스크립트 자체가 Bullet_Homing인지 체크하는 것이 더 정확합니다.
        this.IsHoming = (this is Bullet_Homing) || (specificType != ElementType.None);



        // [수정 3] 사운드 매니저 싱글톤 체크 강화
        if (_data != null && _data.shootSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(_data.shootSound, transform.position);
        }

        // [핵심] 상태 활성화 (이게 Update를 돌게 합니다)
        this.IsActive = true;
        gameObject.SetActive(true);

        // [핵심] 비주얼 초기화 시점
        // 반드시 IsActive 설정 후에 호출되어야 이펙트가 부모를 따라 움직입니다.
        var visuals = GetComponent<EffectVisuals>();
        if (visuals != null) visuals.InitializeVisuals();
    }

    // [공통 충돌 로직]
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        // 1. 환경 충돌
        if (other.CompareTag("Wall"))
        {
            Deactivate();
            return;
        }

        // 2. 적 충돌
        if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            // 자식 클래스에서 중복 히트 등을 체크 (기본값 true)
            if (!CanHit(enemyHealth)) return;

            // 비주얼 효과
            GetComponent<EffectVisuals>()?.PlayHitVisual(transform.position);

            // 데미지 계산
            float finalBaseDamage = Data.damage * Data.damageMultiplier;
            DamageResult damageResult = DamageCalculator.Calculate(
                finalBaseDamage,
                AppliedElements,
                Team.Player,
                enemyHealth.enemyData
            );

            // 데이터 보따리 구성
            HitData hitData = new HitData
            {
                damage = damageResult.finalDamage,
                isCritical = damageResult.isCritical,
                attackerTeam = Team.Player,
                hitPoint = transform.position,
                attackerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position,
                attackElement = AppliedElements.Count > 0 ? AppliedElements[0] : ElementType.None,
                element = ConvertToMonsterElement(AppliedElements.Count > 0 ? AppliedElements[0] : ElementType.None),
                hitEffectPrefab = (Data.hitEffect != null && Data.hitEffect.hitEffectPrefabs.Length > 0)
                                  ? Data.hitEffect.hitEffectPrefabs[0] : null
            };

            enemyHealth.TakeDamage(hitData);

            // 원소 스택 적립
            if (other.TryGetComponent<EnemyStatus>(out var enemyStatus))
            {
                foreach (var element in AppliedElements)
                {
                    enemyStatus.AddElementStack(element, 1);
                }
            }

            // 관통 여부 처리
            if (!Data.isPenetrating) Deactivate();
        }
    }

    // 자식(NormalArrow)에서 오버라이드하여 중복 타격 방지 로직을 넣을 수 있음
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
        {
            PoolManager.Instance.Release(_originPrefab, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
            if (transform.parent == null) Destroy(gameObject, 0.1f);
        }
    }
}