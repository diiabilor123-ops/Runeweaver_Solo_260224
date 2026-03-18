using UnityEngine;
using DG.Tweening; // 하나만 남기고 중복 제거
using System.Collections;

/// <summary>
/// 게임의 모든 효과음을 관리하는 매니저입니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 싱글톤: 어디서든 접근 가능하게 함
    public static SoundManager Instance;

    [Header("BGM")]
    // [수정] SerializeField를 붙여서 인스펙터에서 보이게 합니다.
    [SerializeField] private AudioSource bgmSource;

    [Header("Mixer Groups")]
    public UnityEngine.Audio.AudioMixerGroup sfxGroup; // 인스펙터에서 SFX 그룹 연결

    private void Awake()
    {
        // 씬에 사운드 매니저가 하나만 존재하도록 보장
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 필요 시 해제

            // [중요] BGM 소스를 미리 생성/초기화합니다.
            InitializeBGM();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeBGM()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 1.0f; // 기본 볼륨 설정
        }
    }

    public void ChangeBGM(SoundDataSO data, float fadeDuration = 1.0f)
    {
        if (data == null || data.clip == null) return;
        if (bgmSource.clip == data.clip) return;

        bgmSource.DOFade(0, fadeDuration).OnComplete(() => {
            bgmSource.clip = data.clip;
            // SO에 설정된 기본 볼륨을 타겟으로 설정합니다.
            float targetVolume = data.volume;
            bgmSource.Play();
            bgmSource.DOFade(targetVolume, fadeDuration);
        });
    }

    public void Play(SoundDataSO data, Vector3 position)
    {
        if (data == null || data.clip == null) return;

        GameObject go = new GameObject("TempSFX_" + data.clip.name);
        go.transform.position = position;
        AudioSource source = go.AddComponent<AudioSource>();

        if (data.customMixerGroup != null)
        {
            source.outputAudioMixerGroup = data.customMixerGroup;
        }
        else if (sfxGroup != null)
        {
            source.outputAudioMixerGroup = sfxGroup;
        }

        source.clip = data.clip;
        source.volume = data.volume;
        source.pitch = UnityEngine.Random.Range(data.minPitch, data.maxPitch);
        source.spatialBlend = data.spatialBlend;

        source.time = data.startTime; // 파일의 중간부터 틉니다 (앞쪽 무음 건너뛰기)
        source.PlayDelayed(data.playDelay); // n초 뒤에 재생합니다 (너무 일찍 나올 때 지연)

        float totalLifeTime;
        if (data.playDuration > 0)
        {
            totalLifeTime = data.playDelay + data.playDuration;
        }
        else
        {
            // 남은 길이 = 전체 길이 - 시작 지점
            float remainingLength = data.clip.length - data.startTime;
            totalLifeTime = data.playDelay + remainingLength + 0.1f;
        }

        Destroy(go, totalLifeTime);
    }

    // PlayBGM은 ChangeBGM과 역할이 겹치므로 하나로 통일하거나 
    // 아래처럼 ChangeBGM을 호출하게 만드는 것이 깔끔합니다.
    public void PlayBGM(SoundDataSO data, float fadeDuration = 1.0f)
    {
        ChangeBGM(data , fadeDuration);
    }

    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (bgmSource == null) return;

        // BGM을 서서히 줄이고 완전히 정지시킵니다.
        // SetUpdate(true)를 붙여야 나중에 게임이 일시정지(TimeScale=0)되어도 소리가 꺼집니다.
        bgmSource.DOFade(0, fadeDuration).SetUpdate(true).OnComplete(() => {
            bgmSource.Stop();
        });
    }
}