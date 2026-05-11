using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    public float runFrameRate = 12f;
    public float shootPoseTime = 0.16f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Sprite idleSprite;
    private Sprite shootSprite;
    private Sprite[] runSprites;
    private Sprite jumpSprite;
    private float shootUntil;
    private PlayerMovement playerMovement;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();

        idleSprite = Resources.Load<Sprite>("Sprites/anim/0");
        shootSprite = Resources.Load<Sprite>("Sprites/player_shoot");
        runSprites = new Sprite[8];
        for (int i = 0; i < runSprites.Length; i++)
        {
            runSprites[i] = Resources.Load<Sprite>($"Sprites/anim/{i + 1}");
        }
        jumpSprite = Resources.Load<Sprite>("Sprites/anim/3");

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
        }
    }

    void Update()
    {
        if (spriteRenderer == null || rb == null)
        {
            return;
        }

        if (Time.time < shootUntil && shootSprite != null)
        {
            spriteRenderer.sprite = shootSprite;
            return;
        }

        bool isGrounded = playerMovement != null ? playerMovement.isGrounded : CheckGrounded();

        if (!isGrounded && jumpSprite != null)
        {
            spriteRenderer.sprite = jumpSprite;
            return;
        }

        if (Mathf.Abs(rb.linearVelocity.x) > 0.08f && HasRunSprites())
        {
            int frame = Mathf.FloorToInt(Time.time * runFrameRate) % runSprites.Length;
            spriteRenderer.sprite = runSprites[frame];
            return;
        }

        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    private bool CheckGrounded()
    {
        return Mathf.Abs(rb.linearVelocity.y) < 0.1f;
    }

    public void PlayShoot()
    {
        shootUntil = Time.time + shootPoseTime;
    }

    private bool HasRunSprites()
    {
        foreach (Sprite sprite in runSprites)
        {
            if (sprite == null)
            {
                return false;
            }
        }

        return true;
    }
}
