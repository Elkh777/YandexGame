using UnityEngine;
using System.Collections;

public static class BloodExplosionEffect
{
    private static Sprite _cachedBloodDrop;
    private static Sprite _cachedShockwave;

    public static void Spawn(Vector3 position)
    {
        if (_cachedBloodDrop == null) CreateBloodDropSprite();
        if (_cachedShockwave == null) CreateShockwaveSprite();

        GameObject root = new GameObject("BloodExplosion_VFX");
        root.transform.position = position;

        BloodEffectManager manager = root.AddComponent<BloodEffectManager>();

        // Ударная волна
        manager.StartShockwave(position);

        // Крупные брызги (6-10 шт)
        int bigCount = Random.Range(6, 10);
        for (int i = 0; i < bigCount; i++)
        {
            manager.StartBloodParticle(position, true);
        }

        // Мелкие капли (15-25 шт)
        int smallCount = Random.Range(15, 25);
        for (int i = 0; i < smallCount; i++)
        {
            manager.StartBloodParticle(position, false);
        }

        // Очень мелкие брызги (10-15 шт)
        int tinyCount = Random.Range(10, 15);
        for (int i = 0; i < tinyCount; i++)
        {
            manager.StartBloodParticle(position, false, true);
        }

        manager.StartAutoDestroy(1.8f);
    }

    private static void CreateBloodDropSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / (size / 2f)));

                // Делаем форму капли более irregular (неровной)
                float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.2f;
                alpha = Mathf.Clamp01(alpha + noise);
                alpha = Mathf.Pow(alpha, 1.5f);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedBloodDrop = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void CreateShockwaveSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float innerRadius = size / 2f * 0.6f;
        float outerRadius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 0f;
                if (dist >= innerRadius && dist <= outerRadius)
                {
                    float ringT = (dist - innerRadius) / (outerRadius - innerRadius);
                    alpha = Mathf.Sin(ringT * Mathf.PI);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedShockwave = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}

public class BloodEffectManager : MonoBehaviour
{
    public void StartShockwave(Vector3 pos)
    {
        StartCoroutine(AnimateShockwave(pos));
    }

    public void StartBloodParticle(Vector3 pos, bool isBig, bool isTiny = false)
    {
        StartCoroutine(AnimateDrop(pos, isBig, isTiny));
    }

    public void StartAutoDestroy(float delay)
    {
        Destroy(gameObject, delay);
    }

    private IEnumerator AnimateShockwave(Vector3 pos)
    {
        GameObject ring = new GameObject("Shockwave");
        ring.transform.SetParent(transform);
        ring.transform.position = pos + Vector3.up * 0.2f;

        SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
        sr.sprite = GetCachedShockwave();
        sr.color = new Color(0.95f, 0.15f, 0.15f, 0.7f);
        sr.sortingOrder = 20;
        ring.transform.localScale = Vector3.one * 0.3f;

        float duration = 0.45f;
        float elapsed = 0f;
        Vector3 startScale = ring.transform.localScale;
        Vector3 endScale = startScale * 4.5f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            ring.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.7f * (1f - t * t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(ring);
    }

    private IEnumerator AnimateDrop(Vector3 pos, bool isBig, bool isTiny)
    {
        GameObject drop = new GameObject(isBig ? "BigDrop" : (isTiny ? "TinyDrop" : "Drop"));
        drop.transform.SetParent(transform);
        drop.transform.position = pos + Vector3.up * Random.Range(-0.3f, 0.4f);

        SpriteRenderer sr = drop.AddComponent<SpriteRenderer>();
        sr.sprite = GetCachedBloodDrop();
        sr.sortingOrder = 15;

        // Разные оттенки крови
        float shade = Random.Range(0.4f, 1f);
        sr.color = new Color(shade, 0.02f, 0.02f, 1f);

        // Размер
        float size;
        if (isTiny) size = Random.Range(0.05f, 0.12f);
        else if (isBig) size = Random.Range(0.35f, 0.6f);
        else size = Random.Range(0.15f, 0.35f);

        drop.transform.localScale = Vector3.one * size;

        // Направление и скорость
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed;
        if (isTiny) speed = Random.Range(5f, 9f);
        else if (isBig) speed = Random.Range(2f, 4f);
        else speed = Random.Range(3.5f, 6.5f);

        Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

        // Смещение начальной позиции
        drop.transform.position += (Vector3)(velocity.normalized * 0.1f);

        float lifetime;
        if (isTiny) lifetime = Random.Range(0.5f, 0.8f);
        else if (isBig) lifetime = Random.Range(1f, 1.4f);
        else lifetime = Random.Range(0.7f, 1.1f);

        float elapsed = 0f;
        float gravity = isBig ? -12f : -9f;
        float drag = 0.97f;
        float rotationSpeed = Random.Range(-200f, 200f);

        while (elapsed < lifetime)
        {
            // Физика
            velocity.y += gravity * Time.deltaTime;
            velocity *= drag;
            drop.transform.position += (Vector3)(velocity * Time.deltaTime);

            // Вращение
            drop.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            // Уменьшение размера
            float sizeMultiplier = 1f - (elapsed / lifetime) * 0.4f;
            drop.transform.localScale = Vector3.one * (size * sizeMultiplier);

            // Плавное затухание
            float fadeT = elapsed / lifetime;
            float alpha = Mathf.Pow(1f - fadeT, 2f);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(drop);
    }

    private Sprite GetCachedBloodDrop()
    {
        System.Reflection.FieldInfo field = typeof(BloodExplosionEffect).GetField("_cachedBloodDrop",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (Sprite)field.GetValue(null);
    }

    private Sprite GetCachedShockwave()
    {
        System.Reflection.FieldInfo field = typeof(BloodExplosionEffect).GetField("_cachedShockwave",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (Sprite)field.GetValue(null);
    }
}