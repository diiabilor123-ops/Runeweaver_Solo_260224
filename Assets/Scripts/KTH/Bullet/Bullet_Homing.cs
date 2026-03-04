using UnityEngine;
using Runeweaver;

public class Bullet_Homing : MonoBehaviour
{
    public enum HomingStyle { Fire, Ice, Volt }
    public HomingStyle style;
    public float detectRadius = 20f;
    public float rotateSpeed = 8f;

    private BulletBase bulletBase;
    private Transform target;
    private float timer = 0f;
    private Vector3 startPosition; // 사거리 체크용
    private Vector3 randomCurveAxis; // 불화살용 랜덤 궤적 축

    void Awake() => bulletBase = GetComponent<BulletBase>();

    void OnEnable()
    {
        timer = 0f;
        startPosition = transform.position; // 시작 위치 기록
        target = FindNearestMonster();
        // 불화살이 매번 다른 방향으로 휘도록 랜덤 축 설정
        randomCurveAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;

        // 중요: 만약 BulletMovement가 붙어있다면 로직 충돌을 막기 위해 끕니다.
        if (TryGetComponent<BulletMovement>(out var move)) move.enabled = false;
    }

    void Update()
    {
        if (bulletBase == null || !bulletBase.IsActive) return;
        timer += Time.deltaTime;

        // 1. 사거리 체크 (BulletMovement의 역할을 대신함)
        float distance = Vector3.Distance(startPosition, transform.position);
        if (distance >= bulletBase.Data.maxDistance)
        {
            bulletBase.Deactivate();
            return;
        }

        // 2. 타겟 유효성 체크
        if (target != null && target.TryGetComponent<EnemyHealth>(out var h) && h.IsDead)
            target = null;

        switch (style)
        {
            case HomingStyle.Fire: MoveFire(); break;
            case HomingStyle.Ice: MoveIce(); break;
            case HomingStyle.Volt: MoveVolt(); break;
        }
    }

    private void MoveFire()
    {
        // [특색: 곡선 궤적] 0.6초 동안은 크게 휘어지며 비행 (타겟 유무 상관없이 연출)
        if (timer < 0.6f)
        {
            // 나선형으로 휘어지는 움직임
            transform.Rotate(randomCurveAxis, 150f * Time.deltaTime);
            transform.position += transform.forward * bulletBase.Data.speed * 0.8f * Time.deltaTime;
        }
        else if (target != null)
        {
            RotateAndMove(rotateSpeed); // 이후 유도
        }
        else
        {
            // 타겟 없으면 그대로 휘어져서 밖으로 나감
            transform.Rotate(randomCurveAxis, 50f * Time.deltaTime);
            transform.position += transform.forward * bulletBase.Data.speed * Time.deltaTime;
        }
    }

    private void MoveIce()
    {
        // [특색: 머리 위 정지 후 발사] 0.4초간 제자리 회전하며 에너지를 모으는 연출
        if (timer < 0.4f)
        {
            transform.Rotate(Vector3.up, 1000f * Time.deltaTime);
        }
        else
        {
            if (target != null) RotateAndMove(rotateSpeed * 1.5f); // 빠른 유도
            else transform.position += transform.forward * bulletBase.Data.speed * 1.2f * Time.deltaTime; // 직선
        }
    }

    private void MoveVolt()
    {
        // [특색: 지그재그 전진] 0.3초간 빠르게 직선으로 뻗어나가다 급회전
        if (timer < 0.3f)
        {
            // 살짝 지그재그 느낌 추가
            float shake = Mathf.Sin(Time.time * 20f) * 0.1f;
            transform.position += (transform.forward + transform.right * shake) * bulletBase.Data.speed * Time.deltaTime;
        }
        else if (target != null)
        {
            RotateAndMove(rotateSpeed * 2.5f); // 번개처럼 매우 빠른 유도
        }
        else
        {
            transform.position += transform.forward * bulletBase.Data.speed * Time.deltaTime;
        }
    }

    private void RotateAndMove(float s)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, s * Time.deltaTime);
        transform.position += transform.forward * bulletBase.Data.speed * Time.deltaTime;
    }

    private Transform FindNearestMonster()
    {
        // [에러 해결] Monster 태그를 가진 것들을 찾음
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        if (monsters == null || monsters.Length == 0) return null;

        float closestDist = detectRadius;
        Transform closest = null;

        foreach (var m in monsters)
        {
            // [에러 해결] GetComponent 결과가 null인지 확인하는 로직 추가
            if (m.TryGetComponent<EnemyHealth>(out var health))
            {
                if (health.IsDead) continue;

                float d = Vector3.Distance(transform.position, m.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closest = m.transform;
                }
            }
        }
        return closest;
    }
}