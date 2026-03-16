using System.Collections;
using Unity.Cinemachine; // 시네머신 3.0 네임스페이스 추가
using UnityEngine;


/// <summary>
/// [타격 피드백 매니저]
/// 게임 내 역경직(Hit-Stop)과 카메라 흔들림(Impulse)을 한 곳에서 관리합니다.
/// 특정 화살이 아닌 '상황(크리티컬, 강력한 데미지 등)'에 따른 연출을 담당합니다.
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance;

    [Header("Impulse")]
    // 이 스크립트가 붙은 오브젝트에 'Cinemachine Impulse Source'를 추가하고 여기에 할당하세요.
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    [Header("0. Normal Attack (발사 및 일반 적중)")]
    [SerializeField] private float normalStop = 0.03f;  // 아주 짧은 멈춤
    [SerializeField] private float normalShake = 0.3f; // 아주 약한 흔들림

    [Header("1. Critical Hit (연출: 흔들림만 살짝)")]
    [SerializeField] private float critStop = 0.07f;
    [SerializeField] private float critShake = 0.8f;

    [Header("2. Massive Hit (연출: 묵직한 충격)")]
    [Tooltip("최대 체력의 50% 이상 데미지를 입혔을 때 적용")]
    [SerializeField] private float massiveStop = 0.12f;
    [SerializeField] private float massiveShake = 1.5f;

    [Header("3. One Shot Kill (연출: 가장 강력한 임팩트)")]
    [Tooltip("최대 체력의 100%에 가까운 데미지로 즉사시켰을 때 적용")]
    [SerializeField] private float oneShotStop = 0.25f;
    [SerializeField] private float oneShotShake = 3.5f;

    private Coroutine _hitStopCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_impulseSource == null) _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    // 상황별 피드백 실행 함수들
    public void PlayNormalFeedback() => ExecuteHitFeedback(normalStop, normalShake);
    public void PlayCritFeedback() => ExecuteHitFeedback(critStop, critShake);
    public void PlayMassiveFeedback() => ExecuteHitFeedback(massiveStop, massiveShake);
    public void PlayOneShotFeedback() => ExecuteHitFeedback(oneShotStop, oneShotShake);

    /// 실제 피드백을 실행하는 통합 함수
    public void ExecuteHitFeedback(float duration, float intensity)
    {

        // 1. 역경직 실행 (0초 이상일 때만)
        if (duration > 0) PlayHitStop(duration);

        // 2. 카메라 임펄스 발생
        if (_impulseSource != null && intensity > 0)
        {
            // [수정] GenerateImpulse(intensity) 대신 Force를 사용합니다.
            // 이 함수가 인스펙터의 'Invoke' 버튼과 똑같은 로직(Default Velocity 사용)을 실행합니다.
            _impulseSource.GenerateImpulseWithForce(intensity);
        }
    }

    // 방향을 포함한 피드백 실행
    public void ExecuteDirectionalHitFeedback(float duration, float intensity, Vector3 direction)
    {
        if (duration > 0) PlayHitStop(duration);

        if (_impulseSource != null && intensity > 0)
        {
            // GenerateImpulseWithVelocity는 방향(Vector3)과 힘을 동시에 줄 수 있습니다.
            // 공격 방향(direction)으로 카메라를 툭 밀어주는 느낌을 줍니다.
            _impulseSource.GenerateImpulseWithVelocity(direction * intensity);
        }
    }

    // 역경직 (TimeScale 조절)
    private void PlayHitStop(float duration)
    {
        // 새로운 역경직이 들어오면 기존 코루틴을 끊어 시간 계산 꼬임을 방지합니다.
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
        _hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // 하데스식 미세 멈춤 (완전 정지보다 0.05가 더 부드러움)
        yield return new WaitForSecondsRealtime(duration); // 유니티 시간을 멈췄으므로 현실 시간 기준으로 대기
        Time.timeScale = 1f;
        _hitStopCoroutine = null;
    }

}