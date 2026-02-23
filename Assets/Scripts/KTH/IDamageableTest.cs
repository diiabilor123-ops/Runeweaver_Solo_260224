using UnityEngine;

/// <summary>
/// 역할: 데미지를 입을 수 있는 모든 객체의 공통 인터페이스입니다.
/// 특징: 특정 클래스에 종속되지 않으므로, 투사체와 피격 대상 간의 결합도를 낮춥니다.
/// </summary>
public interface IDamageableTest
{
    /// <summary>
    /// 데미지를 입히는 표준 메서드입니다.
    /// </summary>
    /// <param name="amount">전달할 데미지 수치</param>
    void TakeDamage(float amount);
}