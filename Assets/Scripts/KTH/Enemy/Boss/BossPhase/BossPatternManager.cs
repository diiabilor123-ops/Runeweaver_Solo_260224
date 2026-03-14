using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;
using UnityEngine.AI;
using Unity.Cinemachine;

// 페이즈 전환, 타일 파괴, 보스 이동 등 '큰 흐름'만 담당하는 매니저
public class BossPatternManager : MonoBehaviour
{
    [Header("Managers")]
    public BossAutoAttackManager autoAttackManager; // [중요] 새로 만든 매니저 연결!
    public TileManager tileManager;
    public Transform player;
    public Animator anim;

    [Header("Global Sounds")]
    public SoundDataSO phaseTransitionSound;
    public SoundDataSO tileDemolishSound; // 타일이 박살날 때만 나는 소리

    [Header("Camera Settings (Orthographic)")]
    public CinemachineCamera vCam;
    public List<float> phaseFOVs = new List<float> { 34f, 32f, 30f };
    public float defaultFOV = 36f;

    [Header("Phase State")]
    private List<int> _availableIndices = new List<int> { 0, 1, 2 };
    private Vector3 _originalBossScale;

    private void Awake()
    {
        _originalBossScale = transform.localScale;
        if (vCam != null) vCam.Lens.FieldOfView = defaultFOV;
    }

    public void StartPhaseSequence(int phaseIndex, int tileCount)
    {
        StartCoroutine(PhaseSequence(phaseIndex, tileCount));
    }

    private IEnumerator PhaseSequence(int phaseIndex, int tileCount)
    {
        // 1. 자동 공격 일시정지
        if (autoAttackManager != null) autoAttackManager.PauseAttack();

        if (phaseTransitionSound != null)
            SoundManager.Instance.Play(phaseTransitionSound, transform.position);

        if (vCam != null && phaseIndex < phaseFOVs.Count)
        {
            float targetFOV = phaseFOVs[phaseIndex];
            DOTween.To(() => vCam.Lens.FieldOfView, x => vCam.Lens.FieldOfView = x, targetFOV, 2.0f).SetEase(Ease.OutSine);
        }

        BossBrain brain = GetComponent<BossBrain>();
        if (brain != null)
        {
            while (brain.isPatternRunning) yield return null;
            brain.PauseAI();
        }

        Vector3 targetPos = new Vector3(0, 0.5f, 0);

        float distToCenter = Vector3.Distance(targetPos, player.position);
        if (distToCenter < 2.0f)
        {
            Vector3 pushDir = (player.position - targetPos).normalized;
            if (pushDir == Vector3.zero) pushDir = Vector3.right;
            Vector3 pushedPos = targetPos + pushDir * 2.5f;
            if (NavMesh.SamplePosition(pushedPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                player.position = hit.position;
        }

        if (brain != null && brain.Teleport != null)
            yield return StartCoroutine(brain.Teleport.TeleportRoutine(targetPos, 1f));
        else
        {
            transform.position = targetPos;
            transform.localScale = _originalBossScale;
        }

        if (anim != null)
        {
            anim.SetBool("IsCasting", true);
            yield return new WaitForSeconds(0.5f);
            anim.speed = 0;
        }

        transform.DOShakePosition(3.5f, 0.1f);
        yield return new WaitForSeconds(1.0f);

        // 2. 스킬 선정 및 해금 요청
        int randomPick = _availableIndices[UnityEngine.Random.Range(0, _availableIndices.Count)];
        _availableIndices.Remove(randomPick);

        BossSkillGroup pickedSkill = null;
        if (autoAttackManager != null)
        {
            autoAttackManager.UnlockSkill(randomPick);
            pickedSkill = autoAttackManager.skillPool[randomPick];
        }

        // 3. 타일 파괴
        List<int> targetIndices = (tileCount == -1) ? tileManager.GetAllActiveTilesExceptCenter() : GetTargetIndices(tileCount);

        foreach (int targetIdx in targetIndices)
        {
            Vector3 spawnPos = tileManager.tiles[targetIdx].transform.position;
            spawnPos.y = 0.5f;

            if (pickedSkill != null)
            {
                SpawnPhaseBreakEffect(pickedSkill, spawnPos, () => {
                    tileManager.PerformDemolish(targetIdx);
                    Camera.main.transform.DOShakePosition(0.6f, 0.6f);

                    // [사운드] 타일이 깨질 때 전용 효과음 재생
                    if (tileDemolishSound != null) SoundManager.Instance.Play(tileDemolishSound, spawnPos);
                });
            }
            yield return new WaitForSeconds(0.2f);
        }

        if (pickedSkill != null) yield return new WaitForSeconds(pickedSkill.indicatorDuration);

        if (anim != null)
        {
            anim.speed = 1;
            anim.SetBool("IsCasting", false);
        }

        if (brain != null) brain.ResumeAI();

        // 4. 페이즈 완료 후 자동 공격 다시 시작
        float interval = (phaseIndex == 0) ? 6f : (phaseIndex == 1) ? 4f : 2f;
        if (autoAttackManager != null) autoAttackManager.ResumeAttack(interval);
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

    // 타일 파괴 전용 이펙트 생성기
    private void SpawnPhaseBreakEffect(BossSkillGroup skill, Vector3 position, Action onImpact = null)
    {
        if (skill.phaseBreakPrefab == null) return;
        Vector3 finalPos = new Vector3(position.x, 0.5f, position.z);

        if (skill.indicatorSound != null) SoundManager.Instance.Play(skill.indicatorSound, finalPos);

        GameObject go = PoolManager.Instance.Get(skill.phaseBreakPrefab, finalPos, Quaternion.identity);
        go.transform.localScale = skill.phaseBreakPrefab.transform.localScale;
        go.SetActive(true);

        BossPhaseSkillEffect effect = go.GetComponent<BossPhaseSkillEffect>();
        if (effect != null)
        {
            effect.Play(skill.indicatorDuration, skill.phaseBreakPrefab, () => {
                onImpact?.Invoke();
                if (skill.impactSound != null) SoundManager.Instance.Play(skill.impactSound, finalPos);
            });
        }
    }
}