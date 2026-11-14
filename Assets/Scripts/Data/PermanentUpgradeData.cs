using UnityEngine;

public enum PermanentKind
{
    GlobalProduction,   // tum uretim +%value (seviye basina)
    ClickPower,         // tiklama gucu +%value (seviye basina)
    CostReduction,      // bina maliyetleri -%value (seviye basina, en fazla %75)
    OfflineHours,       // cevrimdisi ilerleme siniri +value saat (seviye basina)
    AdvanceDiscount,    // cag atlama maliyeti -%value (seviye basina, en fazla %75)
    KnowledgeGain,      // kazanilan Bilgi +%value (seviye basina)
    AutoClick,          // saniyede +value otomatik tiklama (seviye basina)
    MilestoneBoost      // kilometre tasi carpanina +value (seviye basina; 0.1 = x2 yerine x2.1)
}

// Bilgi ile alinan KALICI yukseltme. Zaman Makinesi'nde sifirlanmaz.
// Yuzde degerleri 0.10 = %10 seklinde girilir.
[CreateAssetMenu(menuName = "Evrim/Permanent Upgrade", fileName = "perm_")]
public class PermanentUpgradeData : ScriptableObject
{
    [Header("Kimlik")]
    public string id;            // benzersiz: perm_global
    public string displayName;
    [TextArea] public string description;

    [Header("Etki")]
    public PermanentKind kind = PermanentKind.GlobalProduction;
    public double valuePerLevel = 0.10;
    public int maxLevel = 10;

    [Header("Maliyet (Bilgi)")]
    public double baseCost = 5;
    public double costGrowth = 1.6;

    [Header("Agac")]
    [Tooltip("Bos degilse: bu yukseltmenin en az 1. seviyesi alinmadan gorunmez.")]
    public PermanentUpgradeData requires;

    public double GetCost(int currentLevel) { return System.Math.Ceiling(baseCost * System.Math.Pow(costGrowth, currentLevel)); }

    void OnValidate()
    {
        if (id != null) id = id.Trim();
        if (maxLevel < 1) maxLevel = 1;
    }
}
