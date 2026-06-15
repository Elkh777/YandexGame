using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Расширяет сцену меню: кнопки "Выбор уровня" и "Настройки", панель выбора уровней
// с блокировкой непройденных и панель настроек звука. Строится в рантайме поверх меню.
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    public int totalLevels = 3;
    public string gameSceneName = "MainScene";

    private GameObject _levelPanel;
    private GameObject _settingsPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "MenuScene" && Instance == null)
            {
                new GameObject("MenuManager").AddComponent<MenuManager>();
            }
        };

        Scene active = SceneManager.GetActiveScene();
        if (active.name == "MenuScene" && Instance == null)
        {
            new GameObject("MenuManager").AddComponent<MenuManager>();
        }
    }

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
        Canvas canvas = CreateOverlayCanvas();

        // Кнопки внизу по центру (чтобы не перекрывать существующую кнопку "Старт").
        Button levelsBtn = CreateButton(canvas.transform, new Vector2(0.5f, 0f), new Vector2(-180f, 60f),
            new Vector2(320f, 70f), "ВЫБОР УРОВНЯ");
        levelsBtn.onClick.AddListener(() => { AudioManager.Instance?.PlayUiClick(); OpenLevelPanel(); });

        Button settingsBtn = CreateButton(canvas.transform, new Vector2(0.5f, 0f), new Vector2(180f, 60f),
            new Vector2(320f, 70f), "НАСТРОЙКИ");
        settingsBtn.onClick.AddListener(() => { AudioManager.Instance?.PlayUiClick(); _settingsPanel.SetActive(true); });

        _levelPanel = BuildLevelPanel(canvas.transform);
        _settingsPanel = AudioManager.BuildSettingsPanel(canvas.transform, null);
    }

    private void OpenLevelPanel()
    {
        // Перестраиваем кнопки уровней (вдруг прогресс изменился), затем показываем.
        _levelPanel.SetActive(true);
    }

    private GameObject BuildLevelPanel(Transform parent)
    {
        GameObject panel = new GameObject("LevelPanel");
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(720f, 460f);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.02f, 0.025f, 0.96f);

        CreateLabel(panel.transform, "ВЫБОР УРОВНЯ", 42, new Vector2(0f, -55f), new Vector2(640f, 70f), FontStyles.Bold);

        // Ряд кнопок-уровней по центру.
        float spacing = 170f;
        float startX = -(totalLevels - 1) * spacing * 0.5f;
        for (int i = 0; i < totalLevels; i++)
        {
            int levelIndex = i;
            bool unlocked = LevelProgress.IsUnlocked(levelIndex);

            Button btn = CreateButton(panel.transform, new Vector2(0.5f, 0.5f),
                new Vector2(startX + i * spacing, 20f), new Vector2(130f, 130f),
                unlocked ? (levelIndex + 1).ToString() : "🔒");

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.fontSize = 48;

            if (unlocked)
            {
                btn.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayUiClick();
                    LevelProgress.SetStartLevel(levelIndex);
                    SceneManager.LoadScene(gameSceneName);
                });
            }
            else
            {
                btn.interactable = false;
                ColorBlock cb = btn.colors;
                cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                btn.colors = cb;
                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }

        Button backBtn = CreateButton(panel.transform, new Vector2(0.5f, 0f),
            new Vector2(0f, 55f), new Vector2(220f, 64f), "НАЗАД");
        backBtn.onClick.AddListener(() => { AudioManager.Instance?.PlayUiClick(); panel.SetActive(false); });

        panel.SetActive(false);
        return panel;
    }

    private Canvas CreateOverlayCanvas()
    {
        GameObject obj = new GameObject("MenuExtrasCanvas");
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        obj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Button CreateButton(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label)
    {
        GameObject obj = new GameObject("Button");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.85f, 0.7f, 0.25f, 1f);
        Button btn = obj.AddComponent<Button>();
        obj.AddComponent<UIJuicyButton>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform trect = txtObj.AddComponent<RectTransform>();
        trect.anchorMin = Vector2.zero;
        trect.anchorMax = Vector2.one;
        trect.offsetMin = Vector2.zero;
        trect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = new Color(0.05f, 0.03f, 0.02f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return btn;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Vector2 pos, Vector2 sizeDelta, FontStyles style)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }
}
