using UnityEngine;
using Runeweaver; // 추가

public class ElementItem : MonoBehaviour
{
    public ElementType elementType; // 이 아이템이 어떤 원소인지 (인스펙터에서 설정)

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 증강 매니저에게 "원소 먹었어!"라고 알림
            AugmentManager.Instance.OnElementPicked(elementType);

            // 2. 아이템 삭제
            Destroy(gameObject);
        }
    }
}