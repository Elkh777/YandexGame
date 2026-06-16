using System.Collections;
using UnityEngine;
using TMPro;

// Сдержанная система энкаунтеров (вместо спама волн). Игрок продвигается по уровню;
// при входе в зону спавнится 1-3 врага (иногда «Элитный»). Энкаунтер считается зачищенным,
// когда живых Enemy не осталось. Затем — пауза 3-5с, и следующий. После всех — открывается портал.
public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance { get; private set; }

    [Header("Энкаунтеры")]
    public int baseEncounters = 5;          // энкаунтеров на уровне = base + индекс уровня (чуть почаще)
    public int minEnemiesPerEncounter = 2;
    public int maxEnemiesPerEncounter = 3;  // группа маленькая — без толпы
    public float restAfterClear = 2.2f;     // короткая передышка после зачистки (чтобы стычки были почаще)
    public float spawnInterval = 0.4f;      // задержка между появлением врагов внутри энкаунтера
    [Range(0f, 1f)] public float eliteChance = 0.3f;

    [Header("Сложность")]
    public float hpPerLevel = 0.3f;
    public float hpPerEncounter = 0.1f;
    public float eliteHpMultiplier = 2.2f;

    [Header("Честный спавн")]
    public float telegraphTime = 0.8f;      // длительность предупреждения перед материализацией
    public float minSpawnDistance = 4f;     // не спавнить ближе этого радиуса к игроку

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
    private TextMeshProUGUI _statusText;
    private int _pendingSpawns = 0; // враги в фазе телеграфа (ещё не материализованы)
    private static Sprite _telegraphSprite;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

    // Запускает энкаунтеры уровня. Вызывается из LevelManager.InitializeLevel.
    public void BeginLevel(int levelIndex, float minX, float maxX)
    {
        _minX = minX;
        _maxX = maxX;
        EnsureRefs();

        StopAllCoroutines();
        _pendingSpawns = 0; // сбрасываем, иначе прерванные телеграфы заблокируют открытие портала

        if (_enemyPrefab == null || _player == null)
        {
            SetStatus("");
            LevelManager.Instance?.ActivatePortal();
            return;
        }

        StartCoroutine(RunLevel(levelIndex));
    }

    private IEnumerator RunLevel(int levelIndex)
    {
        int total = Mathf.Max(1, baseEncounters + levelIndex);
        float pad = 6f;
        float left = _minX + pad;
        float right = _maxX - pad;

        for (int i = 0; i < total; i++)
        {
            float triggerX = total > 1 ? Mathf.Lerp(left, right, i / (float)(total - 1)) : (left + right) * 0.5f;

            // Ждём, пока игрок войдёт в зону энкаунтера.
            SetStatus("Двигайтесь вперёд...");
            while (_player != null && _player.position.x < triggerX)
                yield return null;

            // Спавним сдержанную группу 1-3 (иногда с элитой). Каждый — через фазу телеграфа.
            SetStatus("Внимание! Враги приближаются...");
            int count = Random.Range(minEnemiesPerEncounter, maxEnemiesPerEncounter + 1);
            bool eliteSpawned = false;
            for (int n = 0; n < count; n++)
            {
                bool elite = !eliteSpawned && Random.value < eliteChance;
                if (elite) eliteSpawned = true;
                StartCoroutine(SpawnOneRoutine(levelIndex, i, elite));
                yield return new WaitForSeconds(spawnInterval);
            }

            // Ждём, пока все враги материализуются (телеграф) и будут зачищены.
            while (_pendingSpawns > 0 || CountAliveEnemies() > 0)
            {
                int alive = CountAliveEnemies();
                SetStatus(alive > 0 ? $"Враги: {alive}" : "Внимание!");
                yield return null;
            }

            SetStatus("Зачищено!");
            if (i < total - 1)
            {
                float t = restAfterClear;
                while (t > 0f) { t -= Time.deltaTime; yield return null; }
            }
        }

        // Ворота стоят в конце уровня статично (их ставит LevelManager при загрузке), поэтому здесь
        // их больше не «открываем» — просто сообщаем, что путь свободен.
        SetStatus("Путь свободен — к воротам!");
        yield return new WaitForSeconds(3f);
        SetStatus("");
    }

    // Телеграф: показываем мерцающий силуэт в точке появления, и только потом материализуем врага.
    private IEnumerator SpawnOneRoutine(int levelIndex, int encounterIndex, bool elite)
    {
        _pendingSpawns++;
        Vector3 pos = PickSpawnPosition();

        GameObject tele = CreateTelegraph(pos, elite);
        float baseScale = elite ? 2.4f : 1.6f;
        float t = 0f;
        while (t < telegraphTime)
        {
            t += Time.deltaTime;
            if (tele != null)
            {
                SpriteRenderer sr = tele.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float pulse = Mathf.Abs(Mathf.Sin(t * 13f));
                    Color c = sr.color;
                    c.a = 0.25f + 0.45f * pulse;
                    sr.color = c;
                    tele.transform.localScale = Vector3.one * baseScale * (0.85f + 0.18f * pulse);
                }
            }
            yield return null;
        }
        if (tele != null) Destroy(tele);

        SpawnEnemyAt(pos, levelIndex, encounterIndex, elite);
        _pendingSpawns--;
    }

    private void SpawnEnemyAt(Vector3 pos, int levelIndex, int encounterIndex, bool elite)
    {
        if (_enemyPrefab == null || _player == null) return;

        GameObject obj = Instantiate(_enemyPrefab, pos, Quaternion.identity);
        obj.tag = "Enemy";

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 1;

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy == null) return;

        float hpMult = 1f + levelIndex * hpPerLevel + encounterIndex * hpPerEncounter;

        if (elite)
        {
            enemy.ApplyKind(EnemyKind.Tank, hpMult * eliteHpMultiplier);
            enemy.visualHeight *= 1.25f;                       // крупнее обычного
            enemy.tintColor = new Color(1f, 0.55f, 0.3f);      // оранжевый — «элитный»
            enemy.scoreReward = 60;
        }
        else
        {
            // Обычный энкаунтер: чаще Normal/Rusher, иногда Tank.
            float r = Random.value;
            EnemyKind kind = r < 0.25f ? EnemyKind.Tank : (r < 0.6f ? EnemyKind.Rusher : EnemyKind.Normal);
            enemy.ApplyKind(kind, hpMult);
        }

        enemy.bodySprite = PickSprite();
        enemy.spriteFacesRight = true;
    }

    private Vector3 PickSpawnPosition()
    {
        float halfH = _camera != null ? _camera.orthographicSize : 5f;
        float halfW = halfH * (_camera != null ? _camera.aspect : 1.78f);
        Vector3 camPos = _camera != null ? _camera.transform.position : _player.position;

        int side = Random.Range(0, 3); // слева / справа / сверху
        float x, y;
        if (side == 0) { x = camPos.x - halfW - offscreenMargin; y = _player.position.y; }
        else if (side == 1) { x = camPos.x + halfW + offscreenMargin; y = _player.position.y; }
        else { x = _player.position.x + Random.Range(-4f, 4f); y = camPos.y + halfH + offscreenMargin; }

        x = Mathf.Clamp(x, _minX, _maxX);
        Vector3 result = new Vector3(x, y, 0f);

        // Безопасная зона: не спавнить в упор к игроку.
        if (_player != null && Vector2.Distance(result, _player.position) < minSpawnDistance)
        {
            float dir = result.x >= _player.position.x ? 1f : -1f;
            result.x = Mathf.Clamp(_player.position.x + dir * minSpawnDistance, _minX, _maxX);
        }
        return result;
    }

    private GameObject CreateTelegraph(Vector3 pos, bool elite)
    {
        GameObject g = new GameObject("SpawnTelegraph");
        g.transform.position = pos;
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = GetTelegraphSprite();
        sr.color = elite ? new Color(1f, 0.5f, 0.15f, 0.5f) : new Color(1f, 0.25f, 0.2f, 0.5f);
        sr.sortingOrder = 7;
        Destroy(g, telegraphTime + 0.3f); // подстраховка: уберётся, даже если корутину прервёт смена уровня
        return g;
    }

    // Программный мягкий кружок (радиальное затухание) для предупреждающего силуэта.
    private static Sprite GetTelegraphSprite()
    {
        if (_telegraphSprite != null) return _telegraphSprite;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(1f - d / r);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _telegraphSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _telegraphSprite;
    }

    // Считает живых врагов на сцене (мёртвые, доигрывающие анимацию 0.5с, не учитываются).
    private int CountAliveEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int n = 0;
        foreach (Enemy e in enemies)
            if (e != null && !e.IsDead) n++;
        return n;
    }

    private Sprite PickSprite()
    {
        if (_enemySprites == null) return null;
        System.Collections.Generic.List<Sprite> available = new System.Collections.Generic.List<Sprite>();
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
        if (_statusText != null) return;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject obj = new GameObject("EncounterStatus");
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -120f);
        rect.sizeDelta = new Vector2(600f, 50f);

        _statusText = obj.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize = 28;
        _statusText.color = new Color(1f, 0.85f, 0.4f);
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.fontStyle = FontStyles.Bold;
        _statusText.textWrappingMode = TextWrappingModes.NoWrap;
        _statusText.text = "";
    }

    private void SetStatus(string text)
    {
        EnsureUI();
        if (_statusText != null) _statusText.text = text;
    }
}
