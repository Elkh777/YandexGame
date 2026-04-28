using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Атака")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;

    [Header("Ближний бой")]
    public int meleeDamage = 2;
    public float meleeRange = 1.2f;

    private float nextFireTime = 0f;

    void Start()
    {
        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = new Vector3(0.5f, 0, 0);
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
        {
            Shoot();
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
        if (bulletScript == null)
        {
            Debug.LogWarning("На префабе пули нет скрипта Bullet!");
            Destroy(bullet);
            return;
        }

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
}
