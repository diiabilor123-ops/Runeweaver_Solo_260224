using UnityEngine;

/// <summary>
/// 생성된 잔상이 서서히 투명해지다가 스스로 파괴되게 만드는 스크립트입니다.
/// </summary>
public class FadeOutDestroy : MonoBehaviour
{
    private Renderer[] _renderers;
    private float _alpha = 1f;

    [Header("Settings")]
    public float fadeSpeed = 3f; // 사라지는 속도 (높을수록 빨리 사라짐)
    public float shrinkSpeed = 0.5f;  // 크기가 줄어드는 속도 (하데스 특유의 소멸감)

    private void Awake()
    {
        // 자식에 있는 모든 렌더러를 가져옵니다.
        _renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        // 1. 시간에 따라 알파값 감소
        _alpha -= Time.deltaTime * fadeSpeed;

        // 2. 하데스식 디테일: 크기를 서서히 줄여서 '소멸'하는 느낌 강조
        transform.localScale *= (1f - shrinkSpeed * Time.deltaTime);


        // 3. lilToon이나 URP 머티리얼의 컬러를 업데이트
        // (색상은 유지하고 알파값만 갱신)
        foreach (var renderer in _renderers)
        {
            if (renderer != null && renderer.material != null)
            {
                Color color = renderer.material.color;
                color.a = _alpha;
                renderer.material.color = color;

                // [하데스 핵심: lilToon 발광(Emission) 조절]
                // 잔상이 그냥 어두워지는 게 아니라 빛이 사그라드는 느낌을 줍니다.
                // 셰이더 프로퍼티 이름이 보통 "_EmissionColor"입니다.
                if (renderer.material.HasProperty("_EmissionColor"))
                {
                    Color emissionColor = renderer.material.GetColor("_EmissionColor");
                    // 알파값에 따라 발광 강도를 함께 낮춥니다.
                    renderer.material.SetColor("_EmissionColor", emissionColor * _alpha);
                }
            }
        }

        // 3. 완전히 투명해지면 오브젝트 파괴
        if (_alpha <= 0)
        {
            Destroy(gameObject);
        }
    }
}