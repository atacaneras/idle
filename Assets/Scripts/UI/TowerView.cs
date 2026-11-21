using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Dikey kule: ortada yer yuzu (0), ustte 7 cennet (+1..+7), altta 7 cehennem (-1..-7).
// "Kamera" = kule kokunu kaydirmak. Cehennemde kat atlayinca kamera asagi, cennette yukari gider.
//
// Kule AYRI bir canvas'ta (Screen Space - Camera) cizilir. Siralama:
//   kamera rengi < zemin (-10) < KULE (-5) < binalar (0+) < parcaciklar (100) < HUD (Overlay)
// Boylece kat arka plani binalarin ARKASINDA kalir, bina sprite'lari gorunur.
public class TowerView : MonoBehaviour
{
    public const int MaxFloor = 7;
    public const int TowerSortingOrder = -5;
    // Boss, sol sutun ile sag sutun arasindaki orta alana oturur (HudLayout.CenterLeft..W-CenterRightInset)
    // BossX = orta sutunun merkezi (kat adi tabelasi ve boss cubugu ile ayni dikey eksen)
    const float BossX = 0.5f + HudLayout.CenterOffsetX / HudLayout.RefWidth, BossY = 0.56f, BossH = 0.34f;
    // Spine karakter: ayaklari yer cizgisinde (ekran altindan oran), boyu ekran yuksekliginin bu orani
    const float BossFeetY = 0.25f, BossSpineH = 0.38f;
    public int CurrentFloor { get; private set; }
    public bool Busy { get; private set; }

    GameObject canvasGo;
    RectTransform tcRt, root;
    readonly Dictionary<int, RectTransform> floors = new Dictionary<int, RectTransform>();
    readonly List<KeyValuePair<RectTransform, float>> bosses = new List<KeyValuePair<RectTransform, float>>();   // rect, en/boy
    readonly Dictionary<int, RectTransform> bossAnchors = new Dictionary<int, RectTransform>();
    readonly Dictionary<int, GameObject> bossSpines = new Dictionary<int, GameObject>();
    readonly Dictionary<int, Vector3> bossBaseScale = new Dictionary<int, Vector3>();
    readonly Dictionary<int, Vector2> bossBasePos = new Dictionary<int, Vector2>();
    readonly List<KeyValuePair<RectTransform, float>> fogs = new List<KeyValuePair<RectTransform, float>>();   // sis katmani, faz
    float hitT = -1f;
    float W = 1920f, H = 1080f;

    static readonly string[] HellNames = {
        "Bekleme Salonu (Numara: 66.666)", "Şikayet Departmanı", "Trafik Cezası Şubesi", "Toplantı Cehennemi",
        "Kıyamet Muhasebesi", "Yönetim Kurulu", "Lucifer'in Penthouse'u" };
    static readonly string[] HeavenNames = {
        "Bulut Kafe", "Teyze Danışma Masası", "Huzur Mahallesi", "Melek Mesaisi",
        "Erdem Borsası", "Başmelekler Kurulu", "Tanrı'nın Ofisi (Tatilde)" };

    public static int FloorIndex(Side side, int n) { return side == Side.Hell ? -n : n; }

    public void Build(Canvas hud)
    {
        canvasGo = new GameObject("TowerCanvas");
        var tc = canvasGo.AddComponent<Canvas>();
        var cam = Camera.main;
        if (cam != null)
        {
            tc.renderMode = RenderMode.ScreenSpaceCamera;
            tc.worldCamera = cam;
            tc.planeDistance = 10f;
            tc.sortingOrder = TowerSortingOrder;
        }
        else { tc.renderMode = RenderMode.ScreenSpaceOverlay; tc.sortingOrder = -100; }   // yedek: kamera yoksa HUD'un altinda

        var sc = canvasGo.AddComponent<CanvasScaler>();   // HUD ile ayni olcekleme
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
        // GraphicRaycaster eklenmiyor: kule fare tiklamalarini yutmaz.

        tcRt = (RectTransform)tc.transform;
        Canvas.ForceUpdateCanvases();
        if (tcRt.rect.width > 1f && tcRt.rect.height > 1f) { W = tcRt.rect.width; H = tcRt.rect.height; }

        var go = new GameObject("Tower", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(tcRt, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(W, H);

        for (int f = -MaxFloor; f <= MaxFloor; f++) BuildFloor(f);
        SnapTo(0);
        GameManager.Instance.OnGathered += OnHit;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGathered -= OnHit;
        if (canvasGo != null) Destroy(canvasGo);
    }

    void BuildFloor(int f)
    {
        int k = Mathf.Abs(f);
        float t = k / (float)MaxFloor;
        Color c = f == 0 ? new Color(0.55f, 0.75f, 0.4f)
                : f < 0 ? Color.Lerp(new Color(0.55f, 0.08f, 0.05f), new Color(0.08f, 0f, 0.02f), t)
                        : Color.Lerp(new Color(0.55f, 0.8f, 1f), new Color(1f, 0.95f, 0.7f), t);

        var p = UIFactory.CreatePanel(root, "Floor_" + f, c, false);
        p.anchorMin = p.anchorMax = new Vector2(0.5f, 0.5f);
        p.sizeDelta = new Vector2(W, H);
        p.anchoredPosition = new Vector2(0, f * H);
        floors[f] = p;

        // Sanatci: Assets/Resources/Art/<kat>/ icindeki arka plan (adi "background" olmak zorunda degil) ve boss.png otomatik gorunur
        string key = f == 0 ? "earth" : (f < 0 ? "hell_" : "heaven_") + k;
        var bgSprite = UIArt.FloorBackground(key);
        if (bgSprite == null && f == 0)
        {
            bgSprite = Resources.Load<Sprite>("Art/earth/earth");
            if (bgSprite == null) { var all = Resources.LoadAll<Sprite>("Art/earth"); if (all.Length > 0) bgSprite = all[0]; }
        }
        if (bgSprite != null) AddBackground(p, bgSprite);
        var bossSprite = Resources.Load<Sprite>("Art/" + key + "/boss");
        if (bossSprite != null)
        {
            var b = UIFactory.CreatePanel(p, "Boss", Color.white, false);
            var bi = b.GetComponent<Image>(); bi.sprite = bossSprite; bi.preserveAspect = true;
            b.anchorMin = b.anchorMax = new Vector2(BossX, BossY);
            float aspect = bossSprite.bounds.size.x / bossSprite.bounds.size.y;
            b.sizeDelta = new Vector2(H * BossH * aspect, H * BossH);
            bosses.Add(new KeyValuePair<RectTransform, float>(b, aspect));
        }
        else if (f < 0) bossAnchors[f] = CreateBossAnchor(p, k);

        if (f != 0)
        {
            // Kat siniri: kamera kaydirilirken "kat degisiyor" hissi versin diye ustte koyu/acik serit.
            // (Ozellikle cehennemde tum katlar kirmizi oldugu icin serit olmazsa inis fark edilmiyordu.)
            var edge = UIFactory.CreatePanel(p, "Edge", f < 0 ? new Color(0f, 0f, 0f, 0.55f) : new Color(1f, 1f, 1f, 0.45f), false);
            edge.anchorMin = new Vector2(0f, 1f); edge.anchorMax = new Vector2(1f, 1f);
            edge.pivot = new Vector2(0.5f, 1f);
            edge.offsetMin = new Vector2(0f, -HudLayout.FloorEdgeH); edge.offsetMax = Vector2.zero;
            var line = UIFactory.CreatePanel(p, "EdgeLine", f < 0 ? new Color(1f, 0.45f, 0.1f, 0.9f) : new Color(1f, 0.95f, 0.6f, 0.9f), false);
            line.anchorMin = new Vector2(0f, 1f); line.anchorMax = new Vector2(1f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.offsetMin = new Vector2(0f, -HudLayout.FloorEdgeH - 6f); line.offsetMax = new Vector2(0f, -HudLayout.FloorEdgeH);
            return;
        }
    }

    // Arka plan: oranini bozmadan ekrani doldurur (tasan kisim kirpilir), ustune hafif vinyet biner.
    void AddBackground(RectTransform floor, Sprite sprite)
    {
        floor.gameObject.AddComponent<RectMask2D>();
        var bg = UIFactory.CreatePanel(floor, "Bg", Color.white, false);
        bg.SetAsFirstSibling();
        var im = bg.GetComponent<Image>();
        im.sprite = sprite;
        im.preserveAspect = false;
        var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);

        var vig = UIFactory.CreatePanel(floor, "Vignette", new Color(0f, 0f, 0f, 0.5f), false);
        vig.SetSiblingIndex(1);
        vig.GetComponent<Image>().sprite = UIFactory.VignetteSprite();
        vig.anchorMin = Vector2.zero; vig.anchorMax = Vector2.one;
        vig.offsetMin = vig.offsetMax = Vector2.zero;
    }

    // Cehennem katlarinda boss.png yoksa sahneye Spine seytan konur (kat numarasina gore Kukla_01..07 gorunumu).
    RectTransform CreateBossAnchor(RectTransform floor, int hellFloor)
    {
        var a = UIFactory.CreatePanel(floor, "BossAnchor", new Color(0f, 0f, 0f, 0f), false);
        a.anchorMin = a.anchorMax = new Vector2(BossX, BossFeetY);
        a.sizeDelta = Vector2.zero;

        var glow = UIFactory.CreatePanel(a, "BossGlow", new Color(1f, 0.38f, 0.1f, 0.30f), false);
        glow.GetComponent<Image>().sprite = UIFactory.RadialSprite();
        glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0.5f);
        glow.sizeDelta = new Vector2(720f, 720f);
        glow.anchoredPosition = new Vector2(0f, 230f);

        // Zemin: lav isigi + koyu tas platform + golge. Seytan havada durmasin.
        Ellipse(a, "BossLava", new Color(1f, 0.35f, 0.08f, 0.45f), new Vector2(760f, 150f), new Vector2(0f, -10f));
        Ellipse(a, "BossPlatform", new Color(0.05f, 0.02f, 0.02f, 0.95f), new Vector2(600f, 105f), new Vector2(0f, -2f));
        Ellipse(a, "BossPlatformTop", new Color(0.2f, 0.07f, 0.05f, 0.9f), new Vector2(470f, 70f), new Vector2(0f, 4f));
        Ellipse(a, "BossShadow", new Color(0f, 0f, 0f, 0.6f), new Vector2(330f, 52f), new Vector2(0f, 6f));

        // Ayaklarin etrafinda yavasca kayan sis (SpineUI karakteri bunlarin USTUNE ekler)
        for (int i = 0; i < 3; i++)
        {
            var fog = Ellipse(a, "Fog" + i, new Color(0.55f, 0.14f, 0.07f, 0.20f), new Vector2(900f - i * 120f, 150f), new Vector2(0f, 18f + i * 14f));
            fogs.Add(new KeyValuePair<RectTransform, float>(fog, i * 2.1f));
        }
        return a;
    }

    static RectTransform Ellipse(RectTransform parent, string name, Color color, Vector2 size, Vector2 pos)
    {
        var e = UIFactory.CreatePanel(parent, name, color, false);
        e.GetComponent<Image>().sprite = UIFactory.RadialSprite();
        e.anchorMin = e.anchorMax = new Vector2(0.5f, 0.5f);
        e.sizeDelta = size;
        e.anchoredPosition = pos;
        return e;
    }

    // Topla'ya basinca seytan sarsilir, kirmiziya doner ve ezilip geri toplanir.
    void OnHit() { hitT = 0f; }

    void AnimateBoss()
    {
        foreach (var f in fogs)
        {
            if (f.Key == null) continue;
            float x = Mathf.Sin(Time.unscaledTime * 0.35f + f.Value) * 60f;
            f.Key.anchoredPosition = new Vector2(x, f.Key.anchoredPosition.y);
        }
        if (hitT < 0f) return;
        hitT += Time.unscaledDeltaTime / 0.28f;
        GameObject go;
        if (!bossSpines.TryGetValue(CurrentFloor, out go) || go == null) { hitT = -1f; return; }
        var rt = (RectTransform)go.transform;
        var g = go.GetComponent<Spine.Unity.SkeletonGraphic>();
        Vector3 bs; Vector2 bp;
        if (!bossBaseScale.TryGetValue(CurrentFloor, out bs)) bs = rt.localScale;
        if (!bossBasePos.TryGetValue(CurrentFloor, out bp)) bp = rt.anchoredPosition;
        if (hitT >= 1f)
        {
            hitT = -1f;
            rt.localScale = bs; rt.anchoredPosition = bp;
            if (g != null) g.color = Color.white;
            return;
        }
        float k = 1f - hitT;                       // 1 -> 0
        float pulse = Mathf.Sin(hitT * Mathf.PI);  // 0 -> 1 -> 0
        rt.localScale = new Vector3(bs.x * (1f + 0.07f * pulse), bs.y * (1f - 0.09f * pulse), bs.z);
        rt.anchoredPosition = bp + new Vector2(Mathf.Sin(hitT * 55f) * 14f * k, 0f);
        if (g != null) g.color = Color.Lerp(Color.white, new Color(1f, 0.42f, 0.38f), pulse);
    }

    // Sadece gorunen (ve yanindaki) kattaki seytan canli tutulur; digerleri uyutulur.
    void UpdateBosses()
    {
        foreach (var kv in bossAnchors)
        {
            int f = kv.Key;
            bool want = Mathf.Abs(f - CurrentFloor) <= 1;
            GameObject go;
            if (!bossSpines.TryGetValue(f, out go))
            {
                if (!want) continue;
                int k = Mathf.Clamp(-f, 1, MaxFloor);
                go = SpineUI.CreateGraphic(kv.Value, SpineUI.Find("tan_spine"), "Kukla_0" + k, "iblis_idle_01", H * BossSpineH);
                bossSpines[f] = go;   // null olsa bile tekrar denenmesin
                if (go != null)
                {
                    var grt = (RectTransform)go.transform;
                    bossBaseScale[f] = grt.localScale;
                    bossBasePos[f] = grt.anchoredPosition;
                }
            }
            if (go != null && go.activeSelf != want) go.SetActive(want);
        }
    }

    // Pencere / cozunurluk degisirse katlari yeni ekran olcusune gore yeniden diz.
    void Update()
    {
        if (tcRt == null) return;
        UpdateBosses();
        AnimateBoss();
        var r = tcRt.rect;
        if (r.width < 1f || r.height < 1f) return;
        if (Mathf.Abs(r.width - W) < 1f && Mathf.Abs(r.height - H) < 1f) return;
        W = r.width; H = r.height;
        foreach (var kv in bossSpines) if (kv.Value != null) Destroy(kv.Value);
        bossBaseScale.Clear(); bossBasePos.Clear();
        bossSpines.Clear();   // boyutlar degisti: seytan yeni olcuyle yeniden kurulur
        root.sizeDelta = new Vector2(W, H);
        foreach (var kv in floors) { kv.Value.sizeDelta = new Vector2(W, H); kv.Value.anchoredPosition = new Vector2(0, kv.Key * H); }
        foreach (var b in bosses) b.Key.sizeDelta = new Vector2(H * BossH * b.Value, H * BossH);
        if (!Busy) SnapTo(CurrentFloor);
    }

    public void SnapTo(int floor) { CurrentFloor = floor; root.anchoredPosition = new Vector2(0, -floor * H); }

    // Kamerayi bir kat kaydir (kat atlayinca). Cehennemde asagi, cennette yukari.
    public void GoToFloor(int floor, float dur = 1.6f)
    {
        if (floor == CurrentFloor) return;
        StartCoroutine(Move(CurrentFloor, floor, dur));
    }

    // Oyun basi: yer yuzu (XP arka plani) ekranda durur, sonra kamera dogrudan 1. kata iner (cehennem) / cikar (cennet)
    public void PlayIntro(Side side, Action done, Action enteringFloor = null)
    {
        SnapTo(0);
        StartCoroutine(Intro(side, done, enteringFloor));
    }

    IEnumerator Intro(Side side, Action done, Action enteringFloor)
    {
        Busy = true;
        for (float t = 0; t < 0.8f; t += Time.unscaledDeltaTime) yield return null;   // yer yuzu bir an gorunsun
        if (enteringFloor != null) enteringFloor();
        yield return Move(0, FloorIndex(side, 1), 2f);
        Busy = false;
        if (done != null) done();
    }

    IEnumerator Move(int from, int to, float dur)
    {
        Busy = true;
        for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
        {
            root.anchoredPosition = new Vector2(0, Mathf.Lerp(-from * H, -to * H, Ease(t / dur)));
            yield return null;
        }
        SnapTo(to);
        Busy = false;
    }

    static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
}
