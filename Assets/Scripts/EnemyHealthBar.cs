using UnityEngine;

// HP-полоска над врагом в мировых координатах. Отдельный объект (не дочерний врага),
// чтобы не наследовать флип/масштаб. Цвет зелёный→жёлтый→красный, плавно убывает,
// скрыта при полном HP, уничтожается вместе с врагом.
public class EnemyHealthBar : MonoBehaviour
{
    private Enemy _enemy;
    private Transform _enemyTf;
    private SpriteRenderer _bg;
    private SpriteRenderer _fill;
    private float _width;
    private float _height;
    private float _headOffset;
    private float _displayed = 1f;

    private static Sprite _spriteCenter;
    private static Sprite _spriteLeft;

    public static void Create(Enemy enemy)
    {
        GameObject root = new GameObject("EnemyHealthBar");
        root.AddComponent<EnemyHealthBar>().Setup(enemy);
    }

    private void Setup(Enemy enemy)
    {
        _enemy = enemy;
        _enemyTf = enemy.transform;

        float vh = enemy.visualHeight > 0f ? enemy.visualHeight : 2.2f;
        _width = Mathf.Clamp(vh * 0.7f, 0.9f, 2.2f);
        _height = 0.18f;
        _headOffset = vh * 0.5f + 0.35f;

        _bg = new GameObject("bg").AddComponent<SpriteRenderer>();
        _bg.transform.SetParent(transform, false);
        _bg.sprite = GetSprite(new Vector2(0.5f, 0.5f));
        _bg.color = new Color(0f, 0f, 0f, 0.7f);
        _bg.sortingOrder = 8;
        _bg.transform.localScale = new Vector3(_width, _height, 1f);

        _fill = new GameObject("fill").AddComponent<SpriteRenderer>();
        _fill.transform.SetParent(transform, false);
        _fill.sprite = GetSprite(new Vector2(0f, 0.5f)); // пивот слева — растёт вправо
        _fill.color = Color.green;
        _fill.sortingOrder = 9;
        _fill.transform.localPosition = new Vector3(-_width * 0.5f, 0f, 0f);
        _fill.transform.localScale = new Vector3(_width, _height * 0.72f, 1f);
    }

    void LateUpdate()
    {
        if (_enemy == null || _enemyTf == null || _enemy.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        float target = _enemy.HealthNormalized;
        _displayed = Mathf.MoveTowards(_displayed, target, Time.deltaTime * 2.5f);

        transform.position = _enemyTf.position + Vector3.up * _headOffset;

        bool visible = _displayed < 0.999f; // прячем при полном HP
        if (_bg != null) _bg.enabled = visible;
        if (_fill != null)
        {
            _fill.enabled = visible;
            _fill.transform.localScale = new Vector3(_width * Mathf.Clamp01(_displayed), _fill.transform.localScale.y, 1f);
            _fill.color = ColorFor(_displayed);
        }
    }

    private static Color ColorFor(float n)
    {
        // зелёный (1) → жёлтый (0.5) → красный (0)
        if (n > 0.5f) return Color.Lerp(Color.yellow, Color.green, (n - 0.5f) * 2f);
        return Color.Lerp(Color.red, Color.yellow, n * 2f);
    }

    private static Sprite GetSprite(Vector2 pivot)
    {
        bool left = pivot.x == 0f;
        if (left && _spriteLeft != null) return _spriteLeft;
        if (!left && _spriteCenter != null) return _spriteCenter;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, 1, 1), pivot, 1f);
        if (left) _spriteLeft = s; else _spriteCenter = s;
        return s;
    }
}
