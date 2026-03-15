using UnityEngine;
using System.Collections.Generic;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private FootstepSetSO defaultSet; // 기본 발소리 세트
    [SerializeField] private List<FootstepSetSO> surfaceSets; // 재질별 세트 목록

    [Header("Foot Transforms")]
    [SerializeField] private Transform leftFoot;  // 왼쪽 발 위치 (뼈대 연결)
    [SerializeField] private Transform rightFoot; // 오른쪽 발 위치 (뼈대 연결)

    // 애니메이션 이벤트에서 이 함수를 호출할 겁니다.
    // 인자(int footIndex)는 0: 왼쪽, 1: 오른쪽으로 약속합니다.
    public void OnFootstep(int footIndex)
    {
        Transform targetFoot = (footIndex == 0) ? leftFoot : rightFoot;
        if (targetFoot == null) return;

        // 1. 바닥 재질 체크 (Raycast)
        FootstepSetSO currentSet = defaultSet;
        RaycastHit hit;

        // 발 위치에서 아래로 레이를 쏴서 바닥 태그를 확인합니다.
        if (Physics.Raycast(targetFoot.position + Vector3.up * 0.5f, Vector3.down, out hit, 1f))
        {
            var found = surfaceSets.Find(s => hit.collider.CompareTag(s.surfaceTag));
            if (found != null) currentSet = found;
        }

        // 2. 사운드 재생
        if (currentSet != null && currentSet.stepSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(currentSet.stepSound, targetFoot.position);
        }

        // 3. 먼지 파티클 생성
        if (currentSet != null && currentSet.dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(currentSet.dustEffectPrefab, targetFoot.position, Quaternion.identity);
            Destroy(dust, 1.5f); // 넉넉히 1.5초 뒤 삭제
        }
    }
}