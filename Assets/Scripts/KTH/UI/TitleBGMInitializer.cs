using UnityEngine;

public class TitleBGMInitializer : MonoBehaviour
{
    public SoundDataSO titleBGM;

    void Start()
    {
        if (SoundManager.Instance != null && titleBGM != null)
        {
            SoundManager.Instance.PlayBGM(titleBGM, 1.5f); // 1.5초 페이드 인
        }
    }
}