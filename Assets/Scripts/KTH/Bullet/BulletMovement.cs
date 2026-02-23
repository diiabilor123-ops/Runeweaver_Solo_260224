using UnityEngine;

/// <summary>
/// 소제목: 독립적 이동 컴포넌트 (Movement)
/// 역할: BaseProjectile의 정보를 바탕으로 실제 좌표를 이동시킵니다.
/// 특징: 충돌이나 삭제 로직은 전혀 모르며, 오직 '이동' 기능만 수행합니다.
/// </summary>
public class BulletMovement : MonoBehaviour
{
    private BulletBase _base;
    private Vector3 _startPosition;

    private void Awake()
    {
        _base = GetComponent<BulletBase>();
    }

    private void OnEnable()
    {
        // 발사 시점의 위치 기록
        _startPosition = transform.position;
    }

    private void Update()
    {
        // 데이터 허브가 없거나 비활성 상태면 이동하지 않음
        if (_base == null || !_base.IsActive) return;

        // 방향과 속도를 곱해 순수하게 이동만 실행 (중력 미사용)
        transform.position += _base.Direction * _base.Data.speed * Time.deltaTime;

        // 투사체가 날아가는 방향을 바라보게 함
        if (_base.Direction != Vector3.zero)
            transform.forward = _base.Direction;

        // 사거리 체크: SO에 설정된 maxDistance를 사용
        float traveledDistance = Vector3.Distance(_startPosition, transform.position);
        if (traveledDistance >= _base.Data.maxDistance)
        {
            _base.Deactivate();
        }
    }
}