using UnityEngine;

[CreateAssetMenu(fileName = "FootstepData", menuName = "FootstepSO/Data/Footstep")]
public class FootstepSetSO : ScriptableObject
{
    public string surfaceTag = "Untagged"; // ¿¹: "Grass", "Dirt", "Water"
    public SoundDataSO stepSound;
    public GameObject dustEffectPrefab;
}