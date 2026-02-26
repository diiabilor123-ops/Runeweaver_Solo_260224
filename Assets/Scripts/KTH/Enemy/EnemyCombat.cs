using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// [몬스터 전투 실행부]
/// 실제 공격 로직(돌진, 평타), 데미지 판정, 공격 애니메이션 실행을 담당합니다.
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    private Animator anim;
    private EnemyData data;
    private Transform player;
    private bool isAttacking = false; // 중복 공격 방지

    public bool IsAttacking => isAttacking;

    public void Init(EnemyData data, Transform player)
    {
        this.data = data;
        this.player = player;
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 하데스식 직선 돌진 공격을 실행합니다.
    /// </summary>
    /// <param name="direction">돌진 방향</param>
    /// <param name="onComplete">돌진 종료 후 실행할 콜백 (예: Cooldown 상태로 전환)</param>
    public void StartCharge(Vector3 direction, Action onComplete)
    {
        if (isAttacking) return;
        StartCoroutine(ChargeRoutine(direction, onComplete));
    }

    private IEnumerator ChargeRoutine(Vector3 dir, Action onComplete)
    {
        isAttacking = true;


        // 1. 애니메이션 설정
        if (anim != null)
        {
            anim.SetBool("IsDashing", true);
            anim.Play("FastRun", 0, 0f);
        }

        // 2. 목표 지점 계산 (플레이어 너머로)
        Vector3 targetPos = transform.position + (dir * (data.attackRange + 3.0f));
        float chargeSpeed = data.moveSpeed * 3.5f;
        bool hasDealtDamage = false;

        float elapsed = 0f;
        while (elapsed < 1.5f) // 최대 지속 시간 1.5초
        {
            float distToTarget = Vector3.Distance(transform.position, targetPos);
            if (distToTarget < 0.2f) break;

            // 이동 실행
            transform.position = Vector3.MoveTowards(transform.position, targetPos, chargeSpeed * Time.deltaTime);

            // 3. 실시간 데미지 판정 (돌진 중 플레이어와 닿으면)
            if (!hasDealtDamage && Vector3.Distance(transform.position, player.position) < 1.2f)
            {
                // 플레이어가 IDamageable을 가지고 있는지 확인합니다.
                if (player.TryGetComponent<IDamageable>(out var target))
                {
                    // 몬스터의 데미지와 본인의 속성(data.mainElement)을 함께 전달합니다.
                    // 세 번째 인자로 Team.Enemy를 전달합니다.
                    target.TakeDamage(data.attackDamage, data.mainElement, Team.Enemy);
                    hasDealtDamage = true;
                    Debug.Log($"{gameObject.name}이(가) 돌진으로 플레이어를 타격!");
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4. 종료 처리
        if (anim != null) anim.SetBool("IsDashing", false);
        isAttacking = false;
        onComplete?.Invoke(); // Brain에게 완료 알림
    }

    /// <summary>
    /// 근접 기본 공격을 실행합니다.
    /// </summary>
    public void StartMeleeAttack(Action onComplete)
    {
        if (isAttacking) return;
        StartCoroutine(MeleeAttackRoutine(onComplete));
    }

    private IEnumerator MeleeAttackRoutine(Action onComplete)
    {
        isAttacking = true;
        if (anim != null) anim.SetTrigger("AttackTrigger");

        // 애니메이션 중간에 데미지 판정 (타이밍 조절 가능)
        yield return new WaitForSeconds(0.5f);

        if (Vector3.Distance(transform.position, player.position) <= 2.2f)
        {
            if (player.TryGetComponent<IDamageable>(out var target))
            {
                // 마찬가지로 Team.Enemy 전달
                target.TakeDamage(data.attackDamage * 0.5f, data.mainElement, Team.Enemy);
            }
        }

        yield return new WaitForSeconds(0.7f); // 남은 후딜레이
        isAttacking = false;
        onComplete?.Invoke();
    }
}