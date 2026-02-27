using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private Vector3 moveDirection;
    private TextMeshProUGUI textMesh;
    private Transform cameraTransform; // [추가] 카메라 참조

    void Start()
    {
        // 1. 카메라 참조 가져오기 (씬에 메인 카메라가 있어야 함)
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // 좌우 랜덤 퍼짐
        moveDirection = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0).normalized;
        Destroy(gameObject, 0.7f); // 0.7초 후 소멸
    }

    void Update()
    {
        // 팝업 위로 이동
        transform.position += moveDirection * 1f * Time.deltaTime;
    }

    // [핵심] 팝업이 카메라를 바라보게 함
    void LateUpdate()
    {
        if (cameraTransform != null)
        {
            // 팝업이 카메라를 바라보도록 회전 설정
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }
    }
    // [기존 매니저 역할] 정적 함수로 만들어 어디서든 호출 가능하게 함
    public static void SpawnPopup(GameObject prefab, Vector3 position, float damageAmount, bool isCritical, Color textColor)
    {
        if (prefab == null) return;

        // 위치 설정 (머리 위)
        Vector3 spawnPos = position + Vector3.up * 2.5f;
        GameObject popupObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        popupObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        if (popupObj.TryGetComponent<DamagePopup>(out var popupScript))
        {
            popupScript.Setup(damageAmount, isCritical, textColor);
        }
    }

    // [기존 팝업 역할] 텍스트 데이터 설정
    public void Setup(float damageAmount, bool isCritical, Color textColor)
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

            if (isCritical)
            {
                textMesh.color = new Color(1f, 0.8f, 0f); // 노란색
                textMesh.fontSize *= 1.5f;
                transform.localScale *= 1.5f;
            }
            else
            {
                textMesh.color = textColor; // 전달받은 색상
            }
        }
    }
}