using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    void Start()
    {
        // 소환되자마자 0.7초 뒤에 죽으라고 예약함
        Destroy(gameObject, 0.7f);
    }
    public void Setup(float damageAmount)
    {
        var textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
        }
    }

    void Update()
    {
        // 매 프레임 조금씩 위로 이동
        transform.Translate(Vector3.up * Time.deltaTime * 1.5f);

        // 2. [추가] 항상 메인 카메라를 바라보게 함
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }
}