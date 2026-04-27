using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Атака")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;

    [Header("Ближний бой")]
    public int meleeDamage = 2;
    public float meleeRange = 1.2f;

    [Header("❤️ Здоровье")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;
    
    [Header("Эффекты")]
    public float invincibilityTime = 1f;
    private bool _isInvincible = false;
    private SpriteRenderer _spriteRenderer;
    private PlayerMovement movement;

    private float nextFireTime = 0f;
    private bool isRangedMode = true;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        if (movement == null)
        {
            movement = gameObject.AddComponent<PlayerMovement>();
        }

        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = new Vector3(0.5f, 0, 0);
        }
        
        currentHealth = maxHealth;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        GameManager.Instance?.UpdateHealthUI();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRangedMode = !isRangedMode;
            Debug.Log("Режим: " + (isRangedMode ? "🔫 Дальний" : "🗡️ Ближний"));
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (isRangedMode) Shoot();
            else MeleeAttack();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Префаб пули не назначен!");
            return;
        }
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        Vector2 shootDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        bulletScript.SetDirection(shootDir);
    }

    void MeleeAttack()
    {
        Vector2 attackPos = transform.position + (transform.localScale.x > 0 ? Vector3.right : Vector3.left);
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, meleeRange);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(meleeDamage);
                    Debug.Log($"🗡️ Удар по врагу! Урон: {meleeDamage}");
                }
            }
        }
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
        GameManager.Instance?.ShowGameOver();
    }
}