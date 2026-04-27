using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth; // сделано публичным для доступа из GameManager
    public float invincibilityTime = 1f;
    private float invincibilityTimer;
    public HealthUI healthUI; // заменено с HPView на HealthUI

    private void Start()
    {
        currentHealth = maxHealth;
        if (healthUI != null) healthUI.UpdateHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invincibilityTimer > 0)
            invincibilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (invincibilityTimer > 0) return;
        currentHealth -= amount;
        if (healthUI != null) healthUI.UpdateHealth(currentHealth, maxHealth);
        invincibilityTimer = invincibilityTime;

        // Отбрасывание
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = knockback;

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        // Просто перезагружаем сцену; сохранённая позиция загрузится из SavePoint в Start() PlayerController
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthUI != null) healthUI.UpdateHealth(currentHealth, maxHealth);
    }
}