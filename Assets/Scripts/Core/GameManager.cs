using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuyMode { One, Ten, Hundred, Max }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Cag listesi (sirayla)")]
    [SerializeField] AgeData[] ages;

    [Header("Kalici yukseltmeler (Bilgi Agaci)")]
    [SerializeField] PermanentUpgradeData[] permanentUpgrades;

    [Header("Ayarlar")]
    [SerializeField] double clickPower = 1;
    [SerializeField] float autosaveInterval = 30f;
    [SerializeField] float maxOfflineHours = 8f;
    [Tooltip("Toplam kazanilan her Bilgi puani icin pasif uretim bonusu (0.02 = %2).")]
    [SerializeField] double lifetimeKnowledgeBonus = 0.02;

    [Header("Kilometre tasi carpanlari")]
    [Tooltip("Bir binadan bu kadar adet alinca o binanin uretimi milestoneMultiplier kat artar.")]
    [SerializeField] int[] milestoneCounts = { 25, 50, 100, 200, 300, 400, 500 };
    [SerializeField] double milestoneMultiplier = 2;

    [Header("Zaman Makinesi")]
    [Tooltip("Son cagda cag atlama maliyetine tam ulasinca kazanilan Bilgi. Daha fazla birikirse karekok ile artar.")]
    [SerializeField] double timeMachineKnowledgeBase = 10;

    const double MaxReduction = 0.75;   // maliyet indirimleri bu oranin ustune cikmaz
    const double FastClickMultiplier = 1000000.0;
    const double FastProductionMultiplier = 1000.0;
    const double FastBuildingCostMultiplier = 0.01;
    const double FastUpgradeCostMultiplier = 0.01;
    const double FastAdvanceCostMultiplier = 0.001;

    public AgeData CurrentAge { get; private set; }
    public double Knowledge { get; private set; }           // harcanabilir
    public double LifetimeKnowledge { get; private set; }   // toplam kazanilan
    public int TimeLoops { get; private set; }
    public double OfflineSecondsApplied { get; private set; }
    public bool FastTestMode { get; private set; }
    public bool SaveRecovered { get { return SaveSystem.LastLoadUsedBackup; } }
    public PermanentUpgradeData[] PermanentUpgrades { get { return permanentUpgrades; } }
    public AgeData[] Ages { get { return ages; } }

    // ---- Istatistikler (basarim ekrani ve basarim kosullari icin) ----
    public double TotalClicks { get; private set; }
    public double RunClicks { get; private set; }
    public double TotalBuildingsBought { get; private set; }
    public double TotalUpgradesBought { get; private set; }
    public double PlaySeconds { get; private set; }
    public int AgesAdvanced { get; private set; }
    public int AchievementCount { get { return unlockedAchievements.Count; } }

    // Genel carpan: pasif Bilgi bonusu x kalici yukseltmeler x bu cagdaki genel yukseltmeler
    public double Multiplier { get { return (1.0 + LifetimeKnowledge * lifetimeKnowledgeBonus) * permGlobal * ageGlobalMult; } }
    public double MaxOfflineHours { get { return maxOfflineHours + permOfflineHours; } }

    public event Action OnStateChanged;   // saniyede ~10 kez + her tiklama/satin almada
    public Side CurrentSide { get; private set; }
    public event Action<Side> OnSideChosen;
    public void ChooseSide(Side s)
    {
        CurrentSide = s;
        ResetRun();
        CurrentAge = FirstAge();
        RecalcCaches();
        SaveGame();
        if (OnSideChosen != null) OnSideChosen(s);
        if (OnAgeChanged != null) OnAgeChanged();
        if (OnUpgradesChanged != null) OnUpgradesChanged();
        RaiseStateChanged();
    }

    // ---- Gorevler ----
    public event Action<QuestDef> OnQuestClaimed;
    public bool IsQuestClaimed(QuestDef q) { return claimedQuests.Contains(q.id); }

    public double QuestProgress(QuestDef q)
    {
        switch (q.kind)
        {
            case QuestKind.Click: return RunClicks;
            case QuestKind.Building: return q.building != null ? GetCount(q.building) : 0;
            case QuestKind.Earn: return GetRunEarned(CurrentAge.clickResource);
            case QuestKind.Upgrades: return purchased.Count;
        }
        return 0;
    }

    public bool ClaimQuest(QuestDef q)
    {
        if (CurrentAge == null || IsQuestClaimed(q) || QuestProgress(q) < q.amount) return false;
        var res = CurrentAge.clickResource;
        double grant = q.rewardFlat + q.rewardSeconds * GetProductionPerSecond(res);
        if (grant > 0) AddResource(res, grant);
        if (q.rewardKnowledge > 0) GrantKnowledge(q.rewardKnowledge);
        claimedQuests.Add(q.id);
        SaveGame();
        RaiseStateChanged();
        if (OnQuestClaimed != null) OnQuestClaimed(q);
        return true;
    }

    // Secilen tarafin ilk kati (taraf yoksa listenin ilki)
    AgeData FirstAge()
    {
        foreach (var a in ages) if (CurrentSide == Side.None || a.side == CurrentSide) return a;
        return ages[0];
    }

    // Tarafin icinde kacinci kat (1..7)
    public int FloorNumber
    {
        get
        {
            int n = 0;
            foreach (var a in ages) { if (a.side == CurrentAge.side) n++; if (a == CurrentAge) break; }
            return Mathf.Max(1, n);
        }
    }

    public event Action OnAgeChanged;     // cag degisince / ilk yukleme
    public event Action<BuildingData, int> OnBuildingBought;   // basarili satin almadan sonra (bina, adet): animasyon ve ses icin
    public event Action OnGathered;       // topla butonuna her basista (ses icin)
    public event Action OnBuyModeChanged; // x1 / x10 / x100 / Max degisince
    public event Action OnUpgradesChanged;      // cag ici yukseltme alininca / sifirlaninca
    public event Action OnPermanentChanged;     // kalici yukseltme alininca
    public event Action<UpgradeData> OnUpgradeBought;
    public event Action<double> OnTimeMachineUsed;   // kazanilan Bilgi
    public event Action<BuildingData, int> OnMilestoneReached;   // bina, ulasilan esik (adet)
    public event Action<string> OnAchievementUnlocked;           // basarim id

    public BuyMode CurrentBuyMode { get; private set; }
    const int MaxBulk = 100000;

    const float UiInterval = 0.1f;   // arayuz saniyede en fazla 10 kez yenilenir

    readonly Dictionary<string, double> resources = new Dictionary<string, double>();
    readonly Dictionary<string, double> runEarned = new Dictionary<string, double>();
    readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    readonly HashSet<string> purchased = new HashSet<string>();
    readonly HashSet<string> claimedQuests = new HashSet<string>();
    readonly Dictionary<string, int> permLevels = new Dictionary<string, int>();
    readonly HashSet<string> unlockedAchievements = new HashSet<string>();

    // Onbellek: RecalcCaches() ile satin alma / yukleme / cag degisiminde yenilenir
    readonly Dictionary<string, double> upgradeBuildingMult = new Dictionary<string, double>();
    double clickUpgradeMult = 1, ageGlobalMult = 1, clickShare;
    double permGlobal = 1, permClick = 1, permCostMult = 1, permAdvanceMult = 1, permKnowledgeGain = 1, permOfflineHours;
    double permAutoClick, permMilestoneBoost;
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
        if (dt > 0)
        {
            double step = Math.Min(dt, MaxOfflineHours * 3600.0);
            Produce(step);
            ProduceAutoClicks(step);
            PlaySeconds += Math.Min(dt, 60.0);   // bilgisayar uyurken gecen sure oynama sayilmasin
        }

        uiTimer += Time.unscaledDeltaTime;
        if (uiTimer >= UiInterval) { uiTimer = 0; RaiseStateChanged(); }

        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer >= autosaveInterval) { autosaveTimer = 0; SaveGame(); }
    }

    void OnApplicationPause(bool paused) { if (paused && CurrentAge != null) SaveGame(); }
    void OnApplicationFocus(bool focused) { if (!focused && CurrentAge != null) SaveGame(); }
    void OnApplicationQuit() { if (CurrentAge != null) SaveGame(); }

    // ---- Uretim ----
    // Uretim sabit hizli oldugu icin offline ilerleme de ayni fonksiyonla tek adimda hesaplanir.
    void Produce(double dt)
    {
        var list = CurrentAge.buildings;
        for (int i = 0; i < list.Length; i++)
        {
            var b = list[i];
            if (GetCount(b) == 0) continue;
            AddResource(b.produces, GetBuildingProductionPerSecond(b) * dt);
        }
    }

    // Kalici "Otomatik El" yukseltmesi: saniyede belirli sayida tiklamaya esdeger uretim.
    void ProduceAutoClicks(double dt)
    {
        if (permAutoClick <= 0 || CurrentAge == null || CurrentAge.clickResource == null) return;
        AddResource(CurrentAge.clickResource, ClickValue * permAutoClick * dt);
    }

    void RaiseStateChanged()
    {
        if (OnStateChanged != null) OnStateChanged();
    }

    // ---- Sorgular ----
    public double GetResource(ResourceData r) { double v; return resources.TryGetValue(r.id, out v) ? v : 0; }

    // Bu turda toplam kazanilan (harcamalar dusulmez). Cag atlama / Zaman Makinesi buna bakar.
    public double GetRunEarned(ResourceData r)
    {
        if (r == null) return 0;
        double v;
        return runEarned.TryGetValue(r.id, out v) ? v : 0;
    }

    public int GetCount(BuildingData b) { int v; return counts.TryGetValue(b.id, out v) ? v : 0; }
    public double GetCost(BuildingData b)
    {
        return b.baseCost * Math.Pow(b.costGrowth, GetCount(b)) * permCostMult
            * (FastTestMode ? FastBuildingCostMultiplier : 1.0);
    }
    public bool CanAfford(BuildingData b) { return GetResource(b.costResource) >= GetCost(b); }

    public double GetProductionPerSecond(ResourceData r)
    {
        double total = 0;
        foreach (var b in CurrentAge.buildings)
            if (b.produces == r) total += GetBuildingProductionPerSecond(b);
        return total;
    }

    // ---- Uretim carpanlari ----
    // Bina basina carpan = kilometre tasi x bu binaya ozel yukseltmeler (genel carpan Multiplier ayri)
    public double GetBuildingMultiplier(BuildingData b)
    {
        double m = Math.Pow(milestoneMultiplier + permMilestoneBoost, MilestonesReached(GetCount(b)));
        double u;
        if (upgradeBuildingMult.TryGetValue(b.id, out u)) m *= u;
        return m;
    }

    // Tek bir binanin (adet 1) saniyedeki uretimi, tum carpanlar dahil
    public double GetUnitProduction(BuildingData b)
    {
        return b.baseProduction * GetBuildingMultiplier(b) * Multiplier
            * (FastTestMode ? FastProductionMultiplier : 1.0);
    }

    public double GetBuildingProductionPerSecond(BuildingData b) { return GetCount(b) * GetUnitProduction(b); }

    public int MilestonesReached(int count)
    {
        int n = 0;
        for (int i = 0; i < milestoneCounts.Length; i++) if (count >= milestoneCounts[i]) n++;
        return n;
    }

    // Siradaki esik (yoksa -1)
    public int GetNextMilestone(BuildingData b)
    {
        int c = GetCount(b);
        for (int i = 0; i < milestoneCounts.Length; i++) if (c < milestoneCounts[i]) return milestoneCounts[i];
        return -1;
    }

    public double MilestoneMultiplier { get { return milestoneMultiplier + permMilestoneBoost; } }

    // Tiklama basina kazanc: temel guc x carpanlar + (varsa) uretimin yuzdesi
    public double ClickValue
    {
        get
        {
            double v = clickPower * clickUpgradeMult * permClick * Multiplier;
            if (clickShare > 0 && CurrentAge != null && CurrentAge.clickResource != null)
                v += clickShare * GetProductionPerSecond(CurrentAge.clickResource);
            return FastTestMode ? v * FastClickMultiplier : v;
        }
    }

    public void ToggleFastTestMode()
    {
        FastTestMode = !FastTestMode;
        RaiseStateChanged();
    }

    // ---- Eylemler ----
    void AddResource(ResourceData r, double amount)
    {
        resources[r.id] = GetResource(r) + amount;
        if (amount > 0)
        {
            double earned;
            runEarned.TryGetValue(r.id, out earned);
            runEarned[r.id] = earned + amount;
        }
    }

    public void Gather()
    {
        if (CurrentAge == null || CurrentAge.clickResource == null) return;
        AddResource(CurrentAge.clickResource, ClickValue);
        TotalClicks++;
        RunClicks++;
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
        int before = GetCount(b);
        counts[b.id] = before + qty;
        TotalBuildingsBought += qty;
        RecalcCaches();
        RaiseStateChanged();   // once gorunumler guncellensin, sonra animasyon baslasin
        if (OnBuildingBought != null) OnBuildingBought(b, qty);
        int nowN = before + qty;
        for (int i = 0; i < milestoneCounts.Length; i++)   // bu alimda gecilen en yuksek esik
            if (before < milestoneCounts[i] && nowN >= milestoneCounts[i] && (i + 1 >= milestoneCounts.Length || nowN < milestoneCounts[i + 1]))
                if (OnMilestoneReached != null) OnMilestoneReached(b, milestoneCounts[i]);
        return qty;
    }

    public AgeData NextAge
    {
        get
        {
            int i = Array.IndexOf(ages, CurrentAge);
            for (int j = i + 1; i >= 0 && j < ages.Length; j++) if (ages[j].side == CurrentAge.side) return ages[j];
            return null;
        }
    }

    public double AdvanceCost { get { return CurrentAge.advanceCost * permAdvanceMult * (FastTestMode ? FastAdvanceCostMultiplier : 1.0); } }

    public bool CanAdvance
    {
        get
        {
            return NextAge != null
                && CurrentAge.advanceResource != null
                && GetRunEarned(CurrentAge.advanceResource) >= AdvanceCost;
        }
    }

    public void AdvanceAge()
    {
        if (!CanAdvance) return;
        var next = NextAge;
        GrantKnowledge(CurrentAge.knowledgeReward);
        AgesAdvanced++;
        ResetRun();
        CurrentAge = next;
        RecalcCaches();
        SaveGame();
        if (OnAgeChanged != null) OnAgeChanged();
        if (OnUpgradesChanged != null) OnUpgradesChanged();
        RaiseStateChanged();
    }

    // Tur sifirlamasi: kaynaklar, binalar, cag ici yukseltmeler. Bilgi ve kalici yukseltmeler kalir.
    void ResetRun()
    {
        resources.Clear();
        runEarned.Clear();
        counts.Clear();
        purchased.Clear(); claimedQuests.Clear(); RunClicks = 0;
    }

    // Ayarlar > "Kaydi Sifirla": her seyi bastan baslatir (kalici yukseltmeler ve Bilgi dahil).
    public void ResetProgress()
    {
        SaveSystem.Delete();
        Knowledge = 0;
        LifetimeKnowledge = 0;
        TimeLoops = 0;
        TotalClicks = TotalBuildingsBought = TotalUpgradesBought = PlaySeconds = 0;
        AgesAdvanced = 0;
        unlockedAchievements.Clear();
        OfflineSecondsApplied = 0;
        resources.Clear();
        runEarned.Clear();
        counts.Clear();
        purchased.Clear(); claimedQuests.Clear(); RunClicks = 0;
        permLevels.Clear();
        CurrentAge = FirstAge();
        RecalcCaches();
        SaveGame();
        if (OnAgeChanged != null) OnAgeChanged();
        if (OnUpgradesChanged != null) OnUpgradesChanged();
        if (OnPermanentChanged != null) OnPermanentChanged();
        RaiseStateChanged();
    }

    double GrantKnowledge(double baseAmount)
    {
        double amount = Math.Floor(baseAmount * permKnowledgeGain + 1e-9);
        if (amount < 0) amount = 0;
        Knowledge += amount;
        LifetimeKnowledge += amount;
        return amount;
    }

    // ---- Cag ici yukseltmeler ----
    public bool IsPurchased(UpgradeData u) { return purchased.Contains(u.id); }

    public bool IsUpgradeVisible(UpgradeData u)
    {
        if (u == null || IsPurchased(u)) return false;
        if (u.prerequisite != null && !IsPurchased(u.prerequisite)) return false;
        if (u.unlockBuilding != null && GetCount(u.unlockBuilding) < u.unlockCount) return false;
        return true;
    }

    // Gorunur (alinabilir ya da yakinda alinabilir) yukseltmeleri, ucuzdan pahaliya dizer
    public void GetVisibleUpgrades(List<UpgradeData> result)
    {
        result.Clear();
        if (CurrentAge == null || CurrentAge.upgrades == null) return;
        foreach (var u in CurrentAge.upgrades) if (IsUpgradeVisible(u)) result.Add(u);
        result.Sort((a, c) => a.cost.CompareTo(c.cost));
    }

    public double GetUpgradeCost(UpgradeData u)
    {
        return u.cost * (FastTestMode ? FastUpgradeCostMultiplier : 1.0);
    }

    public bool CanAffordUpgrade(UpgradeData u)
    {
        return u.costResource != null && GetResource(u.costResource) >= GetUpgradeCost(u);
    }

    public bool TryBuyUpgrade(UpgradeData u)
    {
        if (u == null || !IsUpgradeVisible(u) || !CanAffordUpgrade(u)) return false;
        resources[u.costResource.id] = GetResource(u.costResource) - GetUpgradeCost(u);
        purchased.Add(u.id);
        TotalUpgradesBought++;
        RecalcCaches();
        if (OnUpgradeBought != null) OnUpgradeBought(u);
        if (OnUpgradesChanged != null) OnUpgradesChanged();
        RaiseStateChanged();
        return true;
    }

    // Kayitli yukseltme listesinden onbellegi yeniler
    void RecalcCaches()
    {
        upgradeBuildingMult.Clear();
        clickUpgradeMult = 1; ageGlobalMult = 1; clickShare = 0;
        if (CurrentAge != null && CurrentAge.upgrades != null)
        {
            foreach (var u in CurrentAge.upgrades)
            {
                if (u == null || !purchased.Contains(u.id)) continue;
                switch (u.kind)
                {
                    case UpgradeKind.ClickMultiplier: clickUpgradeMult *= u.value; break;
                    case UpgradeKind.ClickProductionShare: clickShare += u.value; break;
                    case UpgradeKind.GlobalMultiplier: ageGlobalMult *= u.value; break;
                    case UpgradeKind.BuildingMultiplier:
                        if (u.targetBuilding == null) break;
                        double cur;
                        upgradeBuildingMult.TryGetValue(u.targetBuilding.id, out cur);
                        upgradeBuildingMult[u.targetBuilding.id] = (cur == 0 ? 1 : cur) * u.value;
                        break;
                }
            }
        }

        double g = 0, c = 0, cost = 0, adv = 0, kg = 0, off = 0, auto = 0, ms = 0;
        if (permanentUpgrades != null)
        {
            foreach (var p in permanentUpgrades)
            {
                if (p == null) continue;
                int lv = GetPermLevel(p);
                if (lv <= 0) continue;
                double v = p.valuePerLevel * lv;
                switch (p.kind)
                {
                    case PermanentKind.GlobalProduction: g += v; break;
                    case PermanentKind.ClickPower: c += v; break;
                    case PermanentKind.CostReduction: cost += v; break;
                    case PermanentKind.AdvanceDiscount: adv += v; break;
                    case PermanentKind.KnowledgeGain: kg += v; break;
                    case PermanentKind.OfflineHours: off += v; break;
                    case PermanentKind.AutoClick: auto += v; break;
                    case PermanentKind.MilestoneBoost: ms += v; break;
                }
            }
        }
        permGlobal = 1 + g;
        permClick = 1 + c;
        permCostMult = 1 - Math.Min(cost, MaxReduction);
        permAdvanceMult = 1 - Math.Min(adv, MaxReduction);
        permKnowledgeGain = 1 + kg;
        permOfflineHours = off;
        permAutoClick = auto;
        permMilestoneBoost = ms;
    }

    // ---- Kalici yukseltmeler (Bilgi Agaci) ----
    public int GetPermLevel(PermanentUpgradeData p) { int v; return permLevels.TryGetValue(p.id, out v) ? v : 0; }
    public bool IsPermMaxed(PermanentUpgradeData p) { return GetPermLevel(p) >= p.maxLevel; }
    public double GetPermCost(PermanentUpgradeData p) { return p.GetCost(GetPermLevel(p)); }
    public bool IsPermVisible(PermanentUpgradeData p) { return p != null && (p.requires == null || GetPermLevel(p.requires) > 0); }
    public bool CanBuyPerm(PermanentUpgradeData p) { return IsPermVisible(p) && !IsPermMaxed(p) && Knowledge >= GetPermCost(p); }

    public bool TryBuyPerm(PermanentUpgradeData p)
    {
        if (!CanBuyPerm(p)) return false;
        Knowledge -= GetPermCost(p);
        permLevels[p.id] = GetPermLevel(p) + 1;
        RecalcCaches();
        SaveGame();
        if (OnPermanentChanged != null) OnPermanentChanged();
        RaiseStateChanged();
        return true;
    }

    // ---- Basarimlar (kosullari AchievementSystem kontrol eder; durum burada tutulur ve kaydedilir) ----
    public bool IsAchievementUnlocked(string id) { return unlockedAchievements.Contains(id); }

    public bool UnlockAchievement(string id)
    {
        if (string.IsNullOrEmpty(id) || !unlockedAchievements.Add(id)) return false;
        SaveGame();   // basarim kaybolmasin
        if (OnAchievementUnlocked != null) OnAchievementUnlocked(id);
        return true;
    }

    // Tum kalici yukseltmelerin toplam seviyesi (basarim / ipucu kosullari icin)
    public int TotalPermanentLevels
    {
        get
        {
            int n = 0;
            foreach (var kv in permLevels) n += kv.Value;
            return n;
        }
    }

    // ---- Zaman Makinesi ----
    public bool IsFinalAge { get { return NextAge == null; } }

    // Son cagda, cag atlama maliyeti Zaman Makinesi'nin calisma bedeli olur
    public bool CanUseTimeMachine
    {
        get
        {
            return IsFinalAge && CurrentAge.advanceResource != null
                && AdvanceCost > 0 && GetRunEarned(CurrentAge.advanceResource) >= AdvanceCost;
        }
    }

    // Ne kadar fazla biriktirirsen o kadar cok Bilgi (karekok: bekleme getirisi azalir, karar gerektirir)
    public double TimeMachineReward
    {
        get
        {
            if (!IsFinalAge || CurrentAge.advanceResource == null || AdvanceCost <= 0) return 0;
            double ratio = GetRunEarned(CurrentAge.advanceResource) / AdvanceCost;
            if (ratio < 1) return 0;
            return Math.Floor(timeMachineKnowledgeBase * Math.Sqrt(ratio) * permKnowledgeGain + 1e-9);
        }
    }

    public void UseTimeMachine()
    {
        if (!CanUseTimeMachine) return;
        double reward = TimeMachineReward;
        Knowledge += reward;
        LifetimeKnowledge += reward;
        TimeLoops++;
        ResetRun();
        CurrentAge = FirstAge();
        RecalcCaches();
        SaveGame();
        if (OnAgeChanged != null) OnAgeChanged();
        if (OnUpgradesChanged != null) OnUpgradesChanged();
        if (OnTimeMachineUsed != null) OnTimeMachineUsed(reward);
        RaiseStateChanged();
    }

    // ---- Kayit ----
    public void SaveGame()
    {
        var s = new GameState
        {
            lastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            version = GameState.CurrentVersion,
            ageId = CurrentAge.id,
            knowledge = Knowledge,
            lifetimeKnowledge = LifetimeKnowledge,
            timeLoops = TimeLoops,
            totalClicks = TotalClicks,
            totalBuildingsBought = TotalBuildingsBought,
            totalUpgradesBought = TotalUpgradesBought,
            playSeconds = PlaySeconds,
            agesAdvanced = AgesAdvanced,
            side = (int)CurrentSide,
            runClicks = RunClicks
        };
        foreach (var id in claimedQuests) s.claimedQuests.Add(id);
        foreach (var id in unlockedAchievements) s.achievements.Add(id);
        foreach (var kv in resources) s.resources.Add(new NamedDouble { id = kv.Key, value = kv.Value });
        foreach (var kv in runEarned) s.runEarned.Add(new NamedDouble { id = kv.Key, value = kv.Value });
        foreach (var kv in counts) s.buildings.Add(new NamedInt { id = kv.Key, value = kv.Value });
        foreach (var id in purchased) s.upgrades.Add(id);
        foreach (var kv in permLevels) s.permanents.Add(new NamedInt { id = kv.Key, value = kv.Value });
        SaveSystem.Save(s);
    }

    void LoadGame()
    {
        CurrentAge = FirstAge();
        var s = SaveSystem.Load();
        if (s == null) { RecalcCaches(); return; }

        MigrateSave(s);
        CurrentSide = (Side)s.side;
        CurrentAge = FirstAge();

        foreach (var a in ages) if (a.id == s.ageId) CurrentAge = a;
        Knowledge = s.knowledge;
        LifetimeKnowledge = s.lifetimeKnowledge;
        TimeLoops = s.timeLoops;
        TotalClicks = s.totalClicks;
        RunClicks = s.runClicks;
        if (s.claimedQuests != null) foreach (var id in s.claimedQuests) claimedQuests.Add(id);
        TotalBuildingsBought = s.totalBuildingsBought;
        TotalUpgradesBought = s.totalUpgradesBought;
        PlaySeconds = s.playSeconds;
        AgesAdvanced = s.agesAdvanced;
        if (s.achievements != null)
            foreach (var id in s.achievements) unlockedAchievements.Add(id);
        if (s.resources != null)
            foreach (var r in s.resources) resources[r.id] = r.value;
        if (s.runEarned != null)
            foreach (var r in s.runEarned) runEarned[r.id] = r.value;
        if (s.buildings != null)
            foreach (var b in s.buildings) counts[b.id] = b.value;
        if (s.upgrades != null)
            foreach (var id in s.upgrades) purchased.Add(id);
        if (s.permanents != null)
            foreach (var p in s.permanents) permLevels[p.id] = p.value;
        RecalcCaches();

        double elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - s.lastSaveUnix;
        elapsed = Math.Max(0, Math.Min(elapsed, MaxOfflineHours * 3600.0));
        if (elapsed > 1) { OfflineSecondsApplied = elapsed; Produce(elapsed); }
    }

    // Eski kayitlari yeni formata cevirir. Yeni bir kayit alani eklenince burayi genislet.
    public static void MigrateSave(GameState s)
    {
        if (s.version < 2)
        {
            s.lifetimeKnowledge = s.knowledge;   // v1'de Bilgi hem toplam hem bakiyeydi
            s.version = 2;
        }
        if (s.version < 3)
        {
            // Eski kayitta runEarned yok. Mevcut bakiyeyi "bu turda kazanilmis" sayiyoruz;
            // boylece eski oyuncu ilerlemesini kaybetmez.
            s.runEarned = new List<NamedDouble>();
            if (s.resources != null)
                foreach (var r in s.resources)
                    s.runEarned.Add(new NamedDouble { id = r.id, value = r.value });
            s.version = 3;
        }
        if (s.version < 4)
        {
            // v4 alanlari (istatistik, basarim) eski kayitta yok; varsayilan 0 / bos liste dogru baslangic.
            if (s.achievements == null) s.achievements = new List<string>();
            s.version = 4;
        }
    }

    // ---- Veri kontrolu: yanlis girilmis asset'leri baslangicta Console'a yazar ----
    void ValidateData()
    {
        var buildingIds = new HashSet<string>();
        var ageIds = new HashSet<string>();
        var upgradeIds = new HashSet<string>();

        foreach (var a in ages)
        {
            if (a == null) { Debug.LogError("GameManager: ages listesinde bos eleman var."); continue; }
            var ageResourceIds = new HashSet<string>();
            if (string.IsNullOrEmpty(a.id) || a.id != a.id.Trim()) Debug.LogWarning("Cag '" + a.name + "': id bos ya da basinda/sonunda bosluk var.", a);
            if (!ageIds.Add(a.id)) Debug.LogError("Cag id'si tekrar ediyor: " + a.id, a);
            if (a.clickResource == null) Debug.LogWarning("Cag '" + a.name + "': Click Resource bos.", a);
            if (a.advanceResource == null) Debug.LogWarning("Cag '" + a.name + "': Advance Resource bos.", a);

            if (a.resources != null)
                foreach (var r in a.resources)
                {
                    if (r == null) { Debug.LogError("Cag '" + a.name + "': Resources listesinde bos eleman var.", a); continue; }
                    ageResourceIds.Add(r.id);
                }
            if (a.clickResource != null && !ageResourceIds.Contains(a.clickResource.id))
                Debug.LogError("Cag '" + a.name + "': Click Resource kaynak listesinde yok.", a);
            if (a.advanceResource != null && !ageResourceIds.Contains(a.advanceResource.id))
                Debug.LogError("Cag '" + a.name + "': Advance Resource kaynak listesinde yok.", a);

            if (a.upgrades != null)
                foreach (var u in a.upgrades)
                {
                    if (u == null) { Debug.LogError("Cag '" + a.name + "': Upgrades listesinde bos eleman var.", a); continue; }
                    if (string.IsNullOrEmpty(u.id)) Debug.LogWarning("Yukseltme '" + u.name + "': id bos.", u);
                    if (!upgradeIds.Add(u.id)) Debug.LogError("Yukseltme id'si tekrar ediyor: " + u.id, u);
                    if (u.costResource == null) Debug.LogError("Yukseltme '" + u.name + "': Cost Resource bos.", u);
                    else if (!ageResourceIds.Contains(u.costResource.id)) Debug.LogError("Yukseltme '" + u.name + "': maliyet kaynagi bu cagda yok.", u);
                    if (u.kind == UpgradeKind.BuildingMultiplier && u.targetBuilding == null) Debug.LogError("Yukseltme '" + u.name + "': Target Building bos.", u);
                }

            if (a.buildings == null) continue;
            foreach (var b in a.buildings)
            {
                if (b == null) { Debug.LogError("Cag '" + a.name + "': Buildings listesinde bos eleman var.", a); continue; }
                if (string.IsNullOrEmpty(b.id) || b.id != b.id.Trim()) Debug.LogWarning("Bina '" + b.name + "': id bos ya da basinda/sonunda bosluk var.", b);
                if (!buildingIds.Add(b.id)) Debug.LogError("Bina id'si tekrar ediyor: " + b.id, b);
                if (b.produces == null || b.costResource == null) Debug.LogError("Bina '" + b.name + "': Produces ya da Cost Resource bos.", b);
                else if (!ageResourceIds.Contains(b.costResource.id)) Debug.LogError("Bina '" + b.name + "': maliyet kaynagi bu cagda yok.", b);
                if (b.produces != null && !ageResourceIds.Contains(b.produces.id)) Debug.LogError("Bina '" + b.name + "': urettigi kaynak bu cagda yok.", b);
            }
        }
    }

    // ---- Test araclari (Inspector'da bilesenin uc nokta menusunden) ----
    [ContextMenu("DEBUG: Her kaynaktan 1000 ekle")]
    void DebugGrant()
    {
        foreach (var r in CurrentAge.resources) AddResource(r, 1000);
    }

    [ContextMenu("DEBUG: Son caga git")]
    void DebugLastAge()
    {
        ResetRun();
        CurrentAge = ages[ages.Length - 1];
        RecalcCaches();
        if (OnAgeChanged != null) OnAgeChanged();
        if (OnUpgradesChanged != null) OnUpgradesChanged();
        RaiseStateChanged();
    }

    [ContextMenu("DEBUG: 100 Bilgi ekle")]
    void DebugKnowledge() { Knowledge += 100; LifetimeKnowledge += 100; RaiseStateChanged(); }

    [ContextMenu("DEBUG: Kaydi sil")]
    void DebugDeleteSave() { SaveSystem.Delete(); Debug.Log("Kayit silindi."); }
}
