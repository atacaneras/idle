using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Kat hedefi cubugu. Cehennemde: boss cani (azalir). Cennette: huzur / iyilik cubugu (dolar).
// Ilerleme = bu turda kazanilan kaynak / kat atlama maliyeti, yani uretim ve tiklamalar bosa hasar verir.
// Ayrica tiklama butonunun yazisini kata gore degistirir (CEZALANDIR / IYILIK YAP).
public class FloorBar : MonoBehaviour
{
    Image fill;
    RectTransform fillRt;
    TextMeshProUGUI title, info;
    GameObject gatherGo;

    public static FloorBar Create(Canvas canvas)
    {
        var rt = UIFactory.CreatePanel(canvas.transform, "FloorBar", new Color(0f, 0f, 0f, 0f), false);   // kutu yok: sadece baslik + cubuk
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(HudLayout.CenterOffsetX, -HudLayout.CenterTop);   // kat adi tabelasinin hemen altinda
        rt.sizeDelta = new Vector2(HudLayout.CenterWidth - 20f, HudLayout.FloorBarH);
        var bar = rt.gameObject.AddComponent<FloorBar>();
        bar.Build(rt);
        return bar;
    }

    void Build(RectTransform r)
    {
        // Baslik: krem yazi, koyu kontur (parsomen/magara arka planinda okunur)
        title = UIFactory.CreateText(r, "Title", "", 28, TextAlignmentOptions.Center, new Color(1f, 0.93f, 0.78f),
            new Vector2(0, 0.52f), new Vector2(1, 1), Vector2.zero, Vector2.zero, true, true);
        title.outlineWidth = 0.28f;
        title.outlineColor = new Color32(40, 8, 4, 255);

        // Cubuk: koyu iz + altin cerceve + kirmizi dolgu + ustte hafif parlama
        var back = UIFactory.CreatePanel(r, "Back", new Color(0.09f, 0.02f, 0.02f, 0.96f), false);
        back.anchorMin = new Vector2(0.03f, 0.08f); back.anchorMax = new Vector2(0.97f, 0.48f);
        back.offsetMin = back.offsetMax = Vector2.zero;
        var frame = back.gameObject.AddComponent<Outline>();
        frame.effectColor = new Color(0.86f, 0.62f, 0.22f, 1f);
        frame.effectDistance = new Vector2(2.5f, -2.5f);
        var f = UIFactory.CreatePanel(back, "Fill", Color.red, false);
        f.anchorMin = Vector2.zero; f.anchorMax = Vector2.one; f.offsetMin = f.offsetMax = Vector2.zero;
        fill = f.GetComponent<Image>();
        fillRt = f;   // doldurma: sprite olmadigi icin Image.Filled calismaz, genislik anchor ile ayarlanir
        var shine = UIFactory.CreatePanel(f, "Shine", new Color(1f, 1f, 1f, 0.16f), false);
        shine.anchorMin = new Vector2(0f, 0.55f); shine.anchorMax = new Vector2(1f, 1f);
        shine.offsetMin = shine.offsetMax = Vector2.zero;
        info = UIFactory.CreateText(back, "Info", "", 20, TextAlignmentOptions.Center, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true, true);
        info.outlineWidth = 0.25f;
        info.outlineColor = new Color32(30, 5, 3, 255);

        var gm = GameManager.Instance;
        gm.OnStateChanged += Refresh; gm.OnAgeChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null) { gm.OnStateChanged -= Refresh; gm.OnAgeChanged -= Refresh; }
    }

    void SetFill(float amount)
    {
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
    }

    // %10'un altinda bir ondalik: yavas baslayan ilerleme "%0" gorunup moral bozmasin
    static string Percent(double v) { return v < 10 ? v.ToString("0.0") : Mathf.FloorToInt((float)v).ToString(); }

    void Refresh()
    {
        var gm = GameManager.Instance;
        var age = gm.CurrentAge;
        if (age == null || age.advanceResource == null) return;

        bool hell = age.side == Side.Hell;
        double cost = gm.AdvanceCost;
        double prog = cost > 0 ? Math.Min(1.0, gm.GetRunEarned(age.advanceResource) / cost) : 0;

        title.text = Loc.T(string.IsNullOrEmpty(age.goalTitle) ? age.displayName : age.goalTitle);
        if (hell)
        {
            fill.color = new Color(0.9f, 0.1f, 0.08f);
            SetFill((float)(1.0 - prog));   // boss cani azaliyor
            info.text = prog >= 1.0 ? Loc.T("Boss devrildi!") : Loc.T("CAN") + "  %" + Percent((1.0 - prog) * 100);
        }
        else
        {
            fill.color = new Color(1f, 0.85f, 0.3f);
            SetFill((float)prog);           // huzur doluyor
            info.text = prog >= 1.0 ? Loc.T("Huzur tamam!") : Loc.T("HUZUR") + "  %" + Percent(prog * 100);
        }

        // Tiklama butonu yazisi (HUD bunu sadece bir kez "TOPLA" yapiyor)
        if (gatherGo == null) gatherGo = GameObject.Find("GatherButton");
        if (gatherGo != null && !string.IsNullOrEmpty(age.clickLabel))
        {
            var t = gatherGo.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = Loc.T(age.clickLabel);
        }
    }
}
