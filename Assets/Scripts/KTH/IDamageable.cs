using UnityEngine;

/// <summary>
/// 역할: 데미지를 입을 수 있는 모든 객체의 공통 인터페이스입니다.
/// 특징: 특정 클래스에 종속되지 않으므로, 투사체와 피격 대상 간의 결합도를 낮춥니다.
/// 팀장님의 원소 시스템(ElementType)을 인자로 추가하여 상성 계산이 가능하게 합니다.
/// </summary>


// 1. 팀 구분을 위한 열거형 추가
public enum Team { Player, Enemy, Neutral }


public interface IDamageable
{
    // [수정] 이제 모든 데미지 전달은 HitData 보따리 하나로 통일합니다.
    void TakeDamage(HitData hitData);

}

