using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float speed = 5f;
    public float jumpForce = 10f;
    public float crouchSpeed = 2f;
    public float groundCheckDistance = 0.1f;
    public Vector3 playerScale = new Vector3(0.3f, 0.3f, 0.3f);

    [Header("Рывок (Dash)")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public float dashSpeed = 16f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.9f;

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

        // Рывок: короткий импульс по направлению взгляда с кулдауном и неуязвимостью.
        if (Input.GetKeyDown(dashKey) && !isDashing && Time.time >= nextDashTime)
        {
            StartDash();
        }

        // Прыжок разрешен только с земли, чтобы игрок не мог улетать повторными прыжками в воздухе.
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (Input.GetKey(KeyCode.D)) { transform.localScale = new Vector3(playerScale.x, playerScale.y, playerScale.z); facingDirection = 1; }
        if (Input.GetKey(KeyCode.A)) { transform.localScale = new Vector3(-playerScale.x, playerScale.y, playerScale.z); facingDirection = -1; }
    }

    void StartDash()
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
        dashDir = facingDirection;
        health?.GrantInvincibility(dashDuration + 0.05f); // i-frames на время рывка
        AudioManager.Instance?.PlaySfx("dash");
        StartCoroutine(DashTrail());
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
            {
                return true;
            }
        }

        return false;
    }

    void FixedUpdate()
    {
        // Во время рывка движение перехватывается импульсом по направлению взгляда.
        if (isDashing)
        {
            if (Time.time < dashEndTime)
            {
                rb.linearVelocity = new Vector2(dashDir * dashSpeed, rb.linearVelocity.y);
                return;
            }
            isDashing = false;
        }

        float move = 0f;
        if (Input.GetKey(KeyCode.D)) move = 1f;
        if (Input.GetKey(KeyCode.A)) move = -1f;

        float currentSpeed = isCrouching ? crouchSpeed : speed;
        rb.linearVelocity = new Vector2(move * currentSpeed, rb.linearVelocity.y);
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
