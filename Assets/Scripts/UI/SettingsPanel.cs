using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Esc ile acilan, oyunun temasina uygun sade ayarlar penceresi.
// Sahneye elle eklenmez; HUDController tarafindan kodla olusturulur.
public class SettingsPanel : MonoBehaviour
{
    const float WindowWidth = 700f;
    const float WindowHeight = 600f;

    static readonly Color DimColor = new Color(0.015f, 0.008f, 0.012f, 0.82f);
    static readonly Color WindowColor = new Color(0.24f, 0.12f, 0.055f, 0.98f);
    static readonly Color TextColor = new Color(0.24f, 0.085f, 0.035f, 1f);
    static readonly Color MutedTextColor = new Color(0.42f, 0.22f, 0.11f, 1f);

    GameObject overlay;
    GameObject settingsPage, aboutPage;
    TextMeshProUGUI titleLabel, closeLabel, muteLabel, fullscreenLabel, languageLabel;
    TextMeshProUGUI backLabel, aboutName, aboutVersion, openButtonLabel;
    readonly Dictionary<string, TextMeshProUGUI> rowValues = new Dictionary<string, TextMeshProUGUI>();
    float windowWidth, windowHeight;
    float rowHeight;
    bool previousCursorVisible;
    bool open;

    public bool IsOpen { get { return open; } }

    void Start()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogWarning("SettingsPanel: Canvas bulunamadi."); return; }

        BuildOpenButton(canvas.rootCanvas.transform);
        BuildWindow(canvas.rootCanvas.transform);
        SoundManager.MuteChanged += RefreshMuteLabel;
        Loc.OnLanguageChanged += RefreshLocalization;
        overlay.SetActive(false);
        RefreshLocalization();
    }

    void OnDestroy()
    {
        SoundManager.MuteChanged -= RefreshMuteLabel;
        Loc.OnLanguageChanged -= RefreshLocalization;
    }

    // ---- Kurulum ----
    void BuildOpenButton(Transform canvas)
    {
        var btn = UIFactory.CreateButton(canvas, "OpenSettingsButton", Loc.T("Ayarlar"), 28f, out openButtonLabel);
        var rt = (RectTransform)btn.transform;
        rt.SetAsLastSibling();
        HudLayout.PlaceCornerButton(rt, 0);
        var icon = UIArt.Load(UIArt.IconSettings);
        if (icon != null)
        {
            UIArt.StyleButton(btn, openButtonLabel, 14f);
            var iconImage = new GameObject("SettingsIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            iconImage.transform.SetParent(btn.transform, false);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var iconRt = (RectTransform)iconImage.transform;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(13f, 13f); iconRt.offsetMax = new Vector2(-13f, -13f);
            rt.sizeDelta = new Vector2(68f, 68f);
            openButtonLabel.text = "";
        }
        else
        {
            openButtonLabel.fontSize = 22f;
            openButtonLabel.fontSizeMin = 17f;
            openButtonLabel.fontSizeMax = 22f;
            openButtonLabel.enableAutoSizing = true;
        }
        btn.onClick.AddListener(Toggle);
        if (icon == null) UIArt.StyleButton(btn, openButtonLabel);
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

        Canvas.ForceUpdateCanvases();
        Vector2 available = ((RectTransform)canvas).rect.size;
        windowWidth = available.x > 0f ? Mathf.Min(WindowWidth, Mathf.Max(260f, available.x - 32f)) : WindowWidth;
        windowHeight = available.y > 0f ? Mathf.Min(WindowHeight, Mathf.Max(380f, available.y - 32f)) : WindowHeight;
        rowHeight = Mathf.Clamp((windowHeight - 180f) / 5f, 40f, 68f);

        var win = UIFactory.CreatePanel(dim, "Window", WindowColor, true);
        win.anchorMin = win.anchorMax = win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(windowWidth, windowHeight);
        UIArt.Skin(win.GetComponent<Image>(), UIArt.ListPanel, WindowColor);

        var plaqueRt = UIFactory.CreatePanel(win, "TitlePlaque", Color.white, false);
        var plaque = plaqueRt.GetComponent<Image>();
        UIArt.Skin(plaque, UIArt.TitlePlaque, new Color(0.42f, 0.12f, 0.035f, 0.95f));
        plaque.raycastTarget = false;
        plaque.preserveAspect = false;
        plaqueRt.anchorMin = plaqueRt.anchorMax = new Vector2(0.5f, 1f);
        plaqueRt.pivot = new Vector2(0.5f, 1f);
        plaqueRt.anchoredPosition = new Vector2(0f, -8f);
        plaqueRt.sizeDelta = new Vector2(Mathf.Min(500f, windowWidth * 0.68f), 138f);

        titleLabel = TextAt(win, "Title", Loc.T("Ayarlar"), 0f, -38f, windowWidth, 60f, 36f,
            TextAlignmentOptions.Center, TextColor, true);

        var close = UIFactory.CreateButton(win, "Close", "X", 20f, out closeLabel);
        var closeRt = (RectTransform)close.transform;
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-28f, -18f);
        closeRt.sizeDelta = new Vector2(42f, 42f);
        StyleThemedButton(close, closeLabel);
        close.onClick.AddListener(Close);

        settingsPage = new GameObject("SettingsOptions", typeof(RectTransform));
        var pageRt = (RectTransform)settingsPage.transform;
        pageRt.SetParent(win, false);
        pageRt.anchorMin = Vector2.zero; pageRt.anchorMax = Vector2.one;
        pageRt.offsetMin = Vector2.zero; pageRt.offsetMax = Vector2.zero;

        float firstRowY = -Mathf.Max(104f, windowHeight * 0.18f);
        float rowStep = rowHeight + Mathf.Clamp(windowHeight * 0.007f, 4f, 6f);
        muteLabel = BuildRow(settingsPage.transform, "Sound", Loc.T("Ses"), firstRowY, out var soundButton);
        soundButton.onClick.AddListener(SoundManager.ToggleMuted);
        languageLabel = BuildRow(settingsPage.transform, "Language", Loc.T("Dil"), firstRowY - rowStep, out var languageButton);
        languageButton.onClick.AddListener(Loc.Toggle);
        fullscreenLabel = BuildRow(settingsPage.transform, "Display", Loc.T("Ekran"), firstRowY - rowStep * 2f, out var fullscreenButton);
        fullscreenButton.onClick.AddListener(ToggleFullscreen);

        BuildRow(settingsPage.transform, "Achievements", Loc.T("Başarımlar"), firstRowY - rowStep * 3f, out var achievementsButton);
        achievementsButton.onClick.AddListener(OpenAchievements);
        BuildRow(settingsPage.transform, "About", Loc.T("Hakkında"), firstRowY - rowStep * 4f, out var aboutButton);
        aboutButton.onClick.AddListener(ShowAbout);

        BuildAboutPage(win);
    }

    TextMeshProUGUI BuildRow(Transform parent, string id, string text, float topY, out Button button)
    {
        var rowImage = UIFactory.CreateRoundedPanel(parent, "Row_" + id, new Color(0.42f, 0.16f, 0.055f, 0.12f), false);
        var rowRt = rowImage.rectTransform;
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, topY);
        rowRt.sizeDelta = new Vector2(windowWidth * 0.76f, rowHeight);
        rowImage.color = new Color(0f, 0f, 0f, 0f);

        button = UIFactory.CreateButton(rowRt, "Value", "", 20f, out var buttonLabel);
        var buttonRt = (RectTransform)button.transform;
        buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = new Vector2(0f, -rowHeight * 0.5f);
        buttonRt.sizeDelta = new Vector2(windowWidth * 0.58f, rowHeight * 0.82f);
        StyleOptionButton(button, buttonLabel);
        buttonLabel.rectTransform.offsetMin = new Vector2(10f, 2f);
        buttonLabel.rectTransform.offsetMax = new Vector2(-10f, -2f);

        if (id == "Sound") buttonLabel.text = SoundManager.IsMuted ? Loc.T("Ses: Kapalı") : Loc.T("Ses: Açık");
        else if (id == "Language") buttonLabel.text = Loc.T("Dil") + ": " + (Loc.IsEnglish ? "English" : "Türkçe");
        else if (id == "Display") buttonLabel.text = Screen.fullScreen ? Loc.T("Tam Ekran: Açık") : Loc.T("Pencere Modu");
        else buttonLabel.text = Loc.T(text);
        rowValues[id] = buttonLabel;
        return buttonLabel;
    }

    void BuildAboutPage(RectTransform win)
    {
        aboutPage = new GameObject("AboutPage", typeof(RectTransform));
        var rt = (RectTransform)aboutPage.transform;
        rt.SetParent(win, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        aboutPage.SetActive(false);

        aboutName = TextAt(rt, "GameName", Loc.T("ARAF"), windowWidth * 0.1f, -windowHeight * 0.34f,
            windowWidth * 0.8f, 82f, 56f,
            TextAlignmentOptions.Center, TextColor, true);
        aboutVersion = TextAt(rt, "Version", Loc.F("Sürüm {0}", Application.version), windowWidth * 0.1f,
            -windowHeight * 0.46f, windowWidth * 0.8f, 50f, 22f,
            TextAlignmentOptions.Center, MutedTextColor);

        var back = ButtonAt(rt, "Back", Loc.T("Geri"), (windowWidth - 200f) * 0.5f,
            -windowHeight * 0.75f, 200f, 58f, 22f, out backLabel);
        StyleThemedButton(back, backLabel);
        back.onClick.AddListener(ShowSettings);
    }

    static void StyleThemedButton(Button button, TextMeshProUGUI label)
    {
        UIArt.StyleButton(button, label, 12f);
        if (label != null) label.color = UIArt.ButtonText;
    }

    static void StyleOptionButton(Button button, TextMeshProUGUI label)
    {
        if (!UIArt.StyleButton(button, label, 18f))
        {
            var image = button.GetComponent<Image>();
            image.sprite = UIFactory.RoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = new Color(0.28f, 0.13f, 0.095f, 1f);
            var outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.78f, 0.43f, 0.16f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        if (label != null) label.color = UIArt.ButtonText;
    }

    void ShowAbout()
    {
        settingsPage.SetActive(false);
        aboutPage.SetActive(true);
        titleLabel.text = Loc.T("Hakkında");
    }

    void ShowSettings()
    {
        aboutPage.SetActive(false);
        settingsPage.SetActive(true);
        titleLabel.text = Loc.T("Ayarlar");
    }

    void RefreshLanguage()
    {
        if (languageLabel != null)
            languageLabel.text = Loc.T("Dil") + ": " + (Loc.IsEnglish ? "English" : "Türkçe");
    }

    void RefreshLocalization()
    {
        if (titleLabel != null) titleLabel.text = Loc.T(aboutPage != null && aboutPage.activeSelf ? "Hakkında" : "Ayarlar");
        if (openButtonLabel != null && UIArt.Load(UIArt.IconSettings) == null) openButtonLabel.text = Loc.T("Ayarlar");
        SetRowValue("Achievements", "Başarımlar");
        SetRowValue("About", "Hakkında");
        if (backLabel != null) backLabel.text = Loc.T("Geri");
        if (aboutName != null) aboutName.text = Loc.T("ARAF");
        if (aboutVersion != null) aboutVersion.text = Loc.F("Sürüm {0}", Application.version);
        Refresh();
    }

    void SetRowValue(string id, string text)
    {
        TextMeshProUGUI label;
        if (rowValues.TryGetValue(id, out label)) label.text = Loc.T(text);
    }

    void RefreshMuteLabel()
    {
        if (muteLabel != null)
            muteLabel.text = SoundManager.IsMuted ? Loc.T("Ses: Kapalı") : Loc.T("Ses: Açık");
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
        previousCursorVisible = Cursor.visible;
        Cursor.visible = true;
        open = true;
        ShowSettings();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if (overlay == null) return;
        open = false;
        overlay.SetActive(false);
        Cursor.visible = previousCursorVisible;
    }

    public void Toggle() { if (open) Close(); else Open(); }

    // ---- Eylemler ----
    void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        Refresh();
    }

    void OpenAchievements()
    {
        Close();
        var panel = GetComponent<AchievementPanel>();
        if (panel != null) panel.Open();
    }

    void Refresh()
    {
        if (fullscreenLabel != null)
            fullscreenLabel.text = Screen.fullScreen ? Loc.T("Tam Ekran: Açık") : Loc.T("Pencere Modu");
        RefreshLanguage();
        RefreshMuteLabel();
    }
}
