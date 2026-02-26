using UnityEngine;

// 1. 6가지 원소 속성 정의
public enum ElementType
{
    None,
    Pyro,   // 불
    Aqua,   // 물/얼음
    Volt,   // 번개
    Nature, // 자연/풀
    Light,  // 빛
    Dark    // 어둠
}

// 2. 몬스터 데이터 규격 (SO)
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "EnemyDataSO/Data/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;
    public ElementType mainElement; // 몬스터의 주 속성

    [Header("전투 능력치")]
    public float maxHp = 100f;
    public float moveSpeed = 3.5f;
    public float attackDamage = 10f;

    [Header("AI & 공격 범위")]
    public float detectionRange = 10f; // 플레이어 인지 범위
    public float attackRange = 2f;    // 공격 사거리
    public float chargeDistance = 8f;  // 돌진 시도 거리 (추가 추천)

    [Header("공격 타이밍")]
    public float attackCooldown = 2f; // 공격 쿨타임
    public float attackWarningTime = 0.5f; // 하데스식 공격 전조 시간 (번쩍임)

    [Header("액션 디테일 (추천)")]
    public float knockbackForce = 5f;      // 플레이어를 밀어내는 힘
    public float stunDuration = 1.0f;      // 돌진 후 스스로 멈추는 시간 (Dizzy 상태)

    [Header("시각 연출")]
    public GameObject hitEffectPrefab;  // 피격 파티클
    public GameObject deathEffectPrefab; // 사망 파티클
}