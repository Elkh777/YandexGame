using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Централизованный менеджер звука (синглтон, переживает смену сцен).
// Категории: Master, Music, SFX. Значения хранятся в PlayerPrefs и применяются мгновенно.
// Клипы грузятся из Resources/Audio/<name>. Если файла нет — просто тишина (без ошибок).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string KeyMaster = "vol_master";
    private const string KeyMusic = "vol_music";
    private const string KeySfx = "vol_sfx";
    private const string KeyMuted = "vol_muted";

    private float _master = 1f;
    private float _music = 0.6f;
    private float _sfx = 1f;
    private bool _muted = false;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
    private string _currentMusic;

    public float Master { get => _master; set { _master = Mathf.Clamp01(value); Save(); Apply(); } }
    public float Music { get => _music; set { _music = Mathf.Clamp01(value); Save(); Apply(); } }
    public float Sfx { get => _sfx; set { _sfx = Mathf.Clamp01(value); Save(); Apply(); } }
    public bool Muted { get => _muted; set { _muted = value; Save(); Apply(); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        _music = PlayerPrefs.GetFloat(KeyMusic, 0.6f);
        _sfx = PlayerPrefs.GetFloat(KeySfx, 1f);
        _muted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        Apply();
        PlayMusic("music");
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(KeyMaster, _master);
        PlayerPrefs.SetFloat(KeyMusic, _music);
        PlayerPrefs.SetFloat(KeySfx, _sfx);
        PlayerPrefs.SetInt(KeyMuted, _muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Применяем громкость ко всем источникам (мгновенно).
    private void Apply()
    {
        float gate = _muted ? 0f : 1f;
        if (_musicSource != null) _musicSource.volume = _master * _music * gate;
        // SFX-громкость передаётся в PlayOneShot при каждом проигрывании.
    }

    private float SfxVolume => (_muted ? 0f : 1f) * _master * _sfx;

    private AudioClip Load(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_clipCache.TryGetValue(name, out AudioClip cached)) return cached;
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
        _clipCache[name] = clip; // кэшируем даже null, чтобы не грузить повторно
        return clip;
    }

    public void PlaySfx(string name, float volumeScale = 1f)
    {
        AudioClip clip = Load(name);
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(SfxVolume * volumeScale));
    }

    public void PlayMusic(string name)
    {
        AudioClip clip = Load(name);
        if (clip == null || _musicSource == null) return;
        if (_currentMusic == name && _musicSource.isPlaying) return;
        _currentMusic = name;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    // Короткие обёртки под конкретные события игры.
    public void PlayShoot() => PlaySfx("shoot");
    public void PlayHit() => PlaySfx("hit");
    public void PlayEnemyDeath() => PlaySfx("enemy_death");
    public void PlayCoin() => PlaySfx("coin");
    public void PlayUiClick() => PlaySfx("ui_click");
    public void PlayLevelComplete() => PlaySfx("level_complete");

    // ---------- Панель настроек звука (переиспользуется в меню и в паузе) ----------

    // Строит панель со слайдерами Master/Music/SFX + кнопками Mute и Закрыть.
    // onClose вызывается при закрытии. Возвращает корневой объект панели (скрыт по умолчанию).
    public static GameObject BuildSettingsPanel(Transform parent, System.Action onClose)
    {
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(620f, 460f);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.02f, 0.025f, 0.96f);

        CreateLabel(panel.transform, "НАСТРОЙКИ ЗВУКА", 40, new Vector2(0f, -50f), new Vector2(560f, 70f), FontStyles.Bold);

        AudioManager am = Instance;
        CreateSlider(panel.transform, "Общая", new Vector2(0f, 60f), am != null ? am.Master : 1f,
            v => { if (Instance != null) Instance.Master = v; });
        CreateSlider(panel.transform, "Музыка", new Vector2(0f, 0f), am != null ? am.Music : 0.6f,
            v => { if (Instance != null) Instance.Music = v; });
        CreateSlider(panel.transform, "Эффекты", new Vector2(0f, -60f), am != null ? am.Sfx : 1f,
            v => { if (Instance != null) Instance.Sfx = v; });

        // Кнопка Mute.
        Button muteBtn = CreateButton(panel.transform, new Vector2(-130f, -150f), new Vector2(220f, 60f),
            (am != null && am.Muted) ? "ВКЛ. ЗВУК" : "БЕЗ ЗВУКА");
        TextMeshProUGUI muteText = muteBtn.GetComponentInChildren<TextMeshProUGUI>();
        muteBtn.onClick.AddListener(() =>
        {
            if (Instance != null)
            {
                Instance.Muted = !Instance.Muted;
                Instance.PlayUiClick();
                if (muteText != null) muteText.text = Instance.Muted ? "ВКЛ. ЗВУК" : "БЕЗ ЗВУКА";
            }
        });

        // Кнопка Закрыть.
        Button closeBtn = CreateButton(panel.transform, new Vector2(130f, -150f), new Vector2(220f, 60f), "ЗАКРЫТЬ");
        closeBtn.onClick.AddListener(() =>
        {
            if (Instance != null) Instance.PlayUiClick();
            panel.SetActive(false);
            onClose?.Invoke();
        });

        panel.SetActive(false);
        return panel;
    }

    private static void CreateSlider(Transform parent, string label, Vector2 pos, float value, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = new GameObject("Slider_" + label);
        row.transform.SetParent(parent, false);
        RectTransform rrect = row.AddComponent<RectTransform>();
        rrect.anchorMin = new Vector2(0.5f, 0.5f);
        rrect.anchorMax = new Vector2(0.5f, 0.5f);
        rrect.anchoredPosition = pos;
        rrect.sizeDelta = new Vector2(540f, 44f);

        CreateLabel(row.transform, label, 24, new Vector2(-190f, 0f), new Vector2(150f, 44f), FontStyles.Normal)
            .alignment = TextAlignmentOptions.Left;

        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform srect = sliderObj.AddComponent<RectTransform>();
        srect.anchorMin = new Vector2(0.5f, 0.5f);
        srect.anchorMax = new Vector2(0.5f, 0.5f);
        srect.anchoredPosition = new Vector2(70f, 0f);
        srect.sizeDelta = new Vector2(300f, 24f);

        Image bgImg = sliderObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.22f, 1f);

        Slider slider = sliderObj.AddComponent<Slider>();

        GameObject fillArea = new GameObject("Fill");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform frect = fillArea.AddComponent<RectTransform>();
        frect.anchorMin = new Vector2(0f, 0f);
        frect.anchorMax = new Vector2(1f, 1f);
        frect.offsetMin = Vector2.zero;
        frect.offsetMax = Vector2.zero;
        Image fillImg = fillArea.AddComponent<Image>();
        fillImg.color = new Color(0.95f, 0.78f, 0.2f, 1f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderObj.transform, false);
        RectTransform hrect = handle.AddComponent<RectTransform>();
        hrect.sizeDelta = new Vector2(18f, 30f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = frect;
        slider.handleRect = hrect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        slider.onValueChanged.AddListener(onChanged);
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Vector2 pos, Vector2 sizeDelta, FontStyles style)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static Button CreateButton(Transform parent, Vector2 pos, Vector2 size, string label)
    {
        GameObject obj = new GameObject("Button");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.85f, 0.7f, 0.25f, 1f);
        Button btn = obj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform trect = txtObj.AddComponent<RectTransform>();
        trect.anchorMin = Vector2.zero;
        trect.anchorMax = Vector2.one;
        trect.offsetMin = Vector2.zero;
        trect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = new Color(0.05f, 0.03f, 0.02f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return btn;
    }
}
