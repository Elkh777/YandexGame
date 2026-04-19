using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;          // Урон пули (можно менять в инспекторе)
    public float lifetime = 3f;

    private Vector2 _direction;

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.normalized;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(_direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Если попали во врага
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("💥 Попадание! Урон: " + damage);
            
            // Получаем скрипт врага и наносим урон
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
        
        // Уничтожаем пулю
        Destroy(gameObject);
    }
}