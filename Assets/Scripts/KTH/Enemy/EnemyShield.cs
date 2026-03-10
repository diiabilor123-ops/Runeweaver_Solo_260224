using UnityEngine;
using System.Collections;

/// <summary>
/// [쉴드 전용 컴포넌트] 쉴드 로직과 그에 따른 전용 이펙트를 담당합니다.
/// </summary>
public class EnemyShield : MonoBehaviour
{
    [Header("Shield Logic")]
    [SerializeField] private float maxShield = 50f;
    private float currentShield;

    [Header("Shield Visual FX")]
    [SerializeField] private GameObject shieldMeshFX;
    [SerializeField] private float shieldShowDuration = 0.15f;
    [SerializeField] private Vector3 shieldImpactScaleMultiplier = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private Vector3 shieldOffset = new Vector3(0, 1.0f, 0);
    [SerializeField] private float shieldRotationSpeed = 100f;
    [SerializeField] private float shieldHitFlashIntensity = 2.0f;
    [SerializeField] private Color shieldColor = new Color(1f, 0.9f, 0.4f);

    [Header("Shield Particles")]
    [SerializeField] private GameObject shieldParticlePrefab;

    private EnemyVisuals visuals;
    private Vector3 shieldBaseScale;
    private Coroutine shieldCoroutine;

    void Awake()
    {
        currentShield = maxShield;
        visuals = GetComponent<EnemyVisuals>();

        if (shieldMeshFX != null)
        {
            shieldBaseScale = shieldMeshFX.transform.localScale;
            shieldMeshFX.SetActive(false);
        }
    }

    void Update()
    {
        // 쉴드가 활성화 상태일 때만 회전
        if (shieldMeshFX != null && shieldMeshFX.activeSelf)
        {
            shieldMeshFX.transform.Rotate(Vector3.up * shieldRotationSpeed * Time.deltaTime);
        }
    }

    public float AbsorbDamage(float damage, Vector3 hitPoint)
    {
        if (currentShield <= 0)
        {
            if (visuals != null) visuals.HasShield = false;
            return damage;
        }

        // 쉴드 작동 중임을 알림
        if (visuals != null) visuals.HasShield = true;

        float damageToShield = Mathf.Min(currentShield, damage);
        currentShield -= damageToShield;

        // [연출 실행] 쉴드 전용 이펙트 재생
        PlayShieldEffect(hitPoint);

        if (currentShield <= 0)
        {
            Debug.Log($"{gameObject.name} 보호막 파괴!");
            if (visuals != null) visuals.HasShield = false;
            if (shieldMeshFX != null) shieldMeshFX.SetActive(false);
        }

        return damage - damageToShield;
    }

    private void PlayShieldEffect(Vector3 hitPoint)
    {
        // 1. 파티클 연출
        if (shieldParticlePrefab != null)
        {
            GameObject particleObj = Instantiate(shieldParticlePrefab, transform);
            // hitPoint가 Zero라면 캐릭터 중심, 아니면 타격 지점으로 설정
            particleObj.transform.position = (hitPoint == Vector3.zero) ? transform.position + shieldOffset : hitPoint;

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier *= shieldImpactScaleMultiplier.x;
                ps.Play();
            }
            Destroy(particleObj, 1.5f);
        }

        // 2. 메쉬 FX 연출
        if (shieldMeshFX != null)
        {
            if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldVisibleRoutine());
        }
    }

    private IEnumerator ShieldVisibleRoutine()
    {
        shieldMeshFX.SetActive(true);
        Renderer shieldRenderer = shieldMeshFX.GetComponent<Renderer>();
        Material shieldMat = shieldRenderer.material;

        float elapsed = 0f;
        float boostRotation = shieldRotationSpeed * 3f;

        while (elapsed < shieldShowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shieldShowDuration;

            // 회전 가속 및 감쇠
            float currentRotation = Mathf.Lerp(boostRotation, shieldRotationSpeed, t);
            shieldMeshFX.transform.Rotate(Vector3.up * currentRotation * Time.deltaTime);

            // 밝기 반짝임 (Emission)
            float intensity = Mathf.Lerp(shieldHitFlashIntensity, 0f, t);
            shieldMat.SetColor("_EmissionColor", shieldColor * intensity);

            // 스케일 꿀렁임
            shieldMeshFX.transform.localScale = Vector3.Lerp(shieldBaseScale * 1.1f, shieldBaseScale, t);

            yield return null;
        }

        shieldMeshFX.SetActive(false);
    }
}