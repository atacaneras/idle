using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuyMode { One, Ten, Hundred, Max }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Cag listesi (sirayla)")]
    [SerializeField] AgeData[] ages;

    [Header("Ayarlar")]
    [SerializeField] double clickPower = 1;
    [SerializeField] float autosaveInterval = 30f;
    [SerializeField] float maxOfflineHours = 8f;
    [SerializeField] double knowledgeBonusPerPoint = 0.1;

    public AgeData CurrentAge { get; private set; }
    public double Knowledge { get; private set; }
    public double OfflineSecondsApplied { get; private set; }
    public double Multiplier { get { return 1.0 + Knowledge * knowledgeBonusPerPoint; } }
    public double ClickValue { get { return clickPower * Multiplier; } }   // tek tiklamada kazanilan miktar

    public event Action OnStateChanged;   // saniyede ~10 kez + her tiklama/satin almada
    public event Action OnAgeChanged;     // cag degisince / ilk yukleme
    public event Action<BuildingData, int> OnBuildingBought;   // basarili satin almadan sonra (bina, adet): animasyon ve ses icin
    public event Action OnGathered;       // topla butonuna her basista (ses icin)
    public event Action OnBuyModeChanged; // x1 / x10 / x100 / Max degisince

    public BuyMode CurrentBuyMode { get; private set; }
    const int MaxBulk = 100000;

    const float UiInterval = 0.1f;   // arayuz saniyede en fazla 10 kez yenilenir

    readonly Dictionary<string, double> resources = new Dictionary<string, double>();
    readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    float autosaveTimer;
    float uiTimer;
    DateTime lastTickUtc;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (ages == null || ages.Length == 0) { Debug.LogError("GameManager: ages listesi bos."); return; }
        ValidateData();
        LoadGame();
        lastTickUtc = DateTime.UtcNow;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (CurrentAge == null) return;

        // Gercek saate gore ilerle: kare takilsa, pencere arka planda kalsa ya da bilgisayar uyusa da
        // uretim kaybolmaz. Time.deltaTime kullanmiyoruz cunku Unity onu 1/3 saniyede kirpar.
        var now = DateTime.UtcNow;
        double dt = (now - lastTickUtc).TotalSeconds;
        lastTickUtc = now;
        if (dt > 0) Produce(Math.Min(dt, maxOfflineHours * 3600.0));

        uiTimer += Time.unscaledDeltaTime;
        if (uiTimer >= UiInterval) { uiTimer = 0; RaiseStateChanged(); }

        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer >= autosaveInterval) { autosaveTimer = 0; SaveGame(); }
    }

    void OnApplicationPause(bool paused) { if (paused && CurrentAge != null) SaveGame(); }
    void OnApplicationQuit() { if (CurrentAge != null) SaveGame(); }

    // ---- Uretim ----
    // Uretim sabit hizli oldugu icin offline ilerleme de ayni fonksiyonla tek adimda hesaplanir.
    void Produce(double dt)
    {
        var list = CurrentAge.buildings;
        for (int i = 0; i < list.Length; i++)
        {
            var b = list[i];
            int n = GetCount(b);
            if (n == 0) continue;
            AddResource(b.produces, n * b.baseProduction * Multiplier * dt);
        }
    }

    void RaiseStateChanged()
    {
        if (OnStateChanged != null) OnStateChanged();
    }

    // ---- Sorgular ----
    public double GetResource(ResourceData r) { double v; return resources.TryGetValue(r.id, out v) ? v : 0; }
    public int GetCount(BuildingData b) { int v; return counts.TryGetValue(b.id, out v) ? v : 0; }
    public double GetCost(BuildingData b) { return b.baseCost * Math.Pow(b.costGrowth, GetCount(b)); }
    public bool CanAfford(BuildingData b) { return GetResource(b.costResource) >= GetCost(b); }

    public double GetProductionPerSecond(ResourceData r)
    {
        double total = 0;
        foreach (var b in CurrentAge.buildings)
            if (b.produces == r) total += GetCount(b) * b.baseProduction * Multiplier;
        return total;
    }

    // ---- Eylemler ----
    void AddResource(ResourceData r, double amount)
    {
        resources[r.id] = GetResource(r) + amount;
    }

    public void Gather()
    {
        if (CurrentAge == null || CurrentAge.clickResource == null) return;
        AddResource(CurrentAge.clickResource, ClickValue);
        RaiseStateChanged();
        if (OnGathered != null) OnGathered();
    }

    // ---- Satin alma (tekli ve toplu) ----
    public void SetBuyMode(BuyMode mode)
    {
        if (CurrentBuyMode == mode) return;
        CurrentBuyMode = mode;
        if (OnBuyModeChanged != null) OnBuyModeChanged();
        RaiseStateChanged();
    }

    // Mevcut adetten baslayarak qty tane daha almanin toplam maliyeti (geometrik toplam)
    public double GetCostFor(BuildingData b, int qty)
    {
        if (qty <= 0) return 0;
        double first = GetCost(b);
        double g = b.costGrowth;
        if (Math.Abs(g - 1.0) < 1e-9) return first * qty;
        return first * (Math.Pow(g, qty) - 1.0) / (g - 1.0);
    }

    // Eldeki kaynakla en fazla kac tane alinabilir (0 olabilir)
    public int GetMaxAffordable(BuildingData b)
    {
        double r = GetResource(b.costResource);
        double first = GetCost(b);
        if (r < first) return 0;

        double g = b.costGrowth;
        double est = Math.Abs(g - 1.0) < 1e-9 ? r / first : Math.Log(1.0 + r * (g - 1.0) / first) / Math.Log(g);
        if (double.IsNaN(est) || double.IsInfinity(est)) est = 1;
        int q = (int)Math.Max(0, Math.Min(Math.Floor(est), MaxBulk));

        // Kayan nokta hatasini duzelt
        for (int i = 0; i < 8 && q > 0 && GetCostFor(b, q) > r; i++) q--;
        for (int i = 0; i < 8 && q < MaxBulk && GetCostFor(b, q + 1) <= r; i++) q++;
        return q;
    }

    // Su anki moda gore bir tiklamada alinacak adet (Max modunda en az 1: butonda maliyet gorunsun diye)
    public int GetBuyQuantity(BuildingData b)
    {
        switch (CurrentBuyMode)
        {
            case BuyMode.Ten: return 10;
            case BuyMode.Hundred: return 100;
            case BuyMode.Max: return Math.Max(1, GetMaxAffordable(b));
            default: return 1;
        }
    }

    public bool CanBuyCurrent(BuildingData b)
    {
        return GetResource(b.costResource) >= GetCostFor(b, GetBuyQuantity(b));
    }

    public int TryBuyCurrent(BuildingData b) { return Buy(b, GetBuyQuantity(b)); }
    public bool TryBuy(BuildingData b) { return Buy(b, 1) > 0; }

    int Buy(BuildingData b, int qty)
    {
        if (qty <= 0) return 0;
        double cost = GetCostFor(b, qty);
        if (GetResource(b.costResource) < cost) return 0;
        resources[b.costResource.id] = GetResource(b.costResource) - cost;
        counts[b.id] = GetCount(b) + qty;
        RaiseStateChanged();   // once gorunumler guncellensin, sonra animasyon baslasin
        if (OnBuildingBought != null) OnBuildingBought(b, qty);
        return qty;
    }

    public AgeData NextAge
    {
        get
        {
            int i = Array.IndexOf(ages, CurrentAge);
            return (i >= 0 && i + 1 < ages.Length) ? ages[i + 1] : null;
        }
    }

    public bool CanAdvance
    {
        get
        {
            return NextAge != null
                && CurrentAge.advanceResource != null
                && GetResource(CurrentAge.advanceResource) >= CurrentAge.advanceCost;
        }
    }

    public void AdvanceAge()
    {
        if (!CanAdvance) return;
        var next = NextAge;
        Knowledge += CurrentAge.knowledgeReward;
        resources.Clear();
        counts.Clear();
        CurrentAge = next;
        SaveGame();
        if (OnAgeChanged != null) OnAgeChanged();
        RaiseStateChanged();
    }

    // ---- Kayit ----
    public void SaveGame()
    {
        var s = new GameState
        {
            lastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ageId = CurrentAge.id,
            knowledge = Knowledge
        };
        foreach (var kv in resources) s.resources.Add(new NamedDouble { id = kv.Key, value = kv.Value });
        foreach (var kv in counts) s.buildings.Add(new NamedInt { id = kv.Key, value = kv.Value });
        SaveSystem.Save(s);
    }

    void LoadGame()
    {
        CurrentAge = ages[0];
        var s = SaveSystem.Load();
        if (s == null) return;

        foreach (var a in ages) if (a.id == s.ageId) CurrentAge = a;
        Knowledge = s.knowledge;
        foreach (var r in s.resources) resources[r.id] = r.value;
        foreach (var b in s.buildings) counts[b.id] = b.value;

        double elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - s.lastSaveUnix;
        elapsed = Math.Max(0, Math.Min(elapsed, maxOfflineHours * 3600.0));
        if (elapsed > 1) { OfflineSecondsApplied = elapsed; Produce(elapsed); }
    }

    // ---- Veri kontrolu: yanlis girilmis asset'leri baslangicta Console'a yazar ----
    void ValidateData()
    {
        var buildingIds = new HashSet<string>();
        var resourceIds = new HashSet<string>();
        var ageIds = new HashSet<string>();

        foreach (var a in ages)
        {
            if (a == null) { Debug.LogError("GameManager: ages listesinde bos eleman var."); continue; }
            if (string.IsNullOrEmpty(a.id) || a.id != a.id.Trim()) Debug.LogWarning("Cag '" + a.name + "': id bos ya da basinda/sonunda bosluk var.", a);
            if (!ageIds.Add(a.id)) Debug.LogError("Cag id'si tekrar ediyor: " + a.id, a);
            if (a.clickResource == null) Debug.LogWarning("Cag '" + a.name + "': Click Resource bos.", a);
            if (a.advanceResource == null) Debug.LogWarning("Cag '" + a.name + "': Advance Resource bos.", a);

            if (a.resources != null)
                foreach (var r in a.resources)
                {
                    if (r == null) { Debug.LogError("Cag '" + a.name + "': Resources listesinde bos eleman var.", a); continue; }
                    resourceIds.Add(r.id);
                }

            if (a.buildings == null) continue;
            foreach (var b in a.buildings)
            {
                if (b == null) { Debug.LogError("Cag '" + a.name + "': Buildings listesinde bos eleman var.", a); continue; }
                if (string.IsNullOrEmpty(b.id) || b.id != b.id.Trim()) Debug.LogWarning("Bina '" + b.name + "': id bos ya da basinda/sonunda bosluk var.", b);
                if (!buildingIds.Add(b.id)) Debug.LogError("Bina id'si tekrar ediyor: " + b.id, b);
                if (b.produces == null || b.costResource == null) Debug.LogError("Bina '" + b.name + "': Produces ya da Cost Resource bos.", b);
            }
        }
    }

    // ---- Test araclari (Inspector'da bilesenin uc nokta menusunden) ----
    [ContextMenu("DEBUG: Her kaynaktan 1000 ekle")]
    void DebugGrant()
    {
        foreach (var r in CurrentAge.resources) AddResource(r, 1000);
    }

    [ContextMenu("DEBUG: Kaydi sil")]
    void DebugDeleteSave() { SaveSystem.Delete(); Debug.Log("Kayit silindi."); }
}
