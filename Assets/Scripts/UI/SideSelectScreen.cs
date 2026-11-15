using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Ana giris ekrani (mockup: "ARAF"): ustte oyun adi, solda CEHENNEM, sagda CENNET, altta ortada Ayarlar.
// Fare imleci gizlenir, yerine HAYALET (oyuncu karakteri) gecer:
//   ortada (kopru)      : sade hayalet
//   cennet tarafinda    : kanatlar + hale = melek
//   cehennem tarafinda  : boynuzlar + kizil ten = zebani
// Tiklaninca hayaletin oldugu taraf secilir.
//
// Grafikci dosyalari OPSIYONEL: dosya yoksa kodla cizilen placeholder kullanilir.
// Dosyalar Assets/Resources/Art/UI/ altina atilir (yazim birebir, kucuk harf):
//   menu_bg.png            tam ekran arka plan (cehennem | kopru | cennet)
//   Curtain_Frame.png      perde cercevesi (tam ekran, ortasi seffaf)
//   menu_title.png         ustteki tabela (ARAF yazisi kodla uzerine yazilir)
//   menu_plaque_hell.png   sol alttaki CEHENNEM levhasi
//   menu_plaque_heaven.png sag alttaki CENNET levhasi
//   ghost.png              sade hayalet
//   ghost_angel.png        melek hayalet (kanat + hale)
//   ghost_demon.png        zebani hayalet (boynuz)
// Icons/Settings_Button_S.png varsa ayarlar butonunun gorseli olarak kullanilir.
public class SideSelectScreen : MonoBehaviour
{
    public Action<Side> onChosen;

    const string GameTitle = "ARAF";
    const float DeadZone = 0.05f;          // ekranin ortasinda bu kadar genislik "kopru" sayilir

    class Mote
    {
        public RectTransform rt;
        public Image img;
        public float x, y, speed, sway, phase, size;
        public Color color;
    }

    class Plaque
    {
        public RectTransform rt;
        public Image border;
        public TextMeshProUGUI label;
        public Color accent;
        public float scale = 1f, scaleTarget = 1f;
        public bool hasArt;
    }

    class Ghost
    {
        public RectTransform root, wingL, wingR, halo, hornL, hornR;
        public readonly List<Image> bodyParts = new List<Image>();
        public readonly List<Image> eyes = new List<Image>();
        public Image art;
        public Sprite sprNeutral, sprAngel, sprDemon;
        public Vector2 pos = new Vector2(0f, -120f);
        public float angel, demon, tilt, pop;
        public bool useArt;
    }

    RectTransform root, hellRt, heavenRt;
    Image bg, hellImg, heavenImg;
    Plaque hellPlaque, heavenPlaque;
    Ghost ghost;
    CanvasGroup group;
    Canvas ownCanvas;
    SettingsPanel settings;
    readonly List<Mote> motes = new List<Mote>();
    AudioSource audioSrc;
    AudioClip hellClip, heavenClip;
    float introT, findTimer;
    int zone;                               // 0 kopru, 1 cehennem, 2 cennet
    bool chosen, hasBgArt, settingsMode, cursorHidden;

    static readonly Color Neutral = new Color(0.06f, 0.045f, 0.055f);
    static readonly Color HellDim = new Color(0.3f, 0.04f, 0.04f), HellHot = new Color(0.72f, 0.1f, 0.05f);
    static readonly Color HeavenDim = new Color(0.11f, 0.19f, 0.36f), HeavenHot = new Color(0.28f, 0.52f, 0.86f);
    static readonly Color Gold = new Color(0.88f, 0.68f, 0.31f);
    static readonly Color GoldDim = new Color(0.48f, 0.35f, 0.17f);
    static readonly Color Ink = new Color(0.09f, 0.055f, 0.045f, 0.97f);
    static readonly Color Parchment = new Color(0.95f, 0.87f, 0.7f);
    static readonly Color PlaqueText = new Color(0.45f, 0.1f, 0.06f);
    static readonly Color GhostWhite = new Color(0.97f, 0.95f, 0.9f);
    static readonly Color GhostDemon = new Color(0.62f, 0.13f, 0.1f);
    static readonly Color GhostAngel = new Color(0.9f, 0.96f, 1f);

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

        // Kendi Canvas'i: HUD butonlarinin (Basarimlar, Bilgi Agaci, Ayarlar) ustunde kalsin.
        // Ayarlar penceresi (SettingsPanel) daha yuksek sortingOrder ile bunun da ustunde acilir.
        ownCanvas = gameObject.AddComponent<Canvas>();
        ownCanvas.overrideSorting = true;
        ownCanvas.sortingOrder = 100;
        gameObject.AddComponent<GraphicRaycaster>();

        var bgSprite = Resources.Load<Sprite>("Art/UI/menu_bg");
        hasBgArt = bgSprite != null;

        bg = Panel(r, "Bg", hasBgArt ? Color.white : Neutral, true);
        if (hasBgArt) { bg.sprite = bgSprite; bg.preserveAspect = false; }
        Stretch(bg.rectTransform);

        // Sol/sag yarilar: hover rengi + tiklama alani (bolme sabit, ortada kopru).
        hellRt = Panel(r, "HellHalf", hasBgArt ? new Color(HellHot.r, HellHot.g, HellHot.b, 0f) : HellDim, true).rectTransform;
        heavenRt = Panel(r, "HeavenHalf", hasBgArt ? new Color(HeavenHot.r, HeavenHot.g, HeavenHot.b, 0f) : HeavenDim, true).rectTransform;
        hellImg = hellRt.GetComponent<Image>();
        heavenImg = heavenRt.GetComponent<Image>();
        hellRt.anchorMin = Vector2.zero; hellRt.anchorMax = new Vector2(0.5f, 1f);
        heavenRt.anchorMin = new Vector2(0.5f, 0f); heavenRt.anchorMax = Vector2.one;
        hellRt.offsetMin = hellRt.offsetMax = heavenRt.offsetMin = heavenRt.offsetMax = Vector2.zero;

        if (!hasBgArt)
        {
            AddFade(hellRt, new Color(0f, 0f, 0f, 0.55f));
            AddFade(heavenRt, new Color(0f, 0f, 0f, 0.5f));
        }
        var vig = Panel(r, "Vignette", new Color(0f, 0f, 0f, hasBgArt ? 0.35f : 0.8f), false);
        vig.sprite = UIFactory.VignetteSprite();
        vig.type = Image.Type.Simple;
        vig.preserveAspect = false;
        Stretch(vig.rectTransform);

        BuildMotes(hellRt, 18, new Color(1f, 0.45f, 0.12f), new Color(1f, 0.7f, 0.2f));
        BuildMotes(heavenRt, 18, new Color(1f, 0.95f, 0.7f), new Color(0.65f, 0.85f, 1f));

        var frameSprite = Resources.Load<Sprite>("Art/UI/Curtain_Frame");
        if (frameSprite != null)
        {
            var frame = Panel(r, "CurtainFrame", Color.white, false);
            frame.sprite = frameSprite;
            frame.preserveAspect = false;
            Stretch(frame.rectTransform);
        }

        BuildTitle(r);
        hellPlaque = BuildPlaque(r, "HellPlaque", "CEHENNEM", 0.25f, new Color(1f, 0.5f, 0.15f), "Art/UI/menu_plaque_hell", Side.Hell);
        heavenPlaque = BuildPlaque(r, "HeavenPlaque", "CENNET", 0.75f, new Color(0.55f, 0.78f, 1f), "Art/UI/menu_plaque_heaven", Side.Heaven);
        BuildSettingsButton(r);
        ghost = BuildGhost(r);

        hellRt.gameObject.AddComponent<HoverRelay>().onClick = () => ChooseByZone();
        heavenRt.gameObject.AddComponent<HoverRelay>().onClick = () => ChooseByZone();

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
        hellClip = Resources.Load<AudioClip>("Audio/hell");       // Assets/Resources/Audio/hell.mp3
        heavenClip = Resources.Load<AudioClip>("Audio/heaven");   // Assets/Resources/Audio/heaven.mp3

        SetCursorHidden(true);
    }

    void OnDestroy() { SetCursorHidden(false); }
    void OnDisable() { SetCursorHidden(false); }
    void OnEnable() { if (root != null && !settingsMode) SetCursorHidden(true); }

    void SetCursorHidden(bool hide)
    {
        if (cursorHidden == hide) return;
        cursorHidden = hide;
        Cursor.visible = !hide;
    }

    // ---- Ust baslik ----
    void BuildTitle(RectTransform r)
    {
        var titleSprite = Resources.Load<Sprite>("Art/UI/menu_title");
        RectTransform brt;
        RectTransform textParent;
        if (titleSprite != null)
        {
            var art = Panel(r, "TitleBanner", Color.white, false);
            art.sprite = titleSprite;
            art.preserveAspect = true;
            brt = art.rectTransform;
            textParent = brt;
        }
        else
        {
            var banner = UIFactory.CreateRoundedPanel(r, "TitleBanner", GoldDim, false);
            brt = banner.rectTransform;
            var inner = UIFactory.CreateRoundedPanel(brt, "BannerInner", Ink, false);
            Stretch(inner.rectTransform, 6f);
            textParent = inner.rectTransform;
        }
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -20f);
        brt.sizeDelta = new Vector2(620f, 170f);

        var title = UIFactory.CreateText(textParent, "Title", GameTitle, 92,
            TextAlignmentOptions.Center, new Color(0.98f, 0.84f, 0.51f),
            Vector2.zero, Vector2.one, new Vector2(20f, 10f), new Vector2(-20f, -10f), true);
        title.characterSpacing = 10f;
        title.enableAutoSizing = true;
        title.fontSizeMin = 40f;
        title.fontSizeMax = 92f;
    }

    // ---- Alt levhalar (CEHENNEM / CENNET) ----
    Plaque BuildPlaque(RectTransform r, string name, string label, float anchorX, Color accent, string spritePath, Side side)
    {
        var p = new Plaque { accent = accent };
        var art = Resources.Load<Sprite>(spritePath);
        p.hasArt = art != null;

        var btn = UIFactory.CreateButton(r, name, "", 54f, out p.label);
        var img = btn.GetComponent<Image>();
        p.rt = (RectTransform)btn.transform;
        p.rt.anchorMin = p.rt.anchorMax = new Vector2(anchorX, 0f);
        p.rt.pivot = new Vector2(0.5f, 0f);
        p.rt.anchoredPosition = new Vector2(0f, 70f);
        p.rt.sizeDelta = new Vector2(480f, 130f);
        btn.transition = Selectable.Transition.None;

        if (p.hasArt)
        {
            img.sprite = art;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
        {
            UIFactory.MakeRounded(img);
            img.color = new Color(accent.r, accent.g, accent.b, 0.9f);
            p.border = img;
            var inner = UIFactory.CreateRoundedPanel(p.rt, "Inner", Parchment, false);
            Stretch(inner.rectTransform, 6f);
            p.label.transform.SetParent(inner.rectTransform, false);
        }

        p.label.text = Loc.T(label);
        p.label.color = PlaqueText;
        p.label.fontSize = 52f;
        p.label.characterSpacing = 3f;
        p.label.enableAutoSizing = true;
        p.label.fontSizeMin = 28f;
        p.label.fontSizeMax = 52f;
        var lrt = p.label.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(30f, 8f); lrt.offsetMax = new Vector2(-30f, -8f);

        btn.onClick.AddListener(() => Choose(side));
        return p;
    }

    // ---- Alt orta: Ayarlar ----
    void BuildSettingsButton(RectTransform r)
    {
        TextMeshProUGUI label;
        var btn = UIFactory.CreateButton(r, "MenuSettingsButton", Loc.T("Ayarlar"), 26f, out label);
        var img = btn.GetComponent<Image>();
        var rt = (RectTransform)btn.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 22f);

        var art = Resources.Load<Sprite>("Art/Icons/Settings_Button_S");
        if (art != null)
        {
            img.sprite = art;
            img.preserveAspect = true;
            img.color = Color.white;
            float h = 96f;
            float aspect = art.rect.height > 0f ? art.rect.width / art.rect.height : 1f;
            rt.sizeDelta = new Vector2(h * aspect, h);
            label.text = "";
        }
        else
        {
            UIFactory.MakeRounded(img);
            img.color = new Color(0.14f, 0.1f, 0.08f, 0.92f);
            rt.sizeDelta = new Vector2(260f, 60f);
            label.color = new Color(0.96f, 0.88f, 0.7f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 26f;
        }
        btn.onClick.AddListener(() =>
        {
            ResolveSettings(true);
            if (settings != null) settings.Open();
        });
    }

    // ---- Hayalet imleci ----
    Ghost BuildGhost(RectTransform r)
    {
        var g = new Ghost();
        var holder = new GameObject("Ghost", typeof(RectTransform));
        g.root = (RectTransform)holder.transform;
        g.root.SetParent(r, false);
        g.root.anchorMin = g.root.anchorMax = new Vector2(0.5f, 0.5f);
        g.root.pivot = new Vector2(0.5f, 0.5f);
        g.root.sizeDelta = new Vector2(120f, 150f);
        g.root.anchoredPosition = g.pos;

        g.sprNeutral = Resources.Load<Sprite>("Art/UI/ghost");
        g.sprAngel = Resources.Load<Sprite>("Art/UI/ghost_angel");
        g.sprDemon = Resources.Load<Sprite>("Art/UI/ghost_demon");
        g.useArt = g.sprNeutral != null || g.sprAngel != null || g.sprDemon != null;

        if (g.useArt)
        {
            g.art = Panel(g.root, "GhostArt", Color.white, false);
            g.art.preserveAspect = true;
            g.art.sprite = g.sprNeutral != null ? g.sprNeutral : (g.sprAngel != null ? g.sprAngel : g.sprDemon);
            g.art.rectTransform.anchorMin = Vector2.zero; g.art.rectTransform.anchorMax = Vector2.one;
            g.art.rectTransform.offsetMin = g.art.rectTransform.offsetMax = Vector2.zero;
            g.root.sizeDelta = new Vector2(150f, 190f);
            return g;
        }

        // --- Placeholder: kodla cizilen hayalet ---
        // Kanatlar (govdenin arkasinda)
        g.wingL = Part(g.root, "WingL", new Color(0.95f, 0.97f, 1f), new Vector2(-30f, 18f), new Vector2(90f, 46f), new Vector2(1f, 0.3f), 20f);
        g.wingR = Part(g.root, "WingR", new Color(0.95f, 0.97f, 1f), new Vector2(30f, 18f), new Vector2(90f, 46f), new Vector2(0f, 0.3f), -20f);
        // Boynuzlar
        g.hornL = Part(g.root, "HornL", new Color(0.35f, 0.04f, 0.04f), new Vector2(-24f, 48f), new Vector2(14f, 36f), new Vector2(0.5f, 0f), 22f);
        g.hornR = Part(g.root, "HornR", new Color(0.35f, 0.04f, 0.04f), new Vector2(24f, 48f), new Vector2(14f, 36f), new Vector2(0.5f, 0f), -22f);
        // Hale
        g.halo = Part(g.root, "Halo", new Color(1f, 0.88f, 0.42f), new Vector2(0f, 76f), new Vector2(64f, 12f), new Vector2(0.5f, 0.5f), 0f);

        // Govde
        var body = Part(g.root, "Body", GhostWhite, new Vector2(0f, -2f), new Vector2(96f, 118f), new Vector2(0.5f, 0.5f), 0f);
        g.bodyParts.Add(body.GetComponent<Image>());
        // Etek (dalgali alt kenar)
        for (int i = -1; i <= 1; i++)
        {
            var s = Part(g.root, "Hem" + i, GhostWhite, new Vector2(i * 32f, -62f), new Vector2(32f, 28f), new Vector2(0.5f, 0.5f), 0f);
            g.bodyParts.Add(s.GetComponent<Image>());
        }
        // Gozler ve agiz
        for (int e = -1; e <= 1; e += 2)
        {
            var eye = Part(g.root, "Eye", new Color(0.12f, 0.1f, 0.15f), new Vector2(e * 18f, 14f), new Vector2(13f, 19f), new Vector2(0.5f, 0.5f), 0f);
            g.eyes.Add(eye.GetComponent<Image>());
        }
        var mouth = Part(g.root, "Mouth", new Color(0.12f, 0.1f, 0.15f), new Vector2(0f, -14f), new Vector2(15f, 19f), new Vector2(0.5f, 0.5f), 0f);
        g.eyes.Add(mouth.GetComponent<Image>());

        g.wingL.localScale = g.wingR.localScale = g.halo.localScale = g.hornL.localScale = g.hornR.localScale = Vector3.zero;
        return g;
    }

    static RectTransform Part(RectTransform parent, string name, Color color, Vector2 pos, Vector2 size, Vector2 pivot, float rotZ)
    {
        var img = UIFactory.CreateRoundedPanel(parent, name, color, false);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        return rt;
    }

    // ---- Etkilesim ----
    int ComputeZone(Vector2 screenPos)
    {
        if (Screen.width <= 0) return 0;
        if (screenPos.x < 0f || screenPos.y < 0f || screenPos.x > Screen.width || screenPos.y > Screen.height) return 0;
        float x = screenPos.x / Screen.width;
        if (x < 0.5f - DeadZone) return 1;
        if (x > 0.5f + DeadZone) return 2;
        return 0;
    }

    void SetZone(int z)
    {
        zone = z;
        hellPlaque.scaleTarget = z == 1 ? 1.06f : (z == 0 ? 1f : 0.97f);
        heavenPlaque.scaleTarget = z == 2 ? 1.06f : (z == 0 ? 1f : 0.97f);
        ghost.pop = 1f;

        audioSrc.Stop();   // onceki taraf sesi kesilir
        var clip = z == 1 ? hellClip : z == 2 ? heavenClip : null;
        if (clip != null) { audioSrc.clip = clip; audioSrc.Play(); }
    }

    void ChooseByZone()
    {
        if (zone == 1) Choose(Side.Hell);
        else if (zone == 2) Choose(Side.Heaven);
    }

    void Choose(Side s)
    {
        if (chosen) return;
        chosen = true;
        SoundManager.Play(SoundId.LevelUp, 1f, 0.9f);
        audioSrc.Stop();
        SetCursorHidden(false);
        if (onChosen != null) onChosen(s);
    }

    void ResolveSettings(bool force = false)
    {
        if (settings != null) return;
        if (!force && findTimer > 0f) return;
        findTimer = 0.25f;
        settings = FindFirstObjectByType<SettingsPanel>();
    }

    // Ayarlar penceresi acikken: hayalet gizlenir, gercek imlec geri gelir.
    void SetSettingsMode(bool open)
    {
        if (settingsMode == open) return;
        settingsMode = open;
        ghost.root.gameObject.SetActive(!open);
        SetCursorHidden(!open);
        if (open) { zone = 0; audioSrc.Stop(); }
    }

    // ---- Animasyon ----
    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        introT = Mathf.Min(1f, introT + dt * 2.4f);
        group.alpha = Mathf.SmoothStep(0f, 1f, introT);

        findTimer -= dt;
        ResolveSettings();
        SetSettingsMode(settings != null && settings.IsOpen);

        var pointer = Pointer.current;
        Vector2 screenPos = pointer != null ? pointer.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.3f);

        int z = settingsMode ? 0 : ComputeZone(screenPos);
        if (z != zone) SetZone(z);

        AnimateBackground(dt);
        AnimatePlaque(hellPlaque, zone == 1, dt);
        AnimatePlaque(heavenPlaque, zone == 2, dt);
        AnimateGhost(screenPos, dt);
        AnimateMotes(dt);
    }

    void AnimateBackground(float dt)
    {
        float k = 1f - Mathf.Exp(-8f * dt);
        if (hasBgArt)
        {
            // Resim ustte kalir, hover'da taraf rengi hafifce parlar
            var hc = hellImg.color; hc.a = Mathf.Lerp(hc.a, zone == 1 ? 0.2f : 0f, k); hellImg.color = hc;
            var vc = heavenImg.color; vc.a = Mathf.Lerp(vc.a, zone == 2 ? 0.2f : 0f, k); heavenImg.color = vc;
        }
        else
        {
            Color target = zone == 1 ? HellHot : zone == 2 ? HeavenHot : Neutral;
            bg.color = Color.Lerp(bg.color, target * 0.55f, 1f - Mathf.Exp(-6f * dt));
            hellImg.color = Color.Lerp(hellImg.color, zone == 1 ? HellHot : HellDim, k);
            heavenImg.color = Color.Lerp(heavenImg.color, zone == 2 ? HeavenHot : HeavenDim, k);
        }
    }

    void AnimatePlaque(Plaque p, bool active, float dt)
    {
        p.scale = Mathf.Lerp(p.scale, p.scaleTarget, 1f - Mathf.Exp(-10f * dt));
        p.rt.localScale = new Vector3(p.scale, p.scale, 1f);
        if (p.border != null)
        {
            float blink = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 2.6f);
            float a = active ? blink : 0.65f;
            p.border.color = new Color(p.accent.r, p.accent.g, p.accent.b, a);
        }
    }

    void AnimateGhost(Vector2 screenPos, float dt)
    {
        var g = ghost;
        if (g.root == null || settingsMode) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out local);
        Vector2 prev = g.pos;
        g.pos = Vector2.Lerp(g.pos, local, 1f - Mathf.Exp(-20f * dt));   // hafif suzulme gecikmesi
        float vx = dt > 0f ? (g.pos.x - prev.x) / dt : 0f;
        g.tilt = Mathf.Lerp(g.tilt, Mathf.Clamp(-vx * 0.02f, -16f, 16f), 1f - Mathf.Exp(-10f * dt));
        float bob = Mathf.Sin(Time.unscaledTime * 2.4f) * 6f;

        g.root.anchoredPosition = g.pos + new Vector2(0f, bob);
        g.root.localRotation = Quaternion.Euler(0f, 0f, g.tilt);
        g.pop = Mathf.MoveTowards(g.pop, 0f, dt * 3.5f);
        float popScale = 1f + 0.14f * g.pop;
        g.root.localScale = new Vector3(popScale, popScale, 1f);

        g.angel = Mathf.MoveTowards(g.angel, zone == 2 ? 1f : 0f, dt * 4f);
        g.demon = Mathf.MoveTowards(g.demon, zone == 1 ? 1f : 0f, dt * 4f);

        if (g.useArt)
        {
            Sprite s = zone == 2 && g.sprAngel != null ? g.sprAngel
                     : zone == 1 && g.sprDemon != null ? g.sprDemon
                     : g.sprNeutral != null ? g.sprNeutral : g.art.sprite;
            if (g.art.sprite != s) g.art.sprite = s;
            return;
        }

        float angelK = Ease(g.angel), demonK = Ease(g.demon);
        float flap = Mathf.Sin(Time.unscaledTime * 8f) * 10f * g.angel;
        g.wingL.localScale = new Vector3(angelK, angelK, 1f);
        g.wingR.localScale = new Vector3(angelK, angelK, 1f);
        g.wingL.localRotation = Quaternion.Euler(0f, 0f, 20f + flap);
        g.wingR.localRotation = Quaternion.Euler(0f, 0f, -20f - flap);
        g.halo.localScale = new Vector3(angelK, angelK, 1f);
        g.halo.anchoredPosition = new Vector2(0f, 76f + Mathf.Sin(Time.unscaledTime * 3.2f) * 3f);
        g.hornL.localScale = new Vector3(demonK, demonK, 1f);
        g.hornR.localScale = new Vector3(demonK, demonK, 1f);

        Color body = Color.Lerp(Color.Lerp(GhostWhite, GhostDemon, g.demon), GhostAngel, g.angel);
        for (int i = 0; i < g.bodyParts.Count; i++) g.bodyParts[i].color = body;
        Color eye = Color.Lerp(new Color(0.12f, 0.1f, 0.15f), new Color(1f, 0.7f, 0.2f), g.demon);
        for (int i = 0; i < g.eyes.Count; i++) g.eyes[i].color = eye;
    }

    // Kucuk "geri tepmeli" (overshoot) belirme egrisi
    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
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

    static void Stretch(RectTransform r, float padding = 0f)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(padding, padding);
        r.offsetMax = new Vector2(-padding, -padding);
    }
}
