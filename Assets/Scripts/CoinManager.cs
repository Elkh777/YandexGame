using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Менеджер монеток: пул объектов, спавн при убийстве врага, счётчик в HUD, звук и вспышка при сборе.
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Выпадение")]
    [Range(0f, 1f)] public float dropChance = 0.8f; // шанс выпадения монет с врага
    public int minCoinsPerEnemy = 1;
    public int maxCoinsPerEnemy = 3;

    [Header("Подбор и физика")]
    public float pickupRange = 3f;      // дальность, с которой монетка летит к игроку
    public float collectRadius = 0.6f;  // дистанция подбора
    public float magnetSpeed = 9f;
    public float gravity = 18f;
    public float bounce = 0.45f;
    public float lifetime = 12f;        // через сколько монетка исчезает, если не подобрали
    public float coinScale = 1.2f;
    public int initialPoolSize = 16;

    private readonly Queue<Coin> _pool = new Queue<Coin>();
    private Transform _player;
    private Sprite _coinSprite;
    private int _coinCount;
    private TextMeshProUGUI _coinText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        _coinSprite = BuildCoinSprite(32);
        BuildHud();

        for (int i = 0; i < initialPoolSize; i++)
        {
            _pool.Enqueue(CreateCoin());
        }
    }

    // Вызывается из Enemy.Die().
    public void SpawnCoins(Vector3 position)
    {
        if (Random.value > dropChance) return;

        int count = Random.Range(minCoinsPerEnemy, maxCoinsPerEnemy + 1);
        for (int i = 0; i < count; i++)
        {
            Coin coin = GetFromPool();
            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.3f), 0f);
            coin.Launch(position + offset, _player, gravity, bounce, pickupRange, collectRadius, magnetSpeed, lifetime);
        }
    }

    public void OnCoinCollected(Vector3 position)
    {
        _coinCount++;
        UpdateHud();
        AudioManager.Instance?.PlayCoin();
        StartCoroutine(Flash(position));
    }

    public int CoinCount => _coinCount;

    public void ReturnToPool(Coin coin)
    {
        coin.gameObject.SetActive(false);
        _pool.Enqueue(coin);
    }

    private Coin GetFromPool()
    {
        Coin coin = _pool.Count > 0 ? _pool.Dequeue() : CreateCoin();
        return coin;
    }

    private Coin CreateCoin()
    {
        GameObject obj = new GameObject("Coin");
        obj.transform.SetParent(transform);
        obj.transform.localScale = Vector3.one * coinScale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = _coinSprite;
        sr.sortingOrder = 5;

        Coin coin = obj.AddComponent<Coin>();
        coin.Init(sr, this);

        obj.SetActive(false);
        return coin;
    }

    private IEnumerator Flash(Vector3 position)
    {
        GameObject flash = new GameObject("CoinFlash");
        flash.transform.position = position;
        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
        sr.sprite = _coinSprite;
        sr.color = new Color(1f, 0.95f, 0.6f, 0.9f);
        sr.sortingOrder = 6;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float k = t / 0.25f;
            flash.transform.localScale = Vector3.one * Mathf.Lerp(coinScale, coinScale * 2.4f, k);
            Color c = sr.color;
            c.a = Mathf.Lerp(0.9f, 0f, k);
            sr.color = c;
            yield return null;
        }
        Destroy(flash);
    }

    private void BuildHud()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject obj = new GameObject("CoinCounter");
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(180f, -240f); // под счётом очков
        rect.sizeDelta = new Vector2(340f, 50f);

        _coinText = obj.AddComponent<TextMeshProUGUI>();
        _coinText.fontSize = 25;
        _coinText.color = new Color(1f, 0.85f, 0.3f);
        _coinText.alignment = TextAlignmentOptions.Left;
        _coinText.fontStyle = FontStyles.Bold;
        _coinText.textWrappingMode = TextWrappingModes.NoWrap;
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (_coinText != null) _coinText.text = $"МОНЕТЫ: {_coinCount}";
    }

    // Программный спрайт монетки: золотой кружок с тёмным ободком.
    private Sprite BuildCoinSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;
        Vector2 c = new Vector2(r, r);
        Color gold = new Color(0.98f, 0.82f, 0.25f, 1f);
        Color rim = new Color(0.6f, 0.45f, 0.05f, 1f);
        Color shine = new Color(1f, 0.97f, 0.75f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                Color col;
                if (d > r - 1f) col = Color.clear;
                else if (d > r - 3f) col = rim;
                else col = gold;

                // лёгкий блик в левом-верхнем секторе
                if (col == gold && Vector2.Distance(new Vector2(x, y), new Vector2(r * 0.65f, r * 1.35f)) < r * 0.35f)
                    col = shine;

                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
