#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Evrim > Icerik Uret (JSON)
// Tek kaynak: Assets/GameData/Content/content.json
// Bu script JSON'u okur, ScriptableObject asset'lerini gunceller/olusturur ve
// sahnedeki GameManager listelerini JSON sirasina gore yeniden baglar.
// Sprite / ikon alanlarina dokunmaz; grafikci atadiktan sonra da calistirilabilir.
public static class StarterContentGenerator
{
    const string Root = "Assets/GameData";
    const string ContentPath = Root + "/Content/content.json";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Evrim/İçerik Üret (JSON)")]
    public static void Generate()
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), ContentPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError("Icerik dosyasi bulunamadi: " + ContentPath);
            return;
        }

        ContentFile content;
        try { content = JsonUtility.FromJson<ContentFile>(File.ReadAllText(fullPath)); }
        catch (Exception e) { Debug.LogError("JSON okunamadi: " + e.Message); return; }
        if (content == null || content.ages == null || content.ages.Length == 0)
        {
            Debug.LogError("JSON bos ya da gecersiz: " + ContentPath);
            return;
        }

        Validate(content);

        used.Clear();
        var resources = new Dictionary<string, ResourceData>();
        var buildings = new Dictionary<string, BuildingData>();
        var upgrades = new Dictionary<string, UpgradeData>();
        var perms = new Dictionary<string, PermanentUpgradeData>();
        var ages = new List<AgeData>();

        // 1) Kaynaklar
        foreach (var r in content.resources)
        {
            var asset = LoadOrCreate<ResourceData>("Kaynaklar", r.asset);
            asset.id = r.id;
            asset.displayName = r.displayName;
            EditorUtility.SetDirty(asset);
            resources[r.id] = asset;
        }

        // 2) Binalar
        foreach (var age in content.ages)
        {
            foreach (var b in age.buildings)
            {
                var asset = LoadOrCreate<BuildingData>("Binalar", b.asset);
                asset.id = b.id;
                asset.artFolder = age.id;
                asset.displayName = b.displayName;
                asset.produces = Get(resources, b.produces, "bina " + b.id + " produces");
                asset.costResource = Get(resources, b.costResource, "bina " + b.id + " costResource");
                asset.baseCost = b.baseCost;
                asset.costGrowth = b.costGrowth > 0 ? b.costGrowth : 1.15;
                asset.baseProduction = b.baseProduction;
                asset.scenePosition = new Vector2(b.scenePosition[0], b.scenePosition[1]);
                asset.sortingOrder = b.sortingOrder;
                asset.placeholderColor = new Color(b.placeholderColor[0], b.placeholderColor[1], b.placeholderColor[2], 1f);
                asset.placeholderSize = b.placeholderSize != null && b.placeholderSize.Length >= 2
                    ? new Vector2(b.placeholderSize[0], b.placeholderSize[1])
                    : new Vector2(1.2f, 1.2f);
                EditorUtility.SetDirty(asset);
                buildings[b.id] = asset;
            }
        }

        // 3) Cag ici yukseltmeler
        foreach (var age in content.ages)
        {
            foreach (var u in age.upgrades)
            {
                var asset = LoadOrCreate<UpgradeData>("Yukseltmeler", u.asset);
                asset.id = u.id;
                asset.displayName = u.displayName;
                asset.description = u.description;
                asset.kind = (UpgradeKind)Enum.Parse(typeof(UpgradeKind), u.kind);
                asset.value = u.value;
                asset.targetBuilding = string.IsNullOrEmpty(u.targetBuilding) ? null : Get(buildings, u.targetBuilding, "yukseltme " + u.id + " targetBuilding");
                asset.costResource = Get(resources, u.costResource, "yukseltme " + u.id + " costResource");
                asset.cost = u.cost;
                asset.unlockBuilding = string.IsNullOrEmpty(u.unlockBuilding) ? null : Get(buildings, u.unlockBuilding, "yukseltme " + u.id + " unlockBuilding");
                asset.unlockCount = u.unlockCount;
                asset.prerequisite = null;   // 4. adimda baglanir
                EditorUtility.SetDirty(asset);
                upgrades[u.id] = asset;
            }
        }

        // 4) Onkosul baglantilari (tum asset'ler olustuktan sonra)
        foreach (var age in content.ages)
        {
            foreach (var u in age.upgrades)
            {
                if (string.IsNullOrEmpty(u.prerequisite)) continue;
                UpgradeData pre;
                if (upgrades.TryGetValue(u.prerequisite, out pre)) upgrades[u.id].prerequisite = pre;
                else Debug.LogError("Yukseltme onkosulu bulunamadi: " + u.id + " -> " + u.prerequisite);
            }
        }

        // 5) Kalici yukseltmeler
        foreach (var p in content.permanentUpgrades)
        {
            var asset = LoadOrCreate<PermanentUpgradeData>("Kalici", p.asset);
            asset.id = p.id;
            asset.displayName = p.displayName;
            asset.description = p.description;
            asset.kind = (PermanentKind)Enum.Parse(typeof(PermanentKind), p.kind);
            asset.valuePerLevel = p.valuePerLevel;
            asset.maxLevel = p.maxLevel;
            asset.baseCost = p.baseCost;
            asset.costGrowth = p.costGrowth;
            asset.requires = null;
            EditorUtility.SetDirty(asset);
            perms[p.id] = asset;
        }
        foreach (var p in content.permanentUpgrades)
        {
            if (string.IsNullOrEmpty(p.requires)) continue;
            PermanentUpgradeData req;
            if (perms.TryGetValue(p.requires, out req)) perms[p.id].requires = req;
            else Debug.LogError("Kalici onkosulu bulunamadi: " + p.id + " -> " + p.requires);
        }

        // 6) Caglar
        foreach (var a in content.ages)
        {
            var asset = LoadOrCreate<AgeData>("Caglar", a.asset);
            asset.id = a.id;
            asset.displayName = a.displayName;
            asset.side = a.side == "hell" ? Side.Hell : a.side == "heaven" ? Side.Heaven : Side.None;
            asset.clickLabel = a.clickLabel;
            asset.goalTitle = a.goalTitle;
            asset.resources = Map(a.resources, resources, "cag " + a.id + " resources");
            asset.buildings = MapEntries(a.buildings, x => x.id, buildings, "cag " + a.id + " buildings");
            asset.upgrades = MapEntries(a.upgrades, x => x.id, upgrades, "cag " + a.id + " upgrades");
            var qlist = new List<QuestDef>();
            if (a.quests != null)
                foreach (var q in a.quests)
                    qlist.Add(new QuestDef
                    {
                        id = q.id, text = q.text, kind = (QuestKind)Enum.Parse(typeof(QuestKind), q.kind),
                        building = string.IsNullOrEmpty(q.building) ? null : Get(buildings, q.building, "gorev " + q.id),
                        amount = q.amount, rewardFlat = q.rewardFlat, rewardSeconds = q.rewardSeconds, rewardKnowledge = q.rewardKnowledge
                    });
            asset.quests = qlist.ToArray();
            asset.clickResource = Get(resources, a.clickResource, "cag " + a.id + " clickResource");
            asset.advanceResource = Get(resources, a.advanceResource, "cag " + a.id + " advanceResource");
            asset.advanceCost = a.advanceCost;
            asset.knowledgeReward = a.knowledgeReward;   // 0 dahil her degeri yazar
            asset.skyColor = new Color(a.skyColor[0], a.skyColor[1], a.skyColor[2], 1f);
            asset.groundColor = new Color(a.groundColor[0], a.groundColor[1], a.groundColor[2], 1f);
            EditorUtility.SetDirty(asset);
            ages.Add(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CleanOrphans();

        var permList = new List<PermanentUpgradeData>();
        foreach (var p in content.permanentUpgrades) permList.Add(perms[p.id]);
        AssignToGameManager(ages, permList, content.game);
        ArtGuide.Generate();
        Debug.Log("Icerik uretildi: " + ages.Count + " cag, " + buildings.Count + " bina, "
            + upgrades.Count + " yukseltme, " + perms.Count + " kalici yukseltme.");
    }

    // Eski menu adi da ayni hatta baglansin.
    [MenuItem("Evrim/Başlangıç İçeriğini Oluştur")]
    static void GenerateLegacy() { Generate(); }

    // ---- Yardimcilar ----
    static readonly HashSet<string> used = new HashSet<string>();

    // Yeni JSON'da olmayan eski kaynak/bina/yukseltme/cag asset'lerini siler (eski icerigi temizler).
    static void CleanOrphans()
    {
        int n = 0;
        foreach (var folder in new[] { "Kaynaklar", "Binalar", "Yukseltmeler", "Caglar" })
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { Root + "/" + folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!used.Contains(p) && AssetDatabase.DeleteAsset(p)) n++;
            }
        Debug.Log("Eski icerik temizlendi: " + n + " asset silindi.");
    }

    static T LoadOrCreate<T>(string folder, string assetName) where T : ScriptableObject
    {
        string path = Root + "/" + folder + "/" + assetName + ".asset";
        used.Add(path);
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static T Get<T>(Dictionary<string, T> map, string id, string where) where T : UnityEngine.Object
    {
        T value;
        if (map.TryGetValue(id, out value)) return value;
        Debug.LogError("Referans bulunamadi (" + where + "): " + id);
        return null;
    }

    static T[] Map<T>(string[] ids, Dictionary<string, T> map, string where) where T : UnityEngine.Object
    {
        return MapEntries(ids, x => x, map, where);
    }

    // BuildingEntry[] / UpgradeEntry[] gibi nesne dizilerinden id cikarip esler.
    static T[] MapEntries<TEntry, T>(TEntry[] entries, Func<TEntry, string> idOf, Dictionary<string, T> map, string where)
        where T : UnityEngine.Object
    {
        var list = new List<T>();
        if (entries == null) return list.ToArray();
        foreach (var entry in entries)
        {
            string id = idOf(entry);
            T value;
            if (map.TryGetValue(id, out value)) list.Add(value);
            else Debug.LogError("Referans bulunamadi (" + where + "): " + id);
        }
        return list.ToArray();
    }

    static void AssignToGameManager(List<AgeData> ages, List<PermanentUpgradeData> permList, GameEntry game)
    {
        var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            if (File.Exists(ScenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
                gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            }
        }
        if (gm == null)
        {
            Debug.LogWarning("GameManager bulunamadi. Ages ve permanentUpgrades listelerini elle bagla.");
            return;
        }

        var so = new SerializedObject(gm);
        var agesProp = so.FindProperty("ages");
        agesProp.arraySize = ages.Count;
        for (int i = 0; i < ages.Count; i++) agesProp.GetArrayElementAtIndex(i).objectReferenceValue = ages[i];

        var permsProp = so.FindProperty("permanentUpgrades");
        permsProp.arraySize = permList.Count;
        for (int i = 0; i < permList.Count; i++) permsProp.GetArrayElementAtIndex(i).objectReferenceValue = permList[i];

        if (game != null)
        {
            var milestoneProp = so.FindProperty("milestoneCounts");
            milestoneProp.arraySize = game.milestoneCounts.Length;
            for (int i = 0; i < game.milestoneCounts.Length; i++)
                milestoneProp.GetArrayElementAtIndex(i).intValue = game.milestoneCounts[i];
            so.FindProperty("milestoneMultiplier").doubleValue = game.milestoneMultiplier;
            so.FindProperty("timeMachineKnowledgeBase").doubleValue = game.timeMachineKnowledgeBase;
            so.FindProperty("lifetimeKnowledgeBonus").doubleValue = game.lifetimeKnowledgeBonus;
            so.FindProperty("maxOfflineHours").floatValue = game.maxOfflineHours;
            so.FindProperty("autosaveInterval").floatValue = game.autosaveInterval;
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        EditorSceneManager.SaveScene(gm.gameObject.scene);
    }

    // ---- JSON semasi ----
    [Serializable] public class ContentFile
    {
        public int version;
        public GameEntry game;
        public ResourceEntry[] resources;
        public AgeEntry[] ages;
        public PermEntry[] permanentUpgrades;
    }

    [Serializable] public class GameEntry
    {
        public int[] milestoneCounts;
        public double milestoneMultiplier;
        public double timeMachineKnowledgeBase;
        public double lifetimeKnowledgeBonus;
        public float maxOfflineHours;
        public float autosaveInterval;
    }

    [Serializable] public class ResourceEntry
    {
        public string id;
        public string displayName;
        public string asset;
    }

    [Serializable] public class AgeEntry
    {
        public string id;
        public string asset;
        public string displayName;
        public string side;
        public string clickLabel;
        public string goalTitle;
        public string clickResource;
        public string advanceResource;
        public double advanceCost;
        public double knowledgeReward;
        public string[] resources;
        public float[] skyColor;
        public float[] groundColor;
        public BuildingEntry[] buildings;
        public QuestEntry[] quests;
        public UpgradeEntry[] upgrades;
    }

    [Serializable] public class BuildingEntry
    {
        public string id;
        public string asset;
        public string displayName;
        public string produces;
        public string costResource;
        public double baseCost;
        public double costGrowth;
        public double baseProduction;
        public float[] scenePosition;
        public int sortingOrder;
        public float[] placeholderColor;
        public float[] placeholderSize;
    }

    [Serializable] public class QuestEntry
    {
        public string id;
        public string text;
        public string kind;
        public string building;
        public double amount;
        public double rewardFlat;
        public double rewardSeconds;
        public double rewardKnowledge;
    }

    [Serializable] public class UpgradeEntry
    {
        public string id;
        public string asset;
        public string displayName;
        public string description;
        public string kind;
        public double value;
        public string targetBuilding;
        public string costResource;
        public double cost;
        public string unlockBuilding;
        public int unlockCount;
        public string prerequisite;
    }

    [Serializable] public class PermEntry
    {
        public string id;
        public string asset;
        public string displayName;
        public string description;
        public string kind;
        public double valuePerLevel;
        public int maxLevel;
        public double baseCost;
        public double costGrowth;
        public string requires;
    }

    // ---- Basit icerik dogrulama ----
    static void Validate(ContentFile content)
    {
        var resourceIds = new HashSet<string>();
        foreach (var r in content.resources)
            if (!resourceIds.Add(r.id)) Debug.LogError("Kaynak id'si tekrar ediyor: " + r.id);

        var buildingIds = new HashSet<string>();
        var upgradeIds = new HashSet<string>();
        var ageIds = new HashSet<string>();
        var permIds = new HashSet<string>();

        foreach (var a in content.ages)
        {
            if (!ageIds.Add(a.id)) Debug.LogError("Cag id'si tekrar ediyor: " + a.id);
            var ageResources = new HashSet<string>(a.resources ?? new string[0]);
            if (!ageResources.Contains(a.clickResource)) Debug.LogError("Cag " + a.id + ": clickResource cag kaynaklarinda yok.");
            if (!ageResources.Contains(a.advanceResource)) Debug.LogError("Cag " + a.id + ": advanceResource cag kaynaklarinda yok.");
            if (a.advanceCost <= 0) Debug.LogError("Cag " + a.id + ": advanceCost sifir/negatif.");

            foreach (var b in a.buildings)
            {
                if (!buildingIds.Add(b.id)) Debug.LogError("Bina id'si tekrar ediyor: " + b.id);
                if (!ageResources.Contains(b.produces)) Debug.LogError("Bina " + b.id + ": produces cag kaynaklarinda yok.");
                if (!ageResources.Contains(b.costResource)) Debug.LogError("Bina " + b.id + ": costResource cag kaynaklarinda yok.");
            }
            foreach (var u in a.upgrades)
            {
                if (!upgradeIds.Add(u.id)) Debug.LogError("Yukseltme id'si tekrar ediyor: " + u.id);
                if (!ageResources.Contains(u.costResource)) Debug.LogError("Yukseltme " + u.id + ": costResource cag kaynaklarinda yok.");
            }
        }

        foreach (var p in content.permanentUpgrades)
            if (!permIds.Add(p.id)) Debug.LogError("Kalici yukseltme id'si tekrar ediyor: " + p.id);

        Debug.Log("Icerik dogrulama bitti: " + content.ages.Length + " cag.");
    }
}
#endif
