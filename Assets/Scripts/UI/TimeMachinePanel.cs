using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Iki isi birlikte yapan tam ekran pencere:
//   1) Bilgi Agaci: Bilgi harcayarak kalici yukseltmeler alinir (sifirlamada kaybolmaz)
//   2) Zaman Makinesi: son cagda basa donmeyi saglar, karsiliginda Bilgi verir
// Sag ustteki "Bilgi Agaci" butonuyla ya da son cagda alttaki cag butonuyla acilir. Hepsi kodla kurulur.
public class TimeMachinePanel : MonoBehaviour
{
    const int Columns = 2;
    const float NodeWidth = 610f;
    const float NodeHeight = 104f;
    const float NodeGap = 10f;
    const float ConfirmSeconds = 4f;

    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color WindowColor = new Color(0.1f, 0.09f, 0.13f, 0.98f);
    static readonly Color GoldColor = new Color(1f, 0.85f, 0.35f);
    static readonly Color GreenColor = new Color(0.62f, 0.93f, 0.62f);
    static readonly Color GrayColor = new Color(0.75f, 0.75f, 0.8f);

    class Node
    {
        public PermanentUpgradeData data;
        public Button button;
        public TextMeshProUGUI name, desc, cost;
    }

    readonly List<Node> nodes = new List<Node>();
    GameObject overlay;
    TextMeshProUGUI balanceText, tmInfoText, tmLabel;
    Button tmButton;
    Button openButton;
    TextMeshProUGUI openLabel;
    FloatingTextSpawner texts;
    bool open;
    float confirmTimer = -1f;
    bool lastAffordable;

    void Start()
    {
        texts = GetComponent<FloatingTextSpawner>();
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var gm = GameManager.Instance;

        BuildOpenButton(canvas.transform);
        BuildWindow(canvas.transform, gm);

        gm.OnStateChanged += Refresh;
        gm.OnPermanentChanged += Refresh;
        gm.OnTimeMachineUsed += OnUsed;
        overlay.SetActive(false);
        Refresh();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnStateChanged -= Refresh;
        gm.OnPermanentChanged -= Refresh;
        gm.OnTimeMachineUsed -= OnUsed;
    }

    // ---- Kurulum ----
    void BuildOpenButton(Transform canvas)
    {
        openButton = UIFactory.CreateButton(canvas, "OpenTreeButton", "Bilgi Ağacı", 30f, out openLabel);
        var rt = (RectTransform)openButton.transform;
        rt.SetAsLastSibling();
        // Yukseltme panelinin basligina yerlestir; bina satirlarinin ustune tasmasin.
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(415f, -250f);
        rt.sizeDelta = new Vector2(200f, 50f);
        openLabel.fontSize = 22f;
        openButton.onClick.AddListener(Open);
    }

    void BuildWindow(Transform canvas, GameManager gm)
    {
        var dim = UIFactory.CreatePanel(canvas, "TreeOverlay", DimColor, true);
        overlay = dim.gameObject;
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
        dim.offsetMin = Vector2.zero; dim.offsetMax = Vector2.zero;

        int count = gm.PermanentUpgrades != null ? gm.PermanentUpgrades.Length : 0;
        int rowsNeeded = Mathf.Max(1, (count + Columns - 1) / Columns);
        float gridH = rowsNeeded * (NodeHeight + NodeGap);
        float winW = Columns * NodeWidth + (Columns + 1) * NodeGap + 20f;
        float winH = 90f + gridH + 190f;

        var win = UIFactory.CreatePanel(dim, "Window", WindowColor, true);
        win.anchorMin = win.anchorMax = win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(winW, Mathf.Min(winH, 1040f));

        UIFactory.CreateText(win, "Title", "Bilgi Ağacı", 44f, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(24f, -80f), new Vector2(0f, -10f), true);
        balanceText = UIFactory.CreateText(win, "Balance", "", 34f, TextAlignmentOptions.MidlineRight, GoldColor,
            new Vector2(0.45f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), new Vector2(-170f, -10f), true);

        TextMeshProUGUI closeLbl;
        var close = UIFactory.CreateButton(win, "Close", "Kapat", 28f, out closeLbl);
        var crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-16f, -18f);
        crt.sizeDelta = new Vector2(140f, 56f);
        close.onClick.AddListener(Close);

        // Kalici yukseltme kutulari (iki sutun)
        for (int i = 0; i < count; i++)
        {
            var p = gm.PermanentUpgrades[i];
            if (p == null) continue;
            int col = i % Columns, row = i / Columns;

            TextMeshProUGUI unused;
            var btn = UIFactory.CreateButton(win, "Perm_" + p.id, "", 20f, out unused);
            Destroy(unused.gameObject);
            var rt = (RectTransform)btn.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(NodeGap + 10f + col * (NodeWidth + NodeGap), -90f - row * (NodeHeight + NodeGap));
            rt.sizeDelta = new Vector2(NodeWidth, NodeHeight);

            var n = new Node { data = p, button = btn };
            n.name = UIFactory.CreateText(rt, "Name", p.displayName, 28f, TextAlignmentOptions.MidlineLeft, Color.white,
                new Vector2(0f, 0.58f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-170f, -6f), true, true);
            n.desc = UIFactory.CreateText(rt, "Desc", "", 20f, TextAlignmentOptions.TopLeft, GrayColor,
                new Vector2(0f, 0f), new Vector2(1f, 0.58f), new Vector2(14f, 6f), new Vector2(-170f, 0f), false, true);
            n.cost = UIFactory.CreateText(rt, "Cost", "", 26f, TextAlignmentOptions.MidlineRight, GoldColor,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-160f, 0f), new Vector2(-14f, 0f), true, true);

            var captured = p;
            btn.onClick.AddListener(() => BuyPerm(captured));
            nodes.Add(n);
        }

        // Zaman Makinesi bolumu
        var sep = UIFactory.CreatePanel(win, "Separator", new Color(1f, 1f, 1f, 0.15f), false);
        sep.anchorMin = new Vector2(0f, 0f); sep.anchorMax = new Vector2(1f, 0f);
        sep.offsetMin = new Vector2(20f, 176f); sep.offsetMax = new Vector2(-20f, 178f);

        tmInfoText = UIFactory.CreateText(win, "TMInfo", "", 24f, TextAlignmentOptions.TopLeft, GrayColor,
            new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(24f, 16f), new Vector2(0f, 166f), false, true);

        tmButton = UIFactory.CreateButton(win, "TimeMachineButton", "Zaman Makinesi", 30f, out tmLabel);
        var trt = (RectTransform)tmButton.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(1f, 0f);
        trt.pivot = new Vector2(1f, 0f);
        trt.anchoredPosition = new Vector2(-24f, 30f);
        trt.sizeDelta = new Vector2(620f, 120f);
        tmButton.onClick.AddListener(OnTimeMachineClicked);
    }

    // ---- Ac / kapat ----
    public void Open()
    {
        open = true;
        confirmTimer = -1f;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Refresh();
    }

    void Close()
    {
        open = false;
        confirmTimer = -1f;
        overlay.SetActive(false);
    }

    void Update()
    {
        if (confirmTimer < 0f) return;
        confirmTimer -= Time.unscaledDeltaTime;
        if (confirmTimer < 0f) { confirmTimer = -1f; Refresh(); }
    }

    // ---- Eylemler ----
    void BuyPerm(PermanentUpgradeData p)
    {
        if (GameManager.Instance.TryBuyPerm(p)) SoundManager.Play(SoundId.LevelUp, 1f, 0.8f);
    }

    // Ilk basista onay ister (her sey sifirlanacagi icin), 4 sn icinde ikinci basista calistirir.
    void OnTimeMachineClicked()
    {
        var gm = GameManager.Instance;
        if (!gm.CanUseTimeMachine) return;
        if (confirmTimer < 0f)
        {
            confirmTimer = ConfirmSeconds;
            Refresh();
            return;
        }
        confirmTimer = -1f;
        gm.UseTimeMachine();
    }

    void OnUsed(double reward)
    {
        Close();
        if (texts != null)
            texts.Spawn("+" + NumberFormatter.Format(reward) + " Bilgi", new Vector2(Screen.width * 0.5f, Screen.height * 0.45f));
    }

    // ---- Gorunum ----
    void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentAge == null) return;

        // Sag ustteki buton: alinabilir bir sey varsa nokta ile isaretle
        bool any = false;
        foreach (var n in nodes) if (gm.CanBuyPerm(n.data)) { any = true; break; }
        if (any != lastAffordable || openLabel.text.Length == 0)
        {
            lastAffordable = any;
            openLabel.text = any ? "Bilgi Ağacı •" : "Bilgi Ağacı";
        }

        if (!open) return;

        balanceText.text = "Bilgi: " + NumberFormatter.Format(gm.Knowledge);

        foreach (var n in nodes) RefreshNode(gm, n);
        RefreshTimeMachine(gm);
    }

    void RefreshNode(GameManager gm, Node n)
    {
        var p = n.data;
        int lv = gm.GetPermLevel(p);
        bool visible = gm.IsPermVisible(p);
        bool maxed = gm.IsPermMaxed(p);

        n.name.text = p.displayName + (visible ? "   " + lv + "/" + p.maxLevel : "");
        if (!visible)
        {
            n.desc.text = "Kilitli. Önce: " + (p.requires != null ? p.requires.displayName : "?");
            n.cost.text = "";
        }
        else
        {
            n.desc.text = "<color=#" + ColorUtility.ToHtmlStringRGB(GreenColor) + ">" + DescribeEffect(p, lv, maxed) + "</color>"
                + (string.IsNullOrEmpty(p.description) ? "" : "\n" + p.description);
            n.cost.text = maxed ? "MAKS" : NumberFormatter.Format(gm.GetPermCost(p)) + " Bilgi";
        }
        n.button.interactable = gm.CanBuyPerm(p);
    }

    void RefreshTimeMachine(GameManager gm)
    {
        var age = gm.CurrentAge;
        if (!gm.IsFinalAge)
        {
            tmInfoText.text = "<b>Zaman Makinesi</b>\nSon çağa ulaşınca açılır. Her şeyi sıfırlar ama Bilgi ve bu ağaçtaki yükseltmeler kalır.";
            tmLabel.text = "Zaman Makinesi (Kilitli)";
            tmButton.interactable = false;
            return;
        }

        string res = age.advanceResource != null ? age.advanceResource.displayName : "?";
        if (!gm.CanUseTimeMachine)
        {
            tmInfoText.text = "<b>Zaman Makinesi</b>\nÇalıştırmak için " + NumberFormatter.Format(gm.AdvanceCost) + " " + res
                + " gerekiyor. Daha fazla biriktirirsen daha çok Bilgi kazanırsın.";
            tmLabel.text = "Zaman Makinesi (Kilitli)";
            tmButton.interactable = false;
            return;
        }

        tmInfoText.text = "<b>Zaman Makinesi hazır.</b>\nŞu an çalıştırırsan <color=#" + ColorUtility.ToHtmlStringRGB(GoldColor) + ">+"
            + NumberFormatter.Format(gm.TimeMachineReward) + " Bilgi</color> kazanırsın. " + res
            + " biriktikçe ödül büyür (karekökle). Bina, kaynak ve yükseltmelerin sıfırlanır.";
        tmButton.interactable = true;
        tmLabel.text = confirmTimer >= 0f ? "Emin misin? Her şey sıfırlanır. Tekrar bas!" : "Zaman Makinesi'ni Çalıştır";
    }

    static string DescribeEffect(PermanentUpgradeData p, int level, bool maxed)
    {
        var ci = CultureInfo.InvariantCulture;
        double per = p.valuePerLevel;
        double now = per * level;
        string pct = "%" + (per * 100.0).ToString("0.##", ci);
        string nowPct = "%" + (now * 100.0).ToString("0.##", ci);
        string cur = level > 0 ? "  (şu an " : "";
        string tail = level > 0 ? ")" : "";

        switch (p.kind)
        {
            case PermanentKind.GlobalProduction: return "Tüm üretim +" + pct + " / seviye" + cur + "+" + nowPct + tail;
            case PermanentKind.ClickPower: return "Tıklama gücü +" + pct + " / seviye" + cur + "+" + nowPct + tail;
            case PermanentKind.CostReduction: return "Bina maliyeti -" + pct + " / seviye" + cur + "-" + nowPct + tail;
            case PermanentKind.AdvanceDiscount: return "Çağ atlama maliyeti -" + pct + " / seviye" + cur + "-" + nowPct + tail;
            case PermanentKind.KnowledgeGain: return "Kazanılan Bilgi +" + pct + " / seviye" + cur + "+" + nowPct + tail;
            case PermanentKind.OfflineHours:
                return "Çevrimdışı sınır +" + per.ToString("0.##", ci) + " sa / seviye" + (level > 0 ? "  (şu an +" + now.ToString("0.##", ci) + " sa)" : "");
        }
        return "";
    }
}
