using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "SoundSO/Data/Sound")]
public class SoundDataSO : ScriptableObject
{
    // 사운드 피치 랜덤성
    public AudioClip clip;
    [Range(0, 1)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float minPitch = 0.9f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.1f;
    public bool loop = false;
}
