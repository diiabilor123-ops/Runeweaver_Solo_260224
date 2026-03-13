using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TileManager : MonoBehaviour
{
    public List<GameObject> tiles; // 0~8번 (4번 중앙)
    public float destroyDelay = 1.5f; // 파괴 전 흔들리는 시간
    public float fallSpeed = 10f;     // 떨어지는 속도

    // 상하좌우 인접 타일 데이터 (연결성 체크용)
    private readonly int[][] neighbors = new int[][]
    {
        new int[] { 1, 3 },       // 0
        new int[] { 0, 2, 4 },    // 1
        new int[] { 1, 5 },       // 2
        new int[] { 0, 4, 6 },    // 3
        new int[] { 1, 3, 5, 7 }, // 4 (중앙)
        new int[] { 2, 4, 8 },    // 5
        new int[] { 3, 7 },       // 6
        new int[] { 4, 6, 8 },    // 7
        new int[] { 5, 7 }        // 8
    };

    // [75%, 50%, 25% 페이즈에서 호출할 함수]
    public void DestroyTilesByPhase(int count)
    {
        StartCoroutine(DestroyTilesRoutine(count));
    }

    private IEnumerator DestroyTilesRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int targetIndex = GetSafeTileToDestroy();
            if (targetIndex != -1)
            {
                // 하나씩 순차적으로 파괴 예고 시작
                StartCoroutine(DestroySequence(targetIndex));
                // 한꺼번에 무너지면 이상하니 약간의 간격을 둡니다.
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    private int GetSafeTileToDestroy()
    {
        List<int> currentActive = new List<int>();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (i != 4 && tiles[i].activeSelf) currentActive.Add(i);
        }

        // 랜덤하게 섞기
        currentActive = currentActive.OrderBy(x => Random.value).ToList();

        foreach (int testIndex in currentActive)
        {
            // 이 타일을 지워도 중앙(4번)에서 모든 남은 타일로 갈 수 있는지 체크
            if (CanPathToCenterAfterDestroy(testIndex))
                return testIndex;
        }

        return currentActive.Count > 0 ? currentActive[0] : -1;
    }

    // [길 찾기 로직] 고립된 타일이 생기지 않도록 체크
    private bool CanPathToCenterAfterDestroy(int indexToDestroy)
    {
        // 1. 가상으로 타일을 꺼봄
        HashSet<int> activeTiles = new HashSet<int>();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].activeSelf && i != indexToDestroy) activeTiles.Add(i);
        }

        // 2. 중앙(4번)에서 시작해서 연결된 모든 타일을 탐색 (BFS)
        Queue<int> queue = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();

        queue.Enqueue(4);
        visited.Add(4);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int neighbor in neighbors[current])
            {
                if (activeTiles.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // 3. 방문한 타일 수와 현재 살아있는 타일 수가 같으면 모든 길이 연결된 것임
        return visited.Count == activeTiles.Count;
    }

    private IEnumerator DestroySequence(int index)
    {
        GameObject tile = tiles[index];
        Vector3 originalPos = tile.transform.position;

        // 1. 경고 단계: 흔들림 (유니티 에러 해결 지점)
        float elapsed = 0;
        while (elapsed < destroyDelay)
        {
            // Random.insideUnitSphere를 사용하여 덜덜 떨리게 함
            tile.transform.position = originalPos + Random.insideUnitSphere * 0.15f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. 파괴 단계: 아래로 추락 (Destruct 에셋 대신 물리 연출)
        float fallElapsed = 0;
        while (fallElapsed < 1.0f) // 1초 동안 추락
        {
            tile.transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
            fallElapsed += Time.deltaTime;
            yield return null;
        }

        // 3. 비활성화
        tile.SetActive(false);
    }
}