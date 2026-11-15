using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ana giris ekrani: solda CEHENNEM, sagda CENNET.
// Referans mockup'in ahsap/deri cerceve dilini kodla taklit eder:
// altin bordurlu baslik panosu, koyu kartlar, kor parcaciklari ve hover animasyonu.
// Gorsel dosya gerektirmez; tum cerceveler ve isiklar UIFactory'de kodla uretilir.
public class SideSelectScreen : MonoBehaviour
{
    public Action<Side> onChosen;

    class Gang
    {
        public RectTransform[] figures;
        public float visible, target;
    }

    class SideCard
    {
        public RectTransform rt;
        public Image border, inner;
        public TextMeshProUGUI title, sub, ctaLabel;
        public Button cta;
        public Image ctaImage;
        public Gang gang;
        public float scale = 1f, scaleTarget = 1f;
        public Color dark, accent;
    }

    class Mote
    {
        public RectTransform rt;
        public Image img;
        public float x, y, speed, sway, phase, size;
        public Color color;
    }

    RectTransform root, hellRt, heavenRt;
    Image bg, hellImg, heavenImg;
    SideCard hellCard, heavenCard;
    CanvasGroup group;
    TextMeshProUGUI soundLabel;
    readonly List<Mote> motes = new List<Mote>();
    AudioSource audioSrc;
    AudioClip hellClip, heavenClip;
    float split = 0.5f, splitTarget = 0.5f;
    float introT;
    int hover;                         // 0 yok, 1 cehennem, 2 cennet
    bool chosen;
    bool? lastMuted;

    static readonly Color Neutral = new Color(0.06f, 0.045f, 0.055f);
    static readonly Color HellDim = new Color(0.3f, 0.04f, 0.04f), HellHot = new Color(0.72f, 0.1f, 0.05f);
    static readonly Color HeavenDim = new Color(0.11f, 0.19f, 0.36f), HeavenHot = new Color(0.28f, 0.52f, 0.86f);
    static readonly Color Gold = new Color(0.88f, 0.68f, 0.31f);
    static readonly Color GoldDim = new Color(0.48f, 0.35f, 0.17f);
    static readonly Color Ink = new Color(0.09f, 0.055f, 0.045f, 0.97f);
    static readonly Color MutedText = new Color(0.78f, 0.72f, 0.64f);

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
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        bg = Panel(r, "Bg", Neutral, true);
        Stretch(bg.rectTransform);

        hellRt = Panel(r, "HellHalf", HellDim, true).rectTransform;
        heavenRt = Panel(r, "HeavenHalf", HeavenDim, true).rectTransform;
        hellImg = hellRt.GetComponent<Image>();
        heavenImg = heavenRt.GetComponent<Image>();

        // Alt kenara dogru kararan gecis + tum ekrani saran vinyet
        AddFade(hellRt, new Color(0f, 0f, 0f, 0.55f));
        AddFade(heavenRt, new Color(0f, 0f, 0f, 0.5f));
        var vig = Panel(r, "Vignette", new Color(0f, 0f, 0f, 0.8f), false);
        vig.sprite = UIFactory.VignetteSprite();
        vig.type = Image.Type.Simple;
        vig.preserveAspect = false;
        Stretch(vig.rectTransform);

        BuildMotes(hellRt, 22, new Color(1f, 0.45f, 0.12f), new Color(1f, 0.7f, 0.2f));
        BuildMotes(heavenRt, 22, new Color(1f, 0.95f, 0.7f), new Color(0.65f, 0.85f, 1f));

        BuildHeader(r);

        hellCard = BuildSide(hellRt, Side.Hell, "CEHENNEM", "Ofis kıyafetiyle gelmeyin, yanar.",
            "CEHENNEME İN", new Color(0.12f, 0.025f, 0.02f), HellHot, true);
        heavenCard = BuildSide(heavenRt, Side.Heaven, "CENNET", "Wi-Fi şifresi: sevgi.",
            "CENNETE ÇIK", new Color(0.04f, 0.09f, 0.2f), new Color(0.5f, 0.75f, 1f), false);

        BuildFooter(r);

        hellRt.gameObject.AddComponent<HoverRelay>().onEnter = () => SetHover(1);
        hellRt.GetComponent<HoverRelay>().onExit = () => SetHover(0);
        hellRt.GetComponent<HoverRelay>().onClick = () => Choose(Side.Hell);
        heavenRt.gameObject.AddComponent<HoverRelay>().onEnter = () => SetHover(2);
        heavenRt.GetComponent<HoverRelay>().onExit = () => SetHover(0);
        heavenRt.GetComponent<HoverRelay>().onClick = () => Choose(Side.Heaven);

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        hellClip = Resources.Load<AudioClip>("Audio/hell");       // Assets/Resources/Audio/hell.mp3
        heavenClip = Resources.Load<AudioClip>("Audio/heaven");   // Assets/Resources/Audio/heaven.mp3
        audioSrc.loop = false;

        SoundManager.MuteChanged += OnMuteChanged;
        ApplyLayout();
        RefreshSoundLabel();

        // Gamepad / klavye: ilk secili oge cehennem butonu olsun.
        var es = EventSystem.current;
        if (es != null && hellCard.cta != null) es.SetSelectedGameObject(hellCard.cta.gameObject);
    }

    void OnDestroy() { SoundManager.MuteChanged -= OnMuteChanged; }

    // ---- Baslik ----
    void BuildHeader(RectTransform r)
    {
        var banner = UIFactory.CreateRoundedPanel(r, "TitleBanner", GoldDim, false);
        var brt = banner.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -14f);
        brt.sizeDelta = new Vector2(980f, 168f);

        var inner = UIFactory.CreateRoundedPanel(brt, "BannerInner", Ink, false);
        Stretch(inner.rectTransform, 6f);

        var tl = UIFactory.CreatePanel(brt, "TopLine", Gold, false);
        tl.anchorMin = new Vector2(0f, 1f); tl.anchorMax = new Vector2(1f, 1f);
        tl.pivot = new Vector2(0.5f, 1f);
        tl.offsetMin = new Vector2(44f, -7f); tl.offsetMax = new Vector2(-44f, -3f);

        var title = UIFactory.CreateText(inner.rectTransform, "Title", "EVRİM", 88,
            TextAlignmentOptions.Center, new Color(0.98f, 0.84f, 0.51f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -8f), new Vector2(420f, 78f), true);
        title.characterSpacing = 8f;

        var sub = UIFactory.CreateText(inner.rectTransform, "Sub", Loc.T("CENNET & CEHENNEM"), 23,
            TextAlignmentOptions.Center, new Color(0.86f, 0.77f, 0.62f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, -68f), new Vector2(400f, -26f), false);
        sub.characterSpacing = 5f;

        Diamond(inner.rectTransform, new Vector2(-350f, 34f), 17f, Gold);
        Diamond(inner.rectTransform, new Vector2(350f, 34f), 17f, Gold);

        var choose = UIFactory.CreateText(r, "Choose", Loc.T("TARAFINI SEÇ"), 31,
            TextAlignmentOptions.Center, new Color(0.96f, 0.87f, 0.7f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-360f, -242f), new Vector2(360f, -198f), true);
        choose.characterSpacing = 4f;

        GoldLine(r, new Vector2(-330f, -220f), 210f, new Vector2(0.5f, 1f));
        GoldLine(r, new Vector2(330f, -220f), 210f, new Vector2(0.5f, 1f));
    }

    // ---- Taraf karti ----
    SideCard BuildSide(RectTransform half, Side side, string label, string tagline, string ctaText,
        Color dark, Color accent, bool demon)
    {
        var card = new SideCard { dark = dark, accent = accent };

        var border = UIFactory.CreateRoundedPanel(half, "CardBorder", new Color(accent.r, accent.g, accent.b, 0.45f), false);
        card.border = border;
        card.rt = border.rectTransform;
        card.rt.anchorMin = card.rt.anchorMax = new Vector2(0.5f, 0.44f);
        card.rt.pivot = new Vector2(0.5f, 0.5f);
        card.rt.sizeDelta = new Vector2(560f, 500f);

        var inner = UIFactory.CreateRoundedPanel(card.rt, "CardInner", dark, false);
        card.inner = inner;
        Stretch(inner.rectTransform, 5f);

        var emblem = Panel(card.rt, "Emblem", new Color(accent.r, accent.g, accent.b, 0.26f), false);
        emblem.sprite = UIFactory.RadialSprite();
        emblem.type = Image.Type.Simple;
        var ert = emblem.rectTransform;
        ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0.5f);
        ert.anchoredPosition = new Vector2(0f, 128f);
        ert.sizeDelta = new Vector2(360f, 270f);

        var figRoot = new GameObject("Figures", typeof(RectTransform));
        var frt = (RectTransform)figRoot.transform;
        frt.SetParent(card.rt, false);
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = new Vector2(0f, 128f);
        frt.sizeDelta = new Vector2(420f, 170f);
        card.gang = BuildGang(frt, dark, accent, demon);

        var topLine = Panel(card.rt, "AccentLine", new Color(accent.r, accent.g, accent.b, 0.8f), false);
        var trt = topLine.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 1f); trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -18f);
        trt.sizeDelta = new Vector2(280f, 3f);

        card.title = UIFactory.CreateText(card.rt, "Name", Loc.T(label), 52,
            TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -12f), new Vector2(260f, 62f), true);
        card.title.characterSpacing = 3f;

        card.sub = UIFactory.CreateText(card.rt, "Tagline", Loc.T(tagline), 23,
            TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.72f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, -62f), new Vector2(250f, -18f));

        var btn = UIFactory.CreateButton(card.rt, "ChooseButton", Loc.T(ctaText), 31, out var ctaLabel);
        card.cta = btn;
        card.ctaLabel = ctaLabel;
        card.ctaImage = btn.GetComponent<Image>();
        UIFactory.MakeRounded(card.ctaImage);
        var brt = (RectTransform)btn.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, -168f);
        brt.sizeDelta = new Vector2(340f, 66f);
        ctaLabel.color = new Color(0.18f, 0.11f, 0.05f);
        ctaLabel.enableAutoSizing = true;
        ctaLabel.fontSizeMin = 22f;
        ctaLabel.fontSizeMax = 31f;
        btn.onClick.AddListener(() => Choose(side));

        SetCardScale(card, 1f);
        RefreshCardColors(card, false);
        return card;
    }

    Gang BuildGang(RectTransform parent, Color dark, Color accent, bool demon)
    {
        var g = new Gang { figures = new RectTransform[5] };
        Color body = demon ? new Color(0.24f, 0.06f, 0.05f) : new Color(0.93f, 0.96f, 1f);
        Color bodyLight = Color.Lerp(body, Color.white, demon ? 0.12f : 0.04f);

        for (int i = 0; i < g.figures.Length; i++)
        {
            var f = UIFactory.CreateRoundedPanel(parent, "Fig" + i, body, false);
            var frt = f.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.sizeDelta = new Vector2(64f, 100f);
            frt.localScale = Vector3.zero;
            g.figures[i] = frt;

            var head = UIFactory.CreateRoundedPanel(frt, "Head", bodyLight, false);
            var hrt = head.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.sizeDelta = new Vector2(46f, 46f);
            hrt.anchoredPosition = new Vector2(0f, -2f);

            // Gozler
            for (int e = -1; e <= 1; e += 2)
            {
                var ert = UIFactory.CreatePanel(hrt, "Eye", demon ? new Color(1f, 0.62f, 0.2f) : new Color(0.25f, 0.32f, 0.48f), false);
                ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0.5f);
                ert.anchoredPosition = new Vector2(e * 10f, 2f);
                ert.sizeDelta = new Vector2(6f, 6f);
            }

            if (demon)
            {
                // Boynuzlar
                for (int h = -1; h <= 1; h += 2)
                {
                    var hort = UIFactory.CreatePanel(hrt, "Horn", accent, false);
                    hort.anchorMin = hort.anchorMax = new Vector2(0.5f, 1f);
                    hort.pivot = new Vector2(0.5f, 0f);
                    hort.anchoredPosition = new Vector2(h * 16f, -2f);
                    hort.sizeDelta = new Vector2(9f, 26f);
                    hort.localRotation = Quaternion.Euler(0f, 0f, -h * 18f);
                }
                // Kuyruk / penceler
                for (int c = -1; c <= 1; c += 2)
                {
                    var crt = UIFactory.CreatePanel(frt, "Claw", accent, false);
                    crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
                    crt.anchoredPosition = new Vector2(c * 24f, 24f);
                    crt.sizeDelta = new Vector2(8f, 30f);
                    crt.localRotation = Quaternion.Euler(0f, 0f, c * 24f);
                }
            }
            else
            {
                // Halo
                var halo = UIFactory.CreateRoundedPanel(hrt, "Halo", new Color(1f, 0.88f, 0.42f), false);
                var hart = (RectTransform)halo.transform;
                hart.anchorMin = hart.anchorMax = new Vector2(0.5f, 1f);
                hart.anchoredPosition = new Vector2(0f, 10f);
                hart.sizeDelta = new Vector2(48f, 8f);
            }
        }
        return g;
    }

    // ---- Alt bilgi ----
    void BuildFooter(RectTransform r)
    {
        UIFactory.CreateText(r, "Version", Loc.F("Sürüm {0}", Application.version), 18,
            TextAlignmentOptions.MidlineLeft, MutedText, Vector2.zero, Vector2.zero,
            new Vector2(32f, 18f), new Vector2(420f, 58f));

        UIFactory.CreateText(r, "Hint", Loc.T("Esc: Ayarlar"), 18,
            TextAlignmentOptions.Center, MutedText, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-220f, 18f), new Vector2(220f, 58f));

        TextMeshProUGUI label;
        var btn = UIFactory.CreateButton(r, "MenuSoundButton", "", 22f, out label);
        soundLabel = label;
        var img = btn.GetComponent<Image>();
        UIFactory.MakeRounded(img);
        img.color = new Color(0.14f, 0.1f, 0.08f, 0.9f);
        var rt = (RectTransform)btn.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-32f, 22f);
        rt.sizeDelta = new Vector2(250f, 54f);
        label.fontSize = 21f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 17f;
        label.fontSizeMax = 21f;
        btn.onClick.AddListener(SoundManager.ToggleMuted);

        GoldLine(r, new Vector2(0f, 86f), 420f, new Vector2(0.5f, 0f));
    }

    // ---- Etkilesim ----
    void SetHover(int h)
    {
        hover = h;
        hellCard.gang.target = h == 1 ? 1f : 0f;
        heavenCard.gang.target = h == 2 ? 1f : 0f;
        hellCard.scaleTarget = h == 1 ? 1.035f : (h == 0 ? 1f : 0.985f);
        heavenCard.scaleTarget = h == 2 ? 1.035f : (h == 0 ? 1f : 0.985f);
        splitTarget = h == 1 ? 0.59f : h == 2 ? 0.41f : 0.5f;

        audioSrc.Stop();   // onceki taraf sesi kesilir
        var clip = h == 1 ? hellClip : h == 2 ? heavenClip : null;
        if (clip != null) { audioSrc.clip = clip; audioSrc.Play(); }
    }

    void Choose(Side s)
    {
        if (chosen) return;
        chosen = true;
        SoundManager.Play(SoundId.LevelUp, 1f, 0.9f);
        audioSrc.Stop();
        if (onChosen != null) onChosen(s);
    }

    void OnMuteChanged() { RefreshSoundLabel(true); }

    void RefreshSoundLabel(bool force = false)
    {
        if (soundLabel == null) return;
        bool muted = SoundManager.IsMuted;
        if (!force && lastMuted.HasValue && lastMuted.Value == muted) return;
        lastMuted = muted;
        soundLabel.text = muted ? Loc.T("Ses: Kapalı") : Loc.T("Ses: Açık");
    }

    // ---- Animasyon ----
    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        introT = Mathf.Min(1f, introT + dt * 2.4f);
        group.alpha = Mathf.SmoothStep(0f, 1f, introT);

        split = Mathf.Lerp(split, splitTarget, 1f - Mathf.Exp(-8f * dt));
        ApplyLayout();

        Color target = hover == 1 ? HellHot : hover == 2 ? HeavenHot : Neutral;
        bg.color = Color.Lerp(bg.color, target * 0.55f, 1f - Mathf.Exp(-6f * dt));
        hellImg.color = Color.Lerp(hellImg.color, hover == 1 ? HellHot : HellDim, 1f - Mathf.Exp(-8f * dt));
        heavenImg.color = Color.Lerp(heavenImg.color, hover == 2 ? HeavenHot : HeavenDim, 1f - Mathf.Exp(-8f * dt));

        AnimateCard(hellCard, hover == 1, dt);
        AnimateCard(heavenCard, hover == 2, dt);
        AnimateGang(hellCard.gang, 1.1f, dt);
        AnimateGang(heavenCard.gang, 1.7f, dt);
        AnimateMotes(dt);
        RefreshSoundLabel();
    }

    void AnimateCard(SideCard card, bool active, float dt)
    {
        card.scale = Mathf.Lerp(card.scale, card.scaleTarget, 1f - Mathf.Exp(-10f * dt));
        SetCardScale(card, card.scale);
        RefreshCardColors(card, active);
    }

    void SetCardScale(SideCard card, float s)
    {
        if (card != null && card.rt != null) card.rt.localScale = new Vector3(s, s, 1f);
    }

    void RefreshCardColors(SideCard card, bool active)
    {
        if (card == null) return;
        float k = active ? 1f : 0f;
        float blink = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 2.2f);
        card.border.color = Color.Lerp(new Color(card.accent.r, card.accent.g, card.accent.b, 0.42f),
            new Color(card.accent.r, card.accent.g, card.accent.b, 0.95f * blink), k);
        card.inner.color = Color.Lerp(card.dark, Color.Lerp(card.dark, Color.white, 0.08f), k);
        if (card.ctaImage != null)
            card.ctaImage.color = Color.Lerp(new Color(0.55f, 0.42f, 0.24f), new Color(0.96f, 0.75f, 0.3f), k);
        if (card.ctaLabel != null)
            card.ctaLabel.color = k > 0.5f ? new Color(0.18f, 0.11f, 0.05f) : new Color(0.94f, 0.9f, 0.8f);
    }

    void AnimateGang(Gang g, float bobSpeed, float dt)
    {
        g.visible = Mathf.MoveTowards(g.visible, g.target, dt * 3f);
        for (int i = 0; i < g.figures.Length; i++)
        {
            float k = Mathf.Clamp01(g.visible * 1.8f - i * 0.12f);   // sirayla belirir
            float s = k * k * (3f - 2f * k);
            g.figures[i].localScale = new Vector3(s, s, 1f);
            float x = (i - (g.figures.Length - 1) * 0.5f) * 66f;
            float y = Mathf.Abs(Mathf.Sin(Time.unscaledTime * bobSpeed * 2f + i)) * 10f;
            g.figures[i].anchoredPosition = new Vector2(x, y);
        }
    }

    void AnimateMotes(float dt)
    {
        for (int i = 0; i < motes.Count; i++)
        {
            var m = motes[i];
            m.y += m.speed * dt;
            if (m.y > 1.05f)
            {
                m.y = -0.05f;
                m.x = UnityEngine.Random.Range(0.06f, 0.94f);
            }
            float x = m.x + Mathf.Sin(Time.unscaledTime * m.sway + m.phase) * 0.015f;
            m.rt.anchorMin = m.rt.anchorMax = new Vector2(x, m.y);
            m.rt.anchoredPosition = Vector2.zero;
            float fade = Mathf.Sin(Mathf.Clamp01(m.y) * Mathf.PI);
            var c = m.color;
            c.a = 0.06f + 0.3f * fade;
            m.img.color = c;
        }
    }

    // ---- Yardimcilar ----
    void ApplyLayout()
    {
        hellRt.anchorMin = new Vector2(0f, 0f); hellRt.anchorMax = new Vector2(split, 1f);
        heavenRt.anchorMin = new Vector2(split, 0f); heavenRt.anchorMax = new Vector2(1f, 1f);
        hellRt.offsetMin = hellRt.offsetMax = heavenRt.offsetMin = heavenRt.offsetMax = Vector2.zero;
    }

    void AddFade(RectTransform half, Color color)
    {
        var fade = Panel(half, "Fade", color, false);
        fade.sprite = UIFactory.VerticalFadeSprite();
        fade.type = Image.Type.Simple;
        var rt = fade.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0.55f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void BuildMotes(RectTransform half, int count, Color a, Color b)
    {
        for (int i = 0; i < count; i++)
        {
            var img = Panel(half, "Mote" + i, Color.white, false);
            img.sprite = UIFactory.RadialSprite();
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            float size = UnityEngine.Random.Range(4f, 11f);
            rt.anchorMin = rt.anchorMax = new Vector2(UnityEngine.Random.Range(0.05f, 0.95f), UnityEngine.Random.Range(0f, 1f));
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);

            motes.Add(new Mote
            {
                rt = rt,
                img = img,
                x = rt.anchorMin.x,
                y = rt.anchorMin.y,
                speed = UnityEngine.Random.Range(0.025f, 0.085f),
                sway = UnityEngine.Random.Range(0.5f, 1.6f),
                phase = UnityEngine.Random.Range(0f, 6.28f),
                size = size,
                color = Color.Lerp(a, b, UnityEngine.Random.value)
            });
        }
    }

    static Image Panel(Transform parent, string name, Color color, bool blocksRaycast)
    {
        return UIFactory.CreatePanel(parent, name, color, blocksRaycast).GetComponent<Image>();
    }

    static void Diamond(Transform parent, Vector2 position, float size, Color color)
    {
        var img = UIFactory.CreateRoundedPanel(parent, "Diamond", color, false);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(size, size);
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    static void GoldLine(Transform parent, Vector2 position, float width, Vector2 anchor)
    {
        var rt = UIFactory.CreatePanel(parent, "GoldLine", new Color(Gold.r, Gold.g, Gold.b, 0.7f), false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, 2f);
    }

    static void Stretch(RectTransform r, float padding = 0f)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(padding, padding);
        r.offsetMax = new Vector2(-padding, -padding);
    }
}
