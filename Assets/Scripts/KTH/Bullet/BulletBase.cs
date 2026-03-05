using Runeweaver;
using Runeweaver.Player;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


/// <summary>
/// 소제목: 투사체 데이터 식별자 (Identity)
/// 역할: 투사체의 핵심 정보(SO)를 보관하고, 다른 부품들이 이를 참조할 수 있게 합니다.
/// 특징: 스스로 움직이거나 연출을 수행하지 않는 '순수 데이터 전달자'입니다.
/// </summary>
public class BulletBase : MonoBehaviour
{
    [Header("Core Data")]
    [SerializeField] private BulletDataSO _data; // 인스펙터에서 보임
    public BulletDataSO Data => _data;           // 외부에서는 읽기만 가능

    [Header("State")]
    public Vector3 Direction;     // 발사 시 정해진 이동 방향
    public bool IsActive;     // 현재 투사체가 활성화 상태인지 확인 (부품들의 동작 스위치)

    [Header("Augment Info")]
    public SkillSlotType firedFromSlot;

    // [추가/수정] 이 투사체가 유도 화살일 경우 어떤 원소인지 인스펙터에서 지정
    // 예: FireHomingPrefab에는 Fire가 설정되어 있어야 함
    public ElementType specificType = ElementType.None;

    // [수정] 타입을 Runeweaver.ElementType으로 명시
    public List<ElementType> AppliedElements { get; private set; } = new List<ElementType>();
    // [추가] 특정 단계 이상의 효과가 활성화되었는지 체크하는 플래그들
    public bool IsExplosive { get; private set; }
    public bool IsHoming { get; private set; }

    /// <summary>
    /// 외부(Shooter)에서 생성 시 호출하여 데이터를 주입합니다.
    /// 모든 부품 스크립트는 이 함수가 호출된 이후부터 동작을 시작합니다.
    /// </summary>
    public virtual void Setup(BulletDataSO data, Vector3 direction, List<ElementType> elements, SkillSlotType slot)
    {
        this._data = data;
        this.Direction = direction;
        this.firedFromSlot = slot; // 여기서 슬롯 저장

        // [수정] 유도 화살(specificType이 지정됨)인 경우, 해당 원소 1개만 가지도록 처리
        if (specificType != ElementType.None)
        {
            this.AppliedElements = new List<ElementType> { specificType };
        }
        else
        {
            // 일반 화살일 경우에만 전달받은 모든 원소 리스트를 복사
            this.AppliedElements = new List<ElementType>(elements);
        }

        // [참고] IsExplosive나 IsHoming 플래그는 메인 화살의 강화 연출용으로 유지
        this.IsExplosive = (AppliedElements.Count >= 6);
        this.IsHoming = (specificType != ElementType.None); // 유도탄 프리팹이면 true

        this.IsActive = true;
        gameObject.SetActive(true);


        // --- [추가: 비주얼 부착 로직] ---
        if (_data.mainEffect != null && _data.mainEffect.prefab != null)
        {
            // SO에 등록된 비주얼 프리팹을 생성
            GameObject vfx = Instantiate(_data.mainEffect.prefab, transform.position, transform.rotation);

            // 생성된 비주얼을 투사체 본체의 자식으로 설정 (이제 유도탄을 따라다님)
            vfx.transform.SetParent(this.transform);

            // EffectControl이 있다면 초기화
            if (vfx.TryGetComponent<EffectControl>(out var ctrl))
            {
                // [팁] 여기서 원소 색상을 전달하도록 설계했다면 ctrl.Init(_data.mainEffect, color) 호출
                ctrl.Init(_data.mainEffect);
            }
        }

        // [구조적 특징] 여기서 직접 이펙트를 소환하지 않습니다.
        // 이 스크립트를 참조하는 Visuals 스크립트가 데이터 주입을 감지하여 동작합니다.

        // 비주얼 초기화 호출
        GetComponent<EffectVisuals>()?.InitializeVisuals();
    }

    /// <summary>
    /// [핵심 추가] 적 충돌 시 원소 스택 전달 로직
    /// </summary>
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        if (other.CompareTag("Wall"))
        {
            Deactivate();
            return;
        }

        // 1. EnemyHealth 참조 (기존 IDamageable 시스템 활용)
        if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            // [비주얼] 적중 이펙트 재생
            GetComponent<EffectVisuals>()?.PlayHitVisual(transform.position);

            // SO에 설정된 배율(0.25)을 기본 데미지에 곱합니다.
            float finalBaseDamage = Data.damage * Data.damageMultiplier;

            // 데미지 계산기 호출
            DamageResult damageResult = DamageCalculator.Calculate(
                finalBaseDamage,
                AppliedElements,
                Team.Player,
                enemyHealth.enemyData
            );

            // 3. HitData 구성 (EnemyHealth가 사용하는 규격에 맞춤)
            HitData hitData = new HitData
            {
                damage = damageResult.finalDamage,
                isCritical = damageResult.isCritical,
                attackerTeam = Team.Player,
                hitPoint = transform.position,
                attackerPos = PlayerController.Instance.transform.position, // 플레이어 위치
                                                                            // 원소 상성 계산을 위해 첫 번째 원소 혹은 specificType 전달
                attackElement = AppliedElements.Count > 0 ? AppliedElements[0] : ElementType.None,
                // EffectDataSO 구조에 맞게 수정됨
                hitEffectPrefab = (Data.hitEffect != null && Data.hitEffect.hitEffectPrefabs.Length > 0)
                      ? Data.hitEffect.hitEffectPrefabs[0] : null
            };

            // 4. EnemyHealth에게 전달 (여기서 데미지 팝업, 피드백이 다 처리됨!)
            enemyHealth.TakeDamage(hitData);

            // 5. [원소 스택 처리] 별도의 컴포넌트(EnemyStatus)가 있다면 호출
            if (other.TryGetComponent<EnemyStatus>(out var enemyStatus))
            {
                foreach (var element in AppliedElements)
                {
                    enemyStatus.AddElementStack(element, 1);
                }
            }

            // --- [수정: 관통 여부 처리] ---
            // SO의 isPenetrating이 false면 적중 시 즉시 제거 (유도탄은 여기서 사라짐)
            if (!Data.isPenetrating)
            {
                Deactivate();
            }
        }
    }

    /// <summary>
    /// 투사체를 비활성화합니다. (오브젝트 풀링 반납용)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }
}