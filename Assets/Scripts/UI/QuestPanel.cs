using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gorevler: ortada kucuk bir GOREVLER butonu. Tiklayinca bekleyen ilk 4 gorev acilir.
// Odulu alinacak gorev varsa buton altin renkte yanar ve sayiyi gosterir.
// Gorevler AgeData.quests'ten gelir, icerik content.json'da yazilir.
public class QuestPanel : MonoBehaviour
{
    class Row { public QuestDef q; public RectTransform rt; public TextMeshProUGUI text, prog; public Button btn; public Image bg; }

    const int Slots = 4;
    RectTransform popup;
    Button toggle;
    TextMeshProUGUI toggleLabel, header;
    Image toggleImg;
    readonly List<Row> rows = new List<Row>();

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

    void Build(RectTransform r)
    {
        toggle = UIFactory.CreateButton(r, "QuestToggle", "", 20, out toggleLabel);
        var trt = (RectTransform)toggle.transform;
        trt.anchorMin = new Vector2(0.31f, 0.735f); trt.anchorMax = new Vector2(0.47f, 0.795f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        toggleImg = toggle.GetComponent<Image>();
        toggle.onClick.AddListener(() => { popup.gameObject.SetActive(!popup.gameObject.activeSelf); Refresh(); });

        popup = UIFactory.CreatePanel(r, "QuestPopup", new Color(0.05f, 0.05f, 0.08f, 0.92f), true);
        popup.anchorMin = new Vector2(0.30f, 0.30f); popup.anchorMax = new Vector2(0.60f, 0.73f);
        popup.offsetMin = popup.offsetMax = Vector2.zero;
        header = UIFactory.CreateText(popup, "Header", "", 22, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.5f),
            new Vector2(0, 0.88f), new Vector2(1, 1), Vector2.zero, Vector2.zero, true, true);

        float rowH = 0.86f / Slots;
        for (int i = 0; i < Slots; i++)
        {
            var row = new Row();
            row.rt = UIFactory.CreatePanel(popup, "Quest" + i, new Color(1, 1, 1, 0.08f), false);
            row.bg = row.rt.GetComponent<Image>();
            row.rt.anchorMin = new Vector2(0.02f, 0.86f - (i + 1) * rowH + 0.008f);
            row.rt.anchorMax = new Vector2(0.98f, 0.86f - i * rowH - 0.008f);
            row.rt.offsetMin = row.rt.offsetMax = Vector2.zero;
            row.text = UIFactory.CreateText(row.rt, "Text", "", 17, TextAlignmentOptions.TopLeft, Color.white,
                new Vector2(0.03f, 0.40f), new Vector2(0.76f, 0.98f), Vector2.zero, Vector2.zero, false, true);
            row.prog = UIFactory.CreateText(row.rt, "Prog", "", 14, TextAlignmentOptions.BottomLeft, new Color(1, 1, 1, 0.75f),
                new Vector2(0.03f, 0.02f), new Vector2(0.76f, 0.42f), Vector2.zero, Vector2.zero, false, true);
            TextMeshProUGUI lbl;
            row.btn = UIFactory.CreateButton(row.rt, "Claim", Loc.T("AL"), 18, out lbl);
            var brt = (RectTransform)row.btn.transform;
            brt.anchorMin = new Vector2(0.79f, 0.2f); brt.anchorMax = new Vector2(0.97f, 0.8f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            row.btn.GetComponent<Image>().color = new Color(0.9f, 0.7f, 0.1f);
            var captured = row;   // satirin o anki gorevini AL tiklayinca oku
            row.btn.onClick.AddListener(() => { if (captured.q != null) GameManager.Instance.ClaimQuest(captured.q); });
            rows.Add(row);
        }
        popup.gameObject.SetActive(false);

        var gm = GameManager.Instance;
        gm.OnAgeChanged += Refresh; gm.OnStateChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
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
        toggleLabel.text = name + (claimable > 0 ? "  (" + claimable + ")" : "");
        toggleImg.color = claimable > 0 ? new Color(0.9f, 0.7f, 0.1f, 0.95f) : new Color(0.2f, 0.2f, 0.25f, 0.9f);
        toggle.gameObject.SetActive(quests.Length > 0);
        header.text = name + "  " + claimedCount + "/" + quests.Length;

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
            r.bg.color = done ? new Color(1f, 0.85f, 0.2f, 0.25f) : new Color(1, 1, 1, 0.08f);
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
