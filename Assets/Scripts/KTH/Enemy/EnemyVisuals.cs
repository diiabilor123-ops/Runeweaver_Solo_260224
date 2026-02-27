using UnityEngine;
using System.Collections;

/// <summary>
/// [몬스터 시각 효과 및 연출]
/// Hit Flash(번쩍임), 애니메이션 제어, 파티클 생성을 담당합니다.
/// </summary>
public class EnemyVisuals : MonoBehaviour
{
    [SerializeField] private Material hitMaterial;
    [SerializeField] private Material warningMaterial; // 돌진 전 기 모으는 용
    [SerializeField] private GameObject shieldParticlePrefab; // [추가] 쉴드용 파티클 프리팹

    private Material originalMaterial;
    private Renderer targetRenderer;
    private Coroutine flashCoroutine;

    // [추가] 현재 쉴드 상태를 저장하는 변수
    public bool HasShield { get; set; } = false;

    private void Awake()
    {
        targetRenderer = GetComponentInChildren<Renderer>();

    }

    private void Start()
    {
        if (targetRenderer != null)
        {
            // [해결] Start에서 초기화하여 생성 시점 이슈 방지
            // material을 호출하면 자동으로 인스턴스화됨
            originalMaterial = targetRenderer.material;

            // 확실하게 초기 Emission은 꺼둠
            originalMaterial.SetColor("_EmissionColor", Color.black);
            targetRenderer.material = originalMaterial;
        }
    }

    /// <summary>
    /// 실제 데미지를 받았을 때 호출 (빠른 번쩍임)
    /// </summary>
    public void PlayHitFlash()
    {
        // [수정] 쉴드가 있는 상태면 하얀색 번쩍임을 실행하지 않습니다.
        if (HasShield) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(hitMaterial, 0.02f));
    }

    /// <summary>
    /// 돌진 예고 시 호출 (지속되는 번쩍임)
    /// </summary>
    public void PlayWarningSignal()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        // 예고 시간(data.attackWarningTime) 동안 지속되도록 하거나 고정값 적용
        flashCoroutine = StartCoroutine(FlashRoutine(warningMaterial, 0.5f));
    }

    /// <summary>
    /// [추가] 쉴드 피격 시 호출되는 연출 (파티클 재생 등)
    /// </summary>
    public void PlayShieldEffect(Vector3 hitPoint)
    {
        if (shieldParticlePrefab != null)
        {
            Instantiate(shieldParticlePrefab, hitPoint, Quaternion.identity);
        }
        Debug.Log("쉴드 피격! (파티클 재생)");
    }

    private IEnumerator FlashRoutine(Material targetMat, float duration)
    {
        if (targetRenderer == null || targetMat == null) yield break;

        // [최적화 방식 대신 머티리얼 교체 방식을 사용하여 눈에 보이게 함]
        targetRenderer.material = targetMat;

        // [해결] 너무 짧은 시간은 프레임 문제를 일으킬 수 있으므로 최소 1프레임 이상 대기 권장
        // 하데스 스타일 0.04f 적용
        yield return new WaitForSeconds(duration);

        targetRenderer.material = originalMaterial; // 되돌림
    }

}