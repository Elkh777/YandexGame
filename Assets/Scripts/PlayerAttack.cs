using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Атака")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Vector2 firePointOffset = new Vector2(0.78f, 0.15f);
    public float fireRate = 0.5f;

    [Header("Ближний бой")]
    public int meleeDamage = 2;
    public float meleeRange = 1.2f;
    public float meleeCooldown = 0.35f;

    private float nextFireTime = 0f;
    private float nextMeleeTime = 0f;
    private PlayerVisualController visualController;
    private Sprite muzzleFlashSprite;

    void Start()
    {
        visualController = GetComponent<PlayerVisualController>();
        if (visualController == null)
        {
            visualController = gameObject.AddComponent<PlayerVisualController>();
        }

        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
        }

        muzzleFlashSprite = Resources.Load<Sprite>("Sprites/muzzle_flash");
        UpdateFirePoint();
    }

    void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        UpdateFirePoint();

        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        if (Input.GetKeyDown(KeyCode.Return) && Time.time >= nextMeleeTime)
        {
            MeleeAttack();
            nextMeleeTime = Time.time + meleeCooldown;
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
        bulletScript.targetTag = "Enemy";
        bulletScript.ignoreTag = "Player";
        bulletScript.SetDirection(shootDir);
        visualController?.PlayShoot();
        ShowMuzzleFlash(shootDir);
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

    void UpdateFirePoint()
    {
        float direction = transform.localScale.x >= 0f ? 1f : -1f;
        firePoint.localPosition = new Vector3(Mathf.Abs(firePointOffset.x) * direction, firePointOffset.y, 0f);
    }

    void ShowMuzzleFlash(Vector2 shootDir)
    {
        if (muzzleFlashSprite == null)
        {
            return;
        }

        GameObject flash = new GameObject("MuzzleFlash");
        flash.transform.position = firePoint.position + (Vector3)(shootDir * 0.18f);
        flash.transform.localScale = new Vector3(shootDir.x > 0f ? 1f : -1f, 1f, 1f);

        SpriteRenderer flashRenderer = flash.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = muzzleFlashSprite;
        flashRenderer.sortingOrder = 5;

        Destroy(flash, 0.08f);
    }
}
