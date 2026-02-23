using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BulletDataListSO", menuName = "BulletDataListSO/Data/BulletDataList")]
public class BulletDataListSO : ScriptableObject
{
    // 게임에 존재하는 모든 투사체 SO를 여기에 한 번만 등록합니다.
    public List<BulletDataSO> bulletDatas;

    public BulletDataSO GetBulletData(string id)
    {
        return bulletDatas.Find(x => x.name == id);
    }
}