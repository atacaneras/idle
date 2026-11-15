using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sol tarafta, cag ici yukseltmeleri gosteren acilir liste. Sahneye elle bir sey eklemek gerekmez.
// Gorunur yukseltmelerin en ucuz MaxShown tanesi listelenir; biri alininca siradaki gelir.
public class UpgradePanel : MonoBehaviour
{
    const int MaxShown = 4;
    const float RowHeight = 100f;
    const float RowGap = 8f;
    const float HeaderHeight = HudLayout.LeftButtonH;
    const float DropdownMaxHeight = 460f;
    const float DropdownPadding = 8f;

    static readonly Color PanelColor = HudLayout.Panel;
    static readonly Color EffectColor = new Color(0.62f, 0.93f, 0.62f);
    static readonly Color FlavorColor = new Color(0.85f, 0.85f, 0.88f);
    static readonly Color CostColor = new Color(1f, 0.85f, 0.35f);

    class Row
    {
        public UpgradeData data;
        public RectTransform rt;
        public Button button;
        public TextMeshProUGUI cost;
    }

    readonly List<Row> rows = new List<Row>();
    readonly List<UpgradeData> visible = new List<UpgradeData>();
    RectTransform root;
    RectTransform dropdown;
    RectTransform viewport;
    RectTransform content;
    ScrollRect scrollRect;
    TextMeshProUGUI emptyText;
    Button headerButton;
    TextMeshProUGUI headerLabel;
    TextMeshProUGUI plusLabel;
    FloatingTextSpawner texts;
    bool expanded;

    void Start()
    {
        texts = GetComponent<FloatingTextSpawner>();
        var canvas = GetComponentInParent<Canvas>().rootCanvas;

        root = UIFactory.CreatePanel(canvas.transform, "UpgradePanel", PanelColor, false);
        root.SetAsFirstSibling();   // yuzen yazilarin arkasinda kalsin
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(HudLayout.LeftInset, -HudLayout.LeftButtonsTop);   // kaynak haplerinin altinda, Bilgi Agaci ile ayni satir
        root.sizeDelta = new Vector2(HudLayout.UpgradeHeaderW, HeaderHeight);

        headerLabel = UIFactory.CreateText(root, "HeaderLabel", Loc.T("YÜKSELTMELER"), 22f, TextAlignmentOptions.MidlineLeft, Color.white,
            Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-62f, 0f), true);
        headerButton = UIFactory.CreateButton(root, "ExpandButton", "+", 26f, out plusLabel);
        var headerRt = (RectTransform)headerButton.transform;
        // Tum baslik tiklanir; "+" yazisi sagda, buton gorunmez
        headerRt.anchorMin = Vector2.zero; headerRt.anchorMax = Vector2.one;
        headerRt.offsetMin = headerRt.offsetMax = Vector2.zero;
        headerButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        plusLabel.alignment = TextAlignmentOptions.MidlineRight;
        plusLabel.rectTransform.offsetMax = new Vector2(-16f, 0f);
        plusLabel.color = UIArt.ButtonText;
        bool skinned = UIArt.ApplySliced(root.GetComponent<Image>(), UIArt.ModeOff, 34f, 14f, PanelColor);
        if (skinned) headerLabel.color = UIArt.ButtonText;
        headerButton.onClick.AddListener(ToggleExpanded);

        dropdown = UIFactory.CreatePanel(root, "UpgradeDropdown", new Color(0.12f, 0.1f, 0.12f, 0.97f), true);
        dropdown.anchorMin = new Vector2(0f, 1f);
        dropdown.anchorMax = new Vector2(0f, 1f);   // baslik dar (340); acilir liste LeftW genisliginde
        dropdown.pivot = new Vector2(0f, 1f);
        dropdown.anchoredPosition = new Vector2(0f, -HeaderHeight);
        dropdown.sizeDelta = new Vector2(0f, 0f);

        scrollRect = dropdown.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        var maskRect = UIFactory.CreatePanel(dropdown, "Viewport", Color.white, true);
        viewport = maskRect;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(DropdownPadding, DropdownPadding);
        viewport.offsetMax = new Vector2(-DropdownPadding, -DropdownPadding);
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var contentObject = new GameObject("Content", typeof(RectTransform));
        content = (RectTransform)contentObject.transform;
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        emptyText = UIFactory.CreateText(content, "Empty", "", 24f, TextAlignmentOptions.TopLeft, FlavorColor,
            Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f), false, true);
        dropdown.gameObject.SetActive(false);

        var gm = GameManager.Instance;
        gm.OnUpgradesChanged += Rebuild;
        gm.OnStateChanged += Refresh;
        HudLayout.LeftPanelOpened += OnOtherPanelOpened;
        Rebuild();
    }

    void OnOtherPanelOpened(object sender) { if (sender != (object)this && expanded) ClosePopup(); }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnUpgradesChanged -= Rebuild;
        gm.OnStateChanged -= Refresh;
        HudLayout.LeftPanelOpened -= OnOtherPanelOpened;
    }

    // Gorunur liste degistiyse satirlari yeniden kurar, degismediyse sadece tiklanabilirligi gunceller.
    void Refresh()
    {
        var gm = GameManager.Instance;
        gm.GetVisibleUpgrades(visible);

        bool same = true;
        int shown = Mathf.Min(MaxShown, visible.Count);
        UpdateHeader();
        if (shown != rows.Count) same = false;
        else for (int i = 0; i < shown; i++) if (rows[i].data != visible[i]) { same = false; break; }

        if (!same) { Rebuild(); return; }

        foreach (var r in rows)
        {
            r.button.interactable = gm.CanAffordUpgrade(r.data);
            r.cost.text = FormatCost(gm, r.data);
        }
    }

    void Rebuild()
    {
        var gm = GameManager.Instance;
        foreach (var r in rows) Destroy(r.rt.gameObject);
        rows.Clear();

        gm.GetVisibleUpgrades(visible);
        int shown = Mathf.Min(MaxShown, visible.Count);
        float contentHeight = Mathf.Max(1, shown) * (RowHeight + RowGap);
        float dropdownHeight = Mathf.Min(DropdownMaxHeight, contentHeight + DropdownPadding * 2f);
        dropdown.sizeDelta = new Vector2(HudLayout.LeftW, dropdownHeight);
        content.sizeDelta = new Vector2(0f, contentHeight);
        scrollRect.vertical = contentHeight + DropdownPadding * 2f > dropdownHeight;
        UpdateHeader();
        for (int i = 0; i < shown; i++) rows.Add(CreateRow(visible[i], i));

        if (shown == 0)
        {
            emptyText.text = Loc.T("Yeni fikir yok. Biraz daha bina al, belki ilham gelir.");
            emptyText.gameObject.SetActive(true);
        }
        else emptyText.gameObject.SetActive(false);

        foreach (var r in rows) r.button.interactable = gm.CanAffordUpgrade(r.data);
    }

    void ToggleExpanded()
    {
        expanded = !expanded;
        dropdown.gameObject.SetActive(expanded);
        if (expanded) { root.SetAsLastSibling(); HudLayout.RaiseLeftPanelOpened(this); }
        UpdateHeader();
    }

    void ClosePopup()
    {
        expanded = false;
        dropdown.gameObject.SetActive(false);
        UpdateHeader();
    }

    void UpdateHeader()
    {
        headerLabel.text = visible.Count > 0
            ? Loc.F("YÜKSELTMELER ({0})", visible.Count)
            : Loc.T("YÜKSELTMELER");
        plusLabel.text = expanded ? "−" : "+";
    }

    Row CreateRow(UpgradeData u, int index)
    {
        TextMeshProUGUI unused;
        var btn = UIFactory.CreateButton(content, "Upg_" + u.id, "", 20f, out unused);
        Destroy(unused.gameObject);   // CreateButton'in tek satirlik etiketine ihtiyacimiz yok

        var rt = (RectTransform)btn.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, -index * (RowHeight + RowGap));
        rt.sizeDelta = new Vector2(0f, RowHeight);

        UIFactory.CreateText(rt, "Name", Loc.T(u.displayName), 28f, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-190f, -6f), true, true);

        string desc = "<color=#" + ColorUtility.ToHtmlStringRGB(EffectColor) + ">" + DescribeEffect(u) + "</color>";
        if (!string.IsNullOrEmpty(u.description)) desc += "\n" + Loc.T(u.description);
        UIFactory.CreateText(rt, "Desc", desc, 20f, TextAlignmentOptions.TopLeft, FlavorColor,
            new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(12f, 6f), new Vector2(-190f, 0f), false, true);

        var cost = UIFactory.CreateText(rt, "Cost", FormatCost(GameManager.Instance, u), 26f, TextAlignmentOptions.MidlineRight, CostColor,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-180f, 0f), new Vector2(-12f, 0f), true, true);

        var captured = u;
        btn.onClick.AddListener(() => Buy(captured, rt));
        return new Row { data = u, rt = rt, button = btn, cost = cost };
    }

    static string FormatCost(GameManager gm, UpgradeData u)
    {
        return NumberFormatter.Format(gm.GetUpgradeCost(u)) + (u.costResource != null ? " " + Loc.T(u.costResource.displayName) : "");
    }

    void Buy(UpgradeData u, RectTransform where)
    {
        var gm = GameManager.Instance;
        if (!gm.TryBuyUpgrade(u)) return;
        SoundManager.Play(SoundId.Buy, 1.25f, 1f);
        if (texts != null && where != null) texts.SpawnAt(Loc.T(u.displayName), where);
    }

    public static string DescribeEffect(UpgradeData u)
    {
        var ci = CultureInfo.InvariantCulture;
        switch (u.kind)
        {
            case UpgradeKind.ClickMultiplier:
                return Loc.F("Tıklama gücü x{0}", NumberFormatter.Format(u.value));
            case UpgradeKind.ClickProductionShare:
                return Loc.F("Her tıklama +%{0} saniyelik üretim", (u.value * 100.0).ToString("0.##", ci));
            case UpgradeKind.BuildingMultiplier:
                return Loc.F("{0} üretimi x{1}",
                    u.targetBuilding != null ? Loc.T(u.targetBuilding.displayName) : "?",
                    NumberFormatter.Format(u.value));
            case UpgradeKind.GlobalMultiplier:
                return Loc.F("Tüm üretim x{0}", NumberFormatter.Format(u.value));
        }
        return "";
    }
}
