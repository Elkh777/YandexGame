using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public GameObject gameOverPanel;
    public Button restartButton;

    [Header("Здоровье")]
    public GameObject[] healthIcons;

    [Header("Очки")]
    public int targetScore = 1000;

    private bool _isGameOver = false;
    private bool _isGameWon = false;
    private bool _isPaused = false;
    private int _score = 0;

    private TextMeshProUGUI _scoreText;
    private TextMeshProUGUI _pauseButtonText;
    private TextMeshProUGUI _resultTitleText;
    private GameObject _resultPanel;
    private Button _startButton;
    private Button _runtimeRestartButton;
    private Button _exitButton;

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
        Time.timeScale = 1f;
        _isGameOver = false;
        _isGameWon = false;
        _isPaused = false;
        _score = 0;

        EnsureRuntimeUI();
        EnsureRuntimeSystems();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (_resultPanel != null)
        {
            _resultPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        UpdateHealthUI();
        UpdateScoreUI();
        UpdatePauseButtonText();
    }

    public void ShowGameOver()
    {
        if (_isGameOver || _isGameWon) return;
        _isGameOver = true;
        ShowResultPanel("GAME OVER", new Color(0.9f, 0.08f, 0.08f), false);
        Debug.Log("GAME OVER!");
    }

    public void UpdateHealthUI()
    {
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player == null || healthIcons == null) return;

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] != null)
            {
                healthIcons[i].SetActive(i < player.currentHealth);
            }
        }
    }

    public void AddScore(int amount)
    {
        if (_isGameOver || _isGameWon) return;

        _score += amount;
        UpdateScoreUI();

        if (_score >= targetScore)
        {
            ShowGameWin();
        }
    }

    public int GetScore()
    {
        return _score;
    }

    public void ShowGameWin()
    {
        if (_isGameWon || _isGameOver) return;
        _isGameWon = true;
        ShowResultPanel("GAME WIN", new Color(0.25f, 0.95f, 0.35f), true);
        Debug.Log("GAME WIN!");
    }

    public void TogglePause()
    {
        if (_isGameOver || _isGameWon) return;

        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        UpdatePauseButtonText();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
        Application.Quit();
    }

    private void ShowResultPanel(string title, Color titleColor, bool showStartButton)
    {
        EnsureRuntimeUI();
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
        }

        if (_resultTitleText != null)
        {
            _resultTitleText.text = title;
            _resultTitleText.color = titleColor;
        }

        if (_startButton != null)
        {
            _startButton.gameObject.SetActive(showStartButton);
        }
    }

    private void EnsureRuntimeUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (_scoreText == null)
        {
            _scoreText = CreateText("ScoreText", canvas.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(520f, -74f), new Vector2(320f, 60f), "SCORE: 0", 36, Color.white);
            _scoreText.alignment = TextAlignmentOptions.Left;
        }

        if (_pauseButtonText == null)
        {
            Button pauseButton = CreateButton("PauseButton", canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-150f, -74f), new Vector2(180f, 64f), "PAUSE", new Color(1f, 0.63f, 0.1f));
            pauseButton.onClick.AddListener(TogglePause);
            _pauseButtonText = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (_resultPanel == null)
        {
            CreateResultPanel(canvas.transform);
        }
    }

    private void EnsureRuntimeSystems()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CameraFollow>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow>();
        }

        if (FindFirstObjectByType<EnemySpawner>() == null)
        {
            GameObject spawnerObject = new GameObject("EnemySpawner");
            spawnerObject.AddComponent<EnemySpawner>();
        }
    }

    private void CreateResultPanel(Transform parent)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(parent, false);

        RectTransform panelRect = _resultPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 430f);

        Image panelImage = _resultPanel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.02f, 0.025f, 0.94f);

        _resultTitleText = CreateText("ResultTitle", _resultPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -92f), new Vector2(700f, 140f), "GAME OVER", 86, Color.red);
        _resultTitleText.fontStyle = FontStyles.Bold;

        _startButton = CreateButton("StartButton", _resultPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-230f, 90f), new Vector2(190f, 72f), "START", new Color(0.1f, 0.72f, 0.22f));
        _startButton.onClick.AddListener(RestartGame);

        _runtimeRestartButton = CreateButton("RestartButton", _resultPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 90f), new Vector2(220f, 72f), "RESTART", new Color(1f, 0.63f, 0.1f));
        _runtimeRestartButton.onClick.AddListener(RestartGame);

        _exitButton = CreateButton("ExitButton", _resultPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(240f, 90f), new Vector2(180f, 72f), "EXIT", new Color(0.88f, 0.1f, 0.08f));
        _exitButton.onClick.AddListener(ExitGame);

        _resultPanel.SetActive(false);
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, string label, Color color)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.22f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        buttonObject.AddComponent<UIJuicyButton>();

        TextMeshProUGUI text = CreateText("Text", buttonObject.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, label, 34, new Color(0.02f, 0.03f, 0.04f));
        text.fontStyle = FontStyles.Bold;

        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, string text, float fontSize, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return tmp;
    }

    private void UpdateScoreUI()
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"SCORE: {_score}";
        }
    }

    private void UpdatePauseButtonText()
    {
        if (_pauseButtonText != null)
        {
            _pauseButtonText.text = _isPaused ? "START" : "PAUSE";
        }
    }
}
