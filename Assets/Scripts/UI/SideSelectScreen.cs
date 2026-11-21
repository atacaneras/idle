using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Ana giriş ekranı: üstte oyun adı, iki yanda taraf seçimi ve altta ayarlar.
// Fare imleci gizlenir, yerine HAYALET (oyuncu karakteri) gecer:
//   ortada (kopru)      : sade hayalet
//   cennet tarafinda    : kanatlar + hale = melek
//   cehennem tarafinda  : boynuzlar + kizil ten = zebani
// Tiklaninca hayaletin oldugu taraf secilir.
public class SideSelectScreen : MonoBehaviour
{
    public Action<Side> onChosen;

    const string GameTitle = "ARAF";
    const float DeadZone = 0.05f;          // ekranin ortasinda bu kadar genislik "kopru" sayilir

    [System.Serializable]
    class Mote
    {
        public RectTransform rt;
        public Image img;
        public float x, y, speed, sway, phase, size;
        public int side;              // 1 cehennem, 2 cennet
        public Color color;
    }

    [System.Serializable]
    class SideCard
    {
        public int side;
        public RectTransform root, pill;
        public Image glow, pillImg;
        public TextMeshProUGUI label, motto, action;
        public Vector2 basePos;
        public float hover, phase;
        public Color accent, accentBright, accentDim;
    }

    [System.Serializable]
    class Ghost
    {
        public RectTransform root, wingL, wingR, halo, hornL, hornR;
        public readonly List<Image> bodyParts = new List<Image>();
        public readonly List<Image> eyes = new List<Image>();
        public Image art, glow;
        public Sprite sprNeutral, sprAngel, sprDemon;
        public SkeletonGraphic spineGraphic;
        public SkeletonAnimation spineAnimation;
        public Vector2 pos = new Vector2(0f, -120f);
        public float angel, demon, tilt, pop;
        public bool useArt;
    }

    [SerializeField] RectTransform root, hellRt, heavenRt, bgRt, titleBlock;
    Vector2 bgBasePos, bgBaseScale, titleBasePos;
    [SerializeField] Image bg, hellImg, heavenImg, flash;
    [SerializeField] SkeletonGraphic spineBg;
    [SerializeField] SkeletonAnimation spineAnimation;
    Coroutine doorTransition;
    int activeDoorSide;
    float ghostFacingYaw, ghostFacingYawVelocity;
    [SerializeField] Ghost ghost;
    GameObject settingsOpenButton, achievementsOpenButton;
    [SerializeField] CanvasGroup group;
    [SerializeField] Canvas ownCanvas;
    [SerializeField] SettingsPanel settings;
    readonly List<Mote> motes = new List<Mote>();
    [SerializeField] SideCard hellCard, heavenCard;
    [SerializeField] TextMeshProUGUI hintText;
    [SerializeField] AudioSource audioSrc;
    AudioClip hellClip, heavenClip;
    float introT, findTimer, hellTint, heavenTint;
    int zone;                               // 0 kopru, 1 cehennem, 2 cennet
    bool chosen, closing, hasBgArt, settingsMode, cursorHidden;

    static readonly Color Neutral = new Color(0.06f, 0.045f, 0.055f);
    static readonly Color HellDim = new Color(0.3f, 0.04f, 0.04f), HellHot = new Color(0.72f, 0.1f, 0.05f);
    static readonly Color HeavenDim = new Color(0.11f, 0.19f, 0.36f), HeavenHot = new Color(0.28f, 0.52f, 0.86f);
    static readonly Color Gold = new Color(0.88f, 0.68f, 0.31f);
    static readonly Color GoldBright = new Color(1f, 0.85f, 0.49f);
    static readonly Color GoldDim = new Color(0.48f, 0.35f, 0.17f);
    static readonly Color Ink = new Color(0.09f, 0.055f, 0.045f, 0.97f);
    static readonly Color GhostWhite = new Color(0.97f, 0.95f, 0.9f);
    static readonly Color GhostDemon = new Color(0.62f, 0.13f, 0.1f);
    static readonly Color GhostAngel = new Color(0.9f, 0.96f, 1f);
    static readonly Color HellAccent = new Color(1f, 0.42f, 0.18f);
    static readonly Color HellAccentBright = new Color(1f, 0.66f, 0.34f);
    static readonly Color HellAccentDim = new Color(0.62f, 0.25f, 0.15f);
    static readonly Color HeavenAccent = new Color(0.55f, 0.78f, 1f);
    static readonly Color HeavenAccentBright = new Color(0.78f, 0.9f, 1f);
    static readonly Color HeavenAccentDim = new Color(0.31f, 0.47f, 0.68f);

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
        group.ignoreParentGroups = true;

        ownCanvas = gameObject.AddComponent<Canvas>();
        ownCanvas.overrideSorting = true;
        ownCanvas.sortingOrder = 100;
        gameObject.AddComponent<GraphicRaycaster>();

        // 1. Oncelikle Spine arka planini yuklemeyi dene
        var spineData = Resources.Load<SkeletonDataAsset>("Art/main_menu/MainMenu_Bg_Anim/main_menu_bg_SkeletonData");
        var bgSprite = spineData == null ? Resources.Load<Sprite>("Art/UI/menu_bg") : null;
        hasBgArt = spineData != null || bgSprite != null;

        bg = Panel(r, "Bg", hasBgArt ? Color.clear : Neutral, true);
        if (bgSprite != null)
        {
            bg.color = Color.white;
            bg.sprite = bgSprite;
            bg.preserveAspect = false;
        }
        Stretch(bg.rectTransform);

        if (spineData != null)
        {
            BuildSpineBackground(r, spineData);
        }

        // Sol/sag yarilar: hover rengi + tiklama alani
        hellRt = Panel(r, "HellHalf", hasBgArt ? new Color(HellHot.r, HellHot.g, HellHot.b, 0f) : HellDim, true).rectTransform;
        heavenRt = Panel(r, "HeavenHalf", hasBgArt ? new Color(HeavenHot.r, HeavenHot.g, HeavenHot.b, 0f) : HeavenDim, true).rectTransform;
        hellImg = hellRt.GetComponent<Image>();
        heavenImg = heavenRt.GetComponent<Image>();
        hellRt.anchorMin = Vector2.zero; hellRt.anchorMax = new Vector2(0.5f, 1f);
        heavenRt.anchorMin = new Vector2(0.5f, 0f); heavenRt.anchorMax = Vector2.one;
        hellRt.offsetMin = hellRt.offsetMax = heavenRt.offsetMin = heavenRt.offsetMax = Vector2.zero;

        // Alt siyah fade barlari kaldirildi; arka plan ekrani tam kaplasin.
        var vig = Panel(r, "Vignette", new Color(0f, 0f, 0f, hasBgArt ? 0f : 0.8f), false);
        vig.sprite = UIFactory.VignetteSprite();
        vig.type = Image.Type.Simple;
        vig.preserveAspect = false;
        Stretch(vig.rectTransform);

        BuildMotes(hellRt, 18, new Color(1f, 0.45f, 0.12f), new Color(1f, 0.7f, 0.2f), 1);
        BuildMotes(heavenRt, 18, new Color(1f, 0.95f, 0.7f), new Color(0.65f, 0.85f, 1f), 2);

        BuildTitle(r);
        // Yan kartlar (CEHENNEM/CENNET kutulari) kaldirildi.
        BuildHint(r);
        BuildSettingsButton(r);

        // Ana menu acikken oyun ici Ayarlar/Basarimlar butonlari cakismasin.
        settingsOpenButton = GameObject.Find("OpenSettingsButton");
        if (settingsOpenButton != null) settingsOpenButton.SetActive(false);
        achievementsOpenButton = GameObject.Find("OpenAchievementsButton");
        if (achievementsOpenButton != null) achievementsOpenButton.SetActive(false);

        ghost = BuildGhost(r);

        // Secim yapilinca kisa bir renk parlamasi (en ustte, tiklamayi engellemez).
        flash = Panel(r, "ChooseFlash", new Color(1f, 1f, 1f, 0f), false);
        Stretch(flash.rectTransform);

        hellRt.gameObject.AddComponent<HoverRelay>().onClick = () => ChooseByZone();
        heavenRt.gameObject.AddComponent<HoverRelay>().onClick = () => ChooseByZone();

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
        hellClip = Resources.Load<AudioClip>("Audio/hell");
        heavenClip = Resources.Load<AudioClip>("Audio/heaven");

        SetCursorHidden(true);
    }

    void BuildSpineBackground(RectTransform parent, SkeletonDataAsset skeletonData)
    {
        var spineGo = new GameObject("SpineBg", typeof(RectTransform));
        spineGo.transform.SetParent(parent, false);
        var srt = (RectTransform)spineGo.transform;

        // Spine'daki kok nokta (0,0) sol alt kose oldugu icin anchor ve pivotu sol alta (0,0) aliyoruz:
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.zero;
        srt.pivot = Vector2.zero;
        srt.anchoredPosition = Vector2.zero;

        spineBg = spineGo.AddComponent<SkeletonGraphic>();
        spineBg.skeletonDataAsset = skeletonData;
        spineBg.raycastTarget = false;
        spineBg.allowMultipleCanvasRenderers = true;

        var straightMat = Resources.Load<Material>("Materials/UI-StraightAlphaTex/SkeletonGraphicDefault-Straight");
        if (straightMat == null)
        {
            var shader = Shader.Find("Spine/SkeletonGraphic");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
                spineBg.material = mat;
            }
        }
        else
        {
            spineBg.material = straightMat;
        }

        // Animasyon bileseni renderer'dan ONCE eklenmeli: SkeletonGraphic.Initialize
        // eslesen ISkeletonAnimation'i bulup AnimationState'i ilerletir. Once
        // renderer kurulursa menu arka plani tek kare cizilip donar.
        spineAnimation = spineGo.AddComponent<SkeletonAnimation>();
        spineAnimation.skeletonDataAsset = skeletonData;
        spineAnimation.unscaledTime = true;   // Time.timeScale 0 olsa bile menu canli kalsin
        spineAnimation.Initialize(false);

        var data = skeletonData.GetSkeletonData(true);
        if (data != null)
        {
            var ambient = FindAnim(data, "maın_menu_stabil", "cehennnem_seçmeden_ıdle");
            if (ambient != null) spineAnimation.AnimationState.SetAnimation(0, ambient, true);

            // Referans cozunurluk (genelde 1920x1080) ile Spine boyutunu oranla
            Vector2 refSize = parent.rect.size;
            var scaler = parent.GetComponentInParent<CanvasScaler>();
            if (refSize.x <= 0f || refSize.y <= 0f)
            {
                refSize = scaler != null && scaler.referenceResolution.x > 0f && scaler.referenceResolution.y > 0f
                    ? scaler.referenceResolution
                    : new Vector2(1920f, 1080f);
            }

            // The skeleton bounds include door and flame meshes outside the
            // background plane. Fitting to data.Width shifts the backdrop and
            // leaves an exposed strip at the right edge. The exported backdrop
            // itself spans x=0..3840 and y=data.Y..1 (2162 units tall).
            float backgroundX = 0f;
            float backgroundY = data.Y;
            float backgroundW = 3840f;
            float backgroundH = 2162f;

            float scaleX = refSize.x / backgroundW;
            float scaleY = refSize.y / backgroundH;
            float scale = Mathf.Max(scaleX, scaleY) * 1.06f;

            srt.localScale = new Vector3(scale, scale, 1f);

            // Center the actual background plane rather than the wider skeleton bounds.
            // Arka plan alttan ekranin altina hizalanir; boylece altta siyah bar kalmaz.
            srt.anchoredPosition = new Vector2(
                (refSize.x - backgroundW * scale) * 0.5f - backgroundX * scale,
                -backgroundY * scale - 200f);

            bgRt = srt;
            bgBaseScale = srt.localScale;
            bgBasePos = srt.anchoredPosition;
        }
    }

    // Spine animasyon adlari disa aktarimda Turkce karakterlerle/yazim farklariyla
    // degisebiliyor. Once tam adi, sonra Turkce karakterleri sadelestirilmis adi,
    // en son da onek eslesmesini dene. Boylece ufak bir harf farki animasyonu
    // tamamen oldurmuyor.
    static Spine.Animation FindAnim(Spine.SkeletonData data, params string[] names)
    {
        if (data == null) return null;
        for (int i = 0; i < names.Length; i++)
        {
            if (string.IsNullOrEmpty(names[i])) continue;
            var exact = data.FindAnimation(names[i]);
            if (exact != null) return exact;
        }

        var list = data.Animations;
        if (list == null) return null;

        for (int i = 0; i < names.Length; i++)
        {
            if (string.IsNullOrEmpty(names[i])) continue;
            string key = NormAnim(names[i]);
            for (int j = 0; j < list.Count; j++)
            {
                var a = list.Items[j];
                if (a != null && NormAnim(a.Name) == key) return a;
            }
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (string.IsNullOrEmpty(names[i])) continue;
            string key = NormAnim(names[i]);
            if (key.Length < 4) continue;
            for (int j = 0; j < list.Count; j++)
            {
                var a = list.Items[j];
                if (a == null) continue;
                string candidate = NormAnim(a.Name);
                if (candidate.StartsWith(key, StringComparison.Ordinal)) return a;
            }
        }
        return null;
    }

    static string NormAnim(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace('ı', 'i').Replace('İ', 'i')
             .Replace('ş', 's').Replace('Ş', 's')
             .Replace('ç', 'c').Replace('Ç', 'c')
             .Replace('ğ', 'g').Replace('Ğ', 'g')
             .Replace('ö', 'o').Replace('Ö', 'o')
             .Replace('ü', 'u').Replace('Ü', 'u')
             .ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        }
        return sb.ToString();
    }

    void OnDestroy()
    {
        if (ghost != null && ghost.spineAnimation != null)
            ghost.spineAnimation.UpdateComplete -= ApplyDemonEyeTint;
        if (settingsOpenButton != null) settingsOpenButton.SetActive(true);
        if (achievementsOpenButton != null) achievementsOpenButton.SetActive(true);
        SetCursorHidden(false);
    }

    void ApplyDemonEyeTint(ISkeletonRenderer renderer)
    {
        if (ghost == null || ghost.spineAnimation == null || ghost.spineAnimation.Skeleton == null) return;

        var skeleton = ghost.spineAnimation.Skeleton;
        float intensity = Mathf.Clamp01(ghost.demon);
        Color eyeColor = Color.Lerp(Color.white, new Color(1f, 0.06f, 0.035f, 1f), intensity);
        Color glowColor = new Color(1f, 0.015f, 0.01f, intensity);

        TintSlot(skeleton, "goz_sagda", eyeColor);
        TintSlot(skeleton, "goz_solda", eyeColor);
        TintSlot(skeleton, "goz_sagda2", glowColor);
        TintSlot(skeleton, "goz_solda2", glowColor);
    }

    static void TintSlot(Spine.Skeleton skeleton, string slotName, Color color)
    {
        var slot = skeleton.FindSlot(slotName);
        if (slot != null) slot.Pose.SetColor(color);
    }
    void OnDisable() { SetCursorHidden(false); }
    void OnEnable() { if (root != null && !settingsMode) SetCursorHidden(true); }

    void SetCursorHidden(bool hide)
    {
        if (cursorHidden == hide) return;
        cursorHidden = hide;
        Cursor.visible = !hide;
    }

    // ---- Ust baslik ----
    // Ince altin cizgiler + kucuk ust baslik + buyuk ARAF + taraf secme cagrisi.
    void BuildTitle(RectTransform r)
    {
        var block = new GameObject("TitleBlock", typeof(RectTransform));
        titleBlock = (RectTransform)block.transform;
        titleBlock.SetParent(r, false);
        titleBlock.anchorMin = titleBlock.anchorMax = new Vector2(0.5f, 1f);
        titleBlock.pivot = new Vector2(0.5f, 1f);
        titleBlock.sizeDelta = new Vector2(960f, 256f);
        titleBlock.anchoredPosition = new Vector2(0f, -14f);
        titleBasePos = titleBlock.anchoredPosition;

        var eyebrow = UIFactory.CreateText(titleBlock, "Eyebrow", Loc.T("CENNET & CEHENNEM"), 24f,
            TextAlignmentOptions.Center, GoldDim,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), new Vector2(0f, -12f), true);
        eyebrow.characterSpacing = 10f;

        Ornament(titleBlock, -1f);
        Ornament(titleBlock, 1f);

        // Tek baslik (golge kaldirildi, ortali).
        var title = UIFactory.CreateText(titleBlock, "Title", Loc.T(GameTitle), 116f,
            TextAlignmentOptions.Center, GoldBright,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -170f), new Vector2(0f, -50f), true);
        title.characterSpacing = 16f;
        title.enableAutoSizing = true;
        title.fontSizeMin = 52f;
        title.fontSizeMax = 116f;

        var tagline = UIFactory.CreateText(titleBlock, "Tagline", Loc.T("TARAFINI SEÇ"), 30f,
            TextAlignmentOptions.Center, new Color(0.86f, 0.8f, 0.69f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -222f), new Vector2(0f, -174f), true);
        tagline.characterSpacing = 8f;
    }

    static void Ornament(RectTransform parent, float direction)
    {
        var line = UIFactory.CreateRoundedPanel(parent, "Ornament", new Color(Gold.r, Gold.g, Gold.b, 0.55f), false);
        var rt = line.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 2f);
        rt.anchoredPosition = new Vector2(direction * 250f, -29f);
        rt.localRotation = Quaternion.Euler(0f, 0f, -direction * 5f);

        var dot = UIFactory.CreateRoundedPanel(parent, "OrnamentDot", Gold, false);
        var drt = dot.rectTransform;
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 1f);
        drt.pivot = new Vector2(0.5f, 0.5f);
        drt.sizeDelta = new Vector2(7f, 7f);
        drt.anchoredPosition = new Vector2(direction * 334f, -29f);
    }

    // ---- Iki yan kart: CEHENNEM / CENNET ----
    void BuildSideCards(RectTransform r)
    {
        hellCard = BuildSideCard(r, 1, new Vector2(96f, -292f),
            Loc.T("CEHENNEM"), Loc.T("Acıyla yüksel"), Loc.T("CEHENNEME İN"),
            HellAccent, HellAccentBright, HellAccentDim);
        heavenCard = BuildSideCard(r, 2, new Vector2(-96f, -292f),
            Loc.T("CENNET"), Loc.T("İyilikle yüksel"), Loc.T("CENNETE ÇIK"),
            HeavenAccent, HeavenAccentBright, HeavenAccentDim);
    }

    SideCard BuildSideCard(RectTransform r, int side, Vector2 pos, string label, string motto, string action,
        Color accent, Color bright, Color dim)
    {
        var card = new SideCard
        {
            side = side,
            accent = accent,
            accentBright = bright,
            accentDim = dim,
            basePos = pos,
            phase = side == 1 ? 0.4f : 2.1f
        };

        var go = new GameObject(side == 1 ? "HellCard" : "HeavenCard", typeof(RectTransform));
        card.root = (RectTransform)go.transform;
        card.root.SetParent(r, false);
        card.root.anchorMin = card.root.anchorMax = new Vector2(side == 1 ? 0f : 1f, 1f);
        card.root.pivot = new Vector2(side == 1 ? 0f : 1f, 1f);
        card.root.sizeDelta = new Vector2(390f, 260f);
        card.root.anchoredPosition = pos;

        // Yumusak isik halesi (hover'da parlar).
        var glow = Panel(card.root, "Glow", new Color(accent.r, accent.g, accent.b, 0f), false);
        glow.sprite = UIFactory.RadialSprite();
        glow.type = Image.Type.Simple;
        card.glow = glow;
        glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        glow.rectTransform.sizeDelta = new Vector2(620f, 620f);
        glow.rectTransform.anchoredPosition = new Vector2(0f, -10f);

        // Yarı saydam zemin ve ust aksan cizgisi.
        var panel = UIFactory.CreateRoundedPanel(card.root, "Panel", new Color(0.035f, 0.028f, 0.04f, 0.34f), false);
        Stretch(panel.rectTransform);
        var accentLine = UIFactory.CreateRoundedPanel(card.root, "AccentLine", new Color(accent.r, accent.g, accent.b, 0.55f), false);
        var alrt = accentLine.rectTransform;
        alrt.anchorMin = alrt.anchorMax = new Vector2(0.5f, 1f);
        alrt.pivot = new Vector2(0.5f, 0.5f);
        alrt.sizeDelta = new Vector2(86f, 3f);
        alrt.anchoredPosition = new Vector2(0f, -20f);

        card.label = UIFactory.CreateText(card.root, "Label", label, 58f,
            TextAlignmentOptions.Center, dim,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -92f), new Vector2(-14f, -26f), true);
        card.label.characterSpacing = 5f;
        card.label.enableAutoSizing = true;
        card.label.fontSizeMin = 30f;
        card.label.fontSizeMax = 58f;

        card.motto = UIFactory.CreateText(card.root, "Motto", motto, 23f,
            TextAlignmentOptions.Center, new Color(0.78f, 0.75f, 0.72f, 0.72f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -130f), new Vector2(-14f, -94f), false);
        card.motto.characterSpacing = 2f;

        // Cehenneme in / Cennete cik butonlari kaldirildi (gereksiz).

        return card;
    }

    void BuildHint(RectTransform r)
    {
        hintText = UIFactory.CreateText(r, "MenuHint", Loc.T("Fareyi bir tarafa götür, tıkla"), 23f,
            TextAlignmentOptions.Center, new Color(0.78f, 0.75f, 0.72f, 0.8f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 76f), new Vector2(0f, 112f), false);
        hintText.characterSpacing = 3f;
    }

    // ---- Alt orta: Ayarlar ----
    void BuildSettingsButton(RectTransform r)
    {
        TextMeshProUGUI label;
        var btn = UIFactory.CreateButton(r, "MenuSettingsButton", Loc.T("Ayarlar"), 26f, out label);
        var img = btn.GetComponent<Image>();
        var rt = (RectTransform)btn.transform;
        // Alt sag kose: alt ortadaki ipucu metniyle cakismasin.
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-64f, 34f);

        var art = UIArt.Load(UIArt.IconSettings);
        if (art != null)
        {
            img.sprite = art;
            img.preserveAspect = true;
            img.color = Color.white;
            float h = 84f;
            float aspect = art.rect.height > 0f ? art.rect.width / art.rect.height : 1f;
            rt.sizeDelta = new Vector2(h * aspect, h);
            img.type = Image.Type.Simple;
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
        g.root.sizeDelta = new Vector2(150f, 190f);
        g.root.anchoredPosition = g.pos;

        // Hayaletin arkasinda yumusak, tarafa gore renk degistiren isik.
        var glow = Panel(g.root, "GhostGlow", new Color(1f, 1f, 1f, 0f), false);
        glow.sprite = UIFactory.RadialSprite();
        glow.type = Image.Type.Simple;
        g.glow = glow;
        glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        glow.rectTransform.sizeDelta = new Vector2(360f, 360f);
        glow.rectTransform.anchoredPosition = new Vector2(0f, -6f);

        var spineData = Resources.Load<SkeletonDataAsset>("Art/main_menu/Ghost_Animations/ghost_SkeletonData");
        if (spineData != null)
        {
            var artObject = new GameObject("GhostSpine", typeof(RectTransform));
            artObject.transform.SetParent(g.root, false);
            var artRt = (RectTransform)artObject.transform;
            artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 0.5f);

            g.spineGraphic = artObject.AddComponent<SkeletonGraphic>();
            g.spineGraphic.skeletonDataAsset = spineData;
            g.spineGraphic.raycastTarget = false;
            g.spineGraphic.allowMultipleCanvasRenderers = true;
            var straightMat = Resources.Load<Material>("Materials/UI-StraightAlphaTex/SkeletonGraphicDefault-Straight");
            if (straightMat != null) g.spineGraphic.material = straightMat;
            else
            {
                var shader = Shader.Find("Spine/SkeletonGraphic");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
                    g.spineGraphic.material = mat;
                }
            }
            // Animasyon bileseni once eklenir; SkeletonGraphic.Initialize onu bulup
            // AnimationState'i ilerletir. Sira ters olursa hayalet donuk kalir.
            g.spineAnimation = artObject.AddComponent<SkeletonAnimation>();
            g.spineAnimation.skeletonDataAsset = spineData;
            g.spineAnimation.unscaledTime = true;
            g.spineAnimation.Initialize(false);
            g.spineAnimation.UpdateComplete += ApplyDemonEyeTint;

            PlayGhostAnimation(g, 0);
            if (g.spineGraphic.MatchRectTransformWithBounds())
            {
                float artHeight = g.spineGraphic.rectTransform.rect.height;
                if (artHeight > 0f)
                {
                    float artScale = 230f / artHeight;
                    g.spineGraphic.rectTransform.localScale = new Vector3(artScale, artScale, 1f);

                    // Spine iskelet kokunu (ayak) pivot yapar: hayalet imlecin ~115 px USTUNDE kalirdi.
                    // Gorunen sinirlarin merkezini hayalet kokune (imlece) tasi.
                    var artRect = g.spineGraphic.rectTransform;
                    Vector3 centerInRoot = g.root.InverseTransformPoint(artRect.TransformPoint(artRect.rect.center));
                    artRect.anchoredPosition -= new Vector2(centerInRoot.x, centerInRoot.y);
                }
            }
            return g;
        }

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
        g.wingL = Part(g.root, "WingL", new Color(0.95f, 0.97f, 1f), new Vector2(-30f, 18f), new Vector2(90f, 46f), new Vector2(1f, 0.3f), 20f);
        g.wingR = Part(g.root, "WingR", new Color(0.95f, 0.97f, 1f), new Vector2(30f, 18f), new Vector2(90f, 46f), new Vector2(0f, 0.3f), -20f);
        g.hornL = Part(g.root, "HornL", new Color(0.35f, 0.04f, 0.04f), new Vector2(-24f, 48f), new Vector2(14f, 36f), new Vector2(0.5f, 0f), 22f);
        g.hornR = Part(g.root, "HornR", new Color(0.35f, 0.04f, 0.04f), new Vector2(24f, 48f), new Vector2(14f, 36f), new Vector2(0.5f, 0f), -22f);
        g.halo = Part(g.root, "Halo", new Color(1f, 0.88f, 0.42f), new Vector2(0f, 76f), new Vector2(64f, 12f), new Vector2(0.5f, 0.5f), 0f);

        var body = Part(g.root, "Body", GhostWhite, new Vector2(0f, -2f), new Vector2(96f, 118f), new Vector2(0.5f, 0.5f), 0f);
        g.bodyParts.Add(body.GetComponent<Image>());
        for (int i = -1; i <= 1; i++)
        {
            var s = Part(g.root, "Hem" + i, GhostWhite, new Vector2(i * 32f, -62f), new Vector2(32f, 28f), new Vector2(0.5f, 0.5f), 0f);
            g.bodyParts.Add(s.GetComponent<Image>());
        }
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
        if (ghost == null) return;
        ghost.pop = 1f;
        PlayGhostAnimation(z);

        audioSrc.Stop();
        var clip = z == 1 ? hellClip : z == 2 ? heavenClip : null;
        if (clip != null) { audioSrc.clip = clip; audioSrc.Play(); }

        if (spineAnimation != null && spineAnimation.Skeleton != null && doorTransition == null)
            doorTransition = StartCoroutine(TransitionDoors());
    }

    IEnumerator TransitionDoors()
    {
        const float mixDuration = 0.3f;
        const float closeSpeed = 2.5f;
        yield return null; // Let StartCoroutine store its handle before this routine can finish.
        while (true)
        {
            int target = zone;
            var state = spineAnimation.AnimationState;

            // Close the currently open side by playing its opening animation backwards.
            if (activeDoorSide != 0 && activeDoorSide != target)
            {
                int closingSide = activeDoorSide;
                activeDoorSide = 0;
                var close = DoorOpening(closingSide);
                if (close != null)
                {
                    var entry = state.SetAnimation(0, close, false);
                    entry.Reverse = true;
                    entry.TimeScale = closeSpeed;
                    entry.MixDuration = mixDuration;
                    var stable = FindAnim(spineAnimation.Skeleton.Data, "maın_menu_stabil");
                    if (stable != null)
                    {
                        var stableEntry = state.AddAnimation(0, stable, true, 0f);
                        stableEntry.SetMixDuration(mixDuration, 0f);
                    }
                    yield return WaitForZoneOrDuration(target, close.Duration / closeSpeed);
                }
                else
                {
                    var stable = FindAnim(spineAnimation.Skeleton.Data, "maın_menu_stabil");
                    if (stable != null)
                    {
                        var stableEntry = state.SetAnimation(0, stable, true);
                        stableEntry.MixDuration = mixDuration;
                    }
                    yield return WaitForZoneOrDuration(target, mixDuration);
                }
                continue;
            }

            target = zone;
            if (target != 0 && activeDoorSide == 0)
            {
                var opening = DoorOpening(target);
                activeDoorSide = target;
                if (opening != null)
                {
                    var entry = state.SetAnimation(0, opening, false);
                    entry.MixDuration = mixDuration;
                    var ambient = DoorAmbient(target);
                    if (ambient != null)
                    {
                        var ambientEntry = state.AddAnimation(0, ambient, true, 0f);
                        ambientEntry.SetMixDuration(mixDuration, 0f);
                    }
                    yield return WaitForZoneOrDuration(target, opening.Duration);
                }
                else
                {
                    yield return WaitForZoneOrDuration(target, mixDuration);
                }

                continue;
            }

            doorTransition = null;
            yield break;
        }
    }

    IEnumerator WaitForZoneOrDuration(int expectedZone, float duration)
    {
        float end = Time.unscaledTime + duration;
        while (zone == expectedZone && Time.unscaledTime < end) yield return null;
    }

    Spine.Animation DoorOpening(int side)
    {
        var data = spineAnimation.Skeleton.Data;
        return side == 1
            ? FindAnim(data, "cehennnem_seçmeden_önce_açılış", "cehennnem_seçmeden_önce")
            : FindAnim(data, "cennet_seçmeden_önce_açılıs", "cennet_seçmeden_önce");
    }

    Spine.Animation DoorAmbient(int side)
    {
        var data = spineAnimation.Skeleton.Data;
        return side == 1
            ? FindAnim(data, "cehennnem_seçmeden_ıdle")
            : FindAnim(data, "cennet_seçmeden_önce_ıdle");
    }

    void PlayGhostAnimation(int z) { PlayGhostAnimation(ghost, z); }

    void PlayGhostAnimation(Ghost g, int z)
    {
        if (g == null || g.spineAnimation == null || g.spineAnimation.Skeleton == null) return;
        var data = g.spineAnimation.Skeleton.Data;

        // Disa aktarimdaki gercek adlar: ghost_idle / evil_ghost_ıdle /
        // angel_ghosT_ıdle. Eski kod "ghost" aradigi icin hicbir sey oynamiyordu.
        string idleName = z == 1 ? "evil_ghost_ıdle" : z == 2 ? "angel_ghosT_ıdle" : "ghost_idle";
        string introName = z == 1 ? "evil_ghost_baslangıc" : z == 2 ? "angel_ghost_baslangıc" : null;

        var idle = FindAnim(data, idleName, "ghost_idle");
        if (idle == null) return;

        var state = g.spineAnimation.AnimationState;
        var intro = FindAnim(data, introName);
        if (intro != null && z != 0)
        {
            // Once kisa gecis animasyonu, ardindan sonsuz idle.
            var entry = state.SetAnimation(0, intro, false);
            entry.MixDuration = 0.18f;
            var loop = state.AddAnimation(0, idle, true, 0f);
            loop.MixDuration = 0.25f;
        }
        else
        {
            var entry = state.SetAnimation(0, idle, true);
            entry.MixDuration = 0.22f;
        }
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
        StartCoroutine(PlayDoorThenChoose(s));
    }

    IEnumerator PlayDoorThenChoose(Side side)
    {
        // Kapi acilis animasyonu bitsin, sonra menuyu yumusakca karart.
        while (doorTransition != null) yield return null;

        closing = true;
        float t = 0f;
        const float fadeDuration = 0.42f;
        Color tint = side == Side.Hell ? HellAccent : HeavenAccent;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            group.alpha = 1f - Mathf.SmoothStep(0f, 1f, k);
            if (flash != null)
            {
                tint.a = 0.32f * Mathf.Sin(k * Mathf.PI);
                flash.color = tint;
            }
            if (ghost != null && ghost.root != null)
            {
                float gs = 1f + 0.35f * k;
                ghost.root.localScale = new Vector3(gs, gs, 1f);
                ghost.root.localRotation = Quaternion.Euler(0f, ghostFacingYaw, ghost.tilt + k * 10f);
            }
            yield return null;
        }

        if (onChosen != null) onChosen(side);
    }

    void ResolveSettings(bool force = false)
    {
        if (settings != null) return;
        if (!force && findTimer > 0f) return;
        findTimer = 0.25f;
        settings = FindFirstObjectByType<SettingsPanel>();
    }

    void SetSettingsMode(bool open)
    {
        if (settingsMode == open) return;
        settingsMode = open;
        ghost.root.gameObject.SetActive(!open);
        SetCursorHidden(!open);
        if (open && zone != 0) SetZone(0);
        else if (open) audioSrc.Stop();
    }

    // Proje "Input System (New)" modunda: Input.mousePosition HATA firlatir ve Update'i yarida keser (hayalet donar).
    // Pointer.current ise en son kullanilan cihaz olur (kalem / dokunmatik ekran fareyi golgeleyebilir).
    // Bu yuzden once gercek fare, sonra dokunma, sonra son bilinen konum kullanilir.
    Vector2 lastPointer = new Vector2(float.NaN, float.NaN);
    Vector2 ReadPointerPosition()
    {
        var mouse = Mouse.current;
        if (mouse != null) lastPointer = mouse.position.ReadValue();
        else
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed) lastPointer = touch.primaryTouch.position.ReadValue();
            else
            {
                var pointer = Pointer.current;
                if (pointer != null) lastPointer = pointer.position.ReadValue();
            }
        }
        if (float.IsNaN(lastPointer.x)) lastPointer = new Vector2(Screen.width * 0.5f, Screen.height * 0.3f);
        return lastPointer;
    }

    // ---- Animasyon ----
    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (!closing)
        {
            introT = Mathf.Min(1f, introT + dt * 2.4f);
            group.alpha = Mathf.SmoothStep(0f, 1f, introT);
        }

        findTimer -= dt;
        ResolveSettings();
        SetSettingsMode(settings != null && settings.IsOpen);

        Vector2 screenPos = ReadPointerPosition();

        int z = settingsMode || closing ? 0 : ComputeZone(screenPos);
        if (!chosen && !closing && z != zone) SetZone(z);

        AnimateBackground(dt);
        AnimateTitle(dt);
        AnimateSideCards(dt);
        AnimateHint(dt);
        AnimateGhost(screenPos, dt);
        AnimateMotes(dt);
    }

    void AnimateBackground(float dt)
    {
        if (bg != null) bg.color = hasBgArt ? Color.black : Neutral;

        // Arka plan cok yavas nefes alir; menu donuk bir resim gibi durmaz.
        float t = Time.unscaledTime;
        if (bgRt != null)
        {
            float zoom = 1.012f + Mathf.Sin(t * 0.16f) * 0.006f + (zone != 0 ? 0.004f : 0f);
            bgRt.localScale = new Vector3(bgBaseScale.x * zoom, bgBaseScale.y * zoom, 1f);
            bgRt.anchoredPosition = bgBasePos + new Vector2(
                Mathf.Sin(t * 0.13f) * 8f,
                Mathf.Sin(t * 0.19f) * 5f);
        }

        // Secili tarafin rengi hafifce tum yariyi yikar.
        float hellTarget = zone == 1 ? 0.20f : 0f;
        float heavenTarget = zone == 2 ? 0.20f : 0f;
        hellTint = Mathf.MoveTowards(hellTint, hellTarget, dt * 2.4f);
        heavenTint = Mathf.MoveTowards(heavenTint, heavenTarget, dt * 2.4f);
        if (hellImg != null) hellImg.color = new Color(HellHot.r, HellHot.g, HellHot.b, hellTint);
        if (heavenImg != null) heavenImg.color = new Color(HeavenHot.r, HeavenHot.g, HeavenHot.b, heavenTint);
    }

    void AnimateTitle(float dt)
    {
        if (titleBlock == null) return;
        float k = Mathf.SmoothStep(0f, 1f, introT);
        titleBlock.anchoredPosition = titleBasePos + new Vector2(0f, (1f - k) * 34f);
        titleBlock.localScale = Vector3.one * (0.965f + 0.035f * k);
    }

    void AnimateSideCards(float dt)
    {
        AnimateSideCard(hellCard, dt);
        AnimateSideCard(heavenCard, dt);
    }

    void AnimateSideCard(SideCard c, float dt)
    {
        // Yan kartlar menuden kaldirildi (BuildSideCards cagrilmiyor). Ama [Serializable] sinif oldugu icin
        // Unity bu alanlari null yapmaz, ici bos nesne olusturur: c != null ama c.root == null olur.
        if (c == null || c.root == null) return;

        float target = zone == c.side && !settingsMode && !closing ? 1f : 0f;
        c.hover = Mathf.MoveTowards(c.hover, target, dt * 4.2f);

        float t = Time.unscaledTime;
        float breath = Mathf.Sin(t * 1.35f + c.phase);
        c.root.anchoredPosition = c.basePos + new Vector2(0f, breath * 5f + c.hover * 9f);
        float scale = 1f + 0.035f * c.hover + 0.005f * breath;
        c.root.localScale = new Vector3(scale, scale, 1f);

        if (c.glow != null)
        {
            Color glow = c.accent;
            glow.a = 0.10f + 0.30f * c.hover + 0.025f * breath;
            c.glow.color = glow;
            c.glow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.35f + c.phase) * 7f);
        }

        c.label.color = Color.Lerp(c.accentDim, c.accentBright, c.hover);
        Color motto = c.motto.color;
        motto.a = 0.55f + 0.38f * c.hover;
        c.motto.color = motto;

        if (c.pillImg != null)
        {
            Color pill = c.accent;
            pill.a = 0.10f + 0.85f * c.hover;
            c.pillImg.color = pill;
        }
        if (c.pill != null)
        {
            float ps = 0.94f + 0.06f * c.hover;
            c.pill.localScale = new Vector3(ps, ps, 1f);
        }
        if (c.action != null)
            c.action.color = new Color(Ink.r, Ink.g, Ink.b, 0.30f + 0.68f * c.hover);
    }

    void AnimateHint(float dt)
    {
        if (hintText == null) return;
        string text = zone == 0 ? Loc.T("Fareyi bir tarafa götür, tıkla") : Loc.T("TIKLA VE BAŞLA");
        if (hintText.text != text) hintText.text = text;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (zone == 0 ? 2.2f : 5.5f));
        Color c = zone == 1 ? HellAccentBright : zone == 2 ? HeavenAccentBright : new Color(0.78f, 0.75f, 0.72f);
        c.a = 0.55f + 0.35f * pulse;
        hintText.color = c;
    }

    void AnimateGhost(Vector2 screenPos, float dt)
    {
        var g = ghost;
        if (g == null || g.root == null || settingsMode || closing) return;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out local))
        {
            // Yedek: ekran merkezine gore, canvas olcegiyle bol
            float sf = ownCanvas != null && ownCanvas.rootCanvas != null ? Mathf.Max(0.0001f, ownCanvas.rootCanvas.scaleFactor) : 1f;
            local = (screenPos - new Vector2(Screen.width, Screen.height) * 0.5f) / sf;
        }
        Vector2 prev = g.pos;
        g.pos = Vector2.Lerp(g.pos, local, 1f - Mathf.Exp(-20f * dt));
        float vx = dt > 0f ? (g.pos.x - prev.x) / dt : 0f;
        g.tilt = Mathf.Lerp(g.tilt, Mathf.Clamp(-vx * 0.022f, -18f, 18f), 1f - Mathf.Exp(-10f * dt));
        float t = Time.unscaledTime;
        float bob = Mathf.Sin(t * 2.4f) * 8f;

        // Tarafa gore renklenen yumusak hale.
        if (g.glow != null)
        {
            Color gc = zone == 1 ? new Color(1f, 0.28f, 0.12f)
                     : zone == 2 ? new Color(0.6f, 0.8f, 1f)
                     : new Color(0.85f, 0.88f, 1f);
            gc.a = (zone == 0 ? 0.10f : 0.17f) + 0.055f * (0.5f + 0.5f * Mathf.Sin(t * 3.2f));
            g.glow.color = gc;
            float glowScale = 1f + 0.05f * Mathf.Sin(t * 2.1f);
            g.glow.rectTransform.localScale = new Vector3(glowScale, glowScale, 1f);
        }

        g.root.anchoredPosition = g.pos + new Vector2(Mathf.Sin(t * 1.1f) * 2f, bob);
        float targetFacing = zone == 1 ? 180f : 0f;
        ghostFacingYaw = Mathf.SmoothDampAngle(ghostFacingYaw, targetFacing,
            ref ghostFacingYawVelocity, 0.22f, 900f, dt);
        g.root.localRotation = Quaternion.Euler(0f, ghostFacingYaw, g.tilt);
        g.pop = Mathf.MoveTowards(g.pop, 0f, dt * 3.5f);
        float popScale = 1f + 0.14f * g.pop;
        g.root.localScale = new Vector3(popScale, popScale, 1f);

        g.angel = Mathf.MoveTowards(g.angel, zone == 2 ? 1f : 0f, dt * 4f);
        g.demon = Mathf.MoveTowards(g.demon, zone == 1 ? 1f : 0f, dt * 4f);

        if (g.spineGraphic != null) return;

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

    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    void AnimateMotes(float dt)
    {
        float t = Time.unscaledTime;
        for (int i = 0; i < motes.Count; i++)
        {
            var m = motes[i];
            float boost = zone == m.side && !settingsMode ? 1f : 0f;
            m.y += m.speed * (1f + 1.9f * boost) * dt;
            if (m.y > 1.05f)
            {
                m.y = -0.05f;
                m.x = UnityEngine.Random.Range(0.06f, 0.94f);
            }
            float x = m.x + Mathf.Sin(t * m.sway + m.phase) * (0.015f + 0.01f * boost);
            m.rt.anchorMin = m.rt.anchorMax = new Vector2(x, m.y);
            m.rt.anchoredPosition = Vector2.zero;

            float fade = Mathf.Sin(Mathf.Clamp01(m.y) * Mathf.PI);
            var c = m.color;
            c.a = 0.06f + 0.3f * fade + 0.26f * boost * fade;
            m.img.color = c;

            float size = m.size * (1f + 0.45f * boost);
            m.rt.sizeDelta = new Vector2(size, size);
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

    void BuildMotes(RectTransform half, int count, Color a, Color b, int side)
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
                side = side,
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
