using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    public float runFrameRate = 6f;

    private SpriteRenderer spriteRenderer;
    private Transform visualTransform;
    private Rigidbody2D rb;
    private Sprite idleSprite;
    private Sprite[] runSprites;
    private Sprite jumpSprite;
    private Sprite shootSprite;
    private float shootUntil;
    private PlayerMovement playerMovement;
    private float referenceHeight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();

        GameObject visual = new GameObject("Visual");
        visualTransform = visual.transform;
        visualTransform.SetParent(transform);
        visualTransform.localPosition = Vector3.zero;
        visualTransform.localRotation = Quaternion.identity;

        SpriteRenderer original = GetComponent<SpriteRenderer>();
        spriteRenderer = visual.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = original.sprite;
        spriteRenderer.sortingLayerID = original.sortingLayerID;
        spriteRenderer.sortingOrder = original.sortingOrder;
        spriteRenderer.color = original.color;
        original.enabled = false;

        idleSprite = Resources.Load<Sprite>("Sprites/PlayerAnimation/Статика");
        string[] walkOrder = { "6 кадр", "2 кадр", "3 кадр", "5 кадр", "4 кадр", "1 кадр" };
        runSprites = new Sprite[walkOrder.Length];
        for (int i = 0; i < walkOrder.Length; i++)
        {
            runSprites[i] = Resources.Load<Sprite>($"Sprites/PlayerAnimation/{walkOrder[i]}");
        }
        jumpSprite = Resources.Load<Sprite>("Sprites/PlayerAnimation/7 кадр");
        shootSprite = Resources.Load<Sprite>("Sprites/PlayerAnim/7");

        if (idleSprite != null)
        {
            referenceHeight = idleSprite.rect.height;
            spriteRenderer.sprite = idleSprite;
            NormalizeSize(idleSprite);
        }
    }

    void Update()
    {
        if (spriteRenderer == null || rb == null)
        {
            return;
        }

        Sprite targetSprite = null;

        if (Time.time < shootUntil && shootSprite != null)
        {
            targetSprite = shootSprite;
        }
        else
        {
            bool isGrounded = playerMovement != null ? playerMovement.isGrounded : CheckGrounded();

            if (!isGrounded && jumpSprite != null)
            {
                targetSprite = jumpSprite;
            }
            else if (Mathf.Abs(rb.linearVelocity.x) > 0.08f && HasRunSprites())
            {
                int frame = Mathf.FloorToInt(Time.time * runFrameRate) % runSprites.Length;
                targetSprite = runSprites[frame];
            }
            else if (idleSprite != null)
            {
                targetSprite = idleSprite;
            }
        }

        if (targetSprite != null && targetSprite != spriteRenderer.sprite)
        {
            spriteRenderer.sprite = targetSprite;
            NormalizeSize(targetSprite);
        }
    }

    public void PlayShoot()
    {
        shootUntil = Time.time + 0.2f;
    }

    private void NormalizeSize(Sprite sprite)
    {
        float correction = referenceHeight / sprite.rect.height;
        visualTransform.localScale = new Vector3(correction, correction, 1f);
    }

    private bool CheckGrounded()
    {
        return Mathf.Abs(rb.linearVelocity.y) < 0.1f;
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
