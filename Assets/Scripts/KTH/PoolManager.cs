using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;
    private Dictionary<GameObject, IObjectPool<GameObject>> pools = new Dictionary<GameObject, IObjectPool<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            pools.Add(prefab, new ObjectPool<GameObject>(
                createFunc: () => {
                    GameObject obj = Instantiate(prefab);
                    // 초기에는 꺼둔 상태로 생성
                    obj.SetActive(false);
                    return obj;
                },
                // [수정] 꺼낼 때 무조건 활성화
                actionOnGet: (obj) => obj.SetActive(true),
                // [수정] 반납할 때 무조건 비활성화
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 100
            ));
        }

        GameObject instance = pools[prefab].Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public void Release(GameObject prefab, GameObject instance)
    {
        if (pools.ContainsKey(prefab))
        {
            // 이미 풀에 들어있는 상태(비활성)라면 다시 반납하지 않도록 방어 코드 추가
            if (!instance.activeSelf) return;

            pools[prefab].Release(instance);
        }
        else
        {
            Destroy(instance);
        }
    }
}