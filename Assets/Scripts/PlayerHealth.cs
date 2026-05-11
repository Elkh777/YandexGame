using UnityEngine;
using System.Collections;

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

        GameManager.Instance?.UpdateHealthUI();
    }

    public void TakeDamage(int amount)
    {
        if (_isInvincible || currentHealth <= 0) return;

        currentHealth -= amount;
        Debug.Log($"👤 Урон! Здоровье: {currentHealth}/{maxHealth}");

        StartCoroutine(InvincibilityCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
        GameManager.Instance?.UpdateHealthUI();
    }

    IEnumerator InvincibilityCoroutine()
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

    // 🔥 МЕТОД СМЕРТИ С КРОВАВЫМ ВЗРЫВОМ
    void Die()
    {
        Debug.Log("💀 Игрок умер!");

        // 🔴 Вызываем красивую анимацию взрыва крови
        BloodExplosionEffect.Spawn(transform.position);

        // Отключаем управление и физику
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        _spriteRenderer.enabled = false;

        // Ждём, пока эффект проиграется, потом показываем Game Over
        StartCoroutine(DelayedGameOver());
    }

    // 🔥 ОДИН метод задержки Game Over (убран дубликат!)
    IEnumerator DelayedGameOver()
    {
        yield return new WaitForSeconds(0.9f); // Время на просмотр анимации
        GameManager.Instance?.ShowGameOver();
    }
}