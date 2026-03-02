// FeedbackManager.cs 수정본
using UnityEngine;
using System.Collections;
using Unity.Cinemachine; // 시네머신 3.0 네임스페이스 추가

public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance;

    [Header("Impulse")]
    // 이 스크립트가 붙은 오브젝트에 'Cinemachine Impulse Source'를 추가하고 여기에 할당하세요.
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_impulseSource == null) _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    // ⭐ 핵심: 타격 피드백 통합 실행 함수 (역경직 + 흔들림)
    public void ExecuteHitFeedback(BulletDataSO data)
    {
        if (data == null) return;

        // 1. 역경직 실행 (SO 데이터 활용)
        PlayHitStop(data.hitStopDuration);

        // 2. 카메라 흔들림 실행 (SO 데이터 활용)
        if (_impulseSource != null && data.shakeIntensity > 0)
        {
            _impulseSource.GenerateImpulse(data.shakeIntensity);
        }
    }

    // 역경직 (TimeScale 조절)
    private void PlayHitStop(float duration)
    {
        if (duration <= 0) return;
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // 하데스식 미세 멈춤
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}