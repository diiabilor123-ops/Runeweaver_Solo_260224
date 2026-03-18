using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Monster Spawning Settings")]
    [SerializeField] private float minSpawnInterval = 1.0f;
    [SerializeField] private float maxSpawnInterval = 2.0f;
    [SerializeField] private float delayAfterEffect = 0.3f;

    [Header("Monster Spawning")]
    public GameObject monsterPrefab;
    public Transform[] spawnPoints;
    public GameObject spawnEffectPrefab;
    public SoundDataSO spawnSound;

    [Header("Maps & Portal")]
    public GameObject currentRoom;
    public GameObject bossRoom;
    public GameObject portalPrefab;
    public Transform portalSpawnPos;
    public Image fadeImage;

    [Header("Player & Boss")]
    public GameObject player;
    public Transform bossRoomSpawnPos;
    public BossBrain bossBrain;

    [Header("Game Over UI")]
    public GameObject gameOverUI; // 인스펙터에서 GameOverPanel 연결

    [Header("Sounds")]
    public SoundDataSO playerDeathSound;
    public SoundDataSO bossBGM;

    private int _monsterCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 씬 시작 시 초기 상태 세팅
        if (bossRoom != null) bossRoom.SetActive(false);
        if (currentRoom != null) currentRoom.SetActive(true);
        if (gameOverUI != null) gameOverUI.SetActive(false);

        // 페이드 인 효과 (검은 화면에서 밝아짐)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
            fadeImage.DOFade(0f, 1f).SetUpdate(true);
        }

        // 게임 시작 시 무조건 첫 번째 방의 몬스터 스폰부터 시작
        StartCoroutine(SpawnMonstersRoutine());
    }

    private IEnumerator SpawnMonstersRoutine()
    {
        _monsterCount = spawnPoints.Length;
        foreach (Transform t in spawnPoints)
        {
            if (spawnEffectPrefab != null) Instantiate(spawnEffectPrefab, t.position, Quaternion.identity);

            if (spawnSound != null && SoundManager.Instance != null)
                SoundManager.Instance.Play(spawnSound, t.position);

            yield return new WaitForSeconds(delayAfterEffect);

            GameObject monster = Instantiate(monsterPrefab, t.position, t.rotation);
            monster.transform.parent = currentRoom.transform;

            // 나타날 때 뿅 하고 커지는 연출
            monster.transform.localScale = Vector3.zero;
            monster.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
        }
    }

    public void OnMonsterDied()
    {
        _monsterCount--;
        if (_monsterCount <= 0) SpawnPortal();
    }

    private void SpawnPortal()
    {
        if (portalPrefab != null)
        {
            GameObject portal = Instantiate(portalPrefab, portalSpawnPos.position, Quaternion.identity);
            portal.transform.localScale = Vector3.zero;
            portal.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
    }

    public void OnPlayerDied()
    {
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // 1. 사운드 처리
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM(1.5f);
            if (playerDeathSound != null)
                SoundManager.Instance.Play(playerDeathSound, player.transform.position);
        }

        yield return new WaitForSeconds(1.5f);

        // 2. 화면 암전 (Time.timeScale이 0이어도 작동하게 SetUpdate(true))
        if (fadeImage != null)
            yield return fadeImage.DOFade(1f, 1f).SetUpdate(true).WaitForCompletion();

        // 3. 게임 정지 및 UI 표시
        Time.timeScale = 0f;
        if (gameOverUI != null) gameOverUI.SetActive(true);
    }

    // [핵심] 버튼 클릭 시 호출: 모든 상태를 무시하고 씬을 새로 읽어옵니다.
    public void OnClickRetry()
    {
        // 1. 시간 배율 복구 (매우 중요: 안 하면 다음 판도 멈춤)
        Time.timeScale = 1f;

        // 2. 씬을 처음부터 다시 로드 (모든 부서진 물체, 몬스터, 변수가 초기화됨)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public IEnumerator ChangeMapRoutine()
    {
        // 포탈 탔을 때 맵 이동 로직
        yield return fadeImage.DOFade(1f, 0.8f).WaitForCompletion();

        currentRoom.SetActive(false);
        bossRoom.SetActive(true);
        player.transform.position = bossRoomSpawnPos.position;

        var bossHealth = bossBrain.GetComponent<EnemyHealth>();
        var bossUI = Object.FindAnyObjectByType<BossHPUI>();
        if (bossUI != null) bossUI.ShowBossUI(bossHealth);

        bossBrain.PauseAI();
        yield return new WaitForSeconds(1.0f);
        yield return fadeImage.DOFade(0f, 0.8f).WaitForCompletion();
        bossBrain.ResumeAI();
    }
}