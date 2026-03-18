using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween 사용
using Runeweaver.Player;
using TMPro;

public class PlayerHPUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0); // 머리 위 높이 조절

    [SerializeField] private TextMeshProUGUI hpText;

    private float _lastHp;

    void Start()
    {
        if (PlayerStats.Instance != null)
        {
            _lastHp = PlayerStats.Instance.currentHp;
            // 시작할 때 즉시 초기화
            hpSlider.value = _lastHp / PlayerStats.Instance.maxHp;
        }
    }

    // LateUpdate는 모든 이동이 끝난 후 실행되어 덜덜 떨리는 현상을 방지합니다.
    void LateUpdate()
    {
        if (PlayerStats.Instance == null) return;

        // 1. 위치 고정: 플레이어 위치 + 오프셋 (부모가 플레이어여도 상관없음)
        // 만약 부모 자식 관계가 아니라면 아래 주석을 푸세요.
        // transform.position = PlayerStats.Instance.transform.position + offset;

        // 2. 회전 고정 (핵심): 캐릭터가 돌아가도 UI는 카메라만 봅니다.
        transform.rotation = Camera.main.transform.rotation;



        // 3. 체력 실시간 반영 (DOTween)
        float currentHp = PlayerStats.Instance.currentHp;
        float maxHp = PlayerStats.Instance.maxHp;

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }

        if (!Mathf.Approximately(_lastHp, currentHp))
        {
            _lastHp = currentHp;
            float targetRatio = _lastHp / PlayerStats.Instance.maxHp;

            hpSlider.DOKill();
            hpSlider.DOValue(targetRatio, 0.2f).SetEase(Ease.OutQuad);
        }
    }
}