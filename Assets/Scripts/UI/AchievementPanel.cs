using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sag ustteki "Başarımlar" butonuyla acilan liste penceresi. Kodla kurulur, sahneye elle bir sey eklenmez.
// Kilitli basarimlar da aciklamasiyla gorunur (ipucu); kazanilanlar altin renkte.
public class AchievementPanel : MonoBehaviour
{
    const int Columns = 2;
    const float RowWidth = 610f;
    const float RowHeight = 92f;
    const float RowGap = 10f;

    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color WindowColor = new Color(0.1f, 0.09f, 0.13f, 0.98f);
    static readonly Color LockedColor = new Color(0.2f, 0.19f, 0.24f, 1f);
    static readonly Color UnlockedColor = new Color(0.42f, 0.33f, 0.12f, 1f);
    static readonly Color GoldColor = new Color(1f, 0.85f, 0.35f);
    static readonly Color GrayColor = new Color(0.72f, 0.72f, 0.78f);

    class Row
    {
        public AchievementDef def;
        public Image bg;
        public TextMeshProUGUI name, desc, tag;
    }

    readonly List<Row> rows = new List<Row>();
    GameObject overlay;
    TextMeshProUGUI titleText, openLabel;
    Button openButton;
    bool open;
    int lastCount = -1;

    public bool IsOpen { get { return open; } }

    void Start()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogWarning("AchievementPanel: Canvas bulunamadi."); return; }
        BuildOpenButton(canvas.rootCanvas.transform);
        BuildWindow(canvas.rootCanvas.transform);
        overlay.SetActive(false);

        var gm = GameManager.Instance;
        gm.OnAchievementUnlocked += OnUnlocked;
        gm.OnStateChanged += RefreshButton;
        RefreshButton();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnAchievementUnlocked -= OnUnlocked;
        gm.OnStateChanged -= RefreshButton;
    }

    void BuildOpenButton(Transform canvas)
    {
        openButton = UIFactory.CreateButton(canvas, "OpenAchievementsButton", Loc.T("Başarımlar"), 22f, out openLabel);
        var rt = (RectTransform)openButton.transform;
        rt.SetAsLastSibling();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-300f, -86f);   // Bilgi Agaci butonunun soluna
        rt.sizeDelta = new Vector2(250f, 54f);
        openLabel.fontSizeMin = 16f;
        openLabel.fontSizeMax = 22f;
        openLabel.enableAutoSizing = true;
        openButton.onClick.AddListener(Open);
    }

    void BuildWindow(Transform canvas)
    {
        var dim = UIFactory.CreatePanel(canvas, "AchievementOverlay", DimColor, true);
        overlay = dim.gameObject;
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
        dim.offsetMin = Vector2.zero; dim.offsetMax = Vector2.zero;

        float winW = Columns * RowWidth + (Columns - 1) * RowGap + 56f;
        var win = UIFactory.CreatePanel(dim, "Window", WindowColor, true);
        win.anchorMin = win.anchorMax = win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(winW, 940f);

        titleText = UIFactory.CreateText(win, "Title", "", 44f, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0f, 1f), new Vector2(0.7f, 1f), new Vector2(28f, -80f), new Vector2(0f, -10f), true);

        TextMeshProUGUI closeLbl;
        var close = UIFactory.CreateButton(win, "Close", Loc.T("Kapat"), 28f, out closeLbl);
        var crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-16f, -18f);
        crt.sizeDelta = new Vector2(150f, 56f);
        close.onClick.AddListener(Close);

        // Kaydirilabilir liste (fare tekerlegi / surukleme)
        var viewport = UIFactory.CreatePanel(win, "Viewport", new Color(0f, 0f, 0f, 0.001f), true);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(28f, 20f); viewport.offsetMax = new Vector2(-28f, -92f);
        viewport.gameObject.AddComponent<RectMask2D>();

        int count = AchievementSystem.All.Count;
        int rowCount = (count + Columns - 1) / Columns;
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, rowCount * (RowHeight + RowGap));

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        for (int i = 0; i < count; i++)
        {
            int col = i % Columns, row = i / Columns;
            var def = AchievementSystem.All[i];

            var rt = UIFactory.CreatePanel(content, "Ach_" + def.id, LockedColor, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(col * (RowWidth + RowGap), -row * (RowHeight + RowGap));
            rt.sizeDelta = new Vector2(RowWidth, RowHeight);

            var r = new Row { def = def, bg = rt.GetComponent<Image>() };
            r.name = UIFactory.CreateText(rt, "Name", Loc.T(def.name), 28f, TextAlignmentOptions.MidlineLeft, Color.white,
                new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-150f, -6f), true, true);
            r.desc = UIFactory.CreateText(rt, "Desc", Loc.T(def.description), 21f, TextAlignmentOptions.TopLeft, GrayColor,
                new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(16f, 6f), new Vector2(-16f, 0f), false, true);
            r.tag = UIFactory.CreateText(rt, "Tag", "", 22f, TextAlignmentOptions.MidlineRight, GoldColor,
                new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(-140f, 0f), new Vector2(-14f, -6f), true, true);
            rows.Add(r);
        }
    }

    public void Open()
    {
        if (overlay == null) return;
        open = true;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        RefreshRows();
    }

    public void Close()
    {
        open = false;
        if (overlay != null) overlay.SetActive(false);
    }

    void OnUnlocked(string id)
    {
        lastCount = -1;
        RefreshButton();
        if (open) RefreshRows();
    }

    void RefreshButton()
    {
        var gm = GameManager.Instance;
        if (gm == null || openLabel == null) return;
        int n = gm.AchievementCount;
        if (n == lastCount) return;
        lastCount = n;
        openLabel.text = Loc.T("Başarımlar") + " " + n + "/" + AchievementSystem.All.Count;
    }

    void RefreshRows()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        titleText.text = Loc.T("Başarımlar") + "   " + gm.AchievementCount + "/" + AchievementSystem.All.Count;
        foreach (var r in rows)
        {
            bool done = gm.IsAchievementUnlocked(r.def.id);
            r.bg.color = done ? UnlockedColor : LockedColor;
            r.name.color = done ? GoldColor : new Color(0.85f, 0.85f, 0.9f);
            r.tag.text = done ? Loc.T("Açıldı") : Loc.T("Kilitli");
            r.tag.color = done ? GoldColor : new Color(0.55f, 0.55f, 0.62f);
        }
    }
}
