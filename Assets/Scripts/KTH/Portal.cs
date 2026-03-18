using UnityEngine;

public class Portal : MonoBehaviour
{
    private bool _isUsed = false;

    private void OnTriggerEnter(Collider medical)
    {
        if (!_isUsed && medical.CompareTag("Player"))
        {
            _isUsed = true;
            StartCoroutine(LevelManager.Instance.ChangeMapRoutine());
        }
    }
}