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

    /// <summary>
    /// [수정] 위치와 회전값을 함께 받아 즉시 설정해주는 Get 함수
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            pools.Add(prefab, new ObjectPool<GameObject>(
                // [수정] 생성 시점에 비활성화 상태로 생성하여 의도치 않은 Awake/Enable 실행 방지
                createFunc: () => {
                    GameObject obj = Instantiate(prefab);
                    obj.SetActive(false);
                    return obj;
                },
                // [수정] 위치를 잡기 전까지는 활성화하지 않음
                actionOnGet: (obj) => { },
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 100
            ));
        }

        GameObject instance = pools[prefab].Get();

        // 1. 위치와 회전을 먼저 세팅 (OnEnable에서 이 위치를 참조할 수 있게)
        instance.transform.SetPositionAndRotation(position, rotation);


        return instance;
    }

    /// <summary>
    /// [기존] 그냥 오브젝트만 가져오는 함수 (필요할 때를 대비해 유지)
    /// </summary>
    public GameObject Get(GameObject prefab)
    {
        return Get(prefab, Vector3.zero, Quaternion.identity);
    }

    // 사용이 끝난 오브젝트를 풀로 반납하는 함수
    public void Release(GameObject prefab, GameObject instance)
    {
        // 이미 비활성화된 경우(중복 반납) 에러 방지
        if (!instance.activeSelf) return;

        if (pools.ContainsKey(prefab))
        {
            pools[prefab].Release(instance);
        }
        else
        {
            // 풀이 없는데 반납 시도가 오면 그냥 파괴 (방어 코드)
            Destroy(instance);
        }
    }
}