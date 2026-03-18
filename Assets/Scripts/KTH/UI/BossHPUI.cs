using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BossHPUI : MonoBehaviour
{
    [SerializeField] private Slider bossSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI hpNumberText;

    private EnemyHealth _targetBoss;
    private bool _isHiding = false; // 중복 실행 방지용

    void Awake()
    {
        canvasGroup.alpha = 0f;
        bossSlider.value = 0f;
    }

    public void ShowBossUI(EnemyHealth bossHealth)
    {
        _isHiding = false;
        _targetBoss = bossHealth;
        _targetBoss.OnHealthChanged += UpdateBossHP;

        if (bossNameText != null && bossHealth.enemyData != null)
        {
            bossNameText.text = bossHealth.enemyData.enemyName;
        }

        UpdateHPText(bossHealth.enemyData.maxHp, bossHealth.enemyData.maxHp);

        canvasGroup.DOKill(); // 기존 트윈 제거
        canvasGroup.DOFade(1f, 1f);
        bossSlider.DOValue(1f, 1.5f).SetEase(Ease.OutCubic);
    }

    private void UpdateBossHP(float current, float max)
    {
        float ratio = current / max;

        bossSlider.DOKill();
        bossSlider.DOValue(ratio, 0.3f).SetEase(Ease.OutQuad);

        UpdateHPText(current, max);

        // [추가] 체력이 0 이하가 되면 UI 숨기기
        if (current <= 0 && !_isHiding)
        {
            HideBossUI();
        }
    }

    // [추가] 보스 UI 사라지는 함수
    public void HideBossUI()
    {
        _isHiding = true;

        // 이벤트 구독 해제 (더 이상 업데이트 안 함)
        if (_targetBoss != null)
        {
            _targetBoss.OnHealthChanged -= UpdateBossHP;
        }

        // 1초 동안 서서히 사라짐
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, 1f).OnComplete(() => {
            // 완전히 사라진 후 값 초기화 (선택 사항)
            bossSlider.value = 0f;
        });
    }

    private void UpdateHPText(float current, float max)
    {
        if (hpNumberText != null)
        {
            // 죽었을 때 음수가 나오지 않게 Mathf.Max 사용
            hpNumberText.text = $"{Mathf.CeilToInt(Mathf.Max(0, current))} / {Mathf.CeilToInt(max)}";
        }
    }

    private void OnDestroy()
    {
        if (_targetBoss != null)
            _targetBoss.OnHealthChanged -= UpdateBossHP;
    }
}