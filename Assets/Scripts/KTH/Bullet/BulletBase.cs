using Runeweaver;
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

        // [중요] 리스트를 그대로 참조하지 않고 새 리스트로 복사하여 독립시킵니다.
        this.AppliedElements = new List<ElementType>(elements);

        // [추가] 만약 이 화살이 특정 원소 유도탄(specificType)이라면 리스트에 추가
        // 이렇게 해야 적중 시 해당 원소 스택을 쌓을 수 있습니다.
        if (specificType != ElementType.None && !AppliedElements.Contains(specificType))
        {
            AppliedElements.Add(specificType);
        }

        // 원소 개수에 따른 특수 효과 플래그 설정
        this.IsExplosive = (AppliedElements.Count >= 6);
        this.IsHoming = (AppliedElements.Count >= 2); // 예: 2단계 유도

        this.IsActive = true;
        gameObject.SetActive(true);



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

        // 1. 적의 상태 관리 스크립트 확인 (EnemyStatus는 새로 만드셔야 합니다)
        if (other.TryGetComponent<EnemyStatus>(out var enemyStatus))
        {
            // 2. 이 화살이 가진 모든 원소 명찰을 적에게 전달하여 스택을 쌓음
            foreach (var element in AppliedElements)
            {
                // 적에게 해당 원소 1스택 추가 (내부적으로 10스택 시 폭발)
                enemyStatus.AddElementStack(element, 1);
            }

            // 3. 데미지 처리 (필요 시 BulletDataSO의 데미지 적용)
            // enemyStatus.TakeDamage(_data.damage);

            // 4. 투사체 소멸 (오브젝트 풀링 반납)
            Deactivate();
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