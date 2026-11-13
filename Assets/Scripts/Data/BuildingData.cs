using UnityEngine;

[CreateAssetMenu(menuName = "Evrim/Building", fileName = "bld_")]
public class BuildingData : ScriptableObject
{
    [Header("Kimlik")]
    public string id;            // benzersiz, tum caglar arasinda: stone_gatherer
    public string displayName;
    public Sprite icon;

    [Header("Ekonomi")]
    public ResourceData produces;
    public ResourceData costResource;
    public double baseCost = 10;
    public double costGrowth = 1.15;
    public double baseProduction = 0.5;   // adet basina saniyede

    [Header("Sahne gorunumu")]
    [Tooltip("Seviye 1, 2, 3 sprite'lari (stone_bld_xxx_lv1..3). Bos birakilirsa placeholder kutu cizilir.")]
    public Sprite[] levelSprites;
    public Vector2 scenePosition = new Vector2(0, -3.2f);
    public int sortingOrder = 0;

    [Header("Placeholder (sprite yokken)")]
    public Color placeholderColor = Color.gray;
    public Vector2 placeholderSize = new Vector2(1.2f, 1.2f);

    public Sprite GetSprite(int level)
    {
        if (levelSprites == null || levelSprites.Length == 0) return null;
        return levelSprites[Mathf.Clamp(level, 0, levelSprites.Length - 1)];
    }

    void OnValidate()
    {
        if (id != null) id = id.Trim();   // yanlislikla yazilan bosluk/tab kayit sistemini bozmasin
    }
}
