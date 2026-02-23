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

    /// <summary>
    /// 외부(Shooter)에서 생성 시 호출하여 데이터를 주입합니다.
    /// 모든 부품 스크립트는 이 함수가 호출된 이후부터 동작을 시작합니다.
    /// </summary>
    public virtual void Setup(BulletDataSO data, Vector3 direction)
    {
        this._data = data;
        this.Direction = direction;
        this.IsActive = true;
        gameObject.SetActive(true);

        // [구조적 특징] 여기서 직접 이펙트를 소환하지 않습니다.
        // 이 스크립트를 참조하는 Visuals 스크립트가 데이터 주입을 감지하여 동작합니다.

        // 비주얼 초기화 호출
        GetComponent<EffectVisuals>()?.InitializeVisuals();
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