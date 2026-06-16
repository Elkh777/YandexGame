using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Атака")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Vector2 firePointOffset = new Vector2(0.78f, 0.15f);
    public float fireRate = 0.5f;

    [Header("Ближний бой (Nail)")]
    public int meleeDamage = 2;
    public float meleeRange = 0.9f;       // радиус хитбокса
    public float meleeReach = 1.1f;       // вынос центра хитбокса по направлению атаки
    public float attackOriginY = 0.6f;    // высота центра атаки над пивотом игрока (тюнится по ощущению)
    public float meleeCooldown = 0.3f;
    public float meleeKnockback = 7f;     // отброс врага мечом
    public float pogoForce = 11f;         // подброс игрока при ударе вниз по врагу

    private float nextFireTime = 0f;
    private float nextMeleeTime = 0f;
    private int _shotCount = 0; // счётчик выстрелов для «каждой 2-й» замораживающей пули
    private PlayerVisualController visualController;
    private PlayerMovement playerMovement;
    private Sprite muzzleFlashSprite;

    void Start()
    {
        visualController = GetComponent<PlayerVisualController>();
        if (visualController == null)
        {
            visualController = gameObject.AddComponent<PlayerVisualController>();
        }

        playerMovement = GetComponent<PlayerMovement>();

        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
        }
        firePoint.SetParent(null);

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

        // Множитель скорости атаки от улучшения «Скорость атаки»: меньше интервал = чаще атака.
        float atkSpeed = UpgradeManager.AttackSpeedMultiplier;
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Ближний бой — Shift+Пробел (а также прежние ЛКМ / J / Enter). Направление — вверх/вниз/по взгляду.
        bool meleePressed = (Input.GetKeyDown(KeyCode.Space) && shiftHeld)
            || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Return);
        if (meleePressed && Time.time >= nextMeleeTime)
        {
            MeleeAttack(GetAttackDirection());
            nextMeleeTime = Time.time + meleeCooldown / atkSpeed;
        }

        // Дальний выстрел — Пробел без Shift (чтобы Shift+Пробел не стрелял заодно).
        if (Input.GetKeyDown(KeyCode.Space) && !shiftHeld && Time.time >= nextFireTime && Time.timeScale > 0f)
        {
            Shoot();
            nextFireTime = Time.time + fireRate / atkSpeed;
        }
    }

    // Направление удара: вниз (S/↓), вверх (W/↑), иначе — по направлению взгляда.
    Vector2 GetAttackDirection()
    {
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) return Vector2.down;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) return Vector2.up;
        float f = playerMovement != null ? playerMovement.facingDirection : (transform.localScale.x >= 0f ? 1f : -1f);
        return new Vector2(f, 0f);
    }

    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Префаб пули не назначен!");
            return;
        }
        Vector3 bulletPos = firePoint.position;
        bulletPos.y += 1f;
        GameObject bullet = Instantiate(bulletPrefab, bulletPos, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript == null)
        {
            Debug.LogWarning("На префабе пули нет скрипта Bullet!");
            Destroy(bullet);
            return;
        }

        Vector2 shootDir = new Vector2(playerMovement.facingDirection, 0f);

        GameObject nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            Vector2 toEnemy = (Vector2)nearestEnemy.transform.position - (Vector2)bulletPos;
            shootDir = toEnemy.normalized;
        }

        bulletScript.targetTag = "Enemy";
        bulletScript.ignoreTag = "Player";
        bulletScript.damageMultiplier = UpgradeManager.WeaponDamageMultiplier; // «Сила оружия»

        _shotCount++;

        // ❄️ Заморозка: помечаем каждую 2-ю пулю.
        if (UpgradeManager.FreezeEnabled && _shotCount % 2 == 0)
        {
            bulletScript.applyFreeze = true;
            bulletScript.freezeDuration = UpgradeManager.FreezeDuration;
            bulletScript.freezeSlow = UpgradeManager.FreezeSlowFactor;
        }

        // ☠️ Яд: помечаем все пули, пока улучшение куплено.
        if (UpgradeManager.PoisonEnabled)
        {
            bulletScript.applyPoison = true;
            bulletScript.poisonDuration = UpgradeManager.PoisonDuration;
            bulletScript.poisonDps = UpgradeManager.PoisonDps;
        }

        bulletScript.SetDirection(shootDir);
        visualController?.PlayShoot();
        ShowMuzzleFlash(shootDir);
        AudioManager.Instance?.PlayShoot();
    }

    void MeleeAttack(Vector2 dir)
    {
        Vector3 origin = transform.position + Vector3.up * attackOriginY;
        Vector3 center = origin + (Vector3)(dir * meleeReach);

        // Визуальный взмах в направлении удара (всегда, даже если никого не задели).
        SlashEffect.Spawn(center, dir);

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeRange);

        bool hitEnemy = false;
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead) continue;

            enemy.TakeDamage(meleeDamage * UpgradeManager.WeaponDamageMultiplier); // «Сила оружия»
            enemy.ApplyKnockback(dir * meleeKnockback);
            HitSpark.Spawn(hit.bounds.ClosestPoint(center));
            hitEnemy = true;
        }

        visualController?.PlayShoot(); // короткая поза атаки
        if (hitEnemy) AudioManager.Instance?.PlayHit();

        // Pogo: удар вниз по врагу в воздухе подбрасывает игрока вверх.
        if (hitEnemy && dir == Vector2.down && playerMovement != null && !playerMovement.isGrounded)
        {
            playerMovement.ApplyPogo(pogoForce);
        }
    }

    void UpdateFirePoint()
    {
        float direction = playerMovement != null ? playerMovement.facingDirection : (transform.localScale.x >= 0f ? 1f : -1f);
        Vector3 offset = new Vector3(Mathf.Abs(firePointOffset.x) * direction, firePointOffset.y, 0f);
        firePoint.position = transform.position + offset;
    }

    void ShowMuzzleFlash(Vector2 shootDir)
    {
        if (muzzleFlashSprite == null)
        {
            return;
        }

        GameObject flash = new GameObject("MuzzleFlash");
        Vector3 flashPos = firePoint.position + (Vector3)(shootDir * 0.35f);
        flashPos.y += 1f;
        flash.transform.position = flashPos;
        flash.transform.localScale = new Vector3(shootDir.x > 0f ? 1f : -1f, 1f, 1f);

        SpriteRenderer flashRenderer = flash.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = muzzleFlashSprite;
        flashRenderer.sortingOrder = 5;

        Destroy(flash, 0.08f);
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float nearestDist = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            Vector2 dir = (Vector2)enemy.transform.position - (Vector2)transform.position;
            if (Mathf.Sign(dir.x) != Mathf.Sign(playerMovement.facingDirection))
                continue;

            float dist = dir.sqrMagnitude;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
