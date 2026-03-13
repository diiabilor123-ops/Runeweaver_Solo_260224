using UnityEngine;
using System.Collections.Generic;

public class FadeOutDestroy : MonoBehaviour
{
    private GameObject _originPrefab;
    private List<Material> _cachedMaterials = new List<Material>(); // 머티리얼 캐싱용
    private List<Mesh> _meshesToDestroy;
    private float _alpha = 1f;
    private float _fadeSpeed;
    private bool _isInitialized = false;

    [Header("Settings")]
    public float shrinkSpeed = 0.5f;

    // 초기화 시 머티리얼을 단 한 번만 생성/복사합니다.
    public void Init(GameObject prefab, List<MeshRenderer> renderers, List<Mesh> meshes, float speed)
    {
        _originPrefab = prefab;
        _meshesToDestroy = meshes;
        _fadeSpeed = speed;

        // [중요] 모든 상태 초기화
        _alpha = 1f;
        _isInitialized = true;

        _cachedMaterials.Clear();
        foreach (var mr in renderers)
        {
            if (mr != null)
            {
                // .material에 접근하면 인스턴스가 생성됩니다. 이를 리스트에 저장해둡니다.
                _cachedMaterials.Add(mr.material);
            }
        }

        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        _alpha -= Time.deltaTime * _fadeSpeed;

        // 하데스 스타일: 서서히 작아짐
        transform.localScale *= (1f - shrinkSpeed * Time.deltaTime);

        // 저장해둔 머티리얼들의 알파값만 수정 (매 프레임 생성 방지)
        foreach (var mat in _cachedMaterials)
        {
            if (mat == null) continue;

            Color color = mat.color;
            color.a = _alpha;
            mat.color = color;

            if (mat.HasProperty("_EmissionColor"))
            {
                Color emColor = mat.GetColor("_EmissionColor");
                mat.SetColor("_EmissionColor", emColor * (_alpha * _alpha));
            }
        }

        if (_alpha <= 0)
        {
            ReturnToPool();
        }

    }

    private void ReturnToPool()
    {
        _isInitialized = false;

        // 1. 구워진 메쉬 제거 (메모리 누수 방지)
        if (_meshesToDestroy != null)
        {
            foreach (var mesh in _meshesToDestroy) if (mesh != null) Destroy(mesh);
            _meshesToDestroy.Clear();
        }

        // 2. 생성된 머티리얼 복사본 제거 (메모리 누수 방지 핵심!)
        foreach (var mat in _cachedMaterials) if (mat != null) Destroy(mat);
        _cachedMaterials.Clear();

        // 3. 자식 오브젝트 정리
        foreach (Transform child in transform) Destroy(child.gameObject);

        // 4. 풀 반납
        if (PoolManager.Instance != null && _originPrefab != null)
        {
            PoolManager.Instance.Release(_originPrefab, gameObject);
        }
    }
}