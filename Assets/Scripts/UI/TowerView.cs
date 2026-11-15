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
    const float BossX = 0.47f, BossY = 0.56f, BossH = 0.34f;
    public int CurrentFloor { get; private set; }
    public bool Busy { get; private set; }

    GameObject canvasGo;
    RectTransform tcRt, root;
    readonly Dictionary<int, RectTransform> floors = new Dictionary<int, RectTransform>();
    readonly List<KeyValuePair<RectTransform, float>> bosses = new List<KeyValuePair<RectTransform, float>>();   // rect, en/boy
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
    }

    void OnDestroy() { if (canvasGo != null) Destroy(canvasGo); }

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

        // Sanatci: Assets/Resources/Art/<kat>/background.png ve boss.png otomatik gorunur
        string key = f == 0 ? "earth" : (f < 0 ? "hell_" : "heaven_") + k;
        var bgSprite = Resources.Load<Sprite>("Art/" + key + "/background");
        if (bgSprite != null) { var im = p.GetComponent<Image>(); im.sprite = bgSprite; im.color = Color.white; }
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
        // Yer yuzu (Windows XP arka plani): Assets/Resources/Art/earth/background.png
        // (ya da earth.png / klasordeki ilk gorsel). Ayri bir acilma animasyonu yok;
        // oyun basinda kamera dogrudan bu kattan 1. kata kayar.
        if (bgSprite == null)
        {
            var alt = Resources.Load<Sprite>("Art/earth/earth");
            if (alt == null) { var all = Resources.LoadAll<Sprite>("Art/earth"); if (all.Length > 0) alt = all[0]; }
            if (alt != null) { var im0 = p.GetComponent<Image>(); im0.sprite = alt; im0.color = Color.white; }
        }
    }

    // Pencere / cozunurluk degisirse katlari yeni ekran olcusune gore yeniden diz.
    void Update()
    {
        if (tcRt == null) return;
        var r = tcRt.rect;
        if (r.width < 1f || r.height < 1f) return;
        if (Mathf.Abs(r.width - W) < 1f && Mathf.Abs(r.height - H) < 1f) return;
        W = r.width; H = r.height;
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
