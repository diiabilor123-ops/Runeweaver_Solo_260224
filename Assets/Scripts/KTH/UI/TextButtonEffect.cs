using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class TextButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI buttonText;
    public GameObject selectorIcon; // 글자 옆에 나타날 룬 아이콘
    public float moveDistance = 15f; // 글자가 옆으로 살짝 밀리는 거리

    private Vector3 originalPos;

    [Header("Audio")]
    public SoundDataSO hoverSFX; // 인스펙터에서 호버 효과음(SO)을 연결하세요.

    void Start()
    {
        originalPos = buttonText.transform.localPosition;
        selectorIcon.SetActive(false); // 처음엔 아이콘 숨김
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 아이콘 나타내기
        selectorIcon.SetActive(true);
        selectorIcon.transform.localScale = Vector3.zero;
        selectorIcon.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

        // 2. 글자 색상 변경 및 살짝 이동 (로스트아크/하데스 느낌)
        buttonText.DOColor(new Color(0, 1, 1), 0.2f); // 청록색으로 변경
        buttonText.transform.DOLocalMoveX(originalPos.x + moveDistance, 0.2f);

        // 2. 호버 사운드 재생 추가
        if (SoundManager.Instance != null && hoverSFX != null)
        {
            // SoundManager를 통해 호버음을 재생합니다.
            SoundManager.Instance.Play(hoverSFX, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 1. 아이콘 숨기기
        selectorIcon.transform.DOScale(0f, 0.2f).OnComplete(() => selectorIcon.SetActive(false));

        // 2. 글자 원상복구
        buttonText.DOColor(Color.white, 0.2f);
        buttonText.transform.DOLocalMoveX(originalPos.x, 0.2f);
    }
}