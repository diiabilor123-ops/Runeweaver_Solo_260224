using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PingPongVideoSwitcher : MonoBehaviour
{
    [Header("Video Components")]
    public VideoPlayer vpForward;
    public RawImage imgForward;
    public VideoPlayer vpReverse;
    public RawImage imgReverse;

    [Header("Sunlight & Bloom Effect")]
    public Image sunFlash;
    public float flashIntensity = 0.8f;
    public Volume globalVolume;
    public float maxBloomIntensity = 5.0f;

    private Bloom bloom;
    private float defaultBloomIntensity;

    [Header("Style Settings")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.85f;
    public Color tintColor = new Color(0.9f, 0.95f, 1f);

    [Header("Loop Settings")]
    public float startTime = 0.0f;
    public float endTime = 3.5f;
    public float fadeSpeed = 2.0f;

    [Header("Transitions")]
    public float slowDownThreshold = 1.0f;
    public float crossFadeDuration = 0.5f;
    public float minPlaybackSpeed = 0.5f;

    [Header("Sunlight Timing")]
    public float flashInDuration = 0.5f;
    public float flashOutDuration = 1.2f;

    private bool isForwardActive = true;
    private bool isFading = false;

    void Start()
    {
        if (globalVolume != null && globalVolume.profile.TryGet<Bloom>(out var tmpBloom))
        {
            bloom = tmpBloom;
            defaultBloomIntensity = bloom.intensity.value;
        }

        if (sunFlash != null) sunFlash.color = new Color(1, 1, 1, 0);

        imgForward.color = new Color(tintColor.r, tintColor.g, tintColor.b, maxAlpha);
        imgReverse.color = new Color(tintColor.r, tintColor.g, tintColor.b, 0);

        vpForward.isLooping = false;
        vpReverse.isLooping = false;

        // 초기 속도 보장
        vpForward.playbackSpeed = 1.0f;
        vpReverse.playbackSpeed = 1.0f;

        vpForward.Prepare();
        vpReverse.Prepare();

        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        while (!vpForward.isPrepared || !vpReverse.isPrepared) yield return null;
        vpForward.time = startTime;
        vpReverse.time = startTime;
        vpForward.Play();
        StartCoroutine(LoopRoutine());
    }

    IEnumerator LoopRoutine()
    {
        while (true)
        {
            VideoPlayer activeVP = isForwardActive ? vpForward : vpReverse;
            VideoPlayer nextVP = isForwardActive ? vpReverse : vpForward;
            RawImage activeImg = isForwardActive ? imgForward : imgReverse;
            RawImage nextImg = isForwardActive ? imgReverse : imgForward;

            float timeLeft = endTime - (float)activeVP.time;

            // 1. 감속 로직 (수정: 현재 활성화된 영상만 제어)
            if (timeLeft <= slowDownThreshold && timeLeft > 0)
            {
                float t = timeLeft / slowDownThreshold;
                activeVP.playbackSpeed = Mathf.SmoothStep(minPlaybackSpeed, 1.0f, t);
            }
            else
            {
                activeVP.playbackSpeed = 1.0f;
            }

            // 2. 햇빛 번쩍임 트리거
            if (timeLeft <= 0.1f && !isFading)
            {
                isFading = true;
                StartCoroutine(SunFlashEffect());
            }

            // 3. 영상 교체 지점 (핵심 수정 부분)
            if (activeVP.time >= endTime || !activeVP.isPlaying)
            {
                // [검증] 다음 영상 재생 전 속도를 확실히 1.0으로 초기화
                nextVP.playbackSpeed = 1.0f;
                nextVP.time = startTime;
                nextVP.Play();

                nextImg.color = new Color(tintColor.r, tintColor.g, tintColor.b, maxAlpha);
                activeImg.color = new Color(tintColor.r, tintColor.g, tintColor.b, 0);

                // [검증] 멈춘 영상도 다음 차례를 위해 속도를 1.0으로 초기화
                activeVP.Pause();
                activeVP.playbackSpeed = 1.0f;
                activeVP.time = startTime;

                isForwardActive = !isForwardActive;
                isFading = false;
            }
            yield return null;
        }
    }

    IEnumerator SunFlashEffect()
    {
        if (sunFlash == null) yield break;

        float timer = 0f;
        while (timer < flashInDuration)
        {
            timer += Time.deltaTime;
            float t = timer / flashInDuration;
            ApplyFlash(Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        timer = 0f;
        while (timer < flashOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer / flashOutDuration;
            ApplyFlash(Mathf.SmoothStep(1, 0, t));
            yield return null;
        }
        ApplyFlash(0);
    }

    void ApplyFlash(float intensityCurve)
    {
        float currentAlpha = intensityCurve * flashIntensity;
        sunFlash.color = new Color(tintColor.r, tintColor.g, tintColor.b, currentAlpha);

        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(defaultBloomIntensity, maxBloomIntensity, intensityCurve);
        }
    }
}