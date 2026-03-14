using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BossSkillGroup
{
    public string elementName;
    public GameObject phaseBreakPrefab;
    public GameObject autoAttackPrefab;
    public float indicatorDuration = 2f;

    [Header("Skill Specific Sounds")]
    public SoundDataSO indicatorSound;
    public SoundDataSO impactSound;
}

public class BossAutoAttackManager : MonoBehaviour
{
    public TileManager tileManager;
    public Transform player;

    [Header("Skill Pool")]
    public List<BossSkillGroup> skillPool;

    private List<int> _unlockedIndices = new List<int>();
    private Coroutine _attackCoroutine;
    private bool _isPaused = false;

    [Header("Pattern Settings")]
    [Tooltip("타일 하나의 크기 (현재 10)")]
    public float tileSize = 10.0f;

    [Tooltip("물 줄기가 퍼지는 간격 (큐브 2개 정도면 2.0~2.5 추천)")]
    public float waterSpreadDist = 2.5f;

    [Tooltip("불 메테오가 유저 주변 어디까지 떨어질지")]
    public float fireRadius = 15.0f;

    // --- [핵심] 타일이 밑에 있는지 체크만 하는 함수 ---
    private bool IsPositionOverTile(Vector3 targetPos)
    {
        // 타일 크기의 절반(5.0) 안쪽으로 중심점이 있는지 체크
        float checkThreshold = tileSize * 0.6f;

        foreach (GameObject tile in tileManager.tiles)
        {
            if (tile.activeSelf)
            {
                float dist = Vector2.Distance(new Vector2(targetPos.x, targetPos.z),
                                              new Vector2(tile.transform.position.x, tile.transform.position.z));
                if (dist <= checkThreshold) return true; // 타일 위에 있음!
            }
        }
        return false;
    }

    public void UnlockSkill(int index)
    {
        if (!_unlockedIndices.Contains(index))
            _unlockedIndices.Add(index);
    }

    public void PauseAttack()
    {
        _isPaused = true;
    }

    public void ResumeAttack(float interval)
    {
        _isPaused = false;
        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        _attackCoroutine = StartCoroutine(AutoAttackLoop(interval));
    }

    private IEnumerator AutoAttackLoop(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            if (_isPaused || _unlockedIndices.Count == 0) continue;

            int randomIdx = _unlockedIndices[UnityEngine.Random.Range(0, _unlockedIndices.Count)];
            BossSkillGroup skill = skillPool[randomIdx];

            // 대소문자 실수 방지
            string cmd = skill.elementName.Trim().ToUpper();

            switch (cmd)
            {
                case "FIRE": StartCoroutine(Pattern_Fire(skill)); break;
                case "LIGHTNING": StartCoroutine(Pattern_Lightning(skill)); break;
                case "WATER": StartCoroutine(Pattern_Water(skill)); break;
                default:
                    Vector3 playerPos = player.position; playerPos.y = 0.5f;
                    SpawnSingleEffect(skill, skill.autoAttackPrefab, playerPos);
                    break;
            }
        }
    }

    // ==========================================
    // 개별 스킬 패턴 로직
    // ==========================================

    // 1. 화염: 유저 주변 타일 중 랜덤 5곳 (타일이 없으면 안 나옴)
    // --- 불 패턴 수정 (유저 주변 랜덤 타일 위) ---
    private IEnumerator Pattern_Fire(BossSkillGroup skill)
    {
        int dropCount = 5;
        for (int i = 0; i < dropCount; i++)
        {
            // 유저 주변 fireRadius 안에서 랜덤 좌표 생성
            Vector2 randCircle = UnityEngine.Random.insideUnitCircle * fireRadius;
            Vector3 targetPos = player.position + new Vector3(randCircle.x, 0, randCircle.y);
            targetPos.y = 0.5f;

            // 그 자리에 타일이 있을 때만 생성
            if (IsPositionOverTile(targetPos))
            {
                SpawnSingleEffect(skill, skill.autoAttackPrefab, targetPos);
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.4f));
        }
    }

    // 2. 번개: 기존 유저 추격 방식 유지
    private IEnumerator Pattern_Lightning(BossSkillGroup skill)
    {
        SpawnSingleEffect(skill, skill.autoAttackPrefab, player.position);
        yield return new WaitForSeconds(1.5f);
        SpawnSingleEffect(skill, skill.autoAttackPrefab, player.position);
        yield return new WaitForSeconds(0.5f);
        SpawnSingleEffect(skill, skill.autoAttackPrefab, player.position);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 3; i++)
        {
            SpawnSingleEffect(skill, skill.autoAttackPrefab, player.position);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // --- 물 패턴 수정 (타일 중앙으로 안 옮기고 지정된 거리만큼만 퍼짐) ---
    private IEnumerator Pattern_Water(BossSkillGroup skill)
    {
        bool isPlusPattern = UnityEngine.Random.value > 0.5f;
        Vector3[] dirs = isPlusPattern ?
            new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right } :
            new Vector3[] { new Vector3(1,0,1).normalized, new Vector3(-1,0,1).normalized,
                            new Vector3(1,0,-1).normalized, new Vector3(-1,0,-1).normalized };

        Vector3 center = player.position;
        center.y = 0.5f;

        // 1단계: 중앙
        if (IsPositionOverTile(center)) SpawnSingleEffect(skill, skill.autoAttackPrefab, center);
        yield return new WaitForSeconds(0.4f);

        // 2단계: 1단계 거리만큼 확산
        foreach (var dir in dirs)
        {
            Vector3 spawnPos = center + (dir * waterSpreadDist);
            if (IsPositionOverTile(spawnPos))
                SpawnSingleEffect(skill, skill.autoAttackPrefab, spawnPos);
        }
        yield return new WaitForSeconds(0.4f);

        // 3단계: 2단계 거리만큼 확산 (총 2배 거리)
        foreach (var dir in dirs)
        {
            Vector3 spawnPos = center + (dir * waterSpreadDist * 2f);
            if (IsPositionOverTile(spawnPos))
                SpawnSingleEffect(skill, skill.autoAttackPrefab, spawnPos);
        }
    }

    private void SpawnSingleEffect(BossSkillGroup skill, GameObject prefab, Vector3 position, Action onImpact = null)
    {
        if (prefab == null) return;
        Vector3 finalPos = new Vector3(position.x, 0.5f, position.z);

        if (skill.indicatorSound != null) SoundManager.Instance.Play(skill.indicatorSound, finalPos);

        GameObject go = PoolManager.Instance.Get(prefab, finalPos, Quaternion.identity);
        if (go == null) return;

        go.transform.localScale = prefab.transform.localScale;
        go.SetActive(true);

        BossPhaseSkillEffect effect = go.GetComponent<BossPhaseSkillEffect>();
        if (effect != null)
        {
            effect.Play(skill.indicatorDuration, prefab, () => {
                onImpact?.Invoke();
                if (skill.impactSound != null) SoundManager.Instance.Play(skill.impactSound, finalPos);
            });
        }
    }
}