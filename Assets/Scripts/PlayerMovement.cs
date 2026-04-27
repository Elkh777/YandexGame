using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float speed = 5f;
    public int maxJumps = 2;
    private int currentJumps;
    public float jumpForce = 10f;
    public float crouchSpeed = 2f;

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
        currentJumps = maxJumps;
    }

    void Update()
    {
        float checkDistance = 0.6f;
        isGrounded = Physics2D.Raycast(transform.position + Vector3.left * 0.2f, Vector2.down, checkDistance) ||
                     Physics2D.Raycast(transform.position + Vector3.right * 0.2f, Vector2.down, checkDistance);

        if (isGrounded)
        {
            currentJumps = maxJumps;
        }

        if (Input.GetKeyDown(KeyCode.Space) && currentJumps > 0 && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            currentJumps--;
        }

        isCrouching = Input.GetKey(KeyCode.LeftControl);
        UpdateCrouchCollider();

        if (Input.GetKey(KeyCode.D)) transform.localScale = new Vector3(1, 1, 1);
        if (Input.GetKey(KeyCode.A)) transform.localScale = new Vector3(-1, 1, 1);
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