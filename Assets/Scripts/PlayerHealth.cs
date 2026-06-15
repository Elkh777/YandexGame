using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("❤️ Здоровье")]
    public int maxHealth = 3;          // число сердец
    public float hpPerHeart = 100f;    // HP на одно сердце (3 сердца = 300 HP)
    public float damagePerHit = 34f;   // во сколько HP конвертируется 1 ед. урона врага (постепенный урон)

    [Header("Эффекты")]
    public float invincibilityTime = 0.5f;
    private bool _isInvincible = false;
    private float _invincibleUntil = 0f; // временная неуязвимость (например, во время рывка)
    private SpriteRenderer _spriteRenderer;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private HealthSystem _health;

    public float CurrentHP => _health != null ? _health.currentHealth : 0f;
    public float MaxHP => _health != null ? _health.maxHealth : maxHealth * hpPerHeart;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null || !_spriteRenderer.enabled)
        {
            Transform visual = transform.Find("Visual");
            if (visual != null) _spriteRenderer = visual.GetComponent<SpriteRenderer>();
        }
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();

        EnsureHealth();
        _health.SetMax(maxHealth * hpPerHeart);
        _health.OnDeath += Die;

        fGameManager.Instance?.SetPlayerHealth(CurrentHP, MaxHP);
    }

    private void EnsureHealth()
    {
        if (_health == null)
        {
            _health = GetComponent<HealthSystem>();
            if (_health == null) _health = gameObject.AddComponent<HealthSystem>();
        }
    }

    // Включает неуязвимость на заданное время (используется рывком — i-frames).
    public void GrantInvincibility(float duration)
    {
        _invincibleUntil = Mathf.Max(_invincibleUntil, Time.time + duration);
    }

    // amount — условные единицы урона врага (1 у обычного, 2 у танка). Конвертируются в HP постепенно.
    public void TakeDamage(int amount)
    {
        EnsureHealth();
        if (_isInvincible || Time.time < _invincibleUntil || _health.IsDead) return;

        _health.TakeDamage(amount * damagePerHit);
        CameraFollow.Instance?.Shake(0.25f, 0.35f); // ощутимая тряска при уроне
        StartCoroutine(InvincibilityCoroutine());
        fGameManager.Instance?.SetPlayerHealth(CurrentHP, MaxHP);
        // Смерть обрабатывается через _health.OnDeath -> Die().
    }

    public void Heal(float amount)
    {
        EnsureHealth();
        _health.Heal(amount);
        fGameManager.Instance?.SetPlayerHealth(CurrentHP, MaxHP);
    }

    System.Collections.IEnumerator InvincibilityCoroutine()
    {
        _isInvincible = true;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invincibilityTime)
        {
            visible = !visible;
            // Красная подсветка при попадании + мигание прозрачностью на время неуязвимости.
            Color c = Color.red;
            c.a = visible ? 1f : 0.4f;
            if (_spriteRenderer != null) _spriteRenderer.color = c;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
        _isInvincible = false;
    }

    void Die()
    {
        Debug.Log("💀 Игрок умер!");
        StopAllCoroutines();

        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (_spriteRenderer != null) _spriteRenderer.color = Color.gray;
        Invoke(nameof(ShowGameOver), 0.5f);
    }

    void ShowGameOver()
    {
        fGameManager.Instance?.ShowGameOver();
    }
}
