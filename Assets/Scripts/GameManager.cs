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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        _isGameOver = false;
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
    PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null || healthIcons == null) return;
        
        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] != null)
            {
                healthIcons[i].SetActive(i < playerHealth.currentHealth);
            }
        }
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Рестарт...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}