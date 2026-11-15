using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SoundId { Click, Buy, LevelUp, AgeUp }

// Ses dosyasi gerektirmez: sesler acilista kodla sentezlenir (kisa sinus/harmonik notalar).
// Ileride gercek ses dosyalarina gecmek icin clips[] dizisini AudioClip'lerle doldurmak yeterli.
// Sag altta "Ses: Acik/Kapali" butonu da olusturur.
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    const int SampleRate = 44100;
    const string MuteKey = "evrim_muted";
    const string VolumeKey = "evrim_volume";
    const float DefaultVolume = 0.6f;

    [SerializeField, Range(0f, 1f)] float volume = DefaultVolume;

    // Ayarlar paneli ve mute butonu icin ortak ses seviyesi (0..1). PlayerPrefs'te saklanir.
    public static float Volume
    {
        get { return Instance != null ? Instance.volume : PlayerPrefs.GetFloat(VolumeKey, DefaultVolume); }
        set
        {
            float v = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, v);
            PlayerPrefs.Save();
            if (Instance != null) Instance.volume = v;
        }
    }

    AudioSource[] sources;
    AudioClip[] clips;
    int next;
    bool muted;
    TextMeshProUGUI muteLabel;

    public static event Action MuteChanged;

    public static bool IsMuted
    {
        get { return Instance != null ? Instance.muted : PlayerPrefs.GetInt(MuteKey, 0) == 1; }
    }

    // Ana menu gibi baska ekranlardan ses ac/kapa icin
    public static void ToggleMuted()
    {
        if (Instance != null) { Instance.ToggleMute(); return; }
        bool muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        PlayerPrefs.SetInt(MuteKey, muted ? 0 : 1);
        PlayerPrefs.Save();
        if (MuteChanged != null) MuteChanged();
    }

    public static void Play(SoundId id, float pitch = 1f, float gain = 1f)
    {
        if (Instance != null) Instance.PlayInternal(id, pitch, gain);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        // Hizli tiklamada sesler birbirini kesmesin diye birkac kaynak sirayla kullanilir
        sources = new AudioSource[6];
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].spatialBlend = 0f;
        }

        clips = new AudioClip[4];
        clips[(int)SoundId.Click] = Synth("click", 0.1f, delegate (float t)
        {
            // Yukaridan asagi kayan kisa "pop"
            float ph = 2f * Mathf.PI * (500f * t + 450f * (1f - Mathf.Exp(-45f * t)) / 45f);
            return 0.45f * Mathf.Sin(ph) * Mathf.Exp(-t * 35f);
        });
        clips[(int)SoundId.Buy] = Synth("buy", 0.4f, delegate (float t)
        {
            return 0.28f * (Note(t, 0f, 523.25f, 14f) + Note(t, 0.07f, 783.99f, 12f));
        });
        clips[(int)SoundId.LevelUp] = Synth("levelup", 0.9f, delegate (float t)
        {
            float v = 0f;
            float[] f = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < f.Length; i++) v += Note(t, i * 0.08f, f[i], 8f);
            return 0.22f * v;
        });
        clips[(int)SoundId.AgeUp] = Synth("ageup", 2f, delegate (float t)
        {
            float v = 0f;
            float[] f = { 392f, 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < f.Length; i++) v += Note(t, i * 0.12f, f[i], 3.5f);
            return 0.18f * v;
        });

        muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, volume));
    }

    void Start()
    {
        var gm = GameManager.Instance;
        gm.OnGathered += OnGathered;
        gm.OnBuildingBought += OnBought;
        gm.OnAgeChanged += OnAge;
        BuildMuteButton();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        MuteChanged -= UpdateMuteLabel;
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnGathered -= OnGathered;
        gm.OnBuildingBought -= OnBought;
        gm.OnAgeChanged -= OnAge;
    }

    void OnGathered() { PlayInternal(SoundId.Click, UnityEngine.Random.Range(0.95f, 1.1f), 0.8f); }
    void OnBought(BuildingData b, int qty) { PlayInternal(SoundId.Buy, qty > 1 ? 1.12f : 1f, 1f); }
    void OnAge() { PlayInternal(SoundId.AgeUp, 1f, 1f); }

    void PlayInternal(SoundId id, float pitch, float gain)
    {
        if (muted || volume <= 0f) return;
        var s = sources[next];
        next = (next + 1) % sources.Length;
        s.pitch = pitch;
        s.PlayOneShot(clips[(int)id], volume * gain);
    }

    // ---- Ses ac/kapa butonu ----
    void BuildMuteButton()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var btn = UIFactory.CreateButton(canvas.rootCanvas.transform, "MuteButton", "", 28f, out muteLabel);
        var rt = (RectTransform)btn.transform;
        rt.SetAsFirstSibling();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-HudLayout.Margin, 28f);   // sag alt kose, x1..Max satirinin altinda
        rt.sizeDelta = new Vector2(230f, 60f);
        btn.onClick.AddListener(ToggleMute);
        UIArt.StyleButton(btn, muteLabel);
        muteLabel.fontSize = 24f;
        MuteChanged += UpdateMuteLabel;
        UpdateMuteLabel();
    }

    void ToggleMute()
    {
        muted = !muted;
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        UpdateMuteLabel();
        if (!muted) PlayInternal(SoundId.Click, 1f, 1f);
        if (MuteChanged != null) MuteChanged();
    }

    void UpdateMuteLabel() { if (muteLabel != null) muteLabel.text = muted ? Loc.T("Ses: Kapalı") : Loc.T("Ses: Açık"); }

    // ---- Sentez ----
    static AudioClip Synth(string clipName, float seconds, Func<float, float> sampler)
    {
        int n = Mathf.CeilToInt(seconds * SampleRate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sampler((float)i / SampleRate), -1f, 1f);

        int fade = SampleRate / 200;   // son 5 ms: sonda "tik" sesi olmasin
        for (int i = 0; i < fade && i < n; i++) data[n - 1 - i] *= i / (float)fade;

        var clip = AudioClip.Create(clipName, n, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // start saniyesinde baslayan, decay hizinda sonen zengin tonlu nota
    static float Note(float t, float start, float freq, float decay)
    {
        if (t < start) return 0f;
        float dt = t - start;
        float env = Mathf.Min(1f, dt * 300f) * Mathf.Exp(-dt * decay);
        float ph = 2f * Mathf.PI * freq * dt;
        return env * (Mathf.Sin(ph) + 0.3f * Mathf.Sin(2f * ph) + 0.1f * Mathf.Sin(3f * ph));
    }
}
