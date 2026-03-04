using Runeweaver;
using Runeweaver.Augment;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;

    [Header("BulletPrefabs")]
    public GameObject normalArrowPrefab;   // 1~3스택 기본 화살
    public GameObject enhancedArrowPrefab; // 4스택 이상 강화 화살 (색/이펙트 변경됨)
    public GameObject homingArrowPrefab;   // 2스택 이상 시 확률적으로 추가되는 유도 화살

    // 원소별 유도 화살 프리팹 (인스펙터에서 할당)
    public GameObject fireHomingPrefab;
    public GameObject iceHomingPrefab;
    public GameObject voltHomingPrefab;

    [Header("Data Database List")]
    [SerializeField] private BulletDataListSO bulletDatabase; // 모든 SO가 담긴 리스트 에셋

    [Header("Current State")]
    [SerializeField] private BulletDataSO activeData; // 현재 플레이어가 쏘는 화살

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 플레이어가 공격할 때 호출하는 함수
    public BulletDataSO GetCurrentEquippedData() => activeData;



    // 메인 화살 프리팹을 결정해서 주는 함수
    public GameObject GetMainArrowPrefab()
    {
        var augment = PlayerAugment.Instance.leftClick;
        if (augment.IsAnyElementConverted()) return enhancedArrowPrefab;
        return normalArrowPrefab;
    }

    // [에러 해결] 원소타입에 따른 유도 화살 프리팹 반환
    public GameObject GetHomingPrefab(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return fireHomingPrefab;
            case ElementType.Ice: return iceHomingPrefab;
            case ElementType.Volt: return voltHomingPrefab;
            default: return homingArrowPrefab;
        }
    }


}