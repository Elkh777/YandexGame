using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float speed = 5f;
    public int maxJumps = 2;
    private int currentJumps;
    public float jumpForce = 10f;
    public float crouchSpeed = 2f;

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

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private bool isCrouching = false;
    private Vector2 originalSize;
    private Vector2 originalOffset;
    private bool isGrounded = false;
    private float nextFireTime = 0f;
    private bool isRangedMode = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        originalSize = col.size;
        originalOffset = col.offset;

        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = new Vector3(0.5f, 0, 0);
        }
        
        currentHealth = maxHealth;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        currentJumps = maxJumps;
        
        GameManager.Instance?.UpdateHealthUI();
    }

    void Update()
    {
        // === ПРОВЕРКА ЗЕМЛИ (надёжная через OverlapBox) ===
        // === ПРОВЕРКА ЗЕМЛИ (два луча от ног) ===
        float checkDistance = 0.6f;
        isGrounded = Physics2D.Raycast(transform.position + Vector3.left * 0.2f, Vector2.down, checkDistance) ||
                     Physics2D.Raycast(transform.position + Vector3.right * 0.2f, Vector2.down, checkDistance);

        if (isGrounded)
        {
            currentJumps = maxJumps;
        }

        if (Input.GetKeyDown(KeyCode.Space) && currentJumps > 0 && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            currentJumps--;
        }


        // === ПРИСЕДАНИЕ (CTRL) ===
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        UpdateCrouchCollider();

        // === ПОВОРОТ ===
        if (Input.GetKey(KeyCode.D)) transform.localScale = new Vector3(1, 1, 1);
        if (Input.GetKey(KeyCode.A)) transform.localScale = new Vector3(-1, 1, 1);

        // === СМЕНА РЕЖИМА (ПКМ) ===
        if (Input.GetMouseButtonDown(1))
        {
            isRangedMode = !isRangedMode;
            Debug.Log("Режим: " + (isRangedMode ? "🔫 Дальний" : "🗡️ Ближний"));
        }

        // === АТАКА (ЛКМ) ===
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (isRangedMode) Shoot();
            else MeleeAttack();
            nextFireTime = Time.time + fireRate;
        }
    }

    void FixedUpdate()
    {
        float move = 0f;
        if (Input.GetKey(KeyCode.D)) move = 1f;
        if (Input.GetKey(KeyCode.A)) move = -1f;

        float currentSpeed = isCrouching ? crouchSpeed : speed;
        rb.linearVelocity = new Vector2(move * currentSpeed, rb.linearVelocity.y);
    }

    void UpdateCrouchCollider()
    {
        if (isCrouching)
        {
            col.size = new Vector2(originalSize.x, originalSize.y / 2);
            col.offset = new Vector2(originalOffset.x, -originalSize.y / 4);
        }
        else
        {
            col.size = originalSize;
            col.offset = originalOffset;
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
        enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        _spriteRenderer.color = Color.gray;
        Invoke(nameof(ShowGameOver), 0.5f);
    }

    void ShowGameOver()
    {
        GameManager.Instance?.ShowGameOver();
    }
}