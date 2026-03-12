using UnityEngine;
using System.Collections;

/// <summary>
/// [공통 몬스터 비주얼] 모든 몬스터의 피격 번쩍임, 애니메이션, 시각 효과를 담당합니다.
/// </summary>
public class EnemyVisuals : MonoBehaviour
{
    [Header("Hit Flash (Shader)")]
    [SerializeField] private float baseRimIntensity = 0.5f;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float enemyhitFlashIntensity = 5f;
    [SerializeField] private float rimPower = 2.0f;
    private static readonly int HitIntensityID = Shader.PropertyToID("_HitIntensity");
    private static readonly int FresnelPowerID = Shader.PropertyToID("_Fresnel_Effect_Power");

    [Header("Hit Shake (Hades Style)")]
    [Tooltip("실제 메쉬가 있는 자식 오브젝트를 할당하세요 (Root 이동 방지)")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private float shakeMagnitude = 0.15f;
    [SerializeField] private float shakeDuration = 0.1f;

    [Header("Teleport VFX")]
    [SerializeField] private GameObject teleportStartVFX; // 사라질 때 프리팹
    [SerializeField] private GameObject teleportEndVFX;   // 나타날 때 프리팹

    private Renderer[] allRenderers;
    private MaterialPropertyBlock propBlock;
    private Coroutine flashCoroutine;
    private Animator anim;
    private AfterimageGenerator afterimage;
    private Coroutine shakeCoroutine; // [추가] 흔들림용 코루틴 변수
    private Vector3 originalModelPos; // [추가] 모델의 원래 로컬 위치 저장용

    // [추가] 쉴드 여부에 따라 피격 효과를 제어하는 프로퍼티
    public bool HasShield { get; set; } = false;

    private void Awake()
    {
        allRenderers = GetComponentsInChildren<Renderer>();
        anim = GetComponent<Animator>();
        propBlock = new MaterialPropertyBlock();
        afterimage = GetComponent<AfterimageGenerator>();

        // [추가] 모델의 초기 로컬 위치를 기억합니다.
        if (modelTransform != null)
        {
            originalModelPos = modelTransform.localPosition;
        }
    }

    private void Start()
    {
        // 시작하자마자 기본 테두리를 적용합니다.
        SetDefaultVisuals();
    }

    // 기본 상태로 설정하는 함수
    public void SetDefaultVisuals()
    {
        SetAllHitIntensity(baseRimIntensity);
        SetAllFresnelPower(rimPower);
    }

    // 1. 피격 번쩍임 효과
    public void PlayHitFlash()
    {
        if (HasShield) return; // 쉴드가 있으면 번쩍임 생략
                               // 1. 번쩍임 효과 실행
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlashRoutine());

        // 2. 모델 흔들림 효과 실행
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(HitShakeRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        SetAllHitIntensity(enemyhitFlashIntensity);
        yield return new WaitForSecondsRealtime(flashDuration);
        SetAllHitIntensity(baseRimIntensity);
        flashCoroutine = null;
    }

    // [추가] 모델을 무작위로 흔드는 코루틴
    private IEnumerator HitShakeRoutine()
    {
        if (modelTransform == null) yield break;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // 무작위 방향으로 살짝 오프셋 계산
            Vector3 randomOffset = Random.insideUnitSphere * shakeMagnitude;
            modelTransform.localPosition = originalModelPos + randomOffset;

            // 역경직(TimeScale = 0) 상태에서도 흔들려야 하므로 unscaledDeltaTime 사용
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 흔들림 종료 후 반드시 원래 위치로 복구
        modelTransform.localPosition = originalModelPos;
        shakeCoroutine = null;
    }

    private void SetAllHitIntensity(float value)
    {
        if (allRenderers == null) return;
        foreach (var ren in allRenderers)
        {
            if (ren == null) continue;
            ren.GetPropertyBlock(propBlock);
            propBlock.SetFloat(HitIntensityID, value);
            ren.SetPropertyBlock(propBlock);
        }
    }

    // 2. [에러 해결] 피격 애니메이션 실행 함수 복구
    public void PlayHitAnimation()
    {
        if (anim != null)
        {
            // 현재 애니메이터에 Hit 트리거가 있다면 실행 (없으면 주석 유지)
            // anim.SetTrigger("Hit"); 
        }
    }

    private void SetAllFresnelPower(float value)
    {
        if (allRenderers == null) return;
        foreach (var ren in allRenderers)
        {
            if (ren == null) continue;
            ren.GetPropertyBlock(propBlock);
            propBlock.SetFloat(FresnelPowerID, value);
            ren.SetPropertyBlock(propBlock);
        }
    }

    // 3. [에러 해결] 경고 신호(돌진 전 등) 실행 함수 복구
    public void PlayWarningSignal()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        Debug.Log("보스 경고 신호 시작");
        // 필요 시 여기서 몸을 붉게 만들거나 소리를 재생하는 로직을 넣습니다.
        yield return new WaitForSeconds(0.5f);
        flashCoroutine = null;
    }

    // 4. 타격 이펙트 생성
    public void PlayAttackHitVisual(HitData hitData)
    {
        if (hitData.hitEffectPrefab != null)
            Instantiate(hitData.hitEffectPrefab, hitData.hitPoint, Quaternion.identity);
    }

    // 5. 순간이동 시각 효과 (기존 구조 유지)
    public void PlayTeleportStartVFX()
    {
        if (teleportStartVFX != null)
        {
            Quaternion rotation = Quaternion.Euler(-90f, 0, 0);
            Instantiate(teleportStartVFX, transform.position, rotation);
        }
    }

    public void PlayTeleportEndVFX()
    {
        if (teleportEndVFX != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            Instantiate(teleportEndVFX, spawnPos, Quaternion.identity);
        }
    }

    // 6. 잔상 제어
    public void ToggleAfterimage(bool active)
    {
        if (afterimage == null) return;
        if (active) afterimage.StartAfterimage();
        else afterimage.StopAfterimage();
    }
}