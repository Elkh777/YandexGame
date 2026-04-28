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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        originalSize = col.size;
        originalOffset = col.offset;
    }

    void Update()
    {
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        UpdateCrouchCollider();
        isGrounded = CheckGrounded();

        // Прыжок разрешен только с земли, чтобы игрок не мог улетать повторными прыжками в воздухе.
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (Input.GetKey(KeyCode.D)) transform.localScale = new Vector3(1, 1, 1);
        if (Input.GetKey(KeyCode.A)) transform.localScale = new Vector3(-1, 1, 1);
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
