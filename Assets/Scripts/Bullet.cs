using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;          // Урон пули (можно менять в инспекторе)
    public float lifetime = 3f;
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
        SetupVisual();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(_direction * speed * Time.deltaTime);
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
                enemy.TakeDamage(damage);
            }

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
