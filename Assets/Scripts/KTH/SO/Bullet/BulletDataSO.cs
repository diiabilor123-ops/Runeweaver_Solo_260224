using UnityEngine;
using Runeweaver;

[CreateAssetMenu(fileName = "BulletData", menuName = "BulletDataSO/Data/Bullet")]
public class BulletDataSO : ScriptableObject
{
    [Header("Identity")]
    public string bulletID;

    [Header("Prefab Settings")]
    public GameObject bulletPrefab; // 실제 물리/로직이 들어있는 프리팹

    [Header("Movement")]
    public float speed = 20f;
    public float maxDistance = 8f;
    public bool isPenetrating = true;

    [Header("Combat")]
    public float damage = 10f;
    public float damageMultiplier = 1f;

    [Header("Visuals (Juice)")]
    public GameObject shootVFX;       // 발사 시 바람/파동 효과
    public GameObject[] hitEffectPrefabs; // [0]: 일반 적중, [1]: 패시브 폭발

    [Header("VFX Graph Settings (Optional)")]
    public bool isVFXGraph = false;   // VFX Graph를 사용한다면 체크
    public string startEvent = "OnPlay";
    public string hitEvent = "OnHit";

    [Header("Audio Data")]
    public SoundDataSO shootSound;
    public SoundDataSO flySound;
    public SoundDataSO hitSound;
    public SoundDataSO explosionSound;
}