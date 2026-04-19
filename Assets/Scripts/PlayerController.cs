using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float speed = 5f;
    public float jumpForce = 10f;
    public float crouchSpeed = 2f;

    [Header("Атака")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;

    [Header("Ближний бой")]
    public int meleeDamage = 2;
    public float meleeRange = 1.2f;

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
    }

    void Update()
    {
        // === ПРОВЕРКА ЗЕМЛИ ===
        // Луч вниз от центра игрока. Длина 0.55f гарантирует срабатывание только на полу
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.55f);

        // === ПРЫЖОК (только когда на земле и не приседает) ===
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // === ПРИСЕДАНИЕ ===
        isCrouching = Input.GetKey(KeyCode.S);
        UpdateCrouchCollider();

        // === ПОВОРОТ ===
        if (Input.GetKey(KeyCode.D)) transform.localScale = new Vector3(1, 1, 1);
        if (Input.GetKey(KeyCode.A)) transform.localScale = new Vector3(-1, 1, 1);

        // === ПЕРЕКЛЮЧЕНИЕ РЕЖИМА (R) ===
        if (Input.GetKeyDown(KeyCode.R))
        {
            isRangedMode = !isRangedMode;
            Debug.Log("Режим: " + (isRangedMode ? "🔫 Стрельба" : "🗡️ Ближний бой"));
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
        Debug.Log($"👤 Игрок получил урон: {amount}");
        // Здесь позже добавим логику здоровья
    }
}