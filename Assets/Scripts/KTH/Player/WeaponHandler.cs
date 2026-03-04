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

            // 1. 메인 화살 생성 (0~3스택: 일반, 4스택↑: 강화)
            GameObject mainPrefab = BulletManager.Instance.GetMainArrowPrefab();
            var convertedElements = augment.GetConvertedElements();
            SpawnProjectile(mainPrefab, firePoint.forward, slot, null, convertedElements);

            // 2. 불 화살 (치명타 100% + [수정] 최소 2스택 이상일 때만)
            if (isCrit && augment.GetStack(ElementType.Fire) >= 2)
            {
                SpawnHoming(ElementType.Fire, slot);
            }

            // 3. 얼음 화살 (확률 + [수정] 최소 2스택 이상일 때만)
            if (augment.GetStack(ElementType.Ice) >= 2 && Random.value < augment.iceSpawnChance)
            {
                SpawnHoming(ElementType.Ice, slot);
            }

            // 4. 번개 화살 (확률 + [수정] 최소 2스택 이상일 때만)
            float voltChance = augment.voltBaseChance + (extraAttackSpeed * augment.voltASWeight);
            if (augment.GetStack(ElementType.Volt) >= 2 && Random.value < voltChance)
            {
                SpawnHoming(ElementType.Volt, slot);
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

            GameObject prefab = BulletManager.Instance.GetHomingPrefab(type);
            if (prefab != null) SpawnProjectile(prefab, firePoint.forward, slot, spawnPos);
        }

        /// <summary>
        /// 최종적으로 BulletBase에 데이터를 주입하여 오브젝트를 생성합니다.
        /// </summary>
        // SpawnProjectile에 원소 리스트를 직접 주입할 수 있게 오버로딩/수정
        private void SpawnProjectile(GameObject prefab, Vector3 direction, SkillSlotType slot, Vector3? customPos = null, List<ElementType> customElements = null)
        {
            Vector3 finalPos = customPos ?? firePoint.position;
            GameObject go = Instantiate(prefab, finalPos, Quaternion.LookRotation(direction));

            if (go.TryGetComponent<BulletBase>(out var bullet))
            {
                var bulletData = BulletManager.Instance.GetCurrentEquippedData();
                // 주입할 원소가 따로 지정되지 않았다면 슬롯의 전체 원소를 가져옴
                var elements = customElements ?? PlayerAugment.Instance.GetSortedElements(slot);
                bullet.Setup(bulletData, direction, elements, slot);
            }
        }
    }
}