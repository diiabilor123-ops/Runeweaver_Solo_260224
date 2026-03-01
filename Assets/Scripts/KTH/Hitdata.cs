using UnityEngine;

// [핵심] 이 어트리뷰트가 있어야 인스펙터에 노출됩니다!
[System.Serializable]
public class HitData
{
    [Header("Basic Info")]
    public float damage;
    public ElementType element;
    public Team attackerTeam;

    [Header("Feedback Settings")]
    public Vector3 hitPoint;
    public Vector3 attackerPos;

    public GameObject hitEffectPrefab;
}