using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "EnemyDataSO/Data/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public float hp;
    public float moveSpeed;
    public float attackRange;
    public float attackCooldown;
    // 하데스 느낌을 위한 전조 시간(Wait Time)
    public float attackWarningTime;
}