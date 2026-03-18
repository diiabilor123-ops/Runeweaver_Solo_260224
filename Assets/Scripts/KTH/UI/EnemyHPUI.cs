using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHPUI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private CanvasGroup canvasGroup; // 평소에 숨기기용
    [SerializeField] private TextMeshProUGUI hpText;

    private EnemyHealth _ownerHealth;

    void Awake()
    {
        // 부모나 자신에게서 EnemyHealth를 찾음
        _ownerHealth = GetComponentInParent<EnemyHealth>();

        if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // 시작할 때는 체력바를 숨겨둠 (선택 사항)
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        if (_ownerHealth != null)
            _ownerHealth.OnHealthChanged += UpdateHP;
    }

    void OnDisable()
    {
        if (_ownerHealth != null)
            _ownerHealth.OnHealthChanged -= UpdateHP;
    }

    private void UpdateHP(float current, float max)
    {
        float ratio = current / max;

        // 데미지를 입으면 체력바를 보여줌
        if (canvasGroup != null && canvasGroup.alpha < 1f)
            canvasGroup.DOFade(1f, 0.2f);

        // 부드럽게 감소
        hpSlider.DOKill();
        hpSlider.DOValue(ratio, 0.25f).SetEase(Ease.OutQuad);

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(current)}"; // 몬스터는 현재 수치만 보여줘도 깔끔합니다.
        }

        // 사망 시 체력바 숨기기
        if (current <= 0)
            canvasGroup.DOFade(0f, 0.2f);
    }

    void LateUpdate()
    {
        // 빌보드 효과: 카메라를 항상 바라봄
        transform.rotation = Camera.main.transform.rotation;
    }
}