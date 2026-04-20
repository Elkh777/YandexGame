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
    
    private bool _isGameOver = false;

    void Awake()
    {
        // Если Instance уже есть (значит мы перезагрузили сцену и старый объект еще жив мгновение), уничтожаем дубль.
        // Но в нашем новом подходе мы удалим DontDestroyOnLoad, так что это просто страховка.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Подписываем кнопку на клик
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners(); // Очищаем старые подписки
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    void Start()
    {
        // При старте сцены всегда скрываем Game Over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        _isGameOver = false;
        
        // Обновляем сердечки
        UpdateHealthUI();
    }

    public void ShowGameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
            
        Debug.Log("🎮 GAME OVER!");
    }

    public void UpdateHealthUI()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null || healthIcons == null) return;
        
        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] != null)
            {
                // Показываем иконку, если её индекс меньше текущего здоровья
                healthIcons[i].SetActive(i < player.currentHealth);
            }
        }
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Рестарт...");
        // Просто перезагружаем сцену. GameManager умрет и создастся новый, чистый.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}