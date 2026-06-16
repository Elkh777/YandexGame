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

    [Header("Локация 3 — Катакомбы (склейка 5 картинок, двухъярусная)")]
    public int catacombsLevelIndex = 2;                          // какой уровень — катакомбы
    public string[] catacombsSpritePaths =
    {
        "Sprites/Катакомбы1",
        "Sprites/Катакомбы2",
        "Sprites/Катакомбы3",
        "Sprites/Катакомбы4",
        "Sprites/Катакомбы5",
    };
    public float catacombsCameraSize = 4.5f;        // приближение камеры (чтобы картинки перекрывали экран)
    public float catacombsHeightScale = 1.8f;       // высота картинок относительно обзора (>1 — нет чёрных полос при панораме)
    public float catacombsUpperFloorY = 1f;         // мировой Y верхнего коридора
    public float catacombsLowerFloorY = -10f;       // мировой Y пола подвала (куда падает игрок; чем ниже — тем глубже провал)
    public int catacombsLowerGroupStart = 3;        // с какой картинки начинается нижняя группа (подвал): 3 → Катакомбы4,5
    // Где у КАЖДОЙ картинки нарисован пол (доля высоты снизу). Картинка ставится так, чтобы её пол лёг
    // ровно на целевой Y своей группы → коллайдер совпадает с артом (игрок не висит и не проваливается мимо).
    public float[] catacombsFloorFractions = { 0.40f, 0.40f, 0.40f, 0.14f, 0.27f };
    [Range(0f, 1f)] public float catacombsCliffSegFrac = 0.55f;   // где обрыв в последней верхней картинке (доля ширины)
    // Доля ширины для обрезки прозрачных краёв при стыковке (gap=0).
    public float[] catacombsAdvanceFractions = { 0.98f, 0.98f, 0.98f, 0.98f, 1f };

    private int _currentLevel = 0;
    private GameObject _player;
    private CameraFollow _cameraFollow;
    private Camera _camera;
    private float _defaultCamSize = 5f;
    private EncounterManager _encounterManager;
    private GameObject _currentPortal;
    private Canvas _fadeCanvas;
    private Image _fadeImage;
    private Transform _ground;
    private SpriteRenderer _groundRenderer;
    private Collider2D _groundCollider;
    private Vector3 _groundOriginalScale = Vector3.one;
    private Vector3 _groundOriginalPos;
    private bool _hasGroundOriginal = false;
    private GameObject _levelBounds; // страховочный пол на всю длину уровня + боковые стены
    private float _defaultCamMinY;
    private float _defaultCamMaxY;
    private bool _hasCamDefaults = false;
    private bool _loc2Built = false;
    private float _loc2LowFloorY;
    private float _loc2HighFloorY;
    private float _loc2EndFloorY;
    private bool _catacombsBuilt = false;
    private float _catacombsEndFloorY;             // = lowerFloorY (для ворот)
    private float _catacombsCamMinY, _catacombsCamMaxY; // клампы камеры в пределах арта
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

        // Надёжно получаем EncounterManager независимо от порядка инициализации (создаём, если ещё нет).
        _encounterManager = EncounterManager.Instance != null ? EncounterManager.Instance : FindFirstObjectByType<EncounterManager>();
        if (_encounterManager == null)
        {
            GameObject em = new GameObject("EncounterManager");
            em.AddComponent<EncounterManager>();
            _encounterManager = EncounterManager.Instance;
        }

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
        BuildCatacombsLevel();

        // Старт с уровня, выбранного в меню (по умолчанию — первый).
        int startLevel = Mathf.Clamp(LevelProgress.ConsumeStartLevel(), 0, totalLevels - 1);
        InitializeLevel(startLevel);
    }

    public void InitializeLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= totalLevels) return;

        _currentLevel = levelIndex;

        // Индикатор уровня вверху по центру.
        fGameManager.Instance?.UpdateLevelUI(levelIndex + 1);

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
        BuildLevelBounds(levelIndex); // сплошной пол на всю длину (чтобы не проваливаться)
        if (levelIndex != location2LevelIndex && levelIndex != catacombsLevelIndex) FitBackgroundWidth(levelIndex); // растянуть фон под длину уровня

        if (_player != null && spawnPoints != null && levelIndex < spawnPoints.Length)
        {
            if (spawnPoints[levelIndex] != null)
            {
                _player.transform.position = spawnPoints[levelIndex].position;
                Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }

        // В катакомбах геометрия строится кодом (верхний пол высоко) — ставим игрока на верхний пол у начала,
        // иначе он спавнится по старой точке Level 3 и сразу падает.
        if (levelIndex == catacombsLevelIndex && _catacombsBuilt && _player != null)
        {
            float sx = (levelMinX != null && levelIndex < levelMinX.Length) ? levelMinX[levelIndex] : -4f;
            _player.transform.position = new Vector3(sx + 2f, catacombsUpperFloorY + 1.5f, _player.transform.position.z);
            Rigidbody2D prb = _player.GetComponent<Rigidbody2D>();
            if (prb != null) prb.linearVelocity = Vector2.zero;
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
            else if (levelIndex == catacombsLevelIndex && _catacombsBuilt)
            {
                // Приближённая камера + клампы Y строго внутри арта обеих групп → нет чёрных полос,
                // при этом камера следует за игроком вниз при падении в обрыв.
                if (_camera != null) _camera.orthographicSize = catacombsCameraSize;
                _cameraFollow.offset.y = 0f;
                _cameraFollow.smoothTime = 0.22f;
                _cameraFollow.minY = _catacombsCamMinY;
                _cameraFollow.maxY = _catacombsCamMaxY;
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

        ClearEnemies();
        SpawnPortal(levelIndex);

        // Камера мгновенно встаёт на игрока (под затемнением), а не «едет» через весь уровень.
        if (_cameraFollow != null)
        {
            _cameraFollow.SnapToTarget();
        }

        // Ворота уже стоят в конце уровня (статично, из SpawnPortal). Энкаунтеры — это бои по пути,
        // они не управляют воротами.
        if (_encounterManager != null)
        {
            float spawnMinX = (levelEnemyMinX != null && levelIndex < levelEnemyMinX.Length)
                ? levelEnemyMinX[levelIndex]
                : ((levelMinX != null && levelIndex < levelMinX.Length) ? levelMinX[levelIndex] : -4f);
            float spawnMaxX = (levelEnemyMaxX != null && levelIndex < levelEnemyMaxX.Length)
                ? levelEnemyMaxX[levelIndex]
                : ((levelMaxX != null && levelIndex < levelMaxX.Length) ? levelMaxX[levelIndex] : 105f);
            _encounterManager.BeginLevel(levelIndex, spawnMinX, spawnMaxX);
        }

        Debug.Log($"[LevelManager] Загружен уровень {levelIndex + 1}/{totalLevels}");
    }

    // Открывает портал уровня (вызывается EncounterManager после зачистки всех энкаунтеров).
    public void ActivatePortal()
    {
        if (_currentPortal != null)
        {
            _currentPortal.SetActive(true);
            AudioManager.Instance?.PlayLevelComplete();
            Debug.Log($"[LevelManager] Портал ОТКРЫТ в позиции {_currentPortal.transform.position}");
        }
        else
        {
            Debug.LogWarning("[LevelManager] ActivatePortal вызван, но _currentPortal == null!");
        }
    }

    public void GoToNextLevel()
    {
        // Текущий уровень пройден — разблокируем следующий и играем звук завершения.
        LevelProgress.MarkCompleted(_currentLevel);
        AudioManager.Instance?.PlayLevelComplete();

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
        // GraphicRaycaster намеренно НЕ добавляем: иначе полноэкранное затемнение
        // перехватывало бы все клики по кнопкам после перехода на уровень 2+.

        GameObject fadeObj = new GameObject("FadeImage");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = fadeObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        _fadeImage = fadeObj.AddComponent<Image>();
        _fadeImage.color = new Color(0, 0, 0, 0);
        _fadeImage.raycastTarget = false; // затемнение никогда не должно ловить клики
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
        // В катакомбах ворота стоят на нижнем полу (в подвале), куда игрок спускается через обрыв.
        else if (levelIndex == catacombsLevelIndex && _catacombsBuilt)
        {
            groundY = _catacombsEndFloorY;
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

        // Ворота стоят статично в конце уровня и видны сразу — игрок вбегает в них и переходит дальше.
        _currentPortal.SetActive(true);
        Debug.Log($"[LevelManager] Ворота уровня {levelIndex + 1} стоят в позиции {_currentPortal.transform.position}");
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

    // Строит невидимый сплошной пол на всю длину уровня (minX..maxX), чтобы игрок не проваливался
    // там, где видимая земля сцены заканчивается, и мог дойти до портала. Без боковых стен:
    // верхний край пола стоит ровно на groundY (как у портала), чтобы игрок не «утопал» и не застревал.
    // На location2 свой непрерывный пол — туда не вмешиваемся.
    private void BuildLevelBounds(int levelIndex)
    {
        if (_levelBounds != null)
        {
            Destroy(_levelBounds);
            _levelBounds = null;
        }

        if (levelIndex == location2LevelIndex && _loc2Built) return;
        if (levelIndex == catacombsLevelIndex && _catacombsBuilt) return; // у катакомб свои полы (есть обрыв)

        float minX = (levelMinX != null && levelIndex < levelMinX.Length) ? levelMinX[levelIndex] : -4f;
        float maxX = (levelMaxX != null && levelIndex < levelMaxX.Length) ? levelMaxX[levelIndex] : 105f;
        float groundY = (levelGroundY != null && levelIndex < levelGroundY.Length) ? levelGroundY[levelIndex] : -3f;

        // Верх пола ставим на groundY — это авторитетная высота земли (на ней же стоит портал).
        // Не используем bounds видимой земли: если найден не тот объект «Ground», пол встал бы высоко
        // и игрок «утоп» бы в коллайдере (эффект невидимой стены).
        float floorTopY = groundY;

        // Тот же слой, что у земли сцены, — чтобы коллизии и определение «на земле» работали как обычно.
        int groundLayer = _ground != null ? _ground.gameObject.layer : 0;

        const float floorThickness = 6f;
        // Пол с большим запасом по бокам (особенно справа за порталом) — игрок не свалится в пустоту,
        // даже если пробежит мимо ворот. Стен нет, чтобы ничто не перекрывало движение.
        float left = minX - 10f;
        float right = maxX + 40f;
        float width = right - left;
        float centerX = (left + right) * 0.5f;

        _levelBounds = new GameObject("SafetyFloor");
        _levelBounds.layer = groundLayer;
        _levelBounds.transform.position = new Vector3(centerX, floorTopY - floorThickness * 0.5f, 0f);
        BoxCollider2D fc = _levelBounds.AddComponent<BoxCollider2D>();
        fc.size = new Vector2(width, floorThickness);

        Debug.Log($"[LevelManager] Пол уровня {levelIndex + 1}: X[{minX}..{maxX}], верх Y={floorTopY}, ширина={width}");
    }

    // Растягивает фон обычного уровня по ширине под длину уровня (minX..maxX), чтобы у удлинённого
    // уровня не было пустого «хвоста» за краем картинки. Левый край фиксируем — стартовая часть не уезжает.
    private void FitBackgroundWidth(int levelIndex)
    {
        if (levelBackgrounds == null || levelIndex >= levelBackgrounds.Length) return;
        GameObject bg = levelBackgrounds[levelIndex];
        if (bg == null) return;
        SpriteRenderer sr = bg.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float minX = (levelMinX != null && levelIndex < levelMinX.Length) ? levelMinX[levelIndex] : -4f;
        float maxX = (levelMaxX != null && levelIndex < levelMaxX.Length) ? levelMaxX[levelIndex] : 105f;
        float targetWidth = (maxX - minX) + 16f; // небольшой запас по краям

        float curWidth = sr.bounds.size.x;
        if (curWidth < 0.01f || targetWidth <= curWidth) return; // фон уже достаточно широкий — не трогаем

        float leftEdge = sr.bounds.min.x; // фиксируем левый край, растягиваем вправо
        float factor = targetWidth / curWidth;
        Vector3 s = bg.transform.localScale;
        bg.transform.localScale = new Vector3(s.x * factor, s.y, s.z);

        float newLeft = sr.bounds.min.x;
        Vector3 p = bg.transform.position;
        p.x += (leftEdge - newLeft);
        bg.transform.position = p;
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

    // Склеивает 5 картинок «Катакомбы» в двухъярусный уровень: верхний коридор → обрыв → подвал.
    // Все картинки ставятся на общий базлайн (низы выровнены), поэтому верхний пол (в картинках 1-3)
    // и нижний пол (в картинках 4-5) сами оказываются на разной высоте — это и даёт ярусы.
    // Коллайдеры: верхний пол до обрыва, ниже — сплошной нижний пол на всю длину (ловит падающих).
    private void BuildCatacombsLevel()
    {
        if (levelBackgrounds == null
            || catacombsLevelIndex < 0
            || catacombsLevelIndex >= levelBackgrounds.Length
            || levelBackgrounds[catacombsLevelIndex] == null
            || catacombsSpritePaths == null
            || catacombsSpritePaths.Length == 0)
        {
            return;
        }

        GameObject root = levelBackgrounds[catacombsLevelIndex];

        // Старый одиночный фон Level 3 выключаем.
        SpriteRenderer rootSr = root.GetComponent<SpriteRenderer>();
        if (rootSr != null) rootSr.enabled = false;

        float aspect = (_camera != null) ? _camera.aspect : (16f / 9f);
        float camHalfHeight = catacombsCameraSize;
        float camHalfWidth = catacombsCameraSize * aspect;

        float H = camHalfHeight * 2f * Mathf.Max(0.8f, catacombsHeightScale); // высота всех картинок

        float minX = (levelMinX != null && catacombsLevelIndex < levelMinX.Length) ? levelMinX[catacombsLevelIndex] : -4f;

        float upperFloorY = catacombsUpperFloorY;
        float lowerFloorY = catacombsLowerFloorY;
        _catacombsEndFloorY = lowerFloorY;

        float startX = minX - camHalfWidth; // левый край 1-й картинки = край обзора в начале
        float cursorX = startX;

        // Для обрыва и камеры запоминаем границы верхней/нижней групп.
        float lastUpperLeft = 0f, lastUpperWidth = 0f;
        float upperTopY = float.MinValue, lowerBottomY = float.MaxValue;

        for (int i = 0; i < catacombsSpritePaths.Length; i++)
        {
            Sprite sprite = LoadLargestSprite(catacombsSpritePaths[i]);
            if (sprite == null)
            {
                Debug.LogWarning($"[LevelManager] Не найдена картинка катакомб: {catacombsSpritePaths[i]}");
                continue;
            }

            float spriteW = sprite.bounds.size.x;
            float spriteH = sprite.bounds.size.y;
            if (spriteW <= 0.0001f || spriteH <= 0.0001f) continue;

            float scale = H / spriteH;          // пропорции сохраняем (scale по X и Y одинаков)
            float segWidth = spriteW * scale;
            float segLeftX = cursorX;
            float segCenterX = segLeftX + segWidth * 0.5f;

            // Верхняя группа (1..3) ставится по верхнему полу, нижняя (4..5) — по полу подвала.
            // Вертикальный перепад между ними прячется чёрным провалом на стыке 3/4.
            bool isLower = (i >= catacombsLowerGroupStart);
            float floorY = isLower ? lowerFloorY : upperFloorY;
            float floorFrac = (catacombsFloorFractions != null && i < catacombsFloorFractions.Length)
                ? Mathf.Clamp01(catacombsFloorFractions[i]) : (isLower ? 0.2f : 0.4f);
            // Картинка ставится так, чтобы её нарисованный пол (своя доля floorFrac снизу) лёг ровно на floorY.
            float segCenterY = floorY - (floorFrac - 0.5f) * H;

            GameObject seg = new GameObject($"Loc3_Seg{i + 1}");
            seg.transform.SetParent(root.transform, false);
            seg.transform.position = new Vector3(segCenterX, segCenterY, 0f);
            seg.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer segSr = seg.AddComponent<SpriteRenderer>();
            segSr.sprite = sprite;
            segSr.color = Color.white;
            segSr.sortingOrder = backgroundSortingOrder;

            if (isLower) lowerBottomY = Mathf.Min(lowerBottomY, segCenterY - H * 0.5f);
            else { lastUpperLeft = segLeftX; lastUpperWidth = segWidth; upperTopY = Mathf.Max(upperTopY, segCenterY + H * 0.5f); }

            float advance = (catacombsAdvanceFractions != null && i < catacombsAdvanceFractions.Length)
                ? Mathf.Clamp(catacombsAdvanceFractions[i], 0.5f, 1f) : 1f;
            cursorX = segLeftX + segWidth * advance;
        }

        float totalRightX = cursorX;

        // X обрыва — в последней верхней картинке (где заканчивается верхний коридорный пол).
        float cliffX = (lastUpperWidth > 0f) ? lastUpperLeft + lastUpperWidth * Mathf.Clamp01(catacombsCliffSegFrac)
                                             : startX + (totalRightX - startX) * 0.5f;

        // Верхний пол: от старта до обрыва (дальше игрок падает).
        CreateFloorBox(root.transform, startX - 10f, cliffX, upperFloorY);
        // Пол подвала: от обрыва (ловит падающего) до конца — по нему игрок бежит к воротам.
        CreateFloorBox(root.transform, cliffX - 1f, totalRightX + 10f, lowerFloorY);

        // Большая тёмная подложка на весь вертикальный размах склейки — прячет прозрачные/чёрные края.
        if (upperTopY < lowerBottomY) { upperTopY = upperFloorY + H; lowerBottomY = lowerFloorY - H; }
        float backdropMidY = (upperTopY + lowerBottomY) * 0.5f;
        // Тёмно-каменный (не почти-чёрный) — прозрачные края читаются как тёмный камень, а не как чёрная дыра.
        CreateBackdrop(root.transform, startX, totalRightX, (upperTopY - lowerBottomY) + 16f, backdropMidY,
            new Color(0.16f, 0.15f, 0.17f, 1f));

        // Длина уровня и границы из реальной ширины склейки.
        float newMaxX = totalRightX - camHalfWidth;
        if (levelMaxX != null && catacombsLevelIndex < levelMaxX.Length) levelMaxX[catacombsLevelIndex] = newMaxX;
        if (levelEnemyMinX != null && catacombsLevelIndex < levelEnemyMinX.Length) levelEnemyMinX[catacombsLevelIndex] = minX + 8f;
        if (levelEnemyMaxX != null && catacombsLevelIndex < levelEnemyMaxX.Length) levelEnemyMaxX[catacombsLevelIndex] = newMaxX - 8f;
        // Враги спавнятся над верхним полом и падают на него или в обрыв (на пол подвала).
        if (levelGroundY != null && catacombsLevelIndex < levelGroundY.Length) levelGroundY[catacombsLevelIndex] = upperFloorY + 4f;

        // Камера: обзор строго внутри арта обеих групп → нет чёрных полос.
        _catacombsCamMinY = lowerBottomY + camHalfHeight;
        _catacombsCamMaxY = upperTopY - camHalfHeight;
        if (_catacombsCamMaxY < _catacombsCamMinY) _catacombsCamMaxY = _catacombsCamMinY;

        _catacombsBuilt = true;
        Debug.Log($"[LevelManager] Катакомбы склеены: X[{startX:F1}..{totalRightX:F1}], верх пол Y={upperFloorY:F1}, низ Y={lowerFloorY:F1}, обрыв X={cliffX:F1}, maxX={newMaxX:F1}");
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

    private void CreateBackdrop(Transform parent, float leftX, float rightX, float H, float floorY, Color? color = null)
    {
        GameObject backdrop = new GameObject("Loc2_Backdrop");
        backdrop.transform.SetParent(parent, false);
        backdrop.transform.position = new Vector3((leftX + rightX) * 0.5f, floorY, 0f);

        SpriteRenderer sr = backdrop.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = color ?? new Color(0.05f, 0.045f, 0.06f, 1f); // тёмный, под цвет подземелья
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
