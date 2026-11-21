using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Esc ile acilan ayarlar penceresi: ses, tam ekran, kayit disa aktarma, kaydi sifirlama ve cikis.
// Sahneye elle eklenmez; HUDController tarafindan kodla olusturulur.
public class SettingsPanel : MonoBehaviour
{
    const float ConfirmSeconds = 4f;
    const float VolumeStep = 0.1f;
    const float WindowWidth = 780f;
    const float WindowHeight = 840f;

    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color WindowColor = new Color(0.1f, 0.09f, 0.13f, 0.98f);
    static readonly Color TextColor = new Color(0.95f, 0.95f, 0.98f);
    static readonly Color GrayColor = new Color(0.68f, 0.68f, 0.74f);
    static readonly Color DangerColor = new Color(0.62f, 0.2f, 0.2f, 1f);

    GameObject overlay;
    TextMeshProUGUI volumeValue, muteLabel, fullscreenLabel, resetLabel, infoText, languageLabel;
    float resetTimer = -1f;
    bool open;

    public bool IsOpen { get { return open; } }

    void Start()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogWarning("SettingsPanel: Canvas bulunamadi."); return; }

        BuildOpenButton(canvas.rootCanvas.transform);
        BuildWindow(canvas.rootCanvas.transform);
        SoundManager.MuteChanged += RefreshMuteLabel;
        overlay.SetActive(false);
        Refresh();
        RefreshMuteLabel();
    }

    void OnDestroy() { SoundManager.MuteChanged -= RefreshMuteLabel; }

    // ---- Kurulum ----
    void BuildOpenButton(Transform canvas)
    {
        TextMeshProUGUI label;
        var btn = UIFactory.CreateButton(canvas, "OpenSettingsButton", Loc.T("Ayarlar"), 28f, out label);
        var rt = (RectTransform)btn.transform;
        rt.SetAsLastSibling();
        HudLayout.PlaceCornerButton(rt, 0);
        var icon = UIArt.Load(UIArt.IconSettings);
        if (icon != null)
        {
            UIArt.StyleButton(btn, label, 14f);
            var iconImage = new GameObject("SettingsIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            iconImage.transform.SetParent(btn.transform, false);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var iconRt = (RectTransform)iconImage.transform;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(13f, 13f); iconRt.offsetMax = new Vector2(-13f, -13f);
            rt.sizeDelta = new Vector2(68f, 68f);
            label.text = "";
        }
        else
        {
            label.fontSize = 22f;
            label.fontSizeMin = 17f;
            label.fontSizeMax = 22f;
            label.enableAutoSizing = true;
        }
        btn.onClick.AddListener(Toggle);
        if (icon == null) UIArt.StyleButton(btn, label);
    }

    void BuildWindow(Transform canvas)
    {
        var dim = UIFactory.CreatePanel(canvas, "SettingsOverlay", DimColor, true);
        overlay = dim.gameObject;
        // Ayarlar penceresi her ekranin (ornegin taraf secimi, sortingOrder 100) ustunde acilsin.
        var overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 200;
        var overlayGroup = overlay.AddComponent<CanvasGroup>();
        overlayGroup.ignoreParentGroups = true;
        overlay.AddComponent<GraphicRaycaster>();
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = Vector2.zero;
        dim.offsetMax = Vector2.zero;

        var win = UIFactory.CreatePanel(dim, "Window", WindowColor, true);
        win.anchorMin = win.anchorMax = win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(WindowWidth, WindowHeight);

        TextAt(win, "Title", Loc.T("Ayarlar"), 28f, -84f, 400f, 56f, 44f, TextAlignmentOptions.MidlineLeft, Color.white, true);

        TextMeshProUGUI closeLabel;
        var close = ButtonAt(win, "Close", Loc.T("Kapat"), 600f, -20f, 150f, 56f, 26f, out closeLabel);
        close.onClick.AddListener(Close);

        // 1) Ses
        TextAt(win, "Label_Ses", Loc.T("Ses"), 28f, -140f, 190f, 56f, 30f,
            TextAlignmentOptions.MidlineLeft, TextColor);
        var mute = ButtonAt(win, "MuteToggle", "", 220f, -140f, 220f, 56f, 22f, out muteLabel);
        mute.onClick.AddListener(SoundManager.ToggleMuted);
        volumeValue = TextAt(win, "VolumeValue", "60%", 468f, -140f, 110f, 56f, 30f,
            TextAlignmentOptions.MidlineRight, TextColor);
        ButtonAt(win, "VolumeDown", "-", 596f, -140f, 70f, 56f, 34f, out closeLabel)
            .onClick.AddListener(delegate { AdjustVolume(-VolumeStep); });
        ButtonAt(win, "VolumeUp", "+", 680f, -140f, 70f, 56f, 34f, out closeLabel)
            .onClick.AddListener(delegate { AdjustVolume(VolumeStep); });

        // 2) Tam ekran
        RowLabel(win, Loc.T("Ekran"), -220f);
        var full = ButtonAt(win, "Fullscreen", Loc.T("Tam Ekran"), 468f, -220f, 282f, 56f, 24f, out fullscreenLabel);
        full.onClick.AddListener(ToggleFullscreen);

        // 3) Kaydi disa aktar
        RowLabel(win, Loc.T("Kayıt"), -300f);
        TextMeshProUGUI exportLabel;
        var export = ButtonAt(win, "ExportSave", Loc.T("Dışa Aktar"), 468f, -300f, 282f, 56f, 24f, out exportLabel);
        export.onClick.AddListener(ExportSave);

        // 4) Kaydi sifirla (iki kademeli onay)
        RowLabel(win, Loc.T("Tehlikeli Bölge"), -380f);
        var reset = ButtonAt(win, "ResetSave", Loc.T("Kaydı Sıfırla"), 468f, -380f, 282f, 56f, 24f, out resetLabel);
        reset.GetComponent<Image>().color = DangerColor;
        reset.onClick.AddListener(ResetProgress);

        // 5) Dil (degistirince sahne bastan yuklenir, butun metinler yenilenir)
        RowLabel(win, Loc.T("Dil"), -460f);
        TextMeshProUGUI langLabel;
        var lang = ButtonAt(win, "Language", "", 468f, -460f, 282f, 56f, 24f, out langLabel);
        languageLabel = langLabel;
        RefreshLanguage();
        lang.onClick.AddListener(Loc.ToggleAndReload);

        // 6) Cikis
        TextMeshProUGUI quitLabel;
        var quit = ButtonAt(win, "Quit", Loc.T("Oyundan Çık"), 468f, -540f, 282f, 56f, 24f, out quitLabel);
        quit.onClick.AddListener(QuitGame);

        // Başarımlar ayarlar içinden açılır; ana HUD'da ayrı bir köşe butonu kullanmaz.
        RowLabel(win, Loc.T("Başarımlar"), -620f);
        TextMeshProUGUI achievementsLabel;
        var achievements = ButtonAt(win, "Achievements", Loc.T("Aç"), 468f, -620f, 282f, 56f, 24f, out achievementsLabel);
        achievements.onClick.AddListener(OpenAchievements);

        infoText = TextAt(win, "Info", "", 28f, -690f, 722f, 70f, 20f,
            TextAlignmentOptions.TopLeft, GrayColor);
        infoText.enableAutoSizing = true;
        infoText.fontSizeMin = 15f;
        infoText.fontSizeMax = 20f;

        TextAt(win, "Version", Loc.F("Sürüm {0}  •  {1}", Application.version, SaveSystem.SaveFolder),
            28f, -780f, 722f, 26f, 16f, TextAlignmentOptions.BottomLeft, GrayColor);
    }

    void RefreshLanguage()
    {
        if (languageLabel != null)
            languageLabel.text = Loc.T("Dil") + ": " + (Loc.IsEnglish ? "English" : "Türkçe");
    }

    void RefreshMuteLabel()
    {
        if (muteLabel != null)
            muteLabel.text = SoundManager.IsMuted ? Loc.T("Ses: Kapalı") : Loc.T("Ses: Açık");
    }

    TextMeshProUGUI RowLabel(RectTransform parent, string text, float y)
    {
        return TextAt(parent, "Label_" + text, text, 28f, y, 420f, 56f, 30f,
            TextAlignmentOptions.MidlineLeft, TextColor);
    }

    static TextMeshProUGUI TextAt(RectTransform parent, string name, string text, float x, float topY,
        float width, float height, float size, TextAlignmentOptions align, Color color, bool bold = false)
    {
        return UIFactory.CreateText(parent, name, text, size, align, color,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(x, topY - height), new Vector2(x + width, topY), bold, true);
    }

    static Button ButtonAt(RectTransform parent, string name, string label, float x, float topY,
        float width, float height, float size, out TextMeshProUGUI labelText)
    {
        var btn = UIFactory.CreateButton(parent, name, label, size, out labelText);
        var rt = (RectTransform)btn.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, topY);
        rt.sizeDelta = new Vector2(width, height);
        return btn;
    }

    // ---- Durum ----
    public void Open()
    {
        if (overlay == null) return;
        open = true;
        resetTimer = -1f;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if (overlay == null) return;
        open = false;
        resetTimer = -1f;
        overlay.SetActive(false);
    }

    public void Toggle() { if (open) Close(); else Open(); }

    void Update()
    {
        if (resetTimer < 0f) return;
        resetTimer -= Time.unscaledDeltaTime;
        if (resetTimer < 0f) { resetTimer = -1f; Refresh(); }
    }

    // ---- Eylemler ----
    void AdjustVolume(float delta)
    {
        SoundManager.Volume = Mathf.Clamp01(SoundManager.Volume + delta);
        SoundManager.Play(SoundId.Click, 1f, 1f);
        Refresh();
    }

    void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        Refresh();
    }

    void ExportSave()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.SaveGame();
        string path = SaveSystem.ExportCopy();
        infoText.text = path != null
            ? Loc.F("Kayıt kopyalandı:\n{0}", path)
            : Loc.T("Dışa aktarılacak kayıt bulunamadı.");
    }

    void ResetProgress()
    {
        if (resetTimer < 0f)
        {
            resetTimer = ConfirmSeconds;
            Refresh();
            return;
        }

        resetTimer = -1f;
        var gm = GameManager.Instance;
        if (gm != null) gm.ResetProgress();
        Close();
    }

    void QuitGame()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.SaveGame();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OpenAchievements()
    {
        Close();
        var panel = GetComponent<AchievementPanel>();
        if (panel != null) panel.Open();
    }

    void Refresh()
    {
        if (volumeValue != null)
            volumeValue.text = Mathf.RoundToInt(SoundManager.Volume * 100f) + "%";
        if (fullscreenLabel != null)
            fullscreenLabel.text = Screen.fullScreen ? Loc.T("Tam Ekran: Açık") : Loc.T("Pencere Modu");
        if (resetLabel != null)
            resetLabel.text = resetTimer >= 0f ? Loc.T("Emin misin? Tekrar bas") : Loc.T("Kaydı Sıfırla");
        RefreshLanguage();
    }
}
