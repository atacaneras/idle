using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sol gezinme alaninda tam genislikli bir gorev sekmesi. Tiklayinca bekleyen ilk 4 gorev acilir.
// Odulu alinacak gorev varsa buton altin renkte yanar ve sayiyi gosterir.
// Gorevler AgeData.quests'ten gelir, icerik content.json'da yazilir.
public class QuestPanel : MonoBehaviour
{
    class Row { public QuestDef q; public RectTransform rt; public TextMeshProUGUI text, prog; public Button btn; public Image bg; }

    const int Slots = 4;
    const float HeaderH = HudLayout.LeftButtonH, RowH = 92f, RowGap = 6f, Pad = 8f;

    RectTransform popup;
    Button toggle;
    TextMeshProUGUI toggleLabel, plusLabel;
    Image toggleImg;
    bool expanded, skinned;
    readonly List<Row> rows = new List<Row>();

    static readonly Color IdleColor = HudLayout.Panel;
    static readonly Color ReadyColor = new Color(0.96f, 0.73f, 0.28f, 0.98f);

    public static QuestPanel Create(Canvas canvas)
    {
        var go = new GameObject("QuestPanel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var qp = go.AddComponent<QuestPanel>();
        qp.Build(rt);
        return qp;
    }

    // Sol gezinmenin altinda, tam genislikte gorev sekmesi.
    void Build(RectTransform r)
    {
        toggle = UIFactory.CreateButton(r, "QuestHeader", "", 22f, out toggleLabel);
        var trt = (RectTransform)toggle.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        float questX = HudLayout.LeftInset;
        trt.anchoredPosition = new Vector2(questX, -HudLayout.QuestHeaderY);
        trt.sizeDelta = new Vector2(HudLayout.QuestHeaderW, HeaderH);
        toggleImg = toggle.GetComponent<Image>();
        toggleLabel.alignment = TextAlignmentOptions.MidlineLeft;
        var lrt = toggleLabel.rectTransform;
        lrt.offsetMin = new Vector2(18f, 0f); lrt.offsetMax = new Vector2(-52f, 0f);
        toggleLabel.enableAutoSizing = true;
        toggleLabel.fontSizeMin = 18f; toggleLabel.fontSizeMax = 22f;
        plusLabel = UIFactory.CreateText(trt, "Plus", "+", 26f, TextAlignmentOptions.Center, Color.white,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-38f, 0f), new Vector2(-10f, 0f), true);
        toggle.onClick.AddListener(Toggle);
        skinned = UIArt.StyleButton(toggle, toggleLabel);
        if (skinned) plusLabel.color = UIArt.ButtonText;

        popup = UIFactory.CreatePanel(r, "QuestPopup", new Color(0.12f, 0.1f, 0.12f, 0.97f), true);
        popup.anchorMin = popup.anchorMax = new Vector2(0f, 1f);
        popup.pivot = new Vector2(0f, 1f);
        popup.anchoredPosition = new Vector2(questX, -HudLayout.QuestHeaderY - HeaderH - 8f);
        popup.sizeDelta = new Vector2(HudLayout.LeftW, Pad * 2f + Slots * RowH + (Slots - 1) * RowGap);

        for (int i = 0; i < Slots; i++)
        {
            var row = new Row();
            row.rt = UIFactory.CreatePanel(popup, "Quest" + i, new Color(1, 1, 1, 0.08f), false);
            row.bg = row.rt.GetComponent<Image>();
            row.rt.anchorMin = new Vector2(0f, 1f); row.rt.anchorMax = new Vector2(1f, 1f);
            row.rt.pivot = new Vector2(0.5f, 1f);
            row.rt.anchoredPosition = new Vector2(0f, -(Pad + i * (RowH + RowGap)));
            row.rt.sizeDelta = new Vector2(-2f * Pad, RowH);
            row.text = UIFactory.CreateText(row.rt, "Text", "", 20, TextAlignmentOptions.TopLeft, Color.white,
                new Vector2(0.03f, 0.42f), new Vector2(0.74f, 0.98f), Vector2.zero, Vector2.zero, false, true);
            row.prog = UIFactory.CreateText(row.rt, "Prog", "", 16, TextAlignmentOptions.BottomLeft, HudLayout.TextDim,
                new Vector2(0.03f, 0.02f), new Vector2(0.74f, 0.42f), Vector2.zero, Vector2.zero, false, true);
            TextMeshProUGUI lbl;
            row.btn = UIFactory.CreateButton(row.rt, "Claim", Loc.T("AL"), 22, out lbl);
            var brt = (RectTransform)row.btn.transform;
            brt.anchorMin = new Vector2(0.77f, 0.2f); brt.anchorMax = new Vector2(0.97f, 0.8f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            row.btn.GetComponent<Image>().color = HudLayout.Accent;
            lbl.color = HudLayout.AccentInk;
            var captured = row;   // satirin o anki gorevini AL tiklayinca oku
            row.btn.onClick.AddListener(() => { if (captured.q != null) GameManager.Instance.ClaimQuest(captured.q); });
            rows.Add(row);
        }
        popup.gameObject.SetActive(false);

        HudLayout.LeftPanelOpened += OnOtherPanelOpened;
        Loc.OnLanguageChanged += Refresh;
        var gm = GameManager.Instance;
        gm.OnAgeChanged += Refresh; gm.OnStateChanged += Refresh;
        Refresh();
    }

    void Toggle()
    {
        expanded = !expanded;
        popup.gameObject.SetActive(expanded);
        if (expanded) { popup.SetAsLastSibling(); HudLayout.RaiseLeftPanelOpened(this); }
        Refresh();
    }

    void OnOtherPanelOpened(object sender)
    {
        if (sender == (object)this || !expanded) return;
        expanded = false;
        popup.gameObject.SetActive(false);
        Refresh();
    }

    void OnDestroy()
    {
        Loc.OnLanguageChanged -= Refresh;
        HudLayout.LeftPanelOpened -= OnOtherPanelOpened;
        var gm = GameManager.Instance;
        if (gm != null) { gm.OnAgeChanged -= Refresh; gm.OnStateChanged -= Refresh; }
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        var age = gm.CurrentAge;
        if (age == null) return;
        var quests = age.quests ?? new QuestDef[0];

        // Tamamlanmis (odulu alinmis) gorevler dusur, siradaki 4 bekleyen gorev gosterilir.
        var pending = new List<QuestDef>();
        int claimable = 0, claimedCount = 0;
        foreach (var q in quests)
        {
            if (gm.IsQuestClaimed(q)) { claimedCount++; continue; }
            pending.Add(q);
            if (gm.QuestProgress(q) >= q.amount) claimable++;
        }
        // Odulu hazir olanlar one gelsin
        pending.Sort((a, b) =>
        {
            bool da = gm.QuestProgress(a) >= a.amount, db = gm.QuestProgress(b) >= b.amount;
            return da == db ? 0 : (da ? -1 : 1);
        });

        string name = Loc.T(age.side == Side.Hell ? "İŞ EMİRLERİ" : "İYİLİK LİSTESİ");
        bool ready = claimable > 0;
        toggleLabel.text = name + "  " + claimedCount + "/" + quests.Length + (ready ? "  (" + claimable + ")" : "");
        toggleLabel.fontStyle = FontStyles.Bold;
        plusLabel.text = expanded ? "−" : "+";
        if (skinned)
        {
            // Cerceve sprite'i varsa: hazirken altin tonu, yazi hep krem
            toggleLabel.color = UIArt.ButtonText;
            plusLabel.color = UIArt.ButtonText;
            toggleImg.color = ready ? new Color(1f, 0.8f, 0.35f) : Color.white;
        }
        else
        {
            toggleLabel.color = ready ? HudLayout.AccentInk : Color.white;
            plusLabel.color = ready ? HudLayout.AccentInk : Color.white;
            toggleImg.color = ready ? ReadyColor : IdleColor;
        }
        toggle.gameObject.SetActive(quests.Length > 0);
        if (quests.Length == 0 && popup != null) popup.gameObject.SetActive(false);

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            if (i >= pending.Count) { r.q = null; r.rt.gameObject.SetActive(false); continue; }
            var q = pending[i];
            r.q = q;
            r.rt.gameObject.SetActive(true);
            double p = gm.QuestProgress(q);
            bool done = p >= q.amount;
            r.text.text = Loc.T(q.text);
            r.prog.text = NumberFormatter.Format(Math.Min(p, q.amount)) + " / " + NumberFormatter.Format(q.amount) + "  •  " + RewardText(gm, q);
            r.btn.gameObject.SetActive(done);
            r.bg.color = done ? new Color(0.96f, 0.73f, 0.28f, 0.28f) : new Color(1, 1, 1, 0.08f);
        }
    }

    static string RewardText(GameManager gm, QuestDef q)
    {
        var parts = new List<string>();
        var res = gm.CurrentAge.clickResource;
        if (q.rewardFlat > 0) parts.Add("+" + NumberFormatter.Format(q.rewardFlat) + " " + Loc.T(res.displayName));
        if (q.rewardSeconds > 0) parts.Add("+" + NumberFormatter.Format(q.rewardSeconds) + Loc.T("sn") + " " + Loc.T("üretim"));
        if (q.rewardKnowledge > 0) parts.Add("+" + NumberFormatter.Format(q.rewardKnowledge) + " " + Loc.T("Bilgi"));
        return string.Join(" ", parts.ToArray());
    }
}
