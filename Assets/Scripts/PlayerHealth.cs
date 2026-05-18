using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("❤️ Здоровье")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;
    
    [Header("Эффекты")]
    public float invincibilityTime = 1f;
    private bool _isInvincible = false;
    private SpriteRenderer _spriteRenderer;
    private PlayerMovement movement;
    private PlayerAttack attack;

    void Start()
    {
        currentHealth = maxHealth;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        
        fGameManager.Instance?.UpdateHealthUI();
    }

    public void TakeDamage(int amount)
    {
        if (_isInvincible || currentHealth <= 0) return;
        
        currentHealth -= amount;
        Debug.Log($"👤 Урон! Здоровье: {currentHealth}/{maxHealth} | Источник урона: {new System.Diagnostics.StackTrace()}");
        
        StartCoroutine(InvincibilityCoroutine());
        
        if (currentHealth <= 0)
        {
            Die();
        }
        fGameManager.Instance?.UpdateHealthUI();
    }

    System.Collections.IEnumerator InvincibilityCoroutine()
    {
        _isInvincible = true;
        float elapsed = 0f;
        
        while (elapsed < invincibilityTime)
        {
            _spriteRenderer.enabled = !_spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        _spriteRenderer.enabled = true;
        _isInvincible = false;
    }

    void Die()
    {
        Debug.Log("💀 Игрок умер!");
        if (movement != null)
        {
            movement.enabled = false;
        }
        if (attack != null)
        {
            attack.enabled = false;
        }
        enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        _spriteRenderer.color = Color.gray;
        Invoke(nameof(ShowGameOver), 0.5f);
    }

    void ShowGameOver()
    {
        fGameManager.Instance?.ShowGameOver();
    }
}