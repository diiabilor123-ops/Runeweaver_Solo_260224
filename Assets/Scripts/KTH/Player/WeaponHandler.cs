using UnityEngine;
using System.Collections.Generic;
using Runeweaver.Augment;

namespace Runeweaver.Player
{
    /// <summary>
    /// [발사기 담당] 투사체 생성, 원소 시너지 확률 계산, 특수 스킬 자동 발동을 관리합니다.
    /// </summary>
    public class WeaponHandler : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;

        /// <summary>
        /// 메인 화살, 유도 화살, 자동 Q스킬 등 모든 발사 시퀀스를 실행합니다.
        /// </summary>
        public void ExecuteAttack(SkillSlotType slot, bool isCrit, float extraAttackSpeed)
        {
            var augment = PlayerAugment.Instance.leftClick;

            // 1. 메인 화살 생성
            GameObject mainPrefab = BulletManager.Instance.GetMainArrowPrefab();
            // 4스택 이상인 원소들만 메인 화살에 비주얼/효과로 주입
            var convertedElements = augment.GetConvertedElements();
            SpawnProjectile(mainPrefab, firePoint.forward, slot, null, convertedElements);

            // 2. 유도 화살 로직 (기타 슬롯이 아닌 기본 공격 슬롯일 때만 실행)
            if (slot == SkillSlotType.LeftClick)
            {
                // 현재 스택이 하나라도 쌓인 모든 원소 타입을 루프
                foreach (var element in augment.GetOwnedElementTypes())
                {
                    // [수정 포인트] AugmentLeftClick에 만든 확률 계산기 활용
                    float chance = augment.GetHomingChance(element, isCrit, extraAttackSpeed);

                    if (Random.value < chance)
                    {
                        SpawnHoming(element, slot);
                    }
                }
            }
        }


        /// <summary>
        /// 원소 특성에 따라 생성 위치를 다르게 하여 유도탄을 소환합니다.
        /// </summary>
        private void SpawnHoming(ElementType type, SkillSlotType slot)
        {
            Vector3 spawnPos = firePoint.position;
            switch (type)
            {
                case ElementType.Volt: spawnPos += transform.right * 1.0f; break; // 옆에서 생성
                case ElementType.Ice: spawnPos += Vector3.up * 1.5f; break;       // 위에서 생성
                case ElementType.Fire: spawnPos += transform.right * Random.Range(-0.5f, 0.5f) + transform.up * Random.Range(0, 0.5f); break; // 주변 랜덤
            }

            // 1. 프리팹 가져오기
            GameObject prefab = BulletManager.Instance.GetHomingPrefab(type);

            // [여기가 핵심!] homingData 변수를 선언하고 데이터를 가져옵니다.
            // BulletManager에 GetHomingData 함수가 이미 만들어져 있어야 합니다.
            BulletDataSO homingData = BulletManager.Instance.GetHomingData(type);

            if (prefab != null)
            {
                // 이제 homingData 변수를 사용할 수 있습니다.
                SpawnProjectile(prefab, firePoint.forward, slot, spawnPos, null, homingData);
            }
        }

        /// <summary>
        /// 최종적으로 BulletBase에 데이터를 주입하여 오브젝트를 생성합니다.
        /// </summary>
        // SpawnProjectile에 원소 리스트를 직접 주입할 수 있게 오버로딩/수정
        private void SpawnProjectile(GameObject prefab, Vector3 direction, SkillSlotType slot, Vector3? customPos = null, List<ElementType> customElements = null, BulletDataSO overrideData = null)
        {
            if (prefab == null) return;

            Vector3 finalPos = customPos ?? firePoint.position;

            // direction이 zero일 경우를 대비한 방어 로직
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            Quaternion finalRot = Quaternion.LookRotation(direction);

            // 1. 풀에서 오브젝트 가져오기
            GameObject go = PoolManager.Instance.Get(prefab, finalPos, finalRot);

            // [수정 포인트] GetComponent 대신 GetComponentInChildren을 사용하여 더 확실하게 찾습니다.
            BulletBase bullet = go.GetComponent<BulletBase>();
            if (bullet == null) bullet = go.GetComponentInChildren<BulletBase>();

            if (bullet != null)
            {
                // 2. 데이터 준비
                var bulletData = overrideData ?? BulletManager.Instance.GetCurrentEquippedData();
                var elements = customElements ?? PlayerAugment.Instance.GetSortedElements(slot);

                // [추가] 머즐 이펙트(shootVFX) 생성
                if (bulletData.shootVFX != null)
                {
                    // 발사 위치에 이펙트 생성 후 자동 파괴되도록 설정 (프리팹 자체에 파괴 로직이 없다면 아래처럼 처리)
                    GameObject vfx = Instantiate(bulletData.shootVFX, finalPos, finalRot);
                    Destroy(vfx, 1.0f);
                }

                // 3. Setup 호출 (이게 실행되어야 디버그 로그가 뜹니다!)
                bullet.Setup(bulletData, direction, elements, slot, prefab);
            }
            else
            {
                Debug.LogError($"[WeaponHandler] {prefab.name} 프리팹에서 BulletBase를 찾을 수 없습니다!");
            }
        }
    }
}