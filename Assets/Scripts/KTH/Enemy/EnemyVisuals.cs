using UnityEngine;
using System.Collections;

/// <summary>
/// [몬스터 시각 효과 및 연출]
/// Hit Flash(번쩍임), 애니메이션 제어, 파티클 생성을 담당합니다.
/// </summary>
public class EnemyVisuals : MonoBehaviour
{
    [SerializeField] private Material targetMaterial; // lilToon 적용된 머티리얼
    private Coroutine flashCoroutine;

    private void Awake()
    {
        // 런타임에 머티리얼 복제본 생성 (다른 몬스터에게 영향 안 주게 함)
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null) targetMaterial = rend.material;
    }

    /// <summary>
    /// 하데스 스타일의 순간적인 번쩍임 연출 (Hit Flash)
    /// </summary>
    public void PlayHitFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Emission 강도를 높여 하얗게 만듦
        targetMaterial.SetColor("_EmissionColor", Color.white * 2.5f);
        yield return new WaitForSeconds(0.1f);
        targetMaterial.SetColor("_EmissionColor", Color.black);
    }

    public void PlayDeathEffect()
    {
        // 사망 파티클 Instantiate 로직 예정
    }
}