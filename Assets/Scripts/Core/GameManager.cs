using System;
using System.Collections.Generic;
using UnityEngine;

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

    public event Action OnStateChanged;   // her tick
    public event Action OnAgeChanged;     // cag degisince / ilk yukleme

    readonly Dictionary<string, double> resources = new Dictionary<string, double>();
    readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    float autosaveTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (ages == null || ages.Length == 0) { Debug.LogError("GameManager: ages listesi bos."); return; }
        LoadGame();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (CurrentAge == null) return;
        Tick(Time.deltaTime);
        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer >= autosaveInterval) { autosaveTimer = 0; SaveGame(); }
    }

    void OnApplicationPause(bool paused) { if (paused && CurrentAge != null) SaveGame(); }
    void OnApplicationQuit() { if (CurrentAge != null) SaveGame(); }

    // ---- Uretim ----
    // Uretim sabit hizli oldugu icin offline ilerleme de ayni fonksiyonla tek adimda hesaplanir.
    void Tick(double dt)
    {
        var list = CurrentAge.buildings;
        for (int i = 0; i < list.Length; i++)
        {
            var b = list[i];
            int n = GetCount(b);
            if (n == 0) continue;
            AddResource(b.produces, n * b.baseProduction * Multiplier * dt);
        }
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
        AddResource(CurrentAge.clickResource, clickPower * Multiplier);
        if (OnStateChanged != null) OnStateChanged();
    }

    public bool TryBuy(BuildingData b)
    {
        if (!CanAfford(b)) return false;
        resources[b.costResource.id] = GetResource(b.costResource) - GetCost(b);
        counts[b.id] = GetCount(b) + 1;
        if (OnStateChanged != null) OnStateChanged();
        return true;
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
        get { return NextAge != null && GetResource(CurrentAge.advanceResource) >= CurrentAge.advanceCost; }
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
        if (OnStateChanged != null) OnStateChanged();
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
        if (elapsed > 1) { OfflineSecondsApplied = elapsed; Tick(elapsed); }
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
