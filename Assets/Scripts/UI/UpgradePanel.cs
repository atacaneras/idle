using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sol ortada, cag ici yukseltmeleri gosteren panel. Sahneye elle bir sey eklemek gerekmez.
// Gorunur yukseltmelerin en ucuz MaxShown tanesi listelenir; biri alininca siradaki gelir.
public class UpgradePanel : MonoBehaviour
{
    const int MaxShown = 4;
    const float RowHeight = 100f;
    const float RowGap = 8f;
    const float HeaderHeight = 50f;

    static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.1f, 0.55f);
    static readonly Color EffectColor = new Color(0.62f, 0.93f, 0.62f);
    static readonly Color FlavorColor = new Color(0.85f, 0.85f, 0.88f);
    static readonly Color CostColor = new Color(1f, 0.85f, 0.35f);

    class Row
    {
        public UpgradeData data;
        public RectTransform rt;
        public Button button;
    }

    readonly List<Row> rows = new List<Row>();
    readonly List<UpgradeData> visible = new List<UpgradeData>();
    RectTransform root;
    TextMeshProUGUI emptyText;
    FloatingTextSpawner texts;

    void Start()
    {
        texts = GetComponent<FloatingTextSpawner>();
        var canvas = GetComponentInParent<Canvas>().rootCanvas;

        root = UIFactory.CreatePanel(canvas.transform, "UpgradePanel", PanelColor, false);
        root.SetAsFirstSibling();   // yuzen yazilarin arkasinda kalsin
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(30f, -250f);
        root.sizeDelta = new Vector2(600f, HeaderHeight + MaxShown * (RowHeight + RowGap) + 8f);

        UIFactory.CreateText(root, "Header", "Yükseltmeler", 34f, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -HeaderHeight), new Vector2(-14f, 0f), true);

        emptyText = UIFactory.CreateText(root, "Empty", "", 24f, TextAlignmentOptions.TopLeft, FlavorColor,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 8f), new Vector2(-14f, -HeaderHeight - 8f), false, true);

        var gm = GameManager.Instance;
        gm.OnUpgradesChanged += Rebuild;
        gm.OnStateChanged += Refresh;
        Rebuild();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnUpgradesChanged -= Rebuild;
        gm.OnStateChanged -= Refresh;
    }

    // Gorunur liste degistiyse satirlari yeniden kurar, degismediyse sadece tiklanabilirligi gunceller.
    void Refresh()
    {
        var gm = GameManager.Instance;
        gm.GetVisibleUpgrades(visible);

        bool same = true;
        int shown = Mathf.Min(MaxShown, visible.Count);
        if (shown != rows.Count) same = false;
        else for (int i = 0; i < shown; i++) if (rows[i].data != visible[i]) { same = false; break; }

        if (!same) { Rebuild(); return; }

        foreach (var r in rows) r.button.interactable = gm.CanAffordUpgrade(r.data);
    }

    void Rebuild()
    {
        var gm = GameManager.Instance;
        foreach (var r in rows) Destroy(r.rt.gameObject);
        rows.Clear();

        gm.GetVisibleUpgrades(visible);
        int shown = Mathf.Min(MaxShown, visible.Count);
        for (int i = 0; i < shown; i++) rows.Add(CreateRow(visible[i], i));

        if (shown == 0)
        {
            emptyText.text = "Yeni fikir yok. Biraz daha bina al, belki ilham gelir.";
            emptyText.gameObject.SetActive(true);
        }
        else emptyText.gameObject.SetActive(false);

        foreach (var r in rows) r.button.interactable = gm.CanAffordUpgrade(r.data);
    }

    Row CreateRow(UpgradeData u, int index)
    {
        TextMeshProUGUI unused;
        var btn = UIFactory.CreateButton(root, "Upg_" + u.id, "", 20f, out unused);
        Destroy(unused.gameObject);   // CreateButton'in tek satirlik etiketine ihtiyacimiz yok

        var rt = (RectTransform)btn.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(8f, -HeaderHeight - index * (RowHeight + RowGap));
        rt.sizeDelta = new Vector2(584f, RowHeight);

        UIFactory.CreateText(rt, "Name", u.displayName, 28f, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-190f, -6f), true, true);

        string desc = "<color=#" + ColorUtility.ToHtmlStringRGB(EffectColor) + ">" + DescribeEffect(u) + "</color>";
        if (!string.IsNullOrEmpty(u.description)) desc += "\n" + u.description;
        UIFactory.CreateText(rt, "Desc", desc, 20f, TextAlignmentOptions.TopLeft, FlavorColor,
            new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(12f, 6f), new Vector2(-190f, 0f), false, true);

        string cost = NumberFormatter.Format(u.cost) + (u.costResource != null ? " " + u.costResource.displayName : "");
        UIFactory.CreateText(rt, "Cost", cost, 26f, TextAlignmentOptions.MidlineRight, CostColor,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-180f, 0f), new Vector2(-12f, 0f), true, true);

        var captured = u;
        btn.onClick.AddListener(() => Buy(captured, rt));
        return new Row { data = u, rt = rt, button = btn };
    }

    void Buy(UpgradeData u, RectTransform where)
    {
        var gm = GameManager.Instance;
        if (!gm.TryBuyUpgrade(u)) return;
        SoundManager.Play(SoundId.Buy, 1.25f, 1f);
        if (texts != null && where != null) texts.SpawnAt(u.displayName, where);
    }

    public static string DescribeEffect(UpgradeData u)
    {
        var ci = CultureInfo.InvariantCulture;
        switch (u.kind)
        {
            case UpgradeKind.ClickMultiplier:
                return "Tıklama gücü x" + NumberFormatter.Format(u.value);
            case UpgradeKind.ClickProductionShare:
                return "Her tıklama +%" + (u.value * 100.0).ToString("0.##", ci) + " saniyelik üretim";
            case UpgradeKind.BuildingMultiplier:
                return (u.targetBuilding != null ? u.targetBuilding.displayName : "?") + " üretimi x" + NumberFormatter.Format(u.value);
            case UpgradeKind.GlobalMultiplier:
                return "Tüm üretim x" + NumberFormatter.Format(u.value);
        }
        return "";
    }
}
