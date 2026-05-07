using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }
    
    [Header("UI")]
    public GameObject mainMenuPanel;
    
    private bool _isInMainMenu = true;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        ShowMainMenu();
    }
    
    public void ShowMainMenu()
    {
        Time.timeScale = 1f;
        _isInMainMenu = true;
        
        // Загружаем сцену с главным меню или показываем панель
        EnsureMainMenuUI();
    }
    
    public void StartGame()
    {
        _isInMainMenu = false;
        SceneManager.LoadScene("MainScene");
    }
    
    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        _isInMainMenu = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void QuitGame()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }
    
    public bool IsInMainMenu()
    {
        return _isInMainMenu;
    }
    
    private void EnsureMainMenuUI()
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
        
        if (mainMenuPanel == null)
        {
            CreateMainMenuPanel(canvas.transform);
        }
    }
    
    private void CreateMainMenuPanel(Transform parent)
    {
        // Создаем основной панель меню
        mainMenuPanel = new GameObject("MainMenuPanel");
        mainMenuPanel.transform.SetParent(parent, false);
        
        RectTransform panelRect = mainMenuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;
        
        Image panelImage = mainMenuPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Заголовок "MAIN MENU"
        TextMeshProUGUI titleText = CreateText("TitleText", mainMenuPanel.transform, 
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, 150f), new Vector2(600f, 120f), 
            "MAIN MENU", 72, new Color(0.2f, 0.8f, 0.3f));
        titleText.fontStyle = FontStyles.Bold;
        
        // Декоративная рамка вокруг заголовка
        GameObject titleFrame = new GameObject("TitleFrame");
        titleFrame.transform.SetParent(mainMenuPanel.transform, false);
        RectTransform frameRect = titleFrame.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 1f);
        frameRect.anchorMax = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = new Vector2(0f, 150f);
        frameRect.sizeDelta = new Vector2(650f, 140f);
        
        Image frameImage = titleFrame.AddComponent<Image>();
        frameImage.color = new Color(0.2f, 0.8f, 0.3f, 0.3f);
        frameImage.type = Image.Type.Sliced;
        
        // Кнопка START (зеленая, по центру)
        Button startButton = CreateButton("StartButton", mainMenuPanel.transform, 
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(280f, 90f), 
            "START", new Color(0.2f, 0.8f, 0.3f));
        startButton.onClick.AddListener(StartGame);
        
        // Кнопка QUIT (красная, ниже)
        Button quitButton = CreateButton("QuitButton", mainMenuPanel.transform, 
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -90f), new Vector2(280f, 90f), 
            "QUIT", new Color(0.8f, 0.2f, 0.2f));
        quitButton.onClick.AddListener(QuitGame);
        
        // Добавляем декоративные элементы - лес по бокам (упрощенно)
        CreateDecorativeElement(parent);
    }
    
    private void CreateDecorativeElement(Transform parent)
    {
        // Создаем декоративные деревья по бокам меню
        GameObject leftTree = new GameObject("LeftTreeDecoration");
        leftTree.transform.SetParent(parent, false);
        RectTransform leftRect = leftTree.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.2f, 1f);
        leftRect.anchoredPosition = Vector2.zero;
        leftRect.sizeDelta = Vector2.zero;
        
        Image leftImage = leftTree.AddComponent<Image>();
        leftImage.color = new Color(0.05f, 0.15f, 0.05f, 0.5f);
        
        GameObject rightTree = new GameObject("RightTreeDecoration");
        rightTree.transform.SetParent(parent, false);
        RectTransform rightRect = rightTree.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.8f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.anchoredPosition = Vector2.zero;
        rightRect.sizeDelta = Vector2.zero;
        
        Image rightImage = rightTree.AddComponent<Image>();
        rightImage.color = new Color(0.05f, 0.15f, 0.05f, 0.5f);
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
        
        // Фон кнопки
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        
        // Скругленные углы (через sprite если есть, или просто цвет)
        
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        
        // Эффект нажатия
        buttonObject.AddComponent<UIJuicyButton>();
        
        // Текст кнопки
        TextMeshProUGUI text = CreateText("Text", buttonObject.transform, 
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 
            label, 42, new Color(0.02f, 0.03f, 0.04f));
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
}
