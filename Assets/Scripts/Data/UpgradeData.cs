using UnityEngine;

public enum UpgradeKind
{
    ClickMultiplier,      // tiklama gucu x value
    ClickProductionShare, // her tiklama, tiklama kaynaginin saniyelik uretiminin (value) kadarini ekler (0.01 = %1)
    BuildingMultiplier,   // hedef binanin uretimi x value
    GlobalMultiplier      // bu cagdaki tum binalarin uretimi x value
}

// Cag ici tek seferlik yukleme. Cag atlayinca / Zaman Makinesi'nde sifirlanir.
[CreateAssetMenu(menuName = "Cennet ve Cehennem/Yukseltme", fileName = "yukseltme_")]
public class UpgradeData : ScriptableObject
{
    [Header("Kimlik")]
    public string id;              // benzersiz, tum caglar arasinda: stone_sharp_rock
    public string displayName;     // Daha Sivri Tas
    [TextArea] public string description;   // esprili aciklama / ne yaptigi
    public Sprite icon;

    [Header("Etki")]
    public UpgradeKind kind = UpgradeKind.ClickMultiplier;
    public BuildingData targetBuilding;     // sadece BuildingMultiplier icin
    public double value = 2;

    [Header("Maliyet")]
    public ResourceData costResource;
    public double cost = 100;

    [Header("Ne zaman gorunur")]
    [Tooltip("Bos degilse: bu bina unlockCount adede ulasinca gorunur.")]
    public BuildingData unlockBuilding;
    public int unlockCount = 1;
    [Tooltip("Bos degilse: bu yukseltme alindiktan sonra gorunur (zincir icin).")]
    public UpgradeData prerequisite;

    void OnValidate()
    {
        if (id != null) id = id.Trim();   // yanlislikla yazilan bosluk/tab kayit sistemini bozmasin
    }
}
