using System.Collections;
using UnityEngine;

// Белая искра-вспышка в точке попадания пули. Самодостаточна: расширяется, гаснет и уничтожается.
// Это надёжный маркер хита, работающий при любом шейдере спрайтов (в отличие от тинта на белых спрайтах).
public class HitSpark : MonoBehaviour
{
    public float duration = 0.13f;
    private static Sprite _sprite;

    public static void Spawn(Vector3 position)
    {
        GameObject obj = new GameObject("HitSpark");
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * 0.5f;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = new Color(1f, 1f, 0.85f, 1f);
        sr.sortingOrder = 7;

        obj.AddComponent<HitSpark>();
    }

    void Start()
    {
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector3 s0 = transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            transform.localScale = s0 * (1f + k * 1.8f);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, k);
                sr.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    // Программный мягкий белый кружок (радиальное затухание прозрачности).
    private static Sprite GetSprite()
    {
        if (_sprite != null) return _sprite;
        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(1f - d / r);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        _sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _sprite;
    }
}
