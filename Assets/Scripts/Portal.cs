using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Анимация")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.15f;

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

        gameObject.SetActive(false);
    }
}
