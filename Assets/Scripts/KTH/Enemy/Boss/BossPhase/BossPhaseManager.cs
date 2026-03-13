using UnityEngine;

public class BossPhaseManager : MonoBehaviour
{
    private EnemyHealth health;
    public BossPatternManager patternManager; // 패턴 매니저 연결

    private bool[] phaseTriggered = new bool[3]; // 75%, 50%, 25%

    private void Awake() => health = GetComponent<EnemyHealth>();

    private void OnEnable() { if (health != null) health.OnHealthChanged += CheckPhase; }
    private void OnDisable() { if (health != null) health.OnHealthChanged -= CheckPhase; }

    private void CheckPhase(float currentHp, float maxHp)
    {
        float hpRatio = currentHp / maxHp;

        // 75%: 2개 파괴
        if (hpRatio <= 0.75f && !phaseTriggered[0]) Trigger(0, 2);

        // 50%: 2개 파괴
        if (hpRatio <= 0.50f && !phaseTriggered[1]) Trigger(1, 2);

        // 25%: 중앙 빼고 전부 파괴 (-1 전달)
        if (hpRatio <= 0.25f && !phaseTriggered[2]) Trigger(2, -1);
    }

    private void Trigger(int index, int tileCount)
    {
        phaseTriggered[index] = true;
        if (patternManager != null)
            patternManager.StartPhaseSequence(index, tileCount);
    }

    // --- [테스트용 치트키 기능] ---
    // 인스펙터의 스크립트 이름을 마우스 우클릭하면 나타납니다.
    [ContextMenu("Test: Give 25% Damage")]
    public void TestDamage()
    {
        if (health == null) return;

        // EnemyHealth 스크립트의 구조에 맞게 25% 데미지를 입힙니다.
        // HitData 구조체나 변수가 다르다면 본인의 EnemyHealth.TakeDamage 인자에 맞춰 수정하세요.
        float damageAmount = health.enemyData.maxHp * 0.25f;

        // 가짜 타격 데이터 생성
        HitData fakeHit = new HitData { damage = damageAmount };

        Debug.Log($"<color=yellow>[Cheat]</color> 보스에게 25% 데미지를 입혔습니다.");
        health.TakeDamage(fakeHit);
    }
}