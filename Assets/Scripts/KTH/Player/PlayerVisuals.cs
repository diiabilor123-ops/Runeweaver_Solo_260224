using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어의 시각적 효과(피격 번쩍임, 피격 파티클)를 담당합니다.
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private Renderer playerRenderer; // 플레이어 모델의 렌더러
    [SerializeField] private Material hitMaterial;     // 피격 시 깜빡일 머티리얼

    private Material originalMaterial;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (playerRenderer != null)
            originalMaterial = playerRenderer.material;
    }

    /// <summary>
    /// 공격받았을 때 이펙트 프리팹을 생성하고 무적 깜빡임을 시작합니다.
    /// </summary>
    public void PlayHitVisual(HitData hitData)
    {
        // 1. 피격 이펙트 프리팹이 있다면 소환
        if (hitData.hitEffectPrefab != null)
        {
            Instantiate(hitData.hitEffectPrefab, hitData.hitPoint, Quaternion.identity);
        }

        // 2. 피격 머티리얼 깜빡임 시작
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// 무적 상태일 때 지속적으로 깜빡이는 연출 (PlayerHealth에서 호출)
    /// </summary>
    public void StartInvincibleFlash()
    {
        // 넉백 중이거나 피격 즉시가 아닐 때 깜빡이는 로직
    }

    private IEnumerator FlashRoutine()
    {
        if (playerRenderer == null || hitMaterial == null) yield break;

        playerRenderer.material = hitMaterial;
        yield return new WaitForSeconds(0.1f); // 아주 짧게 깜빡임
        playerRenderer.material = originalMaterial;
    }
}