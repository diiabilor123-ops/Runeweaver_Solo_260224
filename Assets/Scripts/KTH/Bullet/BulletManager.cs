using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;

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

    // [중요] 증강이나 룬 조합 시 호출할 함수
    // 예: ChangeBullet("Bullet_FireArrow")
    public void ChangeBullet(string bulletId)
    {
        BulletDataSO newData = bulletDatabase.GetBulletData(bulletId);

        if (newData != null)
        {
            activeData = newData;
            Debug.Log($"[BulletManager] 화살이 {bulletId}로 교체되었습니다!");
        }
        else
        {
            Debug.LogError($"[BulletManager] {bulletId}라는 이름의 데이터를 찾을 수 없습니다.");
        }
    }
}