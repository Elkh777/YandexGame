using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{
    public float walkFrameRate = 8f;
    public float attackPoseTime = 0.2f; // Время позы атаки
    
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Sprite[] walkSprites;
    private float attackUntil; // Время до конца анимации атаки

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

        // Проверяем, не в анимации ли атаки
        if (Time.time < attackUntil)
        {
            // Показываем последний кадр как позу атаки
            spriteRenderer.sprite = walkSprites[walkSprites.Length - 1];
            return;
        }

        float speed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 1f;
        if (speed > 0.05f)
        {
            int frame = Mathf.FloorToInt(Time.time * walkFrameRate) % walkSprites.Length;
            spriteRenderer.sprite = walkSprites[frame];
        }
    }

    public void PlayAttack()
    {
        attackUntil = Time.time + attackPoseTime;
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
