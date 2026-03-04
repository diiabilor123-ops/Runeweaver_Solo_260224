using Runeweaver;
using UnityEngine;

// [핵심] 이 어트리뷰트가 있어야 인스펙터에 노출됩니다!
[System.Serializable]
public class HitData
{
    [Header("Basic Info")]
    public float damage;
    public MonsterElement element;
    public Team attackerTeam;

    // [추가] 크리티컬 여부를 저장할 변수
    [Header("Attack Info")]
    public bool isCritical;

    // [중요] 공격자(플레이어)가 가진 원소 스택 정보 (패시브 발동용)
    // [수정] 공격자가 사용한 '플레이어 원소 타입'을 담습니다.
    public Runeweaver.ElementType attackElement;

    [Header("Feedback Settings")]
    public Vector3 hitPoint;
    public Vector3 attackerPos;

    public GameObject hitEffectPrefab;
}