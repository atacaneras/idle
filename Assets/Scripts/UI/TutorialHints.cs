using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Yeni oyuncuya ilk dakikalarda sirayla kisa ipuclari gosterir. Her ipucu bir kez cikar
// (PlayerPrefs'te hatirlanir). Kayit sifirlaninca ipuclari da yeniden baslar.
public class TutorialHints : MonoBehaviour
{
    const string PrefPrefix = "evrim_hint_";
    const float ShowSeconds = 7f;
    const float FadeSeconds = 0.5f;

    class Hint
    {
        public string id, text;
        public Func<GameManager, bool> when;   // gosterilme kosulu
        public Func<GameManager, bool> done;   // oyuncu yaptiysa ipucu erken kapanir
    }

    readonly List<Hint> hints = new List<Hint>();
    readonly List<UpgradeData> upgradeBuffer = new List<UpgradeData>();
    CanvasGroup group;
    TextMeshProUGUI label;
    Hint active;
    float timer;
    bool wasFresh;

    public void Init(FloatingTextSpawner floatingText) { /* ipuclari kendi bannerini kullanir; imza HUD ile uyumlu */ }

    void Start()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { enabled = false; return; }
        BuildBanner(canvas.rootCanvas.transform);
        BuildHints();
    }

    void BuildBanner(Transform canvas)
    {
        var rt = UIFactory.CreatePanel(canvas, "HintBanner", HudLayout.Panel, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(HudLayout.CenterOffsetX, -(HudLayout.CenterTop + HudLayout.FloorBarH + 8f));   // boss cubugunun altinda
        rt.sizeDelta = new Vector2(HudLayout.CenterWidth - 20f, HudLayout.HintH);
        group = rt.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        label = UIFactory.CreateText(rt, "Text", "", 30f, TextAlignmentOptions.Midline, Color.white,
            Vector2.zero, Vector2.one, new Vector2(20f, 8f), new Vector2(-20f, -8f), false, true);
    }

    void BuildHints()
    {
        Add("gather", "Alttaki büyük düğmeye tıkla (ya da Boşluk tuşuna bas).",
            g => g.PlaySeconds >= 3 && g.TotalClicks == 0,
            g => g.TotalClicks > 0);

        Add("buy", "Sağdaki listeden bir bina al. Binalar sen tıklamasan da üretir.",
            g => g.TotalBuildingsBought == 0 && AnyAffordableBuilding(g),
            g => g.TotalBuildingsBought > 0);

        Add("upgrade", "Soldaki Yükseltmeler paneli tıklamayı ve üretimi güçlendirir.",
            g => g.TotalUpgradesBought == 0 && g.TotalBuildingsBought > 0 && AnyAffordableUpgrade(g),
            g => g.TotalUpgradesBought > 0);

        Add("milestone", "Bir binadan 25 adet alırsan o binanın üretimi ikiye katlanır.",
            g => MaxCount(g) >= 15 && MaxCount(g) < 25,
            g => MaxCount(g) >= 25);


        Add("tree", "Bilgi Ağacı'nda Bilgi harcayarak kalıcı olarak güçlenebilirsin.",
            g => g.TotalPermanentLevels == 0 && AnyAffordablePerm(g),
            g => g.TotalPermanentLevels > 0);

        Add("timemachine", "Zaman Makinesi hazır: her şey sıfırlanır ama Bilgi ve kalıcı yükseltmeler kalır.",
            g => g.TimeLoops == 0 && g.CanUseTimeMachine,
            g => g.TimeLoops > 0);
    }

    void Add(string id, string text, Func<GameManager, bool> when, Func<GameManager, bool> done)
    {
        hints.Add(new Hint { id = id, text = text, when = when, done = done });
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentAge == null || group == null) return;

        // Yepyeni oyun (ya da Kaydi Sifirla): ipuclari bastan baslasin
        bool fresh = gm.TotalClicks == 0 && gm.PlaySeconds < 2.0;
        if (fresh && !wasFresh) ClearSeen();
        wasFresh = fresh;

        if (active != null)
        {
            timer -= Time.unscaledDeltaTime;
            if (active.done(gm)) timer = Mathf.Min(timer, FadeSeconds);
            // Ilk yarim saniyede belirir, son yarim saniyede solar
            group.alpha = Mathf.Clamp01(Mathf.Min((ShowSeconds - timer) / FadeSeconds, timer / FadeSeconds));
            if (timer <= 0f)
            {
                MarkSeen(active);
                active = null;
                group.alpha = 0f;
            }
            return;
        }

        foreach (var h in hints)
        {
            if (Seen(h)) continue;
            if (h.done(gm)) { MarkSeen(h); continue; }   // oyuncu zaten kendi kesfetmis
            if (!h.when(gm)) continue;
            active = h;
            timer = ShowSeconds;
            label.text = Loc.T(h.text);
            break;
        }
    }

    // ---- Kosul yardimcilari ----
    static bool AnyAffordableBuilding(GameManager gm)
    {
        foreach (var b in gm.CurrentAge.buildings) if (b != null && gm.CanAfford(b)) return true;
        return false;
    }

    bool AnyAffordableUpgrade(GameManager gm)
    {
        gm.GetVisibleUpgrades(upgradeBuffer);
        foreach (var u in upgradeBuffer) if (gm.CanAffordUpgrade(u)) return true;
        return false;
    }

    static bool AnyAffordablePerm(GameManager gm)
    {
        if (gm.PermanentUpgrades == null) return false;
        foreach (var p in gm.PermanentUpgrades) if (p != null && gm.CanBuyPerm(p)) return true;
        return false;
    }

    static int MaxCount(GameManager gm)
    {
        int m = 0;
        foreach (var b in gm.CurrentAge.buildings) if (b != null) m = Mathf.Max(m, gm.GetCount(b));
        return m;
    }

    // ---- Hatirlama ----
    static bool Seen(Hint h) { return PlayerPrefs.GetInt(PrefPrefix + h.id, 0) == 1; }
    static void MarkSeen(Hint h) { PlayerPrefs.SetInt(PrefPrefix + h.id, 1); }

    void ClearSeen()
    {
        foreach (var h in hints) PlayerPrefs.DeleteKey(PrefPrefix + h.id);
        active = null;
        if (group != null) group.alpha = 0f;
    }
}
