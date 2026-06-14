using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Границы спавна врагов")]
    public float[] levelEnemyMinX;
    public float[] levelEnemyMaxX;

    [Header("Настройки перехода")]
    public float transitionDelay = 0.3f;
    public int totalLevels = 3;

    [Header("Портал / Ворота")]
    public float portalOffsetFromEdge = 5f;
    public string gateSpritePath = "Sprites/gates";
    public float gateWorldHeight = 4.5f;        // высота ворот в игровых единицах
    public float gateGroundOffset = 0f;         // ручная докрутка ворот по высоте
    public float gateTriggerRadius = 1.6f;      // радиус зоны перехода (в игровых единицах)

    [Header("Локация 2 — склейка 5 картинок")]
    public int location2LevelIndex = 1;                          // какой уровень использует location2
    public string[] location2SpritePaths =
    {
        "Sprites/backGrounds/location2(1)",
        "Sprites/backGrounds/location2(2)",
        "Sprites/backGrounds/location2(3)",
        "Sprites/backGrounds/location2(4)",
        "Sprites/backGrounds/location2(5)",
    };
    public float location2StartFloorY = -3f;     // мировая высота пола в начале (низ 1-й картинки)
    public float location2HeightScale = 1.8f;    // высота картинок относительно экрана
    public float location2VerticalOffset = 0f;   // общая докрутка по высоте
    public float location2FloorScreenMargin = 0.3f; // насколько пол выше нижнего края экрана
    public float location2CameraSmoothTime = 0.5f;  // плавность камеры (гасит тряску при прыжках)
    public float location2CameraSize = 4.0f;        // размер камеры на location2 (меньше = ближе/крупнее, меньше чёрных полей)
    public int backgroundSortingOrder = -20;     // картинки фона
    public int backdropSortingOrder = -30;       // тёмная подложка позади всего (прячет стыки)

    [Header("Локация 2 — пол на каждой картинке (доля снизу, замерено по пикселям)")]
    public float[] location2FloorFractions = { 0.25f, 0.213f, 0.27f, 0.26f, 0.2f };

    // Индивидуальный множитель высоты картинки (1-я крупнее, т.к. у её коридора мало контента сверху).
    public float[] location2HeightMultipliers = { 1.6f, 1f, 1f, 1f, 1f };

    [Header("Локация 2 — лестница на 1-й картинке (замерено по пикселям)")]
    public float location2Image1HighFraction = 0.54f;         // высокий пол справа на 1-й картинке
    [Range(0f, 1f)] public float location2RampStartX = 0.567f; // начало подъёма (доля ширины)
    [Range(0f, 1f)] public float location2RampEndX = 0.70f;    // конец подъёма (доля ширины)
    public int location2RampSteps = 5;                         // число ступеней лестницы

    // Доля ширины, на которую сдвигаемся к следующей картинке — обрезает прозрачные поля справа и убирает щели.
    public float[] location2AdvanceFractions = { 0.93f, 1f, 1f, 1f, 1f };

    private int _currentLevel = 0;
    private GameObject _player;
    private CameraFollow _cameraFollow;
    private Camera _camera;
    private float _defaultCamSize = 5f;
    private EnemySpawner _enemySpawner;
    private GameObject _currentPortal;
    private Canvas _fadeCanvas;
    private Image _fadeImage;
    private Transform _ground;
    private SpriteRenderer _groundRenderer;
    private Collider2D _groundCollider;
    private Vector3 _groundOriginalScale = Vector3.one;
    private Vector3 _groundOriginalPos;
    private bool _hasGroundOriginal = false;
    private float _defaultCamMinY;
    private float _defaultCamMaxY;
    private bool _hasCamDefaults = false;
    private bool _loc2Built = false;
    private float _loc2LowFloorY;
    private float _loc2HighFloorY;
    private float _loc2EndFloorY;
    private float _defaultCamOffsetY;
    private float _defaultCamSmooth;
    private bool _hasCamExtraDefaults = false;

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
        _camera = Camera.main;
        _cameraFollow = _camera != null ? _camera.GetComponent<CameraFollow>() : null;
        _enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (_camera != null && _camera.orthographic)
        {
            _defaultCamSize = _camera.orthographicSize;
        }

        if (_cameraFollow != null)
        {
            _defaultCamMinY = _cameraFollow.minY;
            _defaultCamMaxY = _cameraFollow.maxY;
            _hasCamDefaults = true;
            _defaultCamOffsetY = _cameraFollow.offset.y;
            _defaultCamSmooth = _cameraFollow.smoothTime;
            _hasCamExtraDefaults = true;
        }

        FindGround();
        BuildLocation2Level();

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

        // На уровне с кастомным фоном (location2) прячем видимую полосу земли — пол рисует сам фон.
        if (_groundRenderer != null)
        {
            _groundRenderer.enabled = (levelIndex != location2LevelIndex);
        }

        ApplyGroundForLevel(levelIndex);

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

        // Камера на location2: следит по вертикали за подъёмом, держит пол у низа экрана,
        // повышенная плавность гасит тряску при прыжках по ступеням.
        if (_cameraFollow != null && _hasCamDefaults && _hasCamExtraDefaults)
        {
            if (levelIndex == location2LevelIndex && _loc2Built)
            {
                if (_camera != null) _camera.orthographicSize = location2CameraSize;
                // Смещение камеры так, чтобы пол был на location2FloorScreenMargin выше нижнего края экрана.
                float camOffsetY = location2CameraSize - 1.4f - location2FloorScreenMargin;
                _cameraFollow.offset.y = camOffsetY;
                _cameraFollow.smoothTime = location2CameraSmoothTime;
                _cameraFollow.minY = _loc2LowFloorY + 1.4f + camOffsetY;
                _cameraFollow.maxY = _loc2HighFloorY + 1.4f + camOffsetY;
            }
            else
            {
                if (_camera != null) _camera.orthographicSize = _defaultCamSize;
                _cameraFollow.offset.y = _defaultCamOffsetY;
                _cameraFollow.smoothTime = _defaultCamSmooth;
                _cameraFollow.minY = _defaultCamMinY;
                _cameraFollow.maxY = _defaultCamMaxY;
            }
        }

        if (_enemySpawner != null)
        {
            if (levelGroundY != null && levelIndex < levelGroundY.Length)
                _enemySpawner.groundY = levelGroundY[levelIndex];

            if (levelEnemyMinX != null && levelIndex < levelEnemyMinX.Length)
                _enemySpawner.minSpawnX = levelEnemyMinX[levelIndex];

            if (levelEnemyMaxX != null && levelIndex < levelEnemyMaxX.Length)
                _enemySpawner.maxSpawnX = levelEnemyMaxX[levelIndex];
        }

        ClearEnemies();
        SpawnPortal(levelIndex);

        // Камера мгновенно встаёт на игрока (под затемнением), а не «едет» через весь уровень.
        if (_cameraFollow != null)
        {
            _cameraFollow.SnapToTarget();
        }

        Debug.Log($"[LevelManager] Загружен уровень {levelIndex + 1}/{totalLevels}");
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
        EnsureFadeOverlay();

        Time.timeScale = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 3f;
            _fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(t));
            yield return null;
        }

        InitializeLevel(_currentLevel + 1);

        t = 1f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime * 3f;
            _fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(t));
            yield return null;
        }

        Time.timeScale = 1f;
    }

    private void EnsureFadeOverlay()
    {
        if (_fadeCanvas != null) return;

        GameObject canvasObj = new GameObject("FadeCanvas");
        _fadeCanvas = canvasObj.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject fadeObj = new GameObject("FadeImage");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = fadeObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        _fadeImage = fadeObj.AddComponent<Image>();
        _fadeImage.color = new Color(0, 0, 0, 0);
    }

    private void SpawnPortal(int levelIndex)
    {
        if (_currentPortal != null)
            Destroy(_currentPortal);

        float maxX = (levelMaxX != null && levelIndex < levelMaxX.Length)
            ? levelMaxX[levelIndex] : 105f;
        float groundY = (levelGroundY != null && levelIndex < levelGroundY.Length)
            ? levelGroundY[levelIndex] : -3f;

        // На location2 ворота стоят на полу в конце уровня (он приподнят ступенями).
        if (levelIndex == location2LevelIndex && _loc2Built)
        {
            groundY = _loc2EndFloorY;
        }

        float portalX = maxX - portalOffsetFromEdge;

        _currentPortal = new GameObject("Gate");

        SpriteRenderer sr = _currentPortal.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -1; // позади игрока и врагов, но впереди фона
        sr.color = Color.white;

        Sprite gateSprite = LoadLargestSprite(gateSpritePath);
        float scale = 1f;
        if (gateSprite != null)
        {
            sr.sprite = gateSprite;
            float spriteHeight = gateSprite.bounds.size.y;
            if (spriteHeight > 0.0001f)
            {
                scale = gateWorldHeight / spriteHeight;
            }
        }
        else
        {
            // Запасной вариант, если спрайт ворот не найден.
            sr.sprite = Resources.Load<Sprite>("Sprites/muzzle_flash");
            sr.color = new Color(0.2f, 0.7f, 1f, 0.85f);
        }

        _currentPortal.transform.localScale = new Vector3(scale, scale, 1f);

        // Ставим ворота основанием на землю.
        float portalY = groundY + gateWorldHeight * 0.5f + gateGroundOffset;
        _currentPortal.transform.position = new Vector3(portalX, portalY, 0f);

        CircleCollider2D col = _currentPortal.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        // Радиус задаётся в локальных единицах, поэтому делим на масштаб, чтобы получить нужный мировой радиус.
        col.radius = scale > 0.0001f ? gateTriggerRadius / scale : gateTriggerRadius;

        Portal portal = _currentPortal.AddComponent<Portal>();
        portal.pulseAlpha = false;     // камень не должен мерцать прозрачностью
        portal.pulseIntensity = 0.03f; // едва заметное «дыхание»
    }

    private void FindGround()
    {
        GameObject groundObj = GameObject.Find("Ground");
        if (groundObj != null)
        {
            _ground = groundObj.transform;
            _groundRenderer = groundObj.GetComponent<SpriteRenderer>();
            _groundCollider = groundObj.GetComponent<Collider2D>();
            _groundOriginalScale = _ground.localScale;
            _groundOriginalPos = _ground.position;
            _hasGroundOriginal = true;
        }
    }

    private void ApplyGroundForLevel(int levelIndex)
    {
        if (_ground == null || !_hasGroundOriginal) return;

        // На location2 общая плоская земля выключается — там свой ступенчатый пол.
        if (_groundCollider != null)
        {
            _groundCollider.enabled = (levelIndex != location2LevelIndex);
        }

        // Земля всегда в исходном состоянии; на location2 её всё равно не видно и не используют.
        _ground.localScale = _groundOriginalScale;
        _ground.position = _groundOriginalPos;
    }

    // Склеивает 5 картинок location2 в один длинный уровень: фон, непрерывный пол-коллайдер
    // с настоящей лестницей на 1-й картинке, тёмную подложку, длину уровня, камеру и ворота.
    private void BuildLocation2Level()
    {
        if (levelBackgrounds == null
            || location2LevelIndex < 0
            || location2LevelIndex >= levelBackgrounds.Length
            || levelBackgrounds[location2LevelIndex] == null
            || location2SpritePaths == null
            || location2SpritePaths.Length == 0)
        {
            return;
        }

        GameObject root = levelBackgrounds[location2LevelIndex];

        // Прежний одиночный рендерер контейнера больше не нужен.
        SpriteRenderer rootSr = root.GetComponent<SpriteRenderer>();
        if (rootSr != null) rootSr.enabled = false;

        // Считаем под ПРИБЛИЖЁННУЮ камеру location2 (меньший обзор → картинки заведомо перекрывают экран).
        float aspect = (_camera != null) ? _camera.aspect : (16f / 9f);
        float camHalfHeight = location2CameraSize;
        float camHalfWidth = location2CameraSize * aspect;

        float H = camHalfHeight * 2f * Mathf.Max(0.8f, location2HeightScale); // высота всех картинок

        float minX = (levelMinX != null && location2LevelIndex < levelMinX.Length) ? levelMinX[location2LevelIndex] : -4f;
        float startX = minX - camHalfWidth; // левый край первой картинки = край обзора в начале

        float cursorX = startX;
        float currentFloorY = location2StartFloorY; // высота пола слева у 1-й картинки
        _loc2LowFloorY = currentFloorY;
        _loc2HighFloorY = currentFloorY;

        // Точки единого пола (мировые координаты, слева направо) — потом один EdgeCollider2D.
        System.Collections.Generic.List<Vector2> floorPts = new System.Collections.Generic.List<Vector2>();
        floorPts.Add(new Vector2(startX - 10f, currentFloorY)); // продление влево

        for (int i = 0; i < location2SpritePaths.Length; i++)
        {
            Sprite sprite = LoadLargestSprite(location2SpritePaths[i]);
            if (sprite == null)
            {
                Debug.LogWarning($"[LevelManager] Не найдена картинка location2: {location2SpritePaths[i]}");
                continue;
            }

            float spriteW = sprite.bounds.size.x;
            float spriteH = sprite.bounds.size.y;
            if (spriteW <= 0.0001f || spriteH <= 0.0001f) continue;

            // Высота этой картинки (с индивидуальным множителем — 1-я крупнее для заполнения коридора).
            float mult = (location2HeightMultipliers != null && i < location2HeightMultipliers.Length)
                ? Mathf.Max(0.5f, location2HeightMultipliers[i]) : 1f;
            float segH = H * mult;

            float scale = segH / spriteH;
            float segWidth = spriteW * scale;
            float segLeftX = cursorX;
            float segCenterX = segLeftX + segWidth * 0.5f;

            float floorFrac = (location2FloorFractions != null && i < location2FloorFractions.Length)
                ? location2FloorFractions[i] : 0.2f;

            // Картинка ставится так, чтобы её нарисованный нижний пол лёг ровно на currentFloorY —
            // пол получается непрерывным от сегмента к сегменту (без обрывов).
            float segCenterY = currentFloorY - (floorFrac - 0.5f) * segH + location2VerticalOffset;

            GameObject seg = new GameObject($"Loc2_Seg{i + 1}");
            seg.transform.SetParent(root.transform, false);
            seg.transform.position = new Vector3(segCenterX, segCenterY, 0f);
            seg.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer segSr = seg.AddComponent<SpriteRenderer>();
            segSr.sprite = sprite;
            segSr.color = Color.white;
            segSr.sortingOrder = backgroundSortingOrder;

            // Эффективный правый край с учётом обрезки прозрачного поля (чтобы не было щели).
            float advance = (location2AdvanceFractions != null && i < location2AdvanceFractions.Length)
                ? Mathf.Clamp(location2AdvanceFractions[i], 0.5f, 1f) : 1f;
            float segEffRight = segLeftX + segWidth * advance;

            if (i == 0)
            {
                // 1-я картинка: ровный низ → ПЛАВНАЯ диагональ-рампа → ровный верх (как нарисовано).
                float lowY = currentFloorY;
                float highY = segCenterY + (location2Image1HighFraction - 0.5f) * segH;
                float rampL = segLeftX + segWidth * Mathf.Clamp01(location2RampStartX);
                float rampR = segLeftX + segWidth * Mathf.Clamp01(location2RampEndX);

                floorPts.Add(new Vector2(rampL, lowY));   // конец нижней площадки
                floorPts.Add(new Vector2(rampR, highY));  // верх рампы
                floorPts.Add(new Vector2(segEffRight, highY)); // верхняя площадка

                currentFloorY = highY;
                _loc2HighFloorY = Mathf.Max(_loc2HighFloorY, highY);
            }
            else
            {
                floorPts.Add(new Vector2(segEffRight, currentFloorY)); // ровный пол до конца сегмента
                _loc2HighFloorY = Mathf.Max(_loc2HighFloorY, currentFloorY);
            }

            cursorX = segEffRight;
        }

        floorPts.Add(new Vector2(cursorX + 10f, currentFloorY)); // продление вправо

        float totalRightX = cursorX;
        _loc2EndFloorY = currentFloorY;

        // Единый гладкий пол: один EdgeCollider2D по всем точкам — нет внутренних стыков (игрок
        // не застревает), диагональ — плавная рампа, совпадающая с рисунком.
        GameObject floorObj = new GameObject("Location2Floor");
        floorObj.transform.SetParent(root.transform, false);
        floorObj.transform.position = Vector3.zero; // мировые координаты = локальные
        EdgeCollider2D edge = floorObj.AddComponent<EdgeCollider2D>();
        edge.points = floorPts.ToArray();
        edge.sharedMaterial = new PhysicsMaterial2D("Loc2Floor") { friction = 1f, bounciness = 0f };

        // Тёмная подложка позади всего — прячет прозрачные края картинок и стыки.
        CreateBackdrop(root.transform, startX, totalRightX, H, (_loc2LowFloorY + _loc2HighFloorY) * 0.5f);

        // Длина уровня и границы из реальной ширины склейки.
        float newMaxX = totalRightX - camHalfWidth;
        if (levelMaxX != null && location2LevelIndex < levelMaxX.Length)
            levelMaxX[location2LevelIndex] = newMaxX;
        if (levelEnemyMinX != null && location2LevelIndex < levelEnemyMinX.Length)
            levelEnemyMinX[location2LevelIndex] = minX + 8f;
        if (levelEnemyMaxX != null && location2LevelIndex < levelEnemyMaxX.Length)
            levelEnemyMaxX[location2LevelIndex] = newMaxX - 8f;
        // Враги спавнятся выше самого высокого пола и падают на ступени/пол.
        if (levelGroundY != null && location2LevelIndex < levelGroundY.Length)
            levelGroundY[location2LevelIndex] = _loc2HighFloorY + 4f;

        _loc2Built = true;
        Debug.Log($"[LevelManager] location2 склеена: до X={newMaxX:F1}, пол {_loc2LowFloorY:F1}..{_loc2HighFloorY:F1}");
    }

    // Невидимый коллайдер-площадка с верхом на topY (тянется вниз, чтобы образовать сплошной пол/стену ступени).
    private void CreateFloorBox(Transform parent, float xLeft, float xRight, float topY)
    {
        const float depth = 40f;
        float width = xRight - xLeft;
        if (width <= 0.001f) return;

        GameObject box = new GameObject("FloorBox");
        box.transform.SetParent(parent, false);
        box.transform.position = new Vector3((xLeft + xRight) * 0.5f, topY - depth * 0.5f, 0f);

        BoxCollider2D col = box.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, depth);
    }

    private void CreateBackdrop(Transform parent, float leftX, float rightX, float H, float floorY)
    {
        GameObject backdrop = new GameObject("Loc2_Backdrop");
        backdrop.transform.SetParent(parent, false);
        backdrop.transform.position = new Vector3((leftX + rightX) * 0.5f, floorY, 0f);

        SpriteRenderer sr = backdrop.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = new Color(0.05f, 0.045f, 0.06f, 1f); // тёмный, под цвет подземелья
        sr.sortingOrder = backdropSortingOrder;

        float width = (rightX - leftX) + 20f;
        float height = H * 3f;
        backdrop.transform.localScale = new Vector3(width, height, 1f);
    }

    private Sprite _solidSprite;
    private Sprite GetSolidSprite()
    {
        if (_solidSprite != null) return _solidSprite;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _solidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _solidSprite;
    }

    private Sprite LoadLargestSprite(string path)
    {
        Sprite[] loaded = Resources.LoadAll<Sprite>(path);
        if (loaded == null || loaded.Length == 0) return null;

        Sprite best = null;
        float bestArea = -1f;
        foreach (Sprite sprite in loaded)
        {
            if (sprite == null) continue;
            float area = sprite.rect.width * sprite.rect.height;
            if (area > bestArea)
            {
                bestArea = area;
                best = sprite;
            }
        }

        return best;
    }

    private void ClearEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
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
