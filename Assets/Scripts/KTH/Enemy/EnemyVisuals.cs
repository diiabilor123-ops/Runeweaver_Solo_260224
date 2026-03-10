using UnityEngine;
using System.Collections;

/// <summary>
/// [공통 몬스터 비주얼] 모든 몬스터의 피격 번쩍임, 애니메이션, 시각 효과를 담당합니다.
/// </summary>
public class EnemyVisuals : MonoBehaviour
{
    [Header("Hit Flash (Shader)")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float enemyhitFlashIntensity = 2.0f;
    private static readonly int HitIntensityID = Shader.PropertyToID("_HitIntensity");

    [Header("Teleport VFX")]
    [SerializeField] private GameObject teleportStartVFX; // 사라질 때 프리팹
    [SerializeField] private GameObject teleportEndVFX;   // 나타날 때 프리팹

    private Renderer[] allRenderers;
    private MaterialPropertyBlock propBlock;
    private Coroutine flashCoroutine;
    private Animator anim;
    private AfterimageGenerator afterimage;

    // [추가] 쉴드 여부에 따라 피격 효과를 제어하는 프로퍼티
    public bool HasShield { get; set; } = false;

    private void Awake()
    {
        allRenderers = GetComponentsInChildren<Renderer>();
        anim = GetComponent<Animator>();
        propBlock = new MaterialPropertyBlock();
        afterimage = GetComponent<AfterimageGenerator>();
    }

    // 1. 피격 번쩍임 효과
    public void PlayHitFlash()
    {
        if (HasShield) return; // 쉴드가 있으면 번쩍임 생략
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        SetAllHitIntensity(enemyhitFlashIntensity);
        yield return new WaitForSeconds(flashDuration);
        SetAllHitIntensity(0f);
        flashCoroutine = null;
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