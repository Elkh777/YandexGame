using System.Collections;
using UnityEngine;

// Процедурный эффект взмаха ближнего боя: дуга-«слэш», которая прочерчивается в направлении удара.
// Генерируется кодом (без отдельных спрайтов), по образцу HitSpark.
public class SlashEffect : MonoBehaviour
{
    private static Sprite _arcSprite;

    // Спавнит взмах в точке position, развёрнутый по направлению удара direction.
    public static void Spawn(Vector3 position, Vector2 direction)
    {
        if (direction == Vector2.zero) direction = Vector2.right;

        GameObject obj = new GameObject("SlashEffect");
        obj.transform.position = position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = new Color(0.9f, 0.97f, 1f, 1f); // холодный белый «клинок»
        sr.sortingOrder = 7;                        // поверх врагов

        SlashEffect fx = obj.AddComponent<SlashEffect>();
        fx.StartCoroutine(fx.Animate(direction));
    }

    private IEnumerator Animate(Vector2 direction)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        const float dur = 0.18f;
        const float sweep = 90f; // дуга прочерчивается на 90° (имитация замаха)
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            // Поворот-взмах: от -sweep/2 к +sweep/2 относительно направления удара.
            float angle = baseAngle + Mathf.Lerp(-sweep * 0.5f, sweep * 0.5f, k);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.3f, k);

            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, k); // затухание к концу
            sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }

    // Программная дуга-«полумесяц»: кольцевой сегмент, открытый в сторону +X, с мягкими краями.
    private static Sprite GetSprite()
    {
        if (_arcSprite != null) return _arcSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float rOuter = size * 0.46f;
        float rInner = size * 0.32f;
        float arcHalf = 70f * Mathf.Deg2Rad; // полураствор дуги
        float mid = (rInner + rOuter) * 0.5f;
        float half = (rOuter - rInner) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                float dist = p.magnitude;
                float ang = Mathf.Atan2(p.y, p.x); // -PI..PI, дуга открыта к +X

                float a = 0f;
                if (dist <= rOuter && dist >= rInner && Mathf.Abs(ang) <= arcHalf)
                {
                    // Мягкие края: затухание по толщине кольца и к концам дуги.
                    float radialFall = 1f - Mathf.Clamp01(Mathf.Abs(dist - mid) / half);
                    float angularFall = 1f - Mathf.Clamp01(Mathf.Abs(ang) / arcHalf);
                    a = radialFall * Mathf.Lerp(0.3f, 1f, angularFall);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _arcSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return _arcSprite;
    }
}
