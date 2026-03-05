using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BulletDataListSO", menuName = "BulletDataListSO/Data/BulletDataList")]
public class BulletDataListSO : ScriptableObject
{
    // 게임에 존재하는 모든 투사체 SO를 여기에 한 번만 등록합니다.
    public List<BulletDataSO> bulletDatas;

    public BulletDataSO GetBulletData(string id)
    {
        var data = bulletDatas.Find(x => x.bulletID == id);
        if (data == null)
        {
            Debug.LogError($"[BulletDataListSO] ID가 '{id}'인 투사체 데이터를 찾을 수 없습니다! 리스트를 확인하세요.");
        }
        return data;
    }
}