using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Settings")]
    public string gameSceneName = "InGameScene";

    [Header("Audio")]
    public SoundDataSO clickSFX; // 인스펙터에서 클릭 효과음(SO)을 연결하세요.

    // [게임 시작 버튼]
    public void StartGame()
    {
        PlayClickSound();
        SceneManager.LoadScene(gameSceneName);
    }

    // [설정 버튼 - 현재 소리만 재생]
    public void OpenSettings()
    {
        PlayClickSound();
        Debug.Log("설정창 열기 (기능 미구현)");
    }

    // [게임 종료 버튼]
    public void QuitGame()
    {
        PlayClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 공통 클릭 사운드 재생 함수
    private void PlayClickSound()
    {
        if (SoundManager.Instance != null && clickSFX != null)
        {
            // SoundManager의 Play 메서드를 호출합니다.
            SoundManager.Instance.Play(clickSFX, transform.position);
        }
    }
}