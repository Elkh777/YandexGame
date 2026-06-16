using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Магазин улучшений. Полностью строится в рантайме (как и весь UI проекта):
//  - в HUD: иконка-корзина (правый верх) + баланс монет рядом;
//  - всплывающее окно с тремя улучшениями, паузой и затемнением фона.
// Создаётся автоматически из fGameManager.EnsureRuntimeSystems — настройка в редакторе не нужна.
public class ShopUI : MonoBehaviour
{
    // Цвет неактивной (серой) кнопки по ТЗ (#666).
    private static readonly Color GreyTint = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

    private Canvas _canvas;
    private GameObject _overlay;     // затемнение + окно (включается/выключается целиком)
    private bool _isOpen;

    private TextMeshProUGUI _hudBalanceText;     // баланс в HUD рядом с корзиной
    private TextMeshProUGUI _windowBalanceText;  // баланс в шапке окна

    // Спрайты из Assets/Resources/Sprites
    private Sprite _cartSprite, _coinSprite, _closeSprite, _continueSprite, _greenBtn, _greyMax;
    private Sprite _uiRound; // встроенный скруглённый спрайт Unity (для скруглённых углов окна/рядов)

    private readonly UpgradeType[] _types =
    {
        UpgradeType.WeaponDamage, UpgradeType.AttackSpeed, UpgradeType.Invincibility,
        UpgradeType.Freeze, UpgradeType.Poison
    };

    // Ссылки на элементы рядов для обновления при покупке/смене баланса.
    private Button[] _buyButtons;
    private TextMeshProUGUI[] _buyLabels;
    private TextMeshProUGUI[] _levelLabels;
    private TextMeshProUGUI[] _costLabels;
    private Image[] _buyImages;

    void Start()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null) { enabled = false; return; }

        LoadSprites();
        BuildHud();
        BuildWindow();

        CoinManager.OnCoinsChanged += OnCoinsChanged;
        UpgradeManager.OnUpgradesChanged += RefreshWindow;

        int coins = CoinManager.Instance != null ? CoinManager.Instance.CoinCount : 0;
        RefreshBalances(coins);
        RefreshWindow();
    }

    void OnDestroy()
    {
        CoinManager.OnCoinsChanged -= OnCoinsChanged;
        UpgradeManager.OnUpgradesChanged -= RefreshWindow;
    }

    void Update()
    {
        // Закрытие по Escape, когда магазин открыт.
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // ===================== Загрузка спрайтов =====================

    private void LoadSprites()
    {
        _cartSprite = Load("Shopping cart icon (upper right corner)");
        _coinSprite = Load("Coin icon (to display balance)");
        _closeSprite = Load("Close button (red X)");
        _continueSprite = Load("Blue button CONTINUE GAME");
        _greenBtn = Load("Green BUY-UPGRADE-button (active)");
        _greyMax = Load("Gray MAX-button (inactive, maximum reached)");
        _uiRound = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
    }

    // Загрузка спрайта. Спрайты импортированы в режиме Multiple (под-ассеты),
    // поэтому при null пробуем LoadAll и берём первый спрайт.
    private Sprite Load(string name)
    {
        string path = "Sprites/" + name;
        Sprite s = Resources.Load<Sprite>(path);
        if (s == null)
        {
            Sprite[] all = Resources.LoadAll<Sprite>(path);
            if (all != null && all.Length > 0) s = all[0];
        }
        if (s == null) Debug.LogWarning($"ShopUI: не найден спрайт '{path}'");
        return s;
    }

    private Sprite IconFor(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.WeaponDamage: return Load("Swords (Weapon Strength)");
            case UpgradeType.AttackSpeed: return Load("Lightning (Attack Speed)");
            case UpgradeType.Invincibility: return Load("Shield (Invulnerability)");
            case UpgradeType.Freeze: return Load("Freezing bullets");
            default: return Load("Poisonous bullets");
        }
    }

    // ===================== HUD: корзина + баланс =====================

    private void BuildHud()
    {
        Vector2 one = new Vector2(1f, 1f);

        // Иконка-корзина: правый верхний угол, увеличивается при наведении.
        RectTransform cart = NewRect("ShopCartButton", _canvas.transform, one, one, new Vector2(-55f, -155f), new Vector2(72f, 72f));
        Image cartImg = cart.gameObject.AddComponent<Image>();
        cartImg.sprite = _cartSprite;
        Button cartBtn = cart.gameObject.AddComponent<Button>();
        cartBtn.transition = Selectable.Transition.None;
        cartBtn.onClick.AddListener(Open);
        cart.gameObject.AddComponent<ShopHoverScale>();

        // Баланс монет рядом с корзиной: иконка монеты + число.
        CreateImage("ShopBalanceCoin", _canvas.transform, one, one, new Vector2(-130f, -155f), new Vector2(44f, 44f), _coinSprite, Color.white);
        _hudBalanceText = CreateText("ShopBalanceText", _canvas.transform, one, one, new Vector2(-158f, -155f), new Vector2(100f, 48f), "0", 32, Gold, TextAlignmentOptions.Right);
    }

    // ===================== Окно магазина =====================

    private void BuildWindow()
    {
        // Затемнение на весь экран (блокирует клики позади).
        RectTransform overlay = NewRect("ShopOverlay", _canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        _overlay = overlay.gameObject;
        Image overlayImg = _overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.7f);

        // Само окно — по центру, крупное, тёмное, скруглённые углы.
        Vector2 mid = new Vector2(0.5f, 0.5f);
        RectTransform win = NewRect("ShopWindow", overlay, mid, mid, Vector2.zero, new Vector2(760f, 940f));
        Image winImg = win.gameObject.AddComponent<Image>();
        winImg.sprite = _uiRound;
        winImg.type = Image.Type.Sliced;
        winImg.color = new Color(0.10f, 0.10f, 0.14f, 1f);

        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topRight = new Vector2(1f, 1f);
        Vector2 topMid = new Vector2(0.5f, 1f);

        // Шапка: иконка корзины слева + заголовок по центру, крестик закрытия справа.
        CreateImage("HeaderCart", win, topLeft, topLeft, new Vector2(56f, -60f), new Vector2(58f, 58f), _cartSprite, Color.white);
        CreateText("HeaderTitle", win, topMid, topMid, new Vector2(0f, -60f), new Vector2(420f, 62f), "МАГАЗИН", 46, Color.white, TextAlignmentOptions.Center);

        RectTransform close = NewRect("CloseButton", win, topRight, topRight, new Vector2(-58f, -60f), new Vector2(62f, 62f));
        Image closeImg = close.gameObject.AddComponent<Image>();
        closeImg.sprite = _closeSprite;
        Button closeBtn = close.gameObject.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(Close);
        close.gameObject.AddComponent<ShopHoverScale>();

        // Баланс в шапке: иконка монеты + число рядом, слева.
        CreateImage("HeaderCoin", win, topLeft, topLeft, new Vector2(58f, -132f), new Vector2(44f, 44f), _coinSprite, Color.white);
        _windowBalanceText = CreateText("HeaderBalance", win, topLeft, topLeft, new Vector2(108f, -132f), new Vector2(320f, 52f), "0", 38, Gold, TextAlignmentOptions.Left);

        // Три ряда улучшений.
        _buyButtons = new Button[_types.Length];
        _buyLabels = new TextMeshProUGUI[_types.Length];
        _levelLabels = new TextMeshProUGUI[_types.Length];
        _costLabels = new TextMeshProUGUI[_types.Length];
        _buyImages = new Image[_types.Length];

        float rowY = -225f;
        for (int i = 0; i < _types.Length; i++)
        {
            BuildRow(i, _types[i], win, rowY);
            rowY -= 130f;
        }

        // Кнопка «Продолжить игру».
        RectTransform cont = NewRect("ContinueButton", win, topMid, topMid, new Vector2(0f, -872f), new Vector2(470f, 90f));
        Image contImg = cont.gameObject.AddComponent<Image>();
        contImg.sprite = _continueSprite;
        contImg.type = Image.Type.Sliced;
        Button contBtn = cont.gameObject.AddComponent<Button>();
        contBtn.onClick.AddListener(Close);
        cont.gameObject.AddComponent<UIJuicyButton>();
        CreateText("Text", cont, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "ПРОДОЛЖИТЬ ИГРУ", 34, Color.white, TextAlignmentOptions.Center);

        _overlay.SetActive(false);
    }

    private void BuildRow(int i, UpgradeType t, Transform parent, float y)
    {
        Vector2 topMid = new Vector2(0.5f, 1f);
        RectTransform row = NewRect("Row_" + t, parent, topMid, topMid, new Vector2(0f, y), new Vector2(686f, 118f));
        Image rowImg = row.gameObject.AddComponent<Image>();
        rowImg.sprite = _uiRound;
        rowImg.type = Image.Type.Sliced;
        rowImg.color = new Color(0.16f, 0.16f, 0.22f, 1f);

        Vector2 left = new Vector2(0f, 0.5f);
        Vector2 right = new Vector2(1f, 0.5f);

        // Иконка улучшения.
        CreateImage("Icon", row, left, left, new Vector2(56f, 0f), new Vector2(76f, 76f), IconFor(t), Color.white);

        // Название + уровень.
        CreateText("Name", row, left, left, new Vector2(112f, 20f), new Vector2(250f, 38f), UpgradeManager.GetName(t), 28, Color.white, TextAlignmentOptions.Left);
        _levelLabels[i] = CreateText("Level", row, left, left, new Vector2(112f, -22f), new Vector2(250f, 30f), "", 21, new Color(0.7f, 0.7f, 0.78f), TextAlignmentOptions.Left);

        // Цена + иконка монеты.
        _costLabels[i] = CreateText("Cost", row, right, right, new Vector2(-226f, 0f), new Vector2(90f, 42f), "", 30, Gold, TextAlignmentOptions.Right);
        CreateImage("CostCoin", row, right, right, new Vector2(-204f, 0f), new Vector2(36f, 36f), _coinSprite, Color.white);

        // Кнопка покупки.
        RectTransform buy = NewRect("Buy", row, right, right, new Vector2(-96f, 0f), new Vector2(168f, 72f));
        _buyImages[i] = buy.gameObject.AddComponent<Image>();
        _buyImages[i].sprite = _greenBtn;
        _buyImages[i].type = Image.Type.Sliced;
        _buyButtons[i] = buy.gameObject.AddComponent<Button>();
        int captured = i;
        _buyButtons[i].onClick.AddListener(() => Buy(captured));
        _buyLabels[i] = CreateText("Text", buy, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 31, Color.white, TextAlignmentOptions.Center);
    }

    // ===================== Логика =====================

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        fGameManager.Instance?.PauseForShop();
        _overlay.SetActive(true);
        RefreshBalances(CoinManager.Instance != null ? CoinManager.Instance.CoinCount : 0);
        RefreshWindow();
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _overlay.SetActive(false);
        fGameManager.Instance?.ResumeFromShop();
    }

    private void Buy(int i)
    {
        UpgradeType t = _types[i];
        if (UpgradeManager.TryBuy(t))
        {
            AudioManager.Instance?.PlayCoin();        // звук монет
            StartCoroutine(FlashButton(_buyImages[i])); // вспышка на кнопке
            // Обновление UI произойдёт через события OnUpgradesChanged / OnCoinsChanged.
        }
    }

    private void OnCoinsChanged(int coins)
    {
        RefreshBalances(coins);
        if (_isOpen) RefreshWindow(); // доступность кнопок зависит от баланса
    }

    private void RefreshBalances(int coins)
    {
        if (_hudBalanceText != null) _hudBalanceText.text = coins.ToString();
        if (_windowBalanceText != null) _windowBalanceText.text = coins.ToString();
    }

    private void RefreshWindow()
    {
        if (_buyButtons == null) return;
        int coins = CoinManager.Instance != null ? CoinManager.Instance.CoinCount : 0;

        for (int i = 0; i < _types.Length; i++)
        {
            UpgradeType t = _types[i];
            int lvl = UpgradeManager.GetLevel(t);
            int max = UpgradeManager.GetMaxLevel(t);
            _levelLabels[i].text = $"Уровень: {lvl}/{max}";

            if (UpgradeManager.IsMaxed(t))
            {
                // Максимум — серая неактивная кнопка. Надпись «MAX» уже впечатана в спрайт _greyMax,
                // поэтому собственный оверлей-текст очищаем, чтобы не было двойной надписи.
                _costLabels[i].text = "—";
                _buyImages[i].sprite = _greyMax;
                _buyImages[i].color = Color.white;
                _buyLabels[i].text = "";
                _buyButtons[i].interactable = false;
                continue;
            }

            int cost = UpgradeManager.GetNextCost(t);
            _costLabels[i].text = cost.ToString();
            _buyLabels[i].text = lvl == 0 ? "КУПИТЬ" : "УЛУЧШИТЬ";

            bool afford = coins >= cost;
            _buyImages[i].sprite = _greenBtn;
            _buyImages[i].color = afford ? Color.white : GreyTint; // серый, если не хватает монет
            _buyButtons[i].interactable = afford;
        }
    }

    // Кратковременная вспышка-пульсация кнопки (на нереальном времени — работает при паузе).
    private IEnumerator FlashButton(Image img)
    {
        Transform tr = img.transform;
        float t = 0f;
        const float dur = 0.18f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(t / dur * Mathf.PI);
            tr.localScale = Vector3.one * (1f + 0.12f * k);
            yield return null;
        }
        tr.localScale = Vector3.one;
    }

    // ===================== UI-хелперы =====================

    private RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    private Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform r = NewRect(name, parent, anchorMin, anchorMax, pos, size);
        Image img = r.gameObject.AddComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, string text, float fontSize, Color color, TextAlignmentOptions align)
    {
        RectTransform r = NewRect(name, parent, anchorMin, anchorMax, pos, size);
        // Пивот совмещаем с выравниванием, чтобы pos указывал на нужный край текста.
        if (align == TextAlignmentOptions.Left) r.pivot = new Vector2(0f, 0.5f);
        else if (align == TextAlignmentOptions.Right) r.pivot = new Vector2(1f, 0.5f);
        else r.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }
}

// Плавное увеличение элемента при наведении курсора (для иконки магазина и крестика).
public class ShopHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.18f;
    public float speed = 12f;
    private Vector3 _target = Vector3.one;

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _target, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData e) => _target = Vector3.one * hoverScale;
    public void OnPointerExit(PointerEventData e) => _target = Vector3.one;
}
