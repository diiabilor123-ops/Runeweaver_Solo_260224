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

    [Header("Shield FX Settings")]
    [SerializeField] private float shieldRotationSpeed = 100f; // 회전 속도 추가

    [Header("Particles")]
    [SerializeField] private GameObject shieldParticlePrefab; // [추가] 쉴드용 파티클 프리팹

    [Header("Shield Polish")]
    [SerializeField] private float hitFlashIntensity = 2.0f; // 피격 시 얼마나 밝게 빛날지
    [SerializeField] private Color shieldColor = new Color(1f, 0.9f, 0.4f); // 쉴드 기본 색상


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

    private void Update()
    {
        // 쉴드 메쉬가 켜져 있을 때만 계속 회전시킵니다.
        if (shieldMeshFX != null && shieldMeshFX.activeSelf)
        {
            shieldMeshFX.transform.Rotate(Vector3.up * shieldRotationSpeed * Time.deltaTime);
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
        // 1. 파티클에 타격감을 몰아줍니다.
        if (shieldParticlePrefab != null)
        {
            GameObject particleObj = Instantiate(shieldParticlePrefab, transform);
            particleObj.transform.localPosition = hitPoint + shieldOffset;

            // 핵심: 생성된 객체를 활성화하고 파티클을 명시적으로 재생
            particleObj.SetActive(true);
            // 수정: Transform 스케일은 프리팹 그대로(보통 1,1,1) 둡니다.
            particleObj.transform.localScale = shieldParticlePrefab.transform.localScale;

            // 대신 파티클 시스템의 '시작 크기'에 배율을 곱해줍니다.
            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                // shieldImpactScaleMultiplier.x 값을 배율로 활용
                main.startSizeMultiplier *= shieldImpactScaleMultiplier.x;

                ps.Play(); // 확실하게 재생 시작
            }

            // 일정 시간 후 삭제 (안 하면 맵에 프리팹 복사본이 계속 쌓입니다)
            Destroy(particleObj, ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 1f);
        }

        // 2. 메쉬 FX 애니메이션 (스케일 변화 대신 회전은 Update에서 수행, 여기선 활성화만)
        if (shieldMeshFX != null)
        {
            if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldVisibleRoutine());
        }
    }

    // 스케일 변화 없이 일정 시간 보여주기만 하는 루틴으로 변경
    private IEnumerator ShieldVisibleRoutine()
    {
        if (shieldMeshFX == null) yield break;

        // 1. 초기 설정
        shieldMeshFX.transform.localPosition = shieldOffset;
        shieldMeshFX.SetActive(true);

        // 쉴드 전용 머티리얼 제어 (Emission 활용)
        Renderer shieldRenderer = shieldMeshFX.GetComponent<Renderer>();
        Material shieldMat = shieldRenderer.material;

        float elapsed = 0f;
        // 하데스 스타일: 순간적으로 빠르게 돌았다가 멈춤 (가속도)
        float boostRotation = shieldRotationSpeed * 3f;

        while (elapsed < shieldShowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shieldShowDuration;

            // 연출 팁 1: 회전 속도 감쇠 (점점 느려짐)
            float currentRotation = Mathf.Lerp(boostRotation, shieldRotationSpeed, t);
            shieldMeshFX.transform.Rotate(Vector3.up * currentRotation * Time.deltaTime);

            // 연출 팁 2: 밝기 조절 (피격 순간 반짝였다가 어두워짐)
            // "_EmissionColor"는 표준 HDR 머티리얼에서 빛나는 색상을 조절합니다.
            float intensity = Mathf.Lerp(hitFlashIntensity, 0f, t);
            shieldMat.SetColor("_EmissionColor", shieldColor * intensity);

            // 연출 팁 3: 스케일 미세 변화 (살짝 수축되는 느낌)
            shieldMeshFX.transform.localScale = Vector3.Lerp(shieldBaseScale * 1.1f, shieldBaseScale, t);

            yield return null;
        }

        // 마무리
        shieldMeshFX.SetActive(false);
    }

    // --- [추가] 피격 애니메이션 트리거 ---
    public void PlayHitAnimation()
    {
        if (anim != null)
        {
            // Animator Controller에서 "Hit" Trigger가 설정되어 있어야 합니다.
            //anim.SetTrigger("Hit");
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