using UnityEngine;
using System;

public class BossPhaseSkillEffect : MonoBehaviour
{
    [Header("Shader Property")]
    public MeshRenderer indicatorRenderer;
    public string timingPropertyName = "_Timing";

    private Material _mat;
    private float _duration;
    private float _elapsed = 0f;
    private Action _onImpact; // 타격 시점에 실행될 함수 저장소
    private bool _isPlaying = false;

    // 스킬 실행 함수
    public void Play(float duration, Action onImpactCallback = null)
    {
        if (indicatorRenderer != null) _mat = indicatorRenderer.material;
        _duration = duration;
        _onImpact = onImpactCallback;
        _elapsed = 0f;
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);

        if (_mat != null) _mat.SetFloat(timingPropertyName, progress);

        if (progress >= 1f)
        {
            _isPlaying = false;
            // 1. 인디케이터가 다 차면 등록된 콜백(타일 파괴 등) 실행
            _onImpact?.Invoke();

            // 2. 이펙트 오브젝트 삭제 (파티클이 남아야 하니 2~3초 뒤)
            Destroy(gameObject, 3f);
        }
    }
}