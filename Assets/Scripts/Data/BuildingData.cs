using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Evrim/Building", fileName = "bld_")]
public class BuildingData : ScriptableObject
{
    [Header("Kimlik")]
    [HideInInspector] public string artFolder;   // Resources/Art/<artFolder>/ (icerik uretici doldurur)
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

    [System.NonSerialized] Sprite[] autoSprites;
    [System.NonSerialized] bool autoTried;

    // Elle atanmis sprite yoksa Assets/Resources/Art/<kat>/<bina_id>_lv1..3.png (ya da <bina_id>.png) aranir.
    public Sprite GetSprite(int level)
    {
        if (levelSprites != null && levelSprites.Length > 0)
            return levelSprites[Mathf.Clamp(level, 0, levelSprites.Length - 1)];
        if (!autoTried) { autoTried = true; LoadAuto(); }
        if (autoSprites == null || autoSprites.Length == 0) return null;
        return autoSprites[Mathf.Clamp(level, 0, autoSprites.Length - 1)];
    }

    void LoadAuto()
    {
        if (string.IsNullOrEmpty(artFolder) || string.IsNullOrEmpty(id)) return;
        var list = new List<Sprite>();
        for (int lv = 1; lv <= 3; lv++)
        {
            var s = Resources.Load<Sprite>("Art/" + artFolder + "/" + id + "_lv" + lv);
            if (s == null) break;
            list.Add(s);
        }
        if (list.Count == 0)
        {
            var s = Resources.Load<Sprite>("Art/" + artFolder + "/" + id);
            if (s != null) list.Add(s);
        }
        autoSprites = list.ToArray();
    }

    void OnValidate()
    {
        if (id != null) id = id.Trim();   // yanlislikla yazilan bosluk/tab kayit sistemini bozmasin
    }
}
