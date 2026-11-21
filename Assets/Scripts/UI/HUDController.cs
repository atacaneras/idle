using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Metinler")]
    [SerializeField] TMP_Text resourcesText;
    [SerializeField] TMP_Text ageText;
    [SerializeField] TMP_Text knowledgeText;
    [SerializeField] TMP_Text offlineText;

    [Header("Butonlar")]
    [SerializeField] Button gatherButton;
    [SerializeField] Button advanceButton;
    [SerializeField] TMP_Text advanceButtonLabel;
#if UNITY_EDITOR || DEBUG
    Button fastTestButton;
    TMP_Text fastTestButtonLabel;
#endif

    [Header("Bina listesi")]
    [SerializeField] Transform rowParent;
    [SerializeField] UIBuildingRow rowPrefab;

    [Header("Geri bildirim (opsiyonel, bos birakilirsa otomatik eklenir)")]
    [SerializeField] FloatingTextSpawner floatingText;
    [SerializeField] float offlineMessageSeconds = 8f;

    SceneBuildingSpawner sceneSpawner;
    readonly List<UIBuildingRow> rows = new List<UIBuildingRow>();

    // Sol ustteki kaynak hapleri (ikon + "Gunah: 123 (+3/sn)")
    class Pill { public RectTransform rt; public Image icon; public TextMeshProUGUI text; }
    readonly List<Pill> pills = new List<Pill>();
    RectTransform pillParent;
    bool advanceQueued;
    readonly StringBuilder sb = new StringBuilder();

    void Start()
    {
        var gm = GameManager.Instance;
        PolishHud();
        BuildFastTestButton();
        gm.OnAgeChanged += RebuildRows;
        gm.OnStateChanged += Refresh;
        gatherButton.onClick.AddListener(gm.Gather);
        advanceButton.onClick.AddListener(OnAdvanceClicked);

        // Yuzen "+X" yazilari ve buton punch animasyonu: sahneyi elle duzenlemeye gerek yok.
        if (floatingText == null) floatingText = GetComponent<FloatingTextSpawner>();
        if (floatingText == null) floatingText = gameObject.AddComponent<FloatingTextSpawner>();
        var feedback = gatherButton.GetComponent<GatherButtonFeedback>();
        if (feedback == null) feedback = gatherButton.gameObject.AddComponent<GatherButtonFeedback>();
        feedback.Init(floatingText);

        // Toplu satin alma butonlari, sesler, cag gecisi ve pasif uretim baloncuklari (hepsi kodla kurulur)
        GetOrAdd<BuyModeSelector>();
        GetOrAdd<SoundManager>();
        GetOrAdd<AgeTransition>();
        sceneSpawner = FindAnyObjectByType<SceneBuildingSpawner>();
        gm.OnMilestoneReached += OnMilestone;
        GetOrAdd<PassiveIncomeBubbles>().Init(floatingText, sceneSpawner);
        GetOrAdd<UpgradePanel>();
        GetOrAdd<TimeMachinePanel>();
        GetOrAdd<SettingsPanel>();
        GetOrAdd<AchievementPanel>();
        GetOrAdd<Hotkeys>();
        GetOrAdd<TutorialHints>().Init(floatingText);
        GetOrAdd<AchievementSystem>().Init(floatingText);

        // Steam Deck / gamepad: ilk secili oge Topla olsun; yon tuslariyla gezilebilsin.
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null && gatherButton != null) es.SetSelectedGameObject(gatherButton.gameObject);

        RebuildRows();
        Refresh();

        if (offlineText != null)
        {
            if (gm.SaveRecovered)
            {
                offlineText.text = Loc.T("Kayıt dosyası bozuktu, yedekten kurtarıldı.");
                Invoke(nameof(HideOfflineText), offlineMessageSeconds * 2f);
            }
            else if (gm.OfflineSecondsApplied > 1)
            {
                offlineText.text = Loc.F("Sen yokken {0} ilerleme kazandın", FormatDuration(gm.OfflineSecondsApplied));
                Invoke(nameof(HideOfflineText), offlineMessageSeconds);   // sonsuza kadar ekranda kalmasin
            }
            else offlineText.text = "";
        }
    }

    // Bu buton sadece son katta gorunur ve Zaman Makinesi panelini acar (normal kat gecisi otomatik)
    void OnAdvanceClicked()
    {
        var gm = GameManager.Instance;
        if (gm.IsFinalAge) { var tm = GetComponent<TimeMachinePanel>(); if (tm != null) tm.Open(); }
    }

    T GetOrAdd<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }

    void BuildFastTestButton()
    {
        // Test modu sadece editorde ve development build'lerde gorunur; oyuncu build'inde yok.
#if UNITY_EDITOR || DEBUG
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        fastTestButton = UIFactory.CreateButton(canvas.rootCanvas.transform, "FastTestToggle", "", 24f, out var label);
        fastTestButtonLabel = label;
        var rt = (RectTransform)fastTestButton.transform;
        HudLayout.PlaceCornerButton(rt, 2);   // alt sol: Basarimlar | Ayarlar | test
        fastTestButton.onClick.AddListener(GameManager.Instance.ToggleFastTestMode);
        UIArt.StyleButton(fastTestButton, label);
        RefreshFastTestButton();
#endif
    }

    void RefreshFastTestButton()
    {
#if UNITY_EDITOR || DEBUG
        if (fastTestButton == null || GameManager.Instance == null) return;
        bool enabled = GameManager.Instance.FastTestMode;
        fastTestButtonLabel.text = enabled ? "TEST MODU AÇIK\nTık x1M • Üret x1K" : "HIZLI TEST: KAPALI";
        fastTestButtonLabel.fontSize = 17f;
        fastTestButtonLabel.fontSizeMin = 13f;
        fastTestButtonLabel.fontSizeMax = 18f;
        fastTestButtonLabel.enableAutoSizing = true;
        bool skinned = fastTestButton.GetComponent<Image>().type == Image.Type.Sliced;
        fastTestButtonLabel.color = skinned ? UIArt.ButtonText : (enabled ? new Color(0.2f, 0.14f, 0.07f) : Color.white);
        fastTestButton.GetComponent<Image>().color = skinned
            ? (enabled ? new Color(1f, 0.82f, 0.4f) : Color.white)
            : (enabled ? new Color(0.96f, 0.73f, 0.28f) : new Color(0.25f, 0.2f, 0.18f, 0.96f));
#endif
    }

    void PolishHud()
    {
        // SOL UST: kaynak hapleri (kodla kurulur) | ORTA UST: asili kat adi tabelasi | SAG UST: Bilgi hapi
        if (resourcesText != null) resourcesText.enabled = false;   // yerine hapler cizilir (bkz. RefreshPills)
        BuildTitlePlaque();
        BuildKnowledgePill();

        // Cevrimdisi mesaji: orta sutunda, kat cubugu ve ipucu bandinin altinda
        SetTopText(offlineText, new Vector2(0.5f, 1f),
            new Vector2(HudLayout.CenterOffsetX, -(HudLayout.CenterTop + HudLayout.FloorBarH + 8f + HudLayout.HintH + 8f)),
            new Vector2(HudLayout.CenterWidth - 20f, 56f), TextAlignmentOptions.Midline);

        if (ageText != null)
        {
            ageText.enableAutoSizing = true;
            ageText.fontSizeMin = 20f;
            ageText.fontSizeMax = 40f;
            ageText.fontStyle = FontStyles.Bold;
            ageText.color = new Color(0.36f, 0.08f, 0.04f);   // parsomen uzerinde okunur koyu kirmizi
        }
        if (knowledgeText != null)
        {
            knowledgeText.enableAutoSizing = true;
            knowledgeText.fontSizeMin = 16f;
            knowledgeText.fontSizeMax = 26f;
            knowledgeText.fontStyle = FontStyles.Bold;
            knowledgeText.color = new Color(0.3f, 0.16f, 0.06f);
            knowledgeText.textWrappingMode = TextWrappingModes.NoWrap;
        }
        if (offlineText != null)
        {
            offlineText.enableAutoSizing = true;
            offlineText.fontSizeMin = 18f;
            offlineText.fontSizeMax = 26f;
            offlineText.fontStyle = FontStyles.Bold;
            offlineText.color = HudLayout.TextLight;
            offlineText.outlineWidth = 0.25f;
            offlineText.outlineColor = HudLayout.TextOutline;
        }

        // SAG SUTUN: bina listesi Bilgi hapinin altindan, x1..Max satirinin ustune kadar
        var scroll = rowParent != null ? rowParent.GetComponentInParent<ScrollRect>() : null;
        if (scroll != null)
        {
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(1f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(1f, 0.5f);
            srt.offsetMin = new Vector2(-(HudLayout.Margin + HudLayout.RightW) + HudLayout.PanelRightShift, HudLayout.ColumnBottom);
            srt.offsetMax = new Vector2(-HudLayout.Margin + HudLayout.PanelRightShift, -HudLayout.ColumnTop);
            var simg = scroll.GetComponent<Image>();
            if (simg != null) UIArt.Skin(simg, UIArt.ListPanel, HudLayout.PanelSoft);
            var layout = rowParent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 2f;
                layout.padding.top = 4;
                layout.padding.bottom = 4;
            }

            // Kartlar parsomen cercevenin ICINDE kalsin: viewport'u icerden kucult, scrollbar'i gizle (fare tekeri / surukleme calisir)
            if (UIArt.Has(UIArt.ListPanel))
            {
                scroll.verticalScrollbar = null;
                foreach (var sb2 in scroll.GetComponentsInChildren<Scrollbar>(true)) sb2.gameObject.SetActive(false);
                var vp = scroll.viewport;
                if (vp != null)
                {
                    vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one;
                    vp.offsetMin = new Vector2(106f, 112f);
                    vp.offsetMax = new Vector2(-106f, -112f);
                    var viewportImage = vp.GetComponent<Image>();
                    if (viewportImage != null)
                    {
                        // Outer parchment belongs to the ScrollRect; the viewport only clips rows.
                        viewportImage.enabled = false;
                    }
                    var mask = vp.GetComponent<Mask>();
                    if (mask != null) mask.enabled = false;
                    if (vp.GetComponent<RectMask2D>() == null) vp.gameObject.AddComponent<RectMask2D>();
                }
                scroll.vertical = false;
                scroll.horizontal = false;
            }
        }

        // ALT ORTA: tek buyuk buton (CEZALANDIR / IYILIK YAP). Kat gecisi otomatik oldugu icin kat atla butonu yok.
        if (gatherButton != null)
        {
            var grt = (RectTransform)gatherButton.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0f);
            grt.pivot = new Vector2(0.5f, 0f);
            grt.anchoredPosition = new Vector2(HudLayout.CenterOffsetX, HudLayout.GatherY);
            grt.sizeDelta = new Vector2(HudLayout.GatherW, HudLayout.GatherH);
            SkinGatherButton();
        }
        if (advanceButton != null)
        {
            // Sadece SON katta (Zaman Makinesi) gorunur; normal kat gecisi icin buton yok.
            var art = (RectTransform)advanceButton.transform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0f);
            art.pivot = new Vector2(0.5f, 0f);
            art.anchoredPosition = new Vector2(HudLayout.CenterOffsetX, HudLayout.GatherY + HudLayout.GatherH + 10f);
            art.sizeDelta = new Vector2(HudLayout.GatherW, 72f);
            advanceButton.gameObject.SetActive(false);
        }
        if (advanceButtonLabel != null)
        {
            advanceButtonLabel.enableAutoSizing = true;
            advanceButtonLabel.fontSizeMin = 16f;
            advanceButtonLabel.fontSizeMax = 22f;
            advanceButtonLabel.fontStyle = FontStyles.Bold;
        }
    }

    // Asili kat adi tabelasi (Art/UI/Level_UI); ageText bunun uzerine yazilir.
    void BuildTitlePlaque()
    {
        if (ageText == null) return;
        float aspect = UIArt.Aspect(UIArt.TitlePlaque, 2.6f);
        float h = HudLayout.TitleH, w = h * aspect;

        var plaque = UIFactory.CreatePanel(ageText.transform.parent, "TitlePlaque", Color.white, false);
        plaque.anchorMin = plaque.anchorMax = new Vector2(0.5f, 1f);
        plaque.pivot = new Vector2(0.5f, 1f);
        plaque.anchoredPosition = new Vector2(HudLayout.CenterOffsetX, 0f);
        plaque.sizeDelta = new Vector2(w, h);
        UIArt.Skin(plaque.GetComponent<Image>(), UIArt.TitlePlaque, new Color(0.96f, 0.88f, 0.7f, 0.95f));
        plaque.SetSiblingIndex(ageText.transform.GetSiblingIndex());   // yazinin arkasinda

        // Yazi tabelanin ortasindaki kartusun icine oturur
        var rt = ageText.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(HudLayout.CenterOffsetX, -h * 0.55f);
        rt.sizeDelta = new Vector2(w * 0.60f, h * 0.36f);
        ageText.alignment = TextAlignmentOptions.Midline;
        ageText.textWrappingMode = TextWrappingModes.Normal;
        ageText.overflowMode = TextOverflowModes.Truncate;
        ageText.raycastTarget = false;
    }

    // Sag ustte Bilgi hapi (Art/UI/kazanim_UI)
    void BuildKnowledgePill()
    {
        if (knowledgeText == null) return;
        const float w = HudLayout.PillW;
        var pill = UIFactory.CreatePanel(knowledgeText.transform.parent, "KnowledgePill", Color.white, false);
        pill.anchorMin = pill.anchorMax = new Vector2(1f, 1f);
        pill.pivot = new Vector2(1f, 1f);
        pill.anchoredPosition = new Vector2(-HudLayout.Margin, -HudLayout.TopInset);
        pill.sizeDelta = new Vector2(w, HudLayout.PillH);
        UIArt.Skin(pill.GetComponent<Image>(), UIArt.ResourcePill, HudLayout.Panel);
        pill.SetSiblingIndex(knowledgeText.transform.GetSiblingIndex());

        var rt = knowledgeText.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-HudLayout.Margin - 18f, -HudLayout.TopInset);
        rt.sizeDelta = new Vector2(w - 36f, HudLayout.PillH);
        knowledgeText.alignment = TextAlignmentOptions.Midline;
        knowledgeText.raycastTarget = false;
    }

    // TOPLA/CEZALANDIR butonu: Tap_Button_UI_Out (dis cerceve) + Tap_Button_UI_In (ic yuzey).
    // Basili tutarken ic yuzey Tap_Button_UI_In_Tappin olur. Gorsel yoksa eski sari buton.
    void SkinGatherButton()
    {
        var img = gatherButton.GetComponent<Image>();
        var gatherLabel = gatherButton.GetComponentInChildren<TMP_Text>();
        if (img != null)
        {
            bool framed = UIArt.Skin(img, UIArt.TapOuter, HudLayout.Accent);
            var inner = UIArt.Load(UIArt.TapInner);
            if (framed && inner != null)
            {
                var go = new GameObject("TapInner", typeof(RectTransform), typeof(Image));
                var irt = (RectTransform)go.transform;
                irt.SetParent(gatherButton.transform, false);
                irt.SetAsFirstSibling();   // yazinin arkasinda, dis cercevenin onunde
                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
                irt.offsetMin = irt.offsetMax = Vector2.zero;
                var iimg = go.GetComponent<Image>();
                iimg.sprite = inner; iimg.raycastTarget = false;
                go.AddComponent<TapPressSkin>().Init(iimg, inner, UIArt.Load(UIArt.TapPressed));
            }
            var c = gatherButton.colors;
            c.normalColor = c.highlightedColor = Color.white;
            c.pressedColor = Color.white;   // basili hali ic yuzey sprite'i gosterir
            gatherButton.colors = c;
        }
        if (gatherLabel != null)
        {
            gatherLabel.text = Loc.T("TOPLA");
            gatherLabel.fontStyle = FontStyles.Bold;
            gatherLabel.enableAutoSizing = true;
            gatherLabel.fontSizeMin = 28f;
            gatherLabel.fontSizeMax = 46f;
            gatherLabel.alignment = TextAlignmentOptions.Midline;
            gatherLabel.color = UIArt.Has(UIArt.TapInner) ? new Color(0.3f, 0.07f, 0.03f) : HudLayout.AccentInk;
            var lrt = gatherLabel.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(HudLayout.GatherW * 0.12f, HudLayout.GatherH * 0.2f);
            lrt.offsetMax = new Vector2(-HudLayout.GatherW * 0.12f, -HudLayout.GatherH * 0.2f);
        }
    }

    static void SetTopText(TMP_Text text, Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
    }

    // Kilometre tasi esigi gecilince binanin ustunde "x2!" cikar
    void OnMilestone(BuildingData b, int count)
    {
        Vector3 world;
        var cam = Camera.main;
        if (sceneSpawner == null || cam == null || !sceneSpawner.TryGetTopPosition(b, out world)) return;
        string msg = "x" + NumberFormatter.Format(GameManager.Instance.MilestoneMultiplier) + "!";
        floatingText.Spawn(msg, cam.WorldToScreenPoint(world), new Color(1f, 0.85f, 0.25f), 1.4f, 1.2f);
        SoundManager.Play(SoundId.LevelUp, 1.1f, 1f);
    }

    void HideOfflineText() { if (offlineText != null) offlineText.text = ""; }

    static string FormatDuration(double seconds)
    {
        int s = Mathf.RoundToInt((float)seconds);
        if (s < 90) return s + " " + Loc.T("sn");
        int m = s / 60;
        if (m < 90) return m + " " + Loc.T("dk");
        return (m / 60) + " " + Loc.T("sa") + " " + (m % 60) + " " + Loc.T("dk");
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnAgeChanged -= RebuildRows;
        gm.OnStateChanged -= Refresh;
        gm.OnMilestoneReached -= OnMilestone;
    }

    void RebuildRows()
    {
        foreach (var r in rows) Destroy(r.gameObject);
        rows.Clear();
        foreach (var b in GameManager.Instance.CurrentAge.buildings)
        {
            var row = Instantiate(rowPrefab, rowParent);
            row.Setup(b);
            rows.Add(row);
        }
    }

    // Kaynak hapleri: her kaynak icin ikonlu bir hap. Kat degisince sayi degisebilir; fazlalar gizlenir.
    void RefreshPills(AgeData age, GameManager gm)
    {
        if (resourcesText == null) return;
        if (pillParent == null) pillParent = (RectTransform)resourcesText.transform.parent;

        int n = age.resources.Length;
        while (pills.Count < n) pills.Add(CreatePill(pills.Count));
        for (int i = 0; i < pills.Count; i++)
        {
            var p = pills[i];
            if (i >= n) { p.rt.gameObject.SetActive(false); continue; }
            var r = age.resources[i];
            p.rt.gameObject.SetActive(true);
            var sprite = UIArt.ResourceIcon(r);
            p.icon.sprite = sprite;
            p.icon.enabled = sprite != null;
            p.text.text = Loc.F("{0}: {1}  (+{2}/sn)", Loc.T(r.displayName),
                NumberFormatter.Format(gm.GetResource(r)),
                NumberFormatter.Format(gm.GetProductionPerSecond(r)));
        }
    }

    Pill CreatePill(int index)
    {
        var p = new Pill();
        p.rt = UIFactory.CreatePanel(pillParent, "ResourcePill" + index, Color.white, false);
        p.rt.anchorMin = p.rt.anchorMax = new Vector2(0f, 1f);
        p.rt.pivot = new Vector2(0f, 1f);
        p.rt.anchoredPosition = new Vector2(HudLayout.LeftInset, -(HudLayout.TopInset + index * (HudLayout.PillH + HudLayout.PillGap)));
        p.rt.sizeDelta = new Vector2(HudLayout.PillW, HudLayout.PillH);
        UIArt.Skin(p.rt.GetComponent<Image>(), UIArt.ResourcePill, HudLayout.Panel);
        p.rt.SetSiblingIndex(resourcesText.transform.GetSiblingIndex());

        var iconRt = UIFactory.CreatePanel(p.rt, "Icon", Color.white, false);
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(12f, 0f);
        iconRt.sizeDelta = new Vector2(HudLayout.PillH - 10f, HudLayout.PillH - 10f);
        p.icon = iconRt.GetComponent<Image>();
        p.icon.preserveAspect = true;

        p.text = UIFactory.CreateText(p.rt, "Text", "", 26f, TextAlignmentOptions.MidlineLeft, new Color(0.3f, 0.14f, 0.06f),
            Vector2.zero, Vector2.one, new Vector2(HudLayout.PillH + 10f, 0f), new Vector2(-14f, 0f), true, true);
        p.text.textWrappingMode = TextWrappingModes.NoWrap;
        p.text.fontSizeMin = 16f;
        return p;
    }

    // Kat hedefi dolunca bir sonraki kata OTOMATIK gecilir. Boss cubugu bosalsin diye kisa bir bekleme var;
    // kamera animasyonu sirasindaysa (TowerView.Busy) animasyon bitene kadar beklenir.
    void QueueAutoAdvance()
    {
        if (advanceQueued) return;
        advanceQueued = true;
        Invoke(nameof(AutoAdvance), 0.6f);
    }

    void AutoAdvance()
    {
        advanceQueued = false;
        var gm = GameManager.Instance;
        if (gm == null || !gm.CanAdvance) return;
        var tower = FindAnyObjectByType<TowerView>();
        if (tower != null && tower.Busy) { QueueAutoAdvance(); return; }
        gm.AdvanceAge();
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        var age = gm.CurrentAge;
#if UNITY_EDITOR || DEBUG
        RefreshFastTestButton();
#endif

        ageText.text = Loc.T(age.displayName);
        knowledgeText.text = Loc.F("Bilgi: {0}  (x{1})", NumberFormatter.Format(gm.Knowledge),
                gm.Multiplier.ToString("0.0", CultureInfo.InvariantCulture))
            + (gm.TimeLoops > 0 ? Loc.F("  •  Döngü {0}", gm.TimeLoops) : "");

        RefreshPills(age, gm);

        foreach (var row in rows) row.Refresh();

        if (gm.NextAge != null)
        {
            // Normal kat: buton yok, hedef dolunca otomatik gecilir
            if (advanceButton != null) advanceButton.gameObject.SetActive(false);
            if (gm.CanAdvance) QueueAutoAdvance();
            return;
        }

        // Son kat: kat atlama yerine Zaman Makinesi (bu buton sadece burada gorunur)
        if (advanceButton == null) return;
        advanceButton.gameObject.SetActive(true);
        advanceButton.interactable = gm.CanUseTimeMachine;
        if (advanceButtonLabel != null)
            advanceButtonLabel.text = gm.CanUseTimeMachine
                ? Loc.T("Zaman Makinesi") + "\n+" + NumberFormatter.Format(gm.TimeMachineReward) + " " + Loc.T("Bilgi")
                : Loc.T("Zaman Makinesi") + "\n" + NumberFormatter.Format(gm.GetRunEarned(age.advanceResource)) + " / "
                    + NumberFormatter.Format(gm.AdvanceCost) + " " + Loc.T(age.advanceResource.displayName);

        bool canProgress = advanceButton.interactable;
        var advanceImage = advanceButton.GetComponent<Image>();
        if (advanceImage != null)
            advanceImage.color = canProgress ? new Color(0.91f, 0.63f, 0.28f) : new Color(0.66f, 0.58f, 0.53f);
        if (advanceButtonLabel != null)
            advanceButtonLabel.color = canProgress ? new Color(0.25f, 0.17f, 0.1f) : new Color(0.32f, 0.28f, 0.26f);
    }
}
