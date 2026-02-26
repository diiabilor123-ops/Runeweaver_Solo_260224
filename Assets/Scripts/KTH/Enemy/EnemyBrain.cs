using UnityEngine;

/// <summary>
/// [몬스터 AI의 핵심 부모 클래스]
/// 모든 몬스터는 이 클래스를 상속받아 'LogicUpdate'에서 고유 패턴을 구현합니다.
/// </summary>
public abstract class EnemyBrain : MonoBehaviour
{
    [Header("몬스터 설정 데이터")]
    public EnemyData data; // SO로부터 데이터 주입

    // 참조할 컴포넌트들 (Awake에서 자동 연결)
    protected EnemyHealth health;
    protected EnemyMover mover;
    protected EnemyVisuals visuals;
    protected Transform player;

    protected virtual void Awake()
    {
        // 1. 컴포넌트 자동 할당
        health = GetComponent<EnemyHealth>();
        mover = GetComponent<EnemyMover>();
        visuals = GetComponent<EnemyVisuals>();

        // 2. 플레이어 참조 (나중에 플레이어 매니저를 통해 가져오는 것이 더 최적화에 좋음)
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;

        // 3. 각 컴포넌트에 데이터 주입 (데이터 동기화)
        if (data != null)
        {
            health.Init(data);
            mover.Init(data);
        }
    }

    protected virtual void Update()
    {
        // 체력이 없거나 플레이어가 없으면 AI 중지
        if (health.IsDead || player == null) return;

        LogicUpdate(); // 자식 클래스에서 구현한 실제 행동 실행
    }

    /// <summary>
    /// 자식 클래스에서 이 메서드를 오버라이드하여 실제 공격/추격 로직을 짭니다.
    /// </summary>
    protected abstract void LogicUpdate();
}