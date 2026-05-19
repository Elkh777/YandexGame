using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{
    public float walkFrameRate = 8f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Sprite[] walkSprites;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        walkSprites = new Sprite[4];
        for (int i = 0; i < walkSprites.Length; i++)
        {
            walkSprites[i] = Resources.Load<Sprite>($"Sprites/enemy_walk_{i}");
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            if (walkSprites[0] != null)
            {
                spriteRenderer.sprite = walkSprites[0];
            }
        }
    }

    void Update()
    {
        if (spriteRenderer == null || !HasWalkSprites())
        {
            return;
        }

        float speed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 1f;
        if (speed > 0.05f)
        {
            int frame = Mathf.FloorToInt(Time.time * walkFrameRate) % walkSprites.Length;
            spriteRenderer.sprite = walkSprites[frame];
        }
    }

    private bool HasWalkSprites()
    {
        foreach (Sprite sprite in walkSprites)
        {
            if (sprite == null)
            {
                return false;
            }
        }

        return true;
    }
}
