using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{
    public float walkFrameRate = 8f;

    [Header("Нормализация размера")]
    public float targetWorldHeight = 2.2f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Sprite[] walkSprites;
    private bool useStaticSprite = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        Enemy enemy = GetComponent<Enemy>();
        Sprite custom = enemy != null ? enemy.bodySprite : null;

        // Если спавнер выбрал конкретный вид врага — используем его статичный спрайт.
        if (custom != null)
        {
            useStaticSprite = true;
            ApplyStaticSprite(custom);
            return;
        }

        // Иначе — старое поведение: общая анимация ходьбы из enemy_walk_0..3.
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
        // Статичные враги не анимируются по кадрам — за их поворот к герою отвечает Enemy.FlipSprite.
        if (useStaticSprite || spriteRenderer == null || !HasWalkSprites())
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

    private void ApplyStaticSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;

        float ppu = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
        float spriteWorldHeight = sprite.rect.height / ppu;
        float spriteWorldWidth = sprite.rect.width / ppu;
        if (spriteWorldHeight <= 0f)
        {
            return;
        }

        // Приводим всех врагов к одной высоте в игре, независимо от размера их картинки в пикселях.
        float scale = targetWorldHeight / spriteWorldHeight;

        // Враги смотрят влево, в сторону героя. Для спрайтов, нарисованных лицом вправо,
        // это отрицательный знак по X (отзеркаливание); для спрайта лицом влево — положительный.
        Enemy enemy = GetComponent<Enemy>();
        bool facesRight = enemy == null || enemy.spriteFacesRight;
        float signX = facesRight ? -1f : 1f;
        transform.localScale = new Vector3(signX * scale, scale, 1f);

        // Подгоняем коллайдер под реальные размеры спрайта, чтобы попадания и столкновения совпадали с картинкой.
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(spriteWorldWidth, spriteWorldHeight);
            box.offset = Vector2.zero;
        }
    }

    private bool HasWalkSprites()
    {
        if (walkSprites == null)
        {
            return false;
        }

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
