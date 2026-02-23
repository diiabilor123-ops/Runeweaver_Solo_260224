using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 역할: 개별 이펙트 프리팹의 재생, 정지, 파편 소환을 직접 제어합니다.
/// 설계 의도: 일반 파티클과 VFX Graph를 모두 지원하며, 관통 시 독립적 파편 연출을 처리합니다.
/// </summary>
public class EffectControl : MonoBehaviour
{
    private VisualEffect _vfx;              // VFX Graph 컴포넌트 (있을 경우)
    private ParticleSystem[] _allParticles; // 모든 자식 파티클 시스템
    private EffectDataSO _data;             // 해당 이펙트의 설정 데이터

    /// <summary>
    /// 이펙트 시작 전 데이터 및 컴포넌트를 세팅합니다.
    /// </summary>
    public void Init(EffectDataSO effectData)
    {
        _data = effectData;
        _vfx = GetComponent<VisualEffect>();
        _allParticles = GetComponentsInChildren<ParticleSystem>();
    }

    /// <summary>
    /// 발사 연출을 시작합니다.
    /// </summary>
    public void Play()
    {
        if (_vfx != null) _vfx.SendEvent(_data.startEvent);
        foreach (var ps in _allParticles) ps.Play();
    }

    /// <summary>
    /// 적중 시 연출을 처리합니다.
    /// </summary>
    /// <param name="isPenetrating">관통 여부 (false면 화살 소멸)</param>
    /// <param name="hitPosition">적중된 월드 좌표</param>
    public void TriggerHit(bool isPenetrating, Vector3 hitPosition)
    {
        // 1. VFX Graph 적중 이벤트 전송
        if (_vfx != null) _vfx.SendEvent(_data.hitEvent);

        // 2. 다중 파편 소환 (SubExplosion, Stones 등)
        if (_data != null && _data.hitEffectPrefabs != null)
        {
            foreach (GameObject prefab in _data.hitEffectPrefabs)
            {
                if (prefab == null) continue;

                // [핵심] 적중 위치에 독립적인 파편 생성 (부모 설정 안 함 = 그 자리에 남음)
                GameObject hitGo = Instantiate(prefab, hitPosition, Quaternion.identity);

                // 생성된 파편은 일정 시간 뒤 스스로 삭제 (오브젝트 풀링 적용 가능 지점)
                Destroy(hitGo, 1.5f);
            }
        }

        // 3. 관통이 아닌 경우에만 메인 화살(자신)을 일정 시간 뒤 비활성화
        if (!isPenetrating)
        {
            CancelInvoke(nameof(Deactivate));
            Invoke(nameof(Deactivate), 0.3f);
        }
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}