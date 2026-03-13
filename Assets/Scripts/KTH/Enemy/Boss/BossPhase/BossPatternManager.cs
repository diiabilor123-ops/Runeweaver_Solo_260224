using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;
using UnityEngine.AI; // NavMesh 사용을 위해 추가
using Unity.Cinemachine; // [추가] 시네머신 네임스페이스

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

    [Header("Camera Settings (Orthographic)")]
    public CinemachineCamera vCam;
    // 사용자 요청 수치: [0]75%->7, [1]50%->6, [2]25%->5
    public List<float> phaseOrthoSizes = new List<float> { 7f, 6f, 5f };
    public float defaultOrthoSize = 8f;

    [Header("Skill Pool")]
    public List<BossSkillGroup> skillPool; // 3가지 원소 스킬 세트
    private List<int> _availableIndices = new List<int> { 0, 1, 2 }; // 아직 안 쓴 스킬들
    private List<int> _unlockedIndices = new List<int>();            // 해금된 자동 공격 스킬들

    private bool _isPhaseRoutineRunning = false;
    private Coroutine _autoAttackCoroutine;
    private Vector3 _originalBossScale; // 보스의 원래 스케일을 저장할 변수

    public void StartPhaseSequence(int phaseIndex, int tileCount)
    {
        StartCoroutine(PhaseSequence(phaseIndex, tileCount));
    }

    private void Awake()
    {
        // 시작할 때 보스의 스케일(1.6)을 기억해둡니다.
        _originalBossScale = transform.localScale;
    }

    private IEnumerator PhaseSequence(int phaseIndex, int tileCount)
    {
        _isPhaseRoutineRunning = true;

        // [연출] 페이즈 시작 시 카메라 Orthographic Size 변경 (점점 줌인)
        if (vCam != null && phaseIndex < phaseOrthoSizes.Count)
        {
            float targetSize = phaseOrthoSizes[phaseIndex];
            // Lens.OrthographicSize를 DOTween으로 부드럽게 변경
            DOTween.To(() => vCam.Lens.OrthographicSize, x => vCam.Lens.OrthographicSize = x, targetSize, 1.5f);
        }

        BossBrain brain = GetComponent<BossBrain>();
        // [해결 1] 기존 패턴이 실행 중이라면 끝날 때까지 대기
        if (brain != null)
        {
            while (brain.isPatternRunning) // BossBrain에 public bool isPatternRunning이 있다고 가정
            {
                yield return null;
            }
            brain.PauseAI(); // 패턴이 확실히 끝난 후 AI 정지
        }

        // 텔레포트 중 사라졌더라도 원래 크기인 1.6으로 돌아옵니다.
        transform.localScale = _originalBossScale;

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

                // [해결 2] 플레이어 낙사 방지 밀기
                Vector3 nextPos = player.position + pushDir * Time.deltaTime * 5f;
                NavMeshHit hit;
                // 이동하려는 위치가 NavMesh(길) 위인지 확인
                if (NavMesh.SamplePosition(nextPos, out hit, 0.5f, NavMesh.AllAreas))
                {
                    player.position = hit.position;
                }
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

        transform.DOShakePosition(3.5f, 0.1f);
        yield return new WaitForSeconds(1.0f);

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
            // 타일의 X, Z 좌표는 가져오되, Y값은 보스처럼 0.5f로 설정
            Vector3 spawnPos = tileManager.tiles[targetIdx].transform.position;
            spawnPos.y = 0.5f;

            SpawnSkill(pickedSkill.phaseBreakPrefab, pickedSkill.indicatorDuration, spawnPos, () => {
                tileManager.PerformDemolish(targetIdx);
                Camera.main.transform.DOShakePosition(0.6f, 0.6f);
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

        // 1. Y값을 0.5f로 고정하여 바닥 묻힘 방지
        Vector3 finalPos = new Vector3(position.x, 0.5f, position.z);

        // 2. 오브젝트 풀에서 가져오기
        GameObject go = PoolManager.Instance.Get(prefab, finalPos, Quaternion.identity);

        // [핵심 수정] Vector3.one 대신 프리팹 자체에 설정된 스케일(8배 등)을 그대로 적용
        go.transform.localScale = prefab.transform.localScale;

        go.SetActive(true);

        BossPhaseSkillEffect effect = go.GetComponent<BossPhaseSkillEffect>();
        if (effect != null)
        {
            effect.Play(duration, onImpact);
        }
        else
        {
            Debug.LogWarning($"{prefab.name}에 BossPhaseSkillEffect가 없습니다!");
            onImpact?.Invoke();
        }
    }

    private IEnumerator AutoAttackLoop(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            if (_isPhaseRoutineRunning) continue;

            if (_unlockedIndices.Count > 0)
            {
                int randomIdx = _unlockedIndices[UnityEngine.Random.Range(0, _unlockedIndices.Count)];
                BossSkillGroup skill = skillPool[randomIdx];

                // 플레이어 위치 발사 시에도 Y값은 0.5f로 고정
                Vector3 playerPos = player.position;
                playerPos.y = 0.5f;

                SpawnSkill(skill.autoAttackPrefab, skill.indicatorDuration, playerPos, () => {
                    Debug.Log($"{skill.elementName} 자동 공격 적중!");
                });
            }
        }
    }
}