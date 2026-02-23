using UnityEngine;
using System.Collections;

// '역경직(HitStop)'과 '카메라 흔들림'을 호출
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance;

    private void Awake() => Instance = this;

    // 하데스식 역경직 (HitStop)
    public void PlayHitStop(float duration)
    {
        if (duration <= 0) return;
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // 완전히 멈추기보다 아주 미세하게 흐르게 함
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    // 카메라 흔들림 (팀원의 카메라에 시네머신 등이 있다면 연동 가능)
    public void ShakeCamera(float intensity, float time)
    {
        // 여기에 카메라 쉐이크 로직 구현
    }
}
