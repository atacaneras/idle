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
    // Boss, HUD panellerinin arasinda kalan bos orta alana oturur (sol panel < %31, bina listesi > %60)
    const float BossX = 0.45f, BossY = 0.56f, BossH = 0.40f;
    public int CurrentFloor { get; private set; }
    public bool Busy { get; private set; }

    GameObject canvasGo;
    RectTransform tcRt, root, leftHalf, rightHalf, hole;
    readonly Dictionary<int, RectTransform> floors = new Dictionary<int, RectTransform>();
    readonly List<KeyValuePair<RectTransform, float>> bosses = new List<KeyValuePair<RectTransform, float>>();   // rect, en/boy
    bool earthOpen;
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

        if (f != 0) return;
        // Yer yuzu: iki yari + altinda "delik". Acilista yarilar ayrilir.
        hole = UIFactory.CreatePanel(p, "Hole", Color.black, false);
        hole.anchorMin = Vector2.zero; hole.anchorMax = Vector2.one; hole.offsetMin = hole.offsetMax = Vector2.zero;
        leftHalf = UIFactory.CreatePanel(p, "EarthLeft", c, false);
        rightHalf = UIFactory.CreatePanel(p, "EarthRight", c, false);
        leftHalf.anchorMin = leftHalf.anchorMax = rightHalf.anchorMin = rightHalf.anchorMax = new Vector2(0.5f, 0.5f);
        leftHalf.sizeDelta = rightHalf.sizeDelta = new Vector2(W / 2f, H);
        ResetEarth();
    }

    void ResetEarth() { earthOpen = false; leftHalf.anchoredPosition = new Vector2(-W / 4f, 0); rightHalf.anchoredPosition = new Vector2(W / 4f, 0); }

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
        leftHalf.sizeDelta = rightHalf.sizeDelta = new Vector2(W / 2f, H);
        if (earthOpen) { leftHalf.anchoredPosition = new Vector2(-W * 0.75f, 0); rightHalf.anchoredPosition = new Vector2(W * 0.75f, 0); }
        else ResetEarth();
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

    // Oyun basi: yer yuzu acilir, kamera 1. kata iner (cehennem) / cikar (cennet)
    public void PlayIntro(Side side, Action done)
    {
        hole.GetComponent<Image>().color = side == Side.Hell ? new Color(0.4f, 0.03f, 0f) : new Color(1f, 0.97f, 0.8f);
        SnapTo(0);
        ResetEarth();
        StartCoroutine(Intro(side, done));
    }

    IEnumerator Intro(Side side, Action done)
    {
        Busy = true;
        for (float t = 0; t < 1.3f; t += Time.unscaledDeltaTime)   // yer yuzu yariliyor
        {
            float e = Ease(t / 1.3f);
            leftHalf.anchoredPosition = new Vector2(-W / 4f - e * W / 2f, 0);
            rightHalf.anchoredPosition = new Vector2(W / 4f + e * W / 2f, 0);
            yield return null;
        }
        earthOpen = true;
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
