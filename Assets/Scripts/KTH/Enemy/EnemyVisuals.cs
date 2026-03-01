using UnityEngine;
using System.Collections;

/// <summary>
/// [몬스터 시각 효과 및 연출]
/// Hit Flash(번쩍임), 애니메이션 제어, 파티클 생성을 담당합니다.
/// </summary>
public class EnemyVisuals : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material hitMaterial;
    [SerializeField] private Material warningMaterial; // 돌진 전 기 모으는 용

    [Header("Shield FX (Mesh Based)")]
    [SerializeField] private GameObject shieldMeshFX; // 인스펙터에서 FX 메쉬 오브젝트 할당
    [SerializeField] private float shieldShowDuration = 0.15f; // 표시 시간

    //[수정] 이제 이 값은 "배수"로 작동합니다. (예: 1.2면 원래 크기의 120%)
    [SerializeField] private Vector3 shieldImpactScaleMultiplier = new Vector3(1.2f, 1.2f, 1.2f);

    //[추가] 쉴드 위치 오프셋 (예: Y축으로 1만큼 올림)
    [SerializeField] private Vector3 shieldOffset = new Vector3(0, 1.0f, 0);

    [Header("Particles")]
    [SerializeField] private GameObject shieldParticlePrefab; // [추가] 쉴드용 파티클 프리팹


    private Material originalMaterial;
    private Renderer targetRenderer;
    private Coroutine flashCoroutine;
    private Coroutine shieldCoroutine;
    private Animator anim;

    // [추가] 인스펙터에서 맞춰둔 쉴드의 원래 크기를 저장할 변수
    private Vector3 shieldBaseScale;

    // [추가] 현재 쉴드 상태를 저장하는 변수
    public bool HasShield { get; set; } = false;

    private void Awake()
    {
        targetRenderer = GetComponentInChildren<Renderer>();
        anim = GetComponent<Animator>();

        if (shieldMeshFX != null)
        {
            //[추가] 시작할 때 인스펙터에 설정된 크기를 미리 기억해둡니다.
            shieldBaseScale = shieldMeshFX.transform.localScale;
            shieldMeshFX.SetActive(false);
        }
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
        // 1. 파티클 생성 (자식으로 설정)
        if (shieldParticlePrefab != null)
        {
            GameObject particle = Instantiate(shieldParticlePrefab, transform);

            // 위치 설정
            particle.transform.localPosition = hitPoint + shieldOffset;

            // ⭐ 수정 부분: 생성 직후, 프리팹 원본의 스케일 값을 가져와서 적용
            particle.transform.localScale = shieldParticlePrefab.transform.localScale;
        }

        // 2. 메쉬 FX 애니메이션 실행
        if (shieldMeshFX != null)
        {
            if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldImpactRoutine());
        }
    }

    private IEnumerator ShieldImpactRoutine()
    {
        // 쉴드 메쉬의 위치를 오프셋에 맞게 고정
        shieldMeshFX.transform.localPosition = shieldOffset;

        // [계산] 원래 크기에 인스펙터의 배수를 곱해서 충격 시 크기를 결정합니다.
        Vector3 targetScale = new Vector3(
            shieldBaseScale.x * shieldImpactScaleMultiplier.x,
            shieldBaseScale.y * shieldImpactScaleMultiplier.y,
            shieldBaseScale.z * shieldImpactScaleMultiplier.z
        );

        shieldMeshFX.SetActive(true);

        float elapsed = 0f;

        // 하데스 스타일: targetScale(커진 상태)에서 shieldBaseScale(원래 맞춰둔 크기)로 복구
        while (elapsed < shieldShowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shieldShowDuration;

            // 역방향 Lerp (큰 -> 원래대로)
            shieldMeshFX.transform.localScale = Vector3.Lerp(targetScale, shieldBaseScale, t);

            yield return null;
        }

        // 확실하게 원래 크기로 고정 후 비활성화
        shieldMeshFX.transform.localScale = shieldBaseScale;
        shieldMeshFX.SetActive(false);
    }

    // --- [추가] 피격 애니메이션 트리거 ---
    public void PlayHitAnimation()
    {
        if (anim != null)
        {
            // Animator Controller에서 "Hit" Trigger가 설정되어 있어야 합니다.
            anim.SetTrigger("Hit");
        }
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

    // [추가] 몬스터가 공격했을 때 플레이어에게 피격 이펙트 생성
    public void PlayAttackHitVisual(HitData hitData)
    {
        if (hitData.hitEffectPrefab != null)
        {
            Instantiate(hitData.hitEffectPrefab, hitData.hitPoint, Quaternion.identity);
            Debug.Log("플레이어 피격 이펙트 생성!");
        }
        else
        {
            Debug.LogWarning("피격 이펙트 프리팹이 설정되지 않았습니다.");
        }
    }

}