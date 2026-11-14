using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Giris ekrani: solda CEHENNEM, sagda CENNET. Fare hangi tarafa gelirse arka plan o renge doner,
// secili butonun ustunde iblis / melek cetesi belirir. Gorseller simdilik placeholder (renkli sekiller).
public class SideSelectScreen : MonoBehaviour
{
    public Action<Side> onChosen;

    RectTransform root, hellRt, heavenRt;
    Image bg, hellImg, heavenImg;
    Gang hellGang, heavenGang;
    AudioSource audioSrc;
    AudioClip hellClip, heavenClip;
    float split = 0.5f, splitTarget = 0.5f;   // sol tarafin genisligi (0..1)
    int hover;                                 // 0 yok, 1 cehennem, 2 cennet
    bool chosen;

    static readonly Color Neutral = new Color(0.1f, 0.1f, 0.12f);
    static readonly Color HellDim = new Color(0.28f, 0.05f, 0.05f), HellHot = new Color(0.85f, 0.1f, 0.05f);
    static readonly Color HeavenDim = new Color(0.2f, 0.3f, 0.5f), HeavenHot = new Color(0.35f, 0.6f, 1f);

    class Gang
    {
        public RectTransform[] figures;
        public float visible, target;
    }

    public static SideSelectScreen Create(Canvas canvas)
    {
        var go = new GameObject("SideSelectScreen", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var s = go.AddComponent<SideSelectScreen>();
        s.Build(rt);
        return s;
    }

    void Build(RectTransform r)
    {
        root = r;
        bg = UIFactory.CreatePanel(r, "Bg", Neutral, false).GetComponent<Image>();
        Stretch(bg.rectTransform);

        hellRt = UIFactory.CreatePanel(r, "HellHalf", HellDim, true);
        heavenRt = UIFactory.CreatePanel(r, "HeavenHalf", HeavenDim, true);
        hellImg = hellRt.GetComponent<Image>(); heavenImg = heavenRt.GetComponent<Image>();

        hellGang = BuildSide(hellRt, "CEHENNEM", "Ofis kıyafetiyle gelmeyin, yanar.", new Color(0.12f, 0.02f, 0.02f), new Color(1f, 0.15f, 0.1f), true);
        heavenGang = BuildSide(heavenRt, "CENNET", "Wi-Fi şifresi: sevgi.", new Color(0.95f, 0.97f, 1f), new Color(1f, 0.9f, 0.3f), false);

        hellRt.gameObject.AddComponent<HoverRelay>().onEnter = () => SetHover(1);
        hellRt.GetComponent<HoverRelay>().onExit = () => SetHover(0);
        hellRt.GetComponent<HoverRelay>().onClick = () => Choose(Side.Hell);
        heavenRt.gameObject.AddComponent<HoverRelay>().onEnter = () => SetHover(2);
        heavenRt.GetComponent<HoverRelay>().onExit = () => SetHover(0);
        heavenRt.GetComponent<HoverRelay>().onClick = () => Choose(Side.Heaven);

        UIFactory.CreateText(r, "Title", "TARAFINI SEÇ", 64, TextAlignmentOptions.Center, Color.white,
            new Vector2(0, 0.88f), new Vector2(1, 1f), Vector2.zero, Vector2.zero, true);

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        hellClip = Resources.Load<AudioClip>("Audio/hell");       // Assets/Resources/Audio/hell.mp3
        heavenClip = Resources.Load<AudioClip>("Audio/heaven");   // Assets/Resources/Audio/heaven.mp3
        audioSrc.loop = false;
        ApplyLayout(true);
    }

    Gang BuildSide(RectTransform half, string label, string sub, Color body, Color accent, bool demon)
    {
        var btn = UIFactory.CreateButton(half, "Btn", label, 56, out var _);
        var brt = (RectTransform)btn.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.3f);
        brt.sizeDelta = new Vector2(420, 130);
        btn.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        btn.interactable = false;   // tiklama yarimin kendisinde; buton sadece gorsel
        UIFactory.CreateText(half, "Sub", sub, 26, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.75f),
            new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.24f), Vector2.zero, Vector2.zero);

        var g = new Gang { figures = new RectTransform[6] };
        for (int i = 0; i < g.figures.Length; i++)
        {
            var f = UIFactory.CreatePanel(half, "Fig" + i, body, false);
            f.anchorMin = f.anchorMax = new Vector2(0.5f, 0.3f);
            f.sizeDelta = new Vector2(70, 110);
            f.pivot = new Vector2(0.5f, 0f);
            var head = UIFactory.CreatePanel(f, "Head", body, false);
            head.anchorMin = head.anchorMax = new Vector2(0.5f, 1f); head.sizeDelta = new Vector2(50, 50); head.anchoredPosition = new Vector2(0, 25);
            head.GetComponent<Image>().color = Color.Lerp(body, Color.white, demon ? 0.1f : 0f);
            if (demon)   // boynuzlar
                for (int h = -1; h <= 1; h += 2)
                {
                    var horn = UIFactory.CreatePanel(head, "Horn", accent, false);
                    horn.sizeDelta = new Vector2(10, 28); horn.anchoredPosition = new Vector2(h * 18, 34); horn.localRotation = Quaternion.Euler(0, 0, -h * 20);
                }
            else         // hale
            {
                var halo = UIFactory.CreatePanel(head, "Halo", accent, false);
                halo.sizeDelta = new Vector2(44, 9); halo.anchoredPosition = new Vector2(0, 36);
            }
            f.localScale = Vector3.zero;
            g.figures[i] = f;
        }
        return g;
    }

    void SetHover(int h)
    {
        hover = h;
        hellGang.target = h == 1 ? 1 : 0;
        heavenGang.target = h == 2 ? 1 : 0;
        splitTarget = h == 1 ? 0.6f : h == 2 ? 0.4f : 0.5f;
        audioSrc.Stop();   // onceki taraf sesi kesilir
        var clip = h == 1 ? hellClip : h == 2 ? heavenClip : null;
        if (clip != null) { audioSrc.clip = clip; audioSrc.Play(); }
    }

    void Choose(Side s) { if (chosen) return; chosen = true; if (onChosen != null) onChosen(s); }

    void Update()
    {
        split = Mathf.Lerp(split, splitTarget, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        ApplyLayout(false);

        Color target = hover == 1 ? HellHot : hover == 2 ? HeavenHot : Neutral;
        bg.color = Color.Lerp(bg.color, target * 0.6f, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
        hellImg.color = Color.Lerp(hellImg.color, hover == 1 ? HellHot : HellDim, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        heavenImg.color = Color.Lerp(heavenImg.color, hover == 2 ? HeavenHot : HeavenDim, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

        AnimateGang(hellGang, 1f);
        AnimateGang(heavenGang, 1.7f);
    }

    void ApplyLayout(bool snap)
    {
        hellRt.anchorMin = new Vector2(0, 0); hellRt.anchorMax = new Vector2(split, 1);
        heavenRt.anchorMin = new Vector2(split, 0); heavenRt.anchorMax = new Vector2(1, 1);
        hellRt.offsetMin = hellRt.offsetMax = heavenRt.offsetMin = heavenRt.offsetMax = Vector2.zero;
    }

    void AnimateGang(Gang g, float bobSpeed)
    {
        g.visible = Mathf.MoveTowards(g.visible, g.target, Time.unscaledDeltaTime * 3f);
        for (int i = 0; i < g.figures.Length; i++)
        {
            float k = Mathf.Clamp01(g.visible * 1.8f - i * 0.12f);   // sirayla belirir
            float s = k * k * (3f - 2f * k);
            g.figures[i].localScale = new Vector3(s, s, 1f);
            // cete: yarim daire, one dogru kisa kisa ziplar
            float x = (i - 2.5f) * 85f;
            float y = 150f + (i % 2 == 0 ? 0f : -22f) + Mathf.Abs(Mathf.Sin(Time.unscaledTime * bobSpeed * 2f + i)) * 14f;
            g.figures[i].anchoredPosition = new Vector2(x, y);
        }
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
}
