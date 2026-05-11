using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("❤️ Здоровье")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("👁️ Обнаружение")]
    public float detectionRange = 5f;
    public LayerMask obstacleLayer;
    public bool checkLineOfSight = true;

    [Header("🚶 Патруль")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 1.5f;
    private Transform currentTarget;

    [Header("🏃 Преследование и Атака")]
    public float chaseSpeed = 2.5f;
    public float attackRange = 1f;
    public int attackDamage = 1;
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    [Header("Дальний бой")]
    public float rangedAttackRange = 6f;
    public float rangedAttackCooldown = 2.2f;
    public float projectileSpeed = 7f;
    private float nextRangedAttackTime = 0f;

    [Header("Награда")]
    public int scoreReward = 100;

    private Transform player;
    private bool isChasing = false;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb; // Ссылка на физику

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (GetComponent<EnemyVisualController>() == null)
        {
            gameObject.AddComponent<EnemyVisualController>();
        }

        if (pointA == null) CreatePoint(ref pointA, Vector3.left * 3);
        if (pointB == null) CreatePoint(ref pointB, Vector3.right * 3);
        
        currentTarget = pointB;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void CreatePoint(ref Transform point, Vector3 offset)
    {
        point = new GameObject("Point").transform;
        point.position = transform.position + offset;
        point.SetParent(transform);
    }

    void Update()
    {
        if (isDead || player == null) return;
        CheckPlayerDetection();
    }

    // Движение перенесено в FixedUpdate для корректной физики
    void FixedUpdate()
    {
        if (isDead) return;

        if (isChasing) ChasePlayer();
        else Patrol();
    }

    void CheckPlayerDetection()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectionRange)
        {
            bool canSee = true;
            if (checkLineOfSight)
            {
                Vector2 dir = player.position - transform.position;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, obstacleLayer);
                if (hit.collider != null) canSee = false;
            }
            isChasing = canSee;
        }
        else
        {
            isChasing = false;
        }
    }

    void Patrol()
    {
        if (currentTarget == null) return;

        float dir = currentTarget.position.x - transform.position.x;
        float moveX = Mathf.Sign(dir) * patrolSpeed;

        // Двигаем только по оси X. Ось Y управляется гравитацией автоматически!
        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);

        if (Mathf.Abs(dir) < 0.1f)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
        }
        FlipSprite(moveX > 0);
    }

    void ChasePlayer()
    {
        float dir = player.position.x - transform.position.x;
        float moveX = Mathf.Sign(dir) * chaseSpeed;

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
        FlipSprite(moveX > 0);

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float verticalDifference = Mathf.Abs(player.position.y - transform.position.y);

        if (distanceToPlayer <= attackRange && verticalDifference < 0.6f)
        {
            if (Time.time >= nextAttackTime)
            {
                PlayerHealth pScript = player.GetComponent<PlayerHealth>();
                if (pScript != null) pScript.TakeDamage(attackDamage);
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        else if (distanceToPlayer <= rangedAttackRange && verticalDifference < 1.6f)
        {
            TryRangedAttack();
        }
    }

    void FlipSprite(bool faceRight)
    {
        transform.localScale = new Vector3(faceRight ? 1 : -1, 1, 1);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        StartCoroutine(FlashEffect());
        if (currentHealth <= 0) Die();
    }

    System.Collections.IEnumerator FlashEffect()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("💀 Враг уничтожен!");
        GameManager.Instance?.AddScore(scoreReward);
        if (spriteRenderer != null) spriteRenderer.color = Color.gray;
        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Destroy(gameObject, 0.5f);
    }

    void TryRangedAttack()
    {
        if (Time.time < nextRangedAttackTime)
        {
            return;
        }

        Vector2 shootDir = player.position.x >= transform.position.x ? Vector2.right : Vector2.left;
        GameObject projectile = new GameObject("EnemyProjectile");
        projectile.transform.position = transform.position + (Vector3)(shootDir * 0.65f) + Vector3.up * 0.1f;

        SpriteRenderer projectileRenderer = projectile.AddComponent<SpriteRenderer>();
        projectileRenderer.sortingOrder = 4;

        Rigidbody2D projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        CircleCollider2D projectileCollider = projectile.AddComponent<CircleCollider2D>();
        projectileCollider.isTrigger = true;
        projectileCollider.radius = 0.18f;

        Bullet projectileScript = projectile.AddComponent<Bullet>();
        projectileScript.targetTag = "Player";
        projectileScript.ignoreTag = "Enemy";
        projectileScript.damage = attackDamage;
        projectileScript.speed = projectileSpeed;
        projectileScript.lifetime = 4f;
        projectileScript.SetDirection(shootDir);

        nextRangedAttackTime = Time.time + rangedAttackCooldown;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
