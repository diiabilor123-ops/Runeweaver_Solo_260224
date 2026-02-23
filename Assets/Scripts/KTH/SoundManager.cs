using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 게임의 모든 효과음을 관리하는 매니저입니다.
/// 어디서든 SoundManager.Instance.Play(SoundDataSO)로 호출할 수 있습니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 싱글톤: 어디서든 접근 가능하게 함
    public static SoundManager Instance;

    private void Awake()
    {
        // 씬에 사운드 매니저가 하나만 존재하도록 보장
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 파괴되지 않게 하려면 아래 주석 해제 (필요 시)
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// SoundDataSO 데이터를 기반으로 사운드를 재생합니다.
    /// 하데스 스타일의 랜덤 피치가 적용됩니다.
    /// </summary>
    public void Play(SoundDataSO data, Vector3 position)
    {
        if (data == null || data.clip == null) return;

        // 효과음용 임시 게임 오브젝트 생성
        GameObject go = new GameObject("TempSFX_" + data.clip.name);
        go.transform.position = position; // 소리가 날 위치 설정
        AudioSource source = go.AddComponent<AudioSource>();

        // 데이터 적용
        source.clip = data.clip;
        source.volume = data.volume;

        // [하데스 디테일] 매번 미세하게 다른 피치로 재생하여 타격감을 풍성하게 함
        source.pitch = Random.Range(data.minPitch, data.maxPitch);

        // 3D 사운드 설정 (필요 시)
        source.spatialBlend = 1.0f; // 0은 2D, 1은 3D

        // 재생 시작
        source.loop = data.loop;
        source.Play();

        // 재생이 끝나면 임시 오브젝트 삭제
        Destroy(go, data.clip.length);
    }
}