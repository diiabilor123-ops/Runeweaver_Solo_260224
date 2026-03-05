using Runeweaver;
using Runeweaver.Augment;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;

    [Header("Data Database List")]
    [SerializeField] private BulletDataListSO bulletDatabase;

    [Header("Main Arrow Settings")]
    [SerializeField] private string normalArrowId = "Bullet_NormalArrow";
    [SerializeField] private string enhancedArrowId = "Bullet_EnhancedArrow";

    [System.Serializable]
    public struct HomingDataMap // 원소와 파일 ID를 연결하는 구조체
    {
        public ElementType type;
        public string dataId; // 여기에 "Bullet_FireHoming" 등을 적습니다.
    }

    [Header("Homing Mapping (ID 기반)")]
    [SerializeField] private List<HomingDataMap> homingMaps;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- [데이터 추출 핵심 로직] ---

    // 1. 특정 ID로 데이터를 가져오는 함수
    public BulletDataSO GetData(string id) => bulletDatabase.GetBulletData(id);

    // 2. 현재 장착된 기본 화살 데이터 반환
    public BulletDataSO GetCurrentEquippedData() => GetData(normalArrowId);

    // 3. 유도탄 데이터 가져오기 (인스펙터 설정을 따름)
    public BulletDataSO GetHomingData(ElementType type)
    {
        // 리스트에서 해당 원소 타입에 맞는 설정을 찾습니다.
        var map = homingMaps.Find(x => x.type == type);

        if (!string.IsNullOrEmpty(map.dataId))
        {
            var data = GetData(map.dataId);
            if (data != null) return data;
        }

        // 설정이 없으면 기본 화살 반환
        return GetCurrentEquippedData();
    }

    // 4. 메인 화살 프리팹 가져오기
    public GameObject GetMainArrowPrefab()
    {
        var augment = PlayerAugment.Instance.leftClick;
        string id = (augment.GetTotalStackCount() >= 4) ? enhancedArrowId : normalArrowId;

        // [수정] mainEffect.prefab 대신 bulletPrefab을 반환
        return GetData(id)?.bulletPrefab;
    }

    // 5. 유도 화살 프리팹 가져오기
    public GameObject GetHomingPrefab(ElementType type)
    {
        // [수정] GetHomingData를 통해 가져온 SO의 bulletPrefab을 반환
        return GetHomingData(type)?.bulletPrefab;
    }
}