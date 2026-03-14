using UnityEngine;
using System;
using System.Collections;
using UnityEngine.VFX;

public class BossPhaseSkillEffect : MonoBehaviour
{
    [Header("Shader Property")]
    public MeshRenderer indicatorRenderer;
    public string timingPropertyName = "_Timing";

    [Header("Damage Settings")]
    public float damage = 10f;
    public float damageRadius = 2.5f;
    public LayerMask playerLayer;

    [Header("Timing Control")]
    [Tooltip("음수(-)값을 넣으면 인디케이터가 가득 차기 전(더 빠르게) 데미지가 터집니다.")]
    public float impactDelay = 0.0f;

    private Material _mat;
    private float _duration;
    private float _elapsed = 0f;
    private Action _onImpact;
    private bool _isPlaying = false;
    private bool _hasImpactStarted = false; // 타격 시퀀스 중복 방지
    private GameObject _originPrefab;

    private VisualEffect _vfx;
    private ParticleSystem[] _particles;

    private void Awake()
    {
        _vfx = GetComponent<VisualEffect>();
        _particles = GetComponentsInChildren<ParticleSystem>();
        if (indicatorRenderer != null) _mat = indicatorRenderer.material;
    }

    public void Play(float duration, GameObject originPrefab, Action onImpactCallback = null)
    {
        StopAllCoroutines();

        _originPrefab = originPrefab;
        _duration = duration;
        _onImpact = onImpactCallback;
        _elapsed = 0f;
        _isPlaying = true;
        _hasImpactStarted = false;

        ResetVisuals();

        if (_vfx != null)
        {
            _vfx.Reinit();
            _vfx.Play();
        }

        if (_particles != null)
        {
            foreach (var ps in _particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
        }
    }

    private void ResetVisuals()
    {
        if (_mat != null) _mat.SetFloat(timingPropertyName, 0f);
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_isPlaying || _hasImpactStarted) return;

        _elapsed += Time.deltaTime;

        // 쉐이더의 인디케이터 진행률은 원래 duration대로 흐릅니다.
        float progress = Mathf.Clamp01(_elapsed / _duration);
        if (_mat != null) _mat.SetFloat(timingPropertyName, progress);

        // [핵심 수정] 실제 데미지가 터져야 하는 목표 시간 계산
        // impactDelay가 -0.2라면 duration보다 0.2초 일찍 터집니다.
        float targetImpactTime = _duration + impactDelay;

        if (_elapsed >= targetImpactTime)
        {
            _hasImpactStarted = true;

            // 데미지 판정 및 사운드/이펙트 콜백 실행
            TriggerDamage();
            _onImpact?.Invoke();

            // 반납 시퀀스 시작
            StartCoroutine(ReleaseToPool());
        }
    }

    private void TriggerDamage()
    {
        // 범위 내 플레이어 콜라이더 수집
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                HitData data = new HitData();
                data.damage = this.damage;
                data.attackerTeam = Team.Enemy;
                data.attackerPos = transform.position;
                data.hitPoint = hit.ClosestPoint(transform.position);

                target.TakeDamage(data);
            }
        }
    }

    private IEnumerator ReleaseToPool()
    {
        // 타격 후 이펙트 잔상이 보일 수 있도록 넉넉히 대기
        yield return new WaitForSeconds(3.0f);

        _isPlaying = false;

        if (_originPrefab != null)
        {
            PoolManager.Instance.Release(_originPrefab, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}