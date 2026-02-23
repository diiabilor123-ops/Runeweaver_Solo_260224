using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "BulletDataSO/Data/Bullet")]
public class BulletDataSO : ScriptableObject
{
    [Header("Movement")]
    public float speed = 20f;
    public float maxDistance = 8f; // 기획안의 8m를 여기서 조절
    public bool isPenetrating = true; // 기본 공격은 모든 적 관통

    [Header("Combat")]
    public float damageMultiplier = 1f; // 기본 데미지에 곱해질 비율 (다발 화살은 0.25)
    public int manaCost = 0;           // 마나 소모량 (다발 화살은 1)

    [Header("Visuals")]
    public EffectDataSO mainEffect; // 여기에 EffectDataSO를 할당
    public EffectDataSO hitEffect; // 관통 시 적중 위치에 남길 별도 이펙트 (필요 시)

    // 필요에 따라 발사구(Muzzle)나 별도의 타격(Hit) 이펙트를 따로 쓸 때 사용
    public EffectDataSO muzzleEffect;
    public TrailRenderer trailPrefab;

    [Header("Audio Data")]
    public SoundDataSO shootSound;  // 1. 발사 시 (활 시위 소리)
    public SoundDataSO flySound;    // 2. 날아가는 동안 (공기 가르는 소리)
    public SoundDataSO hitSound;    // 3. 적중 시 (퍽/깡 소리)
}