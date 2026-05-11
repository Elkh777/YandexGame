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

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private bool isCrouching = false;
    private Vector2 originalSize;
    private Vector2 originalOffset;
    private bool isGrounded = false;
    private float horizontalInput = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        originalSize = col.size;
        originalOffset = col.offset;

        // 🔑 КРИТИЧЕСКИ ВАЖНО: Включаем интерполяцию для сглаживания между кадрами физики
        if (rb.interpolation != RigidbodyInterpolation2D.Interpolate)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        UpdateCrouchCollider();
        isGrounded = CheckGrounded();

        // Считываем ввод в Update (стандарт Unity)
        horizontalInput = 0f;
        if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;

        // Разворот только при реальном движении
        if (horizontalInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        }

        // Прыжок
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        float currentSpeed = isCrouching ? crouchSpeed : speed;
        // Применяем скорость только по X, ось Y оставляем для гравитации/прыжков
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
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