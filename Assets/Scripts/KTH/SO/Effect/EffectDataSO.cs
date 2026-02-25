using UnityEngine;

/// <summary>
/// 역할: 특정 투사체의 시각적 연출(VFX/Particle)에 필요한 모든 데이터를 보유합니다.
/// 설계 의도: 코드 수정 없이 인스펙터에서 이펙트 구성을 변경할 수 있도록 합니다.
/// </summary>
[CreateAssetMenu(fileName = "EffectData", menuName = "EffectDataSO/Data/EffectData")]
public class EffectDataSO : ScriptableObject
{
    [Header("Base Settings")]
    public GameObject prefab;      // 화살 본체 (날아가는 모양) 프리팹
    public float duration = 2f;    // 전체 이펙트가 유지될 시간

    [Header("Hit Settings")]
    // [관통 대응] 적중 시 해당 위치에 독립적으로 생성될 파편들 (예: SubExplosion, SubStones)
    // 여러 개를 등록하면 적중 시마다 모든 프리팹이 동시에 생성됩니다.
    public GameObject[] hitEffectPrefabs;

    [Header("VFX Graph Settings")]
    public bool isVFXGraph = true;       // VFX Graph 사용 여부 (true면 SendEvent 사용)
    public string startEvent = "create"; // 발사 시 실행할 이벤트 이름
    public string hitEvent = "Hit";      // 적중 시 실행할 이벤트 이름
}