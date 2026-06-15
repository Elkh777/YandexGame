using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Волновой спавнер: вместо непрерывного рандома спавнит врагов волнами из-за краёв экрана,
// масштабирует их HP по номеру волны/уровня, между волнами даёт передышку с UI-таймером,
// а по зачистке всех волн открывает портал на уровне.
public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [Header("Волны")]
    public int baseWavesPerLevel = 3;     // волн на уровне = base + индекс уровня
    public int baseEnemiesPerWave = 3;    // врагов в волне = base + номер волны + индекс уровня
    public float restBetweenWaves = 6f;   // передышка между волнами, сек
    public float spawnInterval = 0.35f;   // задержка между появлением врагов внутри волны

    [Header("Масштаб сложности")]
    public float hpPerWave = 0.2f;        // baseHP * (1 + wave*0.2 + level*0.3)
    public float hpPerLevel = 0.3f;

    [Header("Спавн за краем экрана")]
    public float offscreenMargin = 2.5f;
    public string enemyPrefabPath = "Sprites/Prefabs/Enemy";
    public string[] enemySpritePaths =
    {
        "Sprites/enemy1", "Sprites/enemy2", "Sprites/enemy3",
        "Sprites/enemy4", "Sprites/enemy5", "Sprites/enemy6",
    };

    private Transform _player;
    private Camera _camera;
    private GameObject _enemyPrefab;
    private Sprite[] _enemySprites;
    private float _minX, _maxX;
    private readonly List<Enemy> _waveEnemies = new List<Enemy>();
    private TextMeshProUGUI _waveText;
    private Coroutine _routine;

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
        EnsureRefs();
        EnsureUI();
    }

    private void EnsureRefs()
    {
        if (_player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
        if (_camera == null) _camera = Camera.main;
        if (_enemyPrefab == null) _enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        if (_enemySprites == null)
        {
            _enemySprites = new Sprite[enemySpritePaths.Length];
            for (int i = 0; i < enemySpritePaths.Length; i++)
            {
                Sprite[] loaded = Resources.LoadAll<Sprite>(enemySpritePaths[i]);
                if (loaded != null && loaded.Length > 0) _enemySprites[i] = PickLargest(loaded);
            }
        }
    }

    // Запускает волны для уровня. Вызывается из LevelManager.InitializeLevel.
    public void BeginLevel(int levelIndex, float minX, float maxX)
    {
        _minX = minX;
        _maxX = maxX;
        EnsureRefs();

        StopAllCoroutines();
        _waveEnemies.Clear();

        // Защита от софт-лока: если спавнить нечем — сразу открываем портал.
        if (_enemyPrefab == null || _player == null)
        {
            SetWaveText("");
            LevelManager.Instance?.ActivatePortal();
            return;
        }

        _routine = StartCoroutine(RunLevel(levelIndex));
    }

    private IEnumerator RunLevel(int levelIndex)
    {
        int waves = Mathf.Max(1, baseWavesPerLevel + levelIndex);

        for (int wave = 0; wave < waves; wave++)
        {
            int count = baseEnemiesPerWave + wave + levelIndex;
            float hpMult = 1f + wave * hpPerWave + levelIndex * hpPerLevel;

            SetWaveText($"Волна {wave + 1}/{waves}");
            yield return StartCoroutine(SpawnWave(count, hpMult));

            // Ждём, пока все враги волны не будут уничтожены.
            while (CountAlive() > 0)
            {
                SetWaveText($"Волна {wave + 1}/{waves}    Врагов: {CountAlive()}");
                yield return null;
            }

            // Передышка с обратным отсчётом (кроме последней волны).
            if (wave < waves - 1)
            {
                float t = restBetweenWaves;
                while (t > 0f)
                {
                    SetWaveText($"Следующая волна через: {Mathf.CeilToInt(t)}");
                    t -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        SetWaveText("ПОРТАЛ ОТКРЫТ!");
        LevelManager.Instance?.ActivatePortal();
        yield return new WaitForSeconds(3f);
        SetWaveText("");
    }

    private IEnumerator SpawnWave(int count, float hpMult)
    {
        _waveEnemies.Clear();
        for (int i = 0; i < count; i++)
        {
            SpawnOne(hpMult);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne(float hpMult)
    {
        if (_enemyPrefab == null || _player == null) return;

        Vector3 pos = PickSpawnPosition();
        GameObject obj = Instantiate(_enemyPrefab, pos, Quaternion.identity);
        obj.tag = "Enemy";

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 1;

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.ApplyKind(PickKind(), hpMult);
            enemy.bodySprite = PickSprite();
            enemy.spriteFacesRight = true;
            _waveEnemies.Add(enemy);
        }
    }

    // Позиция за краем видимой области: слева, справа (на уровне игрока) или сверху (падает вниз).
    private Vector3 PickSpawnPosition()
    {
        float halfH = _camera != null ? _camera.orthographicSize : 5f;
        float halfW = halfH * (_camera != null ? _camera.aspect : 1.78f);
        Vector3 camPos = _camera != null ? _camera.transform.position : _player.position;

        int side = Random.Range(0, 3); // 0 — слева, 1 — справа, 2 — сверху
        float x, y;
        if (side == 0) { x = camPos.x - halfW - offscreenMargin; y = _player.position.y; }
        else if (side == 1) { x = camPos.x + halfW + offscreenMargin; y = _player.position.y; }
        else { x = _player.position.x + Random.Range(-5f, 5f); y = camPos.y + halfH + offscreenMargin; }

        x = Mathf.Clamp(x, _minX, _maxX);
        return new Vector3(x, y, 0f);
    }

    private EnemyKind PickKind()
    {
        float r = Random.value;
        if (r < 0.2f) return EnemyKind.Tank;
        if (r < 0.6f) return EnemyKind.Rusher;
        return EnemyKind.Normal;
    }

    private int CountAlive()
    {
        int n = 0;
        for (int i = _waveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy e = _waveEnemies[i];
            if (e == null || e.IsDead) _waveEnemies.RemoveAt(i);
            else n++;
        }
        return n;
    }

    private Sprite PickSprite()
    {
        if (_enemySprites == null || _enemySprites.Length == 0) return null;
        List<Sprite> available = new List<Sprite>();
        foreach (Sprite s in _enemySprites) if (s != null) available.Add(s);
        return available.Count > 0 ? available[Random.Range(0, available.Count)] : null;
    }

    private Sprite PickLargest(Sprite[] sprites)
    {
        Sprite best = null;
        float bestArea = -1f;
        foreach (Sprite s in sprites)
        {
            if (s == null) continue;
            float area = s.rect.width * s.rect.height;
            if (area > bestArea) { bestArea = area; best = s; }
        }
        return best;
    }

    private void EnsureUI()
    {
        if (_waveText != null) return;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject obj = new GameObject("WaveStatus");
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -120f); // под индикатором уровня
        rect.sizeDelta = new Vector2(600f, 50f);

        _waveText = obj.AddComponent<TextMeshProUGUI>();
        _waveText.fontSize = 28;
        _waveText.color = new Color(1f, 0.85f, 0.4f);
        _waveText.alignment = TextAlignmentOptions.Center;
        _waveText.fontStyle = FontStyles.Bold;
        _waveText.textWrappingMode = TextWrappingModes.NoWrap;
        _waveText.text = "";
    }

    private void SetWaveText(string text)
    {
        EnsureUI();
        if (_waveText != null) _waveText.text = text;
    }
}
