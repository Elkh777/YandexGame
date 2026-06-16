using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;          // Базовый урон пули (можно менять в инспекторе)
    public float damageMultiplier = 1f; // множитель урона от улучшения «Сила оружия» (только для пуль игрока)

    [Header("Эффекты улучшений")]
    public bool applyFreeze = false;    // замораживающая пуля (каждая 2-я при купленном улучшении)
    public float freezeDuration = 1.5f;
    public float freezeSlow = 0.5f;     // множитель скорости замороженного врага
    public bool applyPoison = false;    // ядовитая пуля (урон во времени)
    public float poisonDuration = 2.5f;
    public float poisonDps = 0.6f;

    public float lifetime = 3f;
    public float knockbackForce = 6f; // сила отброса врага по направлению пули
    public string targetTag = "Enemy";
    public string ignoreTag = "Player";

    private Vector2 _direction;
    private SpriteRenderer spriteRenderer;

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.normalized;
        if (_direction != Vector2.zero)
        {
            transform.right = _direction;
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (_direction != Vector2.zero)
        {
            transform.right = _direction;
        }
        SetupVisual();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(_direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger || (!string.IsNullOrEmpty(ignoreTag) && other.CompareTag(ignoreTag)))
        {
            return;
        }

        if (other.CompareTag("Enemy") && targetTag == "Enemy")
        {
            Debug.Log("💥 Попадание! Урон: " + damage);

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage * damageMultiplier);
                enemy.ApplyKnockback(_direction.normalized * knockbackForce);
                if (applyFreeze) enemy.ApplyFreeze(freezeDuration, freezeSlow);
                if (applyPoison) enemy.ApplyPoison(poisonDuration, poisonDps);
            }

            HitSpark.Spawn(transform.position); // белая искра в точке попадания
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player") && targetTag == "Player")
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    void SetupVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        string spritePath = targetTag == "Player" ? "Sprites/enemy_projectile" : "Sprites/projectile_dagger";
        Sprite projectileSprite = Resources.Load<Sprite>(spritePath);
        if (projectileSprite != null)
        {
            spriteRenderer.sprite = projectileSprite;
        }

        spriteRenderer.color = Color.white;
        transform.localScale = Vector3.one;
    }
}
