using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SoundData", menuName = "SoundSO/Data/Sound")]
public class SoundDataSO : ScriptableObject
{
    public AudioClip clip;
    [Range(0, 1)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float minPitch = 0.9f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.1f;
    public bool loop = false;

    [Header("Timing Tuning")]
    [Tooltip("소리를 몇 초 뒤에 낼 것인가? (너무 일찍 나올 때 사용)")]
    public float playDelay = 0f;

    [Tooltip("파일의 몇 초 지점부터 재생할 것인가? (앞부분 무음 자를 때 사용)")]
    public float startTime = 0f;
    [Tooltip("0이면 끝까지 재생, 0보다 크면 그 시간(초)만큼만 재생하고 멈춤")]
    public float playDuration = 0f; // 추가된 필드

    [Header("Advanced (선택사항)")]
    [Tooltip("비워두면 기본 SFX 통로를 타고, 지정하면 그 통로를 탑니다.")]
    public AudioMixerGroup customMixerGroup; // 특별 관리가 필요한 소리용
}