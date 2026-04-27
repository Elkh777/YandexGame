using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    [Header("Crouch")]
    public float crouchHeight = 0.5f;
    public float standHeight = 1f;
    public float crouchSpeed = 2.5f;

    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;
    public float meleeRange = 1f;
    public int meleeDamage = 1;
    public float attackCooldown = 0.5f;
    public LayerMask enemyLayer;

    [Header("Weapon")]
    public GameObject meleeIndicator;
    public GameObject rangedIndicator;
    public bool isRangedMode = false;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isCrouching;
    private float attackTimer;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void FixedUpdate()
    {
        float speed = isCrouching ? crouchSpeed : moveSpeed;
        rb.linearVelocity = new Vector2(moveInput.x * speed, rb.linearVelocity.y);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public void OnCrouch(InputValue value)
    {
        isCrouching = value.isPressed;
        Vector3 scale = transform.localScale;
        scale.y = isCrouching ? crouchHeight : standHeight;
        transform.localScale = scale;
    }

    public void OnAttack(InputValue value)
    {
        if (attackTimer > 0) return;
        attackTimer = attackCooldown;

        if (isRangedMode && projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Vector2 direction = (Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - firePoint.position).normalized;
            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null) rbProj.linearVelocity = direction * projectileSpeed;
            Bullet bullet = proj.GetComponent<Bullet>();
            if (bullet != null) bullet.damage = meleeDamage;
        }
        else
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(firePoint.position, meleeRange, enemyLayer);
            foreach (var enemy in hitEnemies)
            {
                enemy.GetComponent<Enemy>()?.TakeDamage(meleeDamage);
            }
        }
    }

    public void OnSwitchMode(InputValue value)
    {
        isRangedMode = !isRangedMode;
        if (meleeIndicator != null) meleeIndicator.SetActive(!isRangedMode);
        if (rangedIndicator != null) rangedIndicator.SetActive(isRangedMode);
    }

    public void TakeDamage(int damage)
    {
        GetComponent<PlayerHealth>().TakeDamage(damage, Vector2.zero);
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
            Gizmos.DrawWireSphere(firePoint.position, meleeRange);
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
    void Start()
{
    if (PlayerPrefs.HasKey("SaveX"))
    {
        float x = PlayerPrefs.GetFloat("SaveX");
        float y = PlayerPrefs.GetFloat("SaveY");
        float z = PlayerPrefs.GetFloat("SaveZ");
        transform.position = new Vector3(x, y, z);
        Debug.Log("📀 Загружено сохранение: " + transform.position);

        if (PlayerPrefs.HasKey("SaveHealth"))
        {
            int savedHealth = PlayerPrefs.GetInt("SaveHealth");
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.currentHealth = savedHealth;
                if (health.healthUI != null)
                    health.healthUI.UpdateHealth(health.currentHealth, health.maxHealth);
            }
        }
    }
}
}