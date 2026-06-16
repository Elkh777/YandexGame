using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Маневренный, "шустрый" контроллер: отзывчивое управление (мгновенная остановка/разворот,
// без увеличения скорости бега), variable jump + утяжелённое падение, полный air-control,
// рывок с i-frames и проходом сквозь врагов, wall jump/slide. Pogo — через ApplyPogo (из PlayerAttack).
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Движение (скорость бега НЕ увеличиваем)")]
    public float speed = 6f;
    public float acceleration = 110f;     // очень быстрый разгон/разворот (отзывчивость)
    public float crouchSpeed = 2f;
    public float groundCheckDistance = 0.12f;
    public Vector3 playerScale = new Vector3(0.3f, 0.3f, 0.3f);

    [Header("Прыжок (variable height)")]
    public KeyCode jumpKey = KeyCode.W;
    public float jumpForce = 11f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.45f; // обрезка прыжка при отпускании
    public float fallMultiplier = 2.2f;                     // утяжелённое падение (снаппи)

    [Header("Рывок (Dash)")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public float dashSpeed = 18f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 1.1f;

    [Header("Стены (Wall Jump / Slide)")]
    public float wallCheckDistance = 0.25f;
    public float wallSlideSpeed = 2.5f;
    public Vector2 wallJumpForce = new Vector2(9f, 11f);
    public float wallJumpLockTime = 0.14f;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private PlayerHealth health;

    private bool isCrouching = false;
    private Vector2 originalSize;
    private Vector2 originalOffset;
    public int facingDirection { get; private set; } = 1;
    public bool isGrounded { get; private set; } = false;

    private bool isDashing = false;
    private float dashEndTime = 0f;
    private float nextDashTime = 0f;
    private float dashDir = 1f;
    private readonly List<Collider2D> _dashIgnored = new List<Collider2D>();

    private int _wallSide = 0;          // +1 стена справа, -1 слева, 0 нет
    private float _wallJumpLockUntil = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        health = GetComponent<PlayerHealth>();
        originalSize = col.size;
        originalOffset = col.offset;
        transform.localScale = playerScale;
    }

    void Update()
    {
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        UpdateCrouchCollider();
        isGrounded = CheckGrounded();
        _wallSide = DetectWallSide();

        // Поворот по вводу.
        if (Input.GetKey(KeyCode.D)) SetFacing(1);
        if (Input.GetKey(KeyCode.A)) SetFacing(-1);

        // Рывок.
        if (Input.GetKeyDown(dashKey) && !isDashing && Time.time >= nextDashTime)
        {
            StartDash();
        }

        // Прыжок: с земли — обычный, у стены в воздухе — wall jump.
        if (Input.GetKeyDown(jumpKey))
        {
            if (isGrounded && !isCrouching) rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            else if (_wallSide != 0) WallJump();
        }

        // Variable jump height: отпустили кнопку на подъёме — обрезаем прыжок.
        if (Input.GetKeyUp(jumpKey) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    void FixedUpdate()
    {
        // Рывок: плоский импульс, перехватывает движение и гравитацию.
        if (isDashing)
        {
            if (Time.time < dashEndTime)
            {
                rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
                return;
            }
            isDashing = false;
            SetEnemyCollisionIgnored(false); // вернуть столкновения с врагами
        }

        // Горизонтальное движение: мгновенная остановка без скольжения, быстрый разгон/разворот.
        // Во время wall-jump лока ввод не перехватываем — даём импульсу отбросить игрока от стены.
        if (Time.time >= _wallJumpLockUntil)
        {
            float move = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
            float targetVx = move * (isCrouching ? crouchSpeed : speed);
            float vx = Mathf.Abs(move) > 0.01f
                ? Mathf.MoveTowards(rb.linearVelocity.x, targetVx, acceleration * Time.fixedDeltaTime)
                : 0f; // мгновенная остановка
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        // Утяжелённое падение (снаппи прыжок).
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }

        // Wall slide: у стены в воздухе при падении и удержании к стене — медленное сползание.
        if (_wallSide != 0 && !isGrounded && rb.linearVelocity.y < 0f)
        {
            bool pressingToWall = (Input.GetKey(KeyCode.D) && _wallSide == 1) || (Input.GetKey(KeyCode.A) && _wallSide == -1);
            if (pressingToWall && rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    // Подброс вверх (Pogo) — вызывается ударом вниз по врагу в воздухе (из PlayerAttack).
    public void ApplyPogo(float upForce)
    {
        isDashing = false;
        SetEnemyCollisionIgnored(false);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, upForce);
    }

    private void WallJump()
    {
        rb.linearVelocity = new Vector2(-_wallSide * wallJumpForce.x, wallJumpForce.y);
        SetFacing(-_wallSide);
        _wallJumpLockUntil = Time.time + wallJumpLockTime;
        AudioManager.Instance?.PlaySfx("jump");
    }

    private void SetFacing(int dir)
    {
        facingDirection = dir;
        transform.localScale = new Vector3(playerScale.x * dir, playerScale.y, playerScale.z);
    }

    void StartDash()
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
        float inX = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
        dashDir = Mathf.Abs(inX) > 0.01f ? Mathf.Sign(inX) : facingDirection;

        health?.GrantInvincibility(dashDuration + 0.05f); // i-frames: не получаем урон
        SetEnemyCollisionIgnored(true);                    // и физически проходим сквозь врагов
        AudioManager.Instance?.PlaySfx("dash");
        StartCoroutine(DashTrail());
    }

    // Включает/выключает игнор столкновений игрока с врагами (для прохода сквозь них во время рывка).
    private void SetEnemyCollisionIgnored(bool ignore)
    {
        if (ignore)
        {
            _dashIgnored.Clear();
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in enemies)
            {
                Collider2D ec = e != null ? e.GetComponent<Collider2D>() : null;
                if (ec != null && col != null)
                {
                    Physics2D.IgnoreCollision(col, ec, true);
                    _dashIgnored.Add(ec);
                }
            }
        }
        else
        {
            foreach (Collider2D ec in _dashIgnored)
                if (ec != null && col != null) Physics2D.IgnoreCollision(col, ec, false);
            _dashIgnored.Clear();
        }
    }

    IEnumerator DashTrail()
    {
        SpriteRenderer src = GetComponentInChildren<SpriteRenderer>();
        float t = 0f;
        while (t < dashDuration && src != null)
        {
            SpawnAfterimage(src);
            t += 0.04f;
            yield return new WaitForSeconds(0.04f);
        }
    }

    void SpawnAfterimage(SpriteRenderer src)
    {
        if (src.sprite == null) return;
        GameObject g = new GameObject("DashAfterimage");
        g.transform.position = src.transform.position;
        g.transform.rotation = src.transform.rotation;
        g.transform.localScale = src.transform.lossyScale;
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = src.sprite;
        sr.sortingOrder = src.sortingOrder - 1;
        sr.color = new Color(0.5f, 0.85f, 1f, 0.5f);
        StartCoroutine(FadeAfterimage(g, sr));
    }

    IEnumerator FadeAfterimage(GameObject g, SpriteRenderer sr)
    {
        float a = 0.5f;
        while (a > 0f && sr != null)
        {
            a -= Time.deltaTime * 2.5f;
            Color c = sr.color;
            c.a = Mathf.Max(0f, a);
            sr.color = c;
            yield return null;
        }
        Destroy(g);
    }

    bool CheckGrounded()
    {
        Bounds bounds = col.bounds;
        float inset = Mathf.Min(bounds.extents.x * 0.5f, 0.1f);
        float startY = bounds.min.y - 0.01f;

        return IsGroundBelow(new Vector2(bounds.min.x + inset, startY)) ||
               IsGroundBelow(new Vector2(bounds.center.x, startY)) ||
               IsGroundBelow(new Vector2(bounds.max.x - inset, startY));
    }

    bool IsGroundBelow(Vector2 origin)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, groundCheckDistance);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider != col && !hit.collider.isTrigger)
                return true;
        }
        return false;
    }

    // Определяет, есть ли стена сбоку (для wall jump/slide). Только в воздухе.
    int DetectWallSide()
    {
        if (isGrounded) return 0;
        if (WallOnSide(1)) return 1;
        if (WallOnSide(-1)) return -1;
        return 0;
    }

    bool WallOnSide(int side)
    {
        Bounds b = col.bounds;
        float dist = b.extents.x + wallCheckDistance;
        Vector2[] origins =
        {
            new Vector2(b.center.x, b.center.y),
            new Vector2(b.center.x, b.center.y + b.extents.y * 0.5f),
            new Vector2(b.center.x, b.center.y - b.extents.y * 0.5f),
        };
        foreach (Vector2 o in origins)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(o, Vector2.right * side, dist);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider != col && !hit.collider.isTrigger
                    && !hit.collider.GetComponent<Enemy>()) // стены, не враги
                    return true;
            }
        }
        return false;
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
}
