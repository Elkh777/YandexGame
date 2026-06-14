using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Анимация")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.15f;
    // Для непрозрачных ворот мерцание прозрачностью выглядит плохо — отключаем его.
    public bool pulseAlpha = true;

    private SpriteRenderer _spriteRenderer;
    private Vector3 _originalScale;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalScale = transform.localScale;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
        transform.localScale = _originalScale * scale;

        if (pulseAlpha && _spriteRenderer != null)
        {
            float alpha = 0.6f + Mathf.Sin(Time.time * pulseSpeed * 1.5f) * 0.25f;
            Color c = _spriteRenderer.color;
            c.a = alpha;
            _spriteRenderer.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("[Portal] LevelManager не найден!");
            return;
        }

        LevelManager.Instance.GoToNextLevel();
    }
}
