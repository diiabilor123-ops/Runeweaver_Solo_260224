using UnityEngine;

public class MapInfo : MonoBehaviour
{
    public string mapName;
    public SoundDataSO backgroundMusicSO; // AudioClip 대신 SO 사용

    void Start()
    {
        if (backgroundMusicSO != null)
        {
            SoundManager.Instance.ChangeBGM(backgroundMusicSO, 1.5f);
        }
    }
}