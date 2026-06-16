using UnityEngine;

// Вид врага. Поведение и характеристики задаются через ApplyKind (data-driven, т.к. враг
// инстанцируется из одного префаба — подмена компонента-подкласса была бы хрупкой).
public enum EnemyKind { Normal, Tank, Rusher }

public class Enemy : MonoBehaviour
{
    [Header("❤️ Здоровье")]
    public int maxHealth = 3;
    private HealthSystem _health;
    private bool _kindApplied = false;
    // Нормализованное HP (0..1) для полоски здоровья над врагом.
    public float HealthNormalized => _health != null ? _health.Normalized : 1f;

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
    public float attackRange = 1.5f;
    public int attackDamage = 1;
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    [Header("Дальний бой")]
    public float rangedAttackRange = 6f;
    public float rangedAttackCooldown = 2.2f;
    public float projectileSpeed = 7f;
    private float nextRangedAttackTime = 0f;

    [Header("Награда")]
    public int scoreReward = 10;

    // Спрайт тела врага. Назначается спавнером, чтобы выбрать один из нескольких видов врагов.
    [HideInInspector] public Sprite bodySprite;

    // true — исходный спрайт нарисован лицом вправо (враги 1-5); false — лицом влево (призрак).
    [HideInInspector] public bool spriteFacesRight = true;
    // Цвет спрайта (по умолчанию белый = без тинта; элитные враги подкрашиваются).
    [HideInInspector] public Color tintColor = Color.white;

    [Header("Вид врага")]
    public EnemyKind kind = EnemyKind.Normal;
    // Высота врага в игровых единицах (читается EnemyVisualController; задаёт и размер хитбокса).
    [HideInInspector] public float visualHeight = 2.2f;
    // Сопротивление отбросу: 0 — отлетает полностью, 1 — не двигается (танк — тяжёлый).
    [HideInInspector] public float knockbackResistance = 0.2f;
    public float knockbackDuration = 0.15f;
    private float _knockbackUntil = 0f;

    // Статус-эффекты от пуль игрока (улучшения «Заморозка»/«Яд»).
    private float _frozenUntil = 0f;
    private float _freezeSlow = 1f;     // множитель скорости, пока заморожен
    private float _poisonUntil = 0f;
    private float _poisonDps = 0f;
    private float _nextPoisonTick = 0f;
    private float _flashUntil = 0f;     // таймер красной вспышки при попадании
    private bool _statusColorApplied = false; // активна ли сейчас покраска статусом
    private static readonly Color FreezeColor = new Color(0.45f, 0.75f, 1f, 1f); // голубой «замёрзший»
    private static readonly Color PoisonColor = new Color(0.5f, 1f, 0.4f, 1f);   // зелёный «отравленный»

    [Header("Честный спавн")]
    public float spawnGrace = 0.4f;      // задержка перед первой атакой/преследованием после появления
    private float _actReadyTime = 0f;

    private Transform player;
    private bool isChasing = false;
    private bool isDead = false;
    public bool IsDead => isDead;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        EnsureHealth();
        if (!_kindApplied) _health.SetMax(maxHealth);
        _health.OnDeath += Die; // смерть — через событие системы здоровья

        if (GetComponent<EnemyVisualController>() == null)
        {
            gameObject.AddComponent<EnemyVisualController>();
        }

        if (pointA == null) CreatePoint(ref pointA, Vector3.left * 3);
        if (pointB == null) CreatePoint(ref pointB, Vector3.right * 3);

        currentTarget = pointB;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        _actReadyTime = Time.time + spawnGrace; // честная задержка перед агрессией

        EnemyHealthBar.Create(this); // полоска здоровья над головой
    }

    private void EnsureHealth()
    {
        if (_health == null)
        {
            _health = GetComponent<HealthSystem>();
            if (_health == null) _health = gameObject.AddComponent<HealthSystem>();
        }
    }

    void CreatePoint(ref Transform point, Vector3 offset)
    {
        point = new GameObject("Point").transform;
        point.position = transform.position + offset;
        point.SetParent(transform);
    }

    void Update()
    {
        if (isDead) return;
        UpdateStatusEffects(); // яд и цвет (заморозка/яд/вспышка) — независимо от агрессии
        if (player == null) return;
        if (Time.time < _actReadyTime) return; // grace: ещё не агрессивен
        CheckPlayerDetection();
    }

    // Тик яда + покраска спрайта по текущему статусу.
    private void UpdateStatusEffects()
    {
        // Яд: периодический урон, пока действует (без красной вспышки).
        if (Time.time < _poisonUntil && _poisonDps > 0f && Time.time >= _nextPoisonTick)
        {
            _nextPoisonTick = Time.time + 0.5f;
            EnsureHealth();
            _health.TakeDamage(_poisonDps * 0.5f); // урон за тик (0.5с)
            if (isDead) return; // враг мог умереть от яда — цвет уже выставлен в Die()
        }

        if (spriteRenderer == null) return;

        // Красим только при активном статусе; иначе возвращаем базовый тинт один раз,
        // чтобы не перезаписывать цвет каждый кадр (не мешать визуальному контроллеру).
        bool anyStatus = Time.time < _flashUntil || Time.time < _frozenUntil || Time.time < _poisonUntil;
        if (anyStatus)
        {
            // Приоритет цвета: вспышка урона > заморозка > яд.
            Color target;
            if (Time.time < _flashUntil) target = Color.red;
            else if (Time.time < _frozenUntil) target = FreezeColor;
            else target = PoisonColor;
            spriteRenderer.color = target;
            _statusColorApplied = true;
        }
        else if (_statusColorApplied)
        {
            spriteRenderer.color = tintColor;
            _statusColorApplied = false;
        }
    }

    // ❄️ Замораживает врага: замедляет движение и красит в голубой на время.
    public void ApplyFreeze(float duration, float slow)
    {
        if (isDead) return;
        _frozenUntil = Mathf.Max(_frozenUntil, Time.time + duration);
        _freezeSlow = Mathf.Clamp01(slow);
    }

    // ☠️ Отравляет врага: урон во времени + зелёный цвет.
    public void ApplyPoison(float duration, float dps)
    {
        if (isDead) return;
        _poisonUntil = Mathf.Max(_poisonUntil, Time.time + duration);
        _poisonDps = Mathf.Max(_poisonDps, dps);
        if (_nextPoisonTick < Time.time) _nextPoisonTick = Time.time + 0.5f;
    }

    // Текущий множитель скорости с учётом заморозки.
    private float CurrentSpeedFactor() => Time.time < _frozenUntil ? _freezeSlow : 1f;

    // Уровень земли везде около -3; всё, что упало ниже -40, считаем выпавшим из мира.
    private const float KillPlaneY = -40f;

    void FixedUpdate()
    {
        if (isDead) return;

        // Страховка от софт-лока: провалившийся за пределы мира враг не должен «висеть» живым
        // и блокировать зачистку энкаунтера (а значит — открытие портала).
        if (transform.position.y < KillPlaneY)
        {
            ForceDespawn();
            return;
        }

        // Честная задержка после появления: стоим на месте (только гравитация), не преследуем.
        if (Time.time < _actReadyTime)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Во время отброса враг летит по инерции — не задаём свою скорость.
        if (Time.time < _knockbackUntil) return;

        if (isChasing) ChasePlayer();
        else Patrol();
    }

    // Отбрасывает врага (вызывается пулей игрока). Танк почти не реагирует.
    public void ApplyKnockback(Vector2 force)
    {
        if (isDead || rb == null) return;
        float k = 1f - Mathf.Clamp01(knockbackResistance);
        rb.linearVelocity = new Vector2(force.x * k, rb.linearVelocity.y);
        _knockbackUntil = Time.time + knockbackDuration;
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
        float moveX = Mathf.Sign(dir) * patrolSpeed * CurrentSpeedFactor();

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
        float moveX = Mathf.Sign(dir) * chaseSpeed * CurrentSpeedFactor();

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
        FlipSprite(moveX > 0);

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float verticalDifference = Mathf.Abs(player.position.y - transform.position.y);

        if (distanceToPlayer <= attackRange && verticalDifference < 1.2f)
        {
            if (Time.time >= nextAttackTime)
            {
                PlayerHealth pScript = player.GetComponent<PlayerHealth>();
                if (pScript != null) pScript.TakeDamage(attackDamage);
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        else if (distanceToPlayer <= rangedAttackRange && verticalDifference < 2f)
        {
            TryRangedAttack();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (Time.time < _actReadyTime) return; // не бьём в фазе появления
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerHealth pScript = collision.gameObject.GetComponent<PlayerHealth>();
        if (pScript != null && Time.time >= nextAttackTime)
        {
            pScript.TakeDamage(attackDamage);
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void FlipSprite(bool faceRight)
    {
        // Для спрайта, нарисованного лицом вправо, взгляд вправо = +X, влево = -X.
        // Для спрайта лицом влево (призрак) — наоборот.
        float sign = (faceRight == spriteFacesRight) ? 1f : -1f;
        float x = Mathf.Abs(transform.localScale.x) * sign;
        transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
    }

    // Настраивает характеристики и поведение под вид врага и силу волны (масштаб HP).
    public void ApplyKind(EnemyKind k, float hpMultiplier)
    {
        kind = k;
        checkLineOfSight = false;
        detectionRange = 9999f; // волновые враги всегда идут на игрока, без патруля

        switch (k)
        {
            case EnemyKind.Tank:
                maxHealth = Mathf.Max(1, Mathf.RoundToInt(8f * hpMultiplier));
                chaseSpeed = 1.1f;
                attackDamage = 2;
                attackRange = 1.9f;
                attackCooldown = 1.8f;
                rangedAttackRange = 0f; // только ближний бой
                visualHeight = 3.1f;    // крупный — большой хитбокс
                knockbackResistance = 0.75f; // тяжёлый, почти не отлетает
                scoreReward = 25;
                break;

            case EnemyKind.Rusher:
                maxHealth = Mathf.Max(1, Mathf.RoundToInt(2f * hpMultiplier));
                chaseSpeed = 4.6f;
                attackDamage = 1;
                attackRange = 1.2f;
                attackCooldown = 1.0f;
                rangedAttackRange = 0f; // только ближний бой, сближается
                visualHeight = 1.7f;    // мелкий и юркий
                knockbackResistance = 0f; // лёгкий — отлетает сильно
                scoreReward = 10;
                break;

            default: // Normal — сбалансированный, умеет стрелять
                maxHealth = Mathf.Max(1, Mathf.RoundToInt(3f * hpMultiplier));
                chaseSpeed = 2.6f;
                attackDamage = 1;
                attackRange = 1.5f;
                attackCooldown = 1.5f;
                rangedAttackRange = 6f;
                visualHeight = 2.2f;
                knockbackResistance = 0.2f;
                scoreReward = 15;
                break;
        }

        EnsureHealth();
        _health.SetMax(maxHealth);
        _kindApplied = true;
    }

    public void TakeDamage(int amount) => TakeDamage((float)amount);

    // Дробный урон: позволяет улучшениям (например, +10% к силе оружия) масштабировать урон
    // без потерь на целочисленном округлении. HealthSystem работает во float.
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        EnsureHealth();
        _health.TakeDamage(amount);
        _flashUntil = Time.time + 0.12f; // красная вспышка (цвет ведёт UpdateStatusEffects)
        // Смерть обрабатывается через _health.OnDeath -> Die().
    }

    void Die()
    {
        isDead = true;
        Debug.Log("💀 Враг уничтожен!");
        fGameManager.Instance?.AddScore(scoreReward);
        CoinManager.Instance?.SpawnCoins(transform.position);
        AudioManager.Instance?.PlayEnemyDeath();
        CameraFollow.Instance?.Shake(0.15f, 0.18f); // лёгкая тряска при убийстве
        if (spriteRenderer != null) spriteRenderer.color = Color.gray;
        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Destroy(gameObject, 0.5f);
    }

    // Тихий деспавн без наград и звука (в отличие от Die) — для врагов, выпавших из мира.
    private void ForceDespawn()
    {
        if (isDead) return;
        isDead = true;
        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Destroy(gameObject);
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
