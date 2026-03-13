using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;

[System.Serializable]
public class BossSkillGroup
{
    public string elementName;          // 원소 이름 (메테오, 번개 등)
    public GameObject phaseBreakPrefab; // [중요] 페이즈 전환 시 타일 파괴용 프리팹
    public GameObject autoAttackPrefab; // [중요] 평소 플레이어 조준 공격용 프리팹
    public float indicatorDuration = 2f;
}

public class BossPatternManager : MonoBehaviour
{
    public TileManager tileManager;
    public Transform player;
    public Animator anim; // [추가] 보스 애니메이터 연결

    [Header("Skill Pool")]
    public List<BossSkillGroup> skillPool; // 3가지 원소 스킬 세트
    private List<int> _availableIndices = new List<int> { 0, 1, 2 }; // 아직 안 쓴 스킬들
    private List<int> _unlockedIndices = new List<int>();            // 해금된 자동 공격 스킬들

    private bool _isPhaseRoutineRunning = false;
    private Coroutine _autoAttackCoroutine;

    public void StartPhaseSequence(int phaseIndex, int tileCount)
    {
        StartCoroutine(PhaseSequence(phaseIndex, tileCount));
    }

    private IEnumerator PhaseSequence(int phaseIndex, int tileCount)
    {
        _isPhaseRoutineRunning = true;

        BossBrain brain = GetComponent<BossBrain>();
        if (brain != null) brain.PauseAI();

        // 1. 중앙 이동 (Y값 0.5 고정) 및 플레이어 밀어내기
        Vector3 targetPos = new Vector3(0, 0.5f, 0); // 목적지 좌표 고정
        float moveDuration = 1.2f;

        transform.DOMove(targetPos, moveDuration).OnUpdate(() => {
            // 이동 중 보스와 플레이어 사이의 거리가 너무 가까우면 플레이어를 밀어냄
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < 1.8f) // 밀어낼 반경
            {
                Vector3 pushDir = (player.position - transform.position).normalized;
                if (pushDir == Vector3.zero) pushDir = Vector3.right; // 겹쳤을 때 예외처리

                // 플레이어의 좌표를 바깥쪽으로 이동시킴
                player.position += pushDir * Time.deltaTime * 5f;
            }
        });

        yield return new WaitForSeconds(moveDuration);

        // 2. 애니메이션 포즈 고정
        if (anim != null)
        {
            anim.SetBool("IsCasting", true);
            float stopTime = 0.5f;
            yield return new WaitForSeconds(stopTime);
            anim.speed = 0; // 기 모으는 느낌으로 정지
        }

        transform.DOShakePosition(1.5f, 0.3f);
        yield return new WaitForSeconds(1f);

        // 3. 스킬 선정 및 타일 파괴
        int randomPick = _availableIndices[UnityEngine.Random.Range(0, _availableIndices.Count)];
        _availableIndices.Remove(randomPick);
        _unlockedIndices.Add(randomPick);
        BossSkillGroup pickedSkill = skillPool[randomPick];

        List<int> targetIndices = (tileCount == -1)
            ? tileManager.GetAllActiveTilesExceptCenter()
            : GetTargetIndices(tileCount);

        foreach (int targetIdx in targetIndices)
        {
            // [오브젝트 풀링 적용] SpawnSkill 내부에서 처리
            SpawnSkill(pickedSkill.phaseBreakPrefab, pickedSkill.indicatorDuration, tileManager.tiles[targetIdx].transform.position, () => {
                tileManager.PerformDemolish(targetIdx);
                Camera.main.transform.DOShakePosition(0.4f, 0.6f);
            });
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(pickedSkill.indicatorDuration);

        // 4. 연출 종료 및 복구
        if (anim != null)
        {
            anim.speed = 1;
            anim.SetBool("IsCasting", false);
        }

        if (brain != null) brain.ResumeAI();

        float interval = (phaseIndex == 0) ? 6f : (phaseIndex == 1) ? 4f : 2f;
        _autoAttackCoroutine = StartCoroutine(AutoAttackLoop(interval));

        _isPhaseRoutineRunning = false;
    }

    private List<int> GetTargetIndices(int count)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int idx = tileManager.GetSafeTileToDestroy();
            if (idx != -1) indices.Add(idx);
        }
        return indices;
    }

    private void SpawnSkill(GameObject prefab, float duration, Vector3 position, Action onImpact = null)
    {
        if (prefab == null) return;

        // [수정] Instantiate 대신 PoolManager 사용
        GameObject go = PoolManager.Instance.Get(prefab, position, Quaternion.identity);
        BossPhaseSkillEffect effect = go.GetComponent<BossPhaseSkillEffect>();

        if (effect != null)
            effect.Play(duration, onImpact); // 원본 프리팹 정보를 같이 넘겨야 반납 가능
    }

    private IEnumerator AutoAttackLoop(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            if (_isPhaseRoutineRunning) continue;

            if (_unlockedIndices.Count > 0)
            {
                // 해금된 원소 중 랜덤 선택하여 플레이어에게 발사 (autoAttackPrefab 사용)
                int randomIdx = _unlockedIndices[UnityEngine.Random.Range(0, _unlockedIndices.Count)];
                BossSkillGroup skill = skillPool[randomIdx];

                SpawnSkill(skill.autoAttackPrefab, skill.indicatorDuration, player.position, () => {
                    Debug.Log($"{skill.elementName} 자동 공격 적중!");
                });
            }
        }
    }
}