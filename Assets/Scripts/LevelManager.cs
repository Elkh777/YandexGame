using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Фоны уровней (SpriteRenderer)")]
    public GameObject[] levelBackgrounds;

    [Header("Декорации уровней (корневые GameObjects)")]
    public GameObject[] levelDecorations;

    [Header("Точки спавна игрока")]
    public Transform[] spawnPoints;

    [Header("Y-координата земли для спавна врагов")]
    public float[] levelGroundY;

    [Header("Границы камеры")]
    public float[] levelMinX;
    public float[] levelMaxX;

    [Header("Настройки перехода")]
    public float transitionDelay = 0.3f;
    public int totalLevels = 3;

    private int _currentLevel = 0;
    private GameObject _player;
    private CameraFollow _cameraFollow;
    private EnemySpawner _enemySpawner;

    public int CurrentLevel => _currentLevel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        _enemySpawner = FindFirstObjectByType<EnemySpawner>();

        InitializeLevel(0);
    }

    public void InitializeLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= totalLevels) return;

        _currentLevel = levelIndex;

        for (int i = 0; i < levelBackgrounds.Length; i++)
        {
            if (levelBackgrounds[i] != null)
                levelBackgrounds[i].SetActive(i == levelIndex);
        }

        for (int i = 0; i < levelDecorations.Length; i++)
        {
            if (levelDecorations[i] != null)
                levelDecorations[i].SetActive(i == levelIndex);
        }

        if (_player != null && spawnPoints != null && levelIndex < spawnPoints.Length)
        {
            if (spawnPoints[levelIndex] != null)
            {
                _player.transform.position = spawnPoints[levelIndex].position;

                Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }

        if (_cameraFollow != null && levelMinX != null && levelMaxX != null
            && levelIndex < levelMinX.Length && levelIndex < levelMaxX.Length)
        {
            _cameraFollow.minX = levelMinX[levelIndex];
            _cameraFollow.maxX = levelMaxX[levelIndex];
        }

        if (_enemySpawner != null && levelGroundY != null && levelIndex < levelGroundY.Length)
        {
            _enemySpawner.groundY = levelGroundY[levelIndex];
        }

        Debug.Log($"[LevelManager] Загружен уровень {levelIndex + 1}");
    }

    public void GoToNextLevel()
    {
        if (_currentLevel + 1 >= totalLevels)
        {
            OnFinalLevelComplete();
            return;
        }

        StartCoroutine(TransitionToNextLevel());
    }

    private IEnumerator TransitionToNextLevel()
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(transitionDelay);

        InitializeLevel(_currentLevel + 1);

        Time.timeScale = 1f;
    }

    private void OnFinalLevelComplete()
    {
        Debug.Log("[LevelManager] Последний уровень пройден! GAME WIN!");

        if (fGameManager.Instance != null)
        {
            fGameManager.Instance.ShowGameWin();
        }
        else
        {
            Time.timeScale = 0f;
        }
    }
}
