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

        idleSprite = Resources.Load<Sprite>("Sprites/PlayerAnim/Статика");
        runSprites = new Sprite[4];
        for (int i = 0; i < runSprites.Length; i++)
        {
            runSprites[i] = Resources.Load<Sprite>($"Sprites/PlayerAnim/{i + 1}");
        }
        jumpSprite = Resources.Load<Sprite>("Sprites/PlayerAnim/5");

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

        bool isGrounded = playerMovement != null ? playerMovement.isGrounded : CheckGrounded();
        Sprite targetSprite = null;

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

        if (targetSprite != null && targetSprite != spriteRenderer.sprite)
        {
            spriteRenderer.sprite = targetSprite;
            NormalizeSize(targetSprite);
        }
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
