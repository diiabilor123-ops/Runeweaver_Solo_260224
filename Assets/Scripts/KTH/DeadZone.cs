// DeadZone.cs
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어 추락사!");
            // 리스폰 로직 또는 게임오버 띄우기
        }
        else if (other.CompareTag("Enemy"))
        {
            Debug.Log("보스 추락사! 승리!");
            // 보스 사망 처리
        }
    }
}