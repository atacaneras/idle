#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu: Evrim > Yayın Öncesi Kontrol (içerik + kayıt migrasyonu)
// Batch: -executeMethod ContentValidator.ValidateBatch
public static class ContentValidator
{
    const string Root = "Assets/GameData";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static int errors;

    [MenuItem("Evrim/Yayın Öncesi Kontrol")]
    public static void RunAll()
    {
        errors = 0;
        ValidateContent(false);
        TestMigrations(false);
        if (errors == 0) Debug.Log("YAYIN ÖNCESİ KONTROL: sorun yok.");
        else Debug.LogError("YAYIN ÖNCESİ KONTROL: " + errors + " sorun bulundu.");
    }

    [MenuItem("Evrim/İçeriği Doğrula")]
    public static void ValidateContent() { ValidateContent(true); }

    [MenuItem("Evrim/Kayıt Migrasyonunu Test Et")]
    public static void TestMigrations() { TestMigrations(true); }

    static void ValidateContent(bool resetErrors)
    {
        if (resetErrors) errors = 0;
        var ages = LoadAll<AgeData>("Caglar");
        var resources = LoadAll<ResourceData>("Kaynaklar");
        var buildings = LoadAll<BuildingData>("Binalar");
        var upgrades = LoadAll<UpgradeData>("Yukseltmeler");
        var perms = LoadAll<PermanentUpgradeData>("Kalici");

        CheckUniqueIds(ages, "çağ");
        CheckUniqueIds(resources, "kaynak");
        CheckUniqueIds(buildings, "bina");
        CheckUniqueIds(upgrades, "yükseltme");
        CheckUniqueIds(perms, "kalıcı yükseltme");

        var resourceSet = new HashSet<ResourceData>(resources);
        var upgradeSet = new HashSet<UpgradeData>(upgrades);
        var permSet = new HashSet<PermanentUpgradeData>(perms);

        foreach (var a in ages)
        {
            if (a == null) { Fail("boş çağ asset'i"); continue; }
            if (a.resources == null || a.resources.Length == 0) Fail(a.name + ": kaynak listesi boş");
            if (a.buildings == null || a.buildings.Length == 0) Fail(a.name + ": bina listesi boş");
            if (a.advanceCost <= 0) Fail(a.name + ": advanceCost <= 0");
            if (a.clickResource == null) Fail(a.name + ": clickResource boş");
            if (a.advanceResource == null) Fail(a.name + ": advanceResource boş");

            var ageResources = new HashSet<ResourceData>(a.resources ?? new ResourceData[0]);
            if (a.clickResource != null && !ageResources.Contains(a.clickResource))
                Fail(a.name + ": clickResource çağ kaynaklarında yok");
            if (a.advanceResource != null && !ageResources.Contains(a.advanceResource))
                Fail(a.name + ": advanceResource çağ kaynaklarında yok");

            foreach (var r in a.resources) if (r == null) Fail(a.name + ": kaynak listesinde boş eleman");
            foreach (var b in a.buildings)
            {
                if (b == null) { Fail(a.name + ": bina listesinde boş eleman"); continue; }
                if (b.produces == null) Fail(b.name + ": produces boş");
                if (b.costResource == null) Fail(b.name + ": costResource boş");
                if (b.baseCost <= 0) Fail(b.name + ": baseCost <= 0");
                if (b.baseProduction <= 0) Fail(b.name + ": baseProduction <= 0");
                if (b.produces != null && !ageResources.Contains(b.produces))
                    Fail(b.name + ": ürettiği kaynak çağ kaynaklarında yok");
            }
            foreach (var u in a.upgrades)
            {
                if (u == null) { Fail(a.name + ": yükseltme listesinde boş eleman"); continue; }
                if (u.costResource == null) Fail(u.name + ": costResource boş");
                if (u.cost <= 0) Fail(u.name + ": cost <= 0");
                if (u.prerequisite != null && !upgradeSet.Contains(u.prerequisite))
                    Fail(u.name + ": ön koşul bu çağda değil");
                if (a.clickResource == null) continue;
                if (u.costResource != null && !ageResources.Contains(u.costResource))
                    Fail(u.name + ": costResource çağ kaynaklarında yok");
            }
        }

        foreach (var p in perms)
        {
            if (p == null) { Fail("boş kalıcı yükseltme asset'i"); continue; }
            if (p.maxLevel <= 0) Fail(p.name + ": maxLevel <= 0");
            if (p.baseCost <= 0) Fail(p.name + ": baseCost <= 0");
            if (p.requires != null && !permSet.Contains(p.requires))
                Fail(p.name + ": ön koşul kalıcı yükseltme bulunamadı");
        }

        // Sahnedeki GameManager bağlantıları
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null && System.IO.File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            gm = Object.FindAnyObjectByType<GameManager>();
        }
        if (gm == null)
        {
            Fail("sahnede GameManager bulunamadı");
        }
        else
        {
            if (gm.Ages == null || gm.Ages.Length == 0) Fail("GameManager.ages boş");
            else
            {
                foreach (var a in gm.Ages) if (a == null) Fail("GameManager.ages içinde boş referans var");
                if (gm.Ages.Length != ages.Count)
                    Fail("GameManager.ages sayısı (" + gm.Ages.Length + ") asset sayısıyla (" + ages.Count + ") uyuşmuyor");
            }
            if (gm.PermanentUpgrades == null || gm.PermanentUpgrades.Length != perms.Count)
                Fail("GameManager.permanentUpgrades sayısı asset sayısıyla uyuşmuyor");
        }

        Debug.Log("İçerik doğrulama: " + ages.Count + " çağ, " + resources.Count + " kaynak, "
            + buildings.Count + " bina, " + upgrades.Count + " yükseltme, " + perms.Count + " kalıcı. Hata: " + errors);
    }

    static void TestMigrations(bool resetErrors)
    {
        if (resetErrors) errors = 0;

        var v1 = new GameState
        {
            version = 1,
            ageId = "stone",
            knowledge = 5,
            resources = new List<NamedDouble> { new NamedDouble { id = "stone", value = 12 } },
            buildings = new List<NamedInt>(),
            upgrades = new List<string>(),
            permanents = new List<NamedInt>()
        };
        GameManager.MigrateSave(v1);
        if (v1.version != GameState.CurrentVersion) Fail("v1 -> güncel sürüm olmadı");
        if (v1.lifetimeKnowledge != 5) Fail("v1: lifetimeKnowledge knowledge'a eşitlenmedi");
        if (!HasRunEarned(v1, "stone", 12)) Fail("v1: runEarned mevcut bakiyeden doldurulmadı");

        var v2 = new GameState
        {
            version = 2,
            ageId = "stone",
            knowledge = 3,
            lifetimeKnowledge = 40,
            resources = new List<NamedDouble> { new NamedDouble { id = "stone", value = 12 } },
            buildings = new List<NamedInt>(),
            upgrades = new List<string>(),
            permanents = new List<NamedInt>()
        };
        GameManager.MigrateSave(v2);
        if (v2.version != GameState.CurrentVersion) Fail("v2 -> güncel sürüm olmadı");
        if (v2.lifetimeKnowledge != 40) Fail("v2: lifetimeKnowledge korunmadı");
        if (!HasRunEarned(v2, "stone", 12)) Fail("v2: runEarned mevcut bakiyeden doldurulmadı");

        var v3 = new GameState
        {
            version = GameState.CurrentVersion,
            ageId = "stone",
            knowledge = 1,
            lifetimeKnowledge = 10,
            resources = new List<NamedDouble>(),
            runEarned = new List<NamedDouble> { new NamedDouble { id = "stone", value = 99 } },
            buildings = new List<NamedInt>(),
            upgrades = new List<string>(),
            permanents = new List<NamedInt>()
        };
        GameManager.MigrateSave(v3);
        if (!HasRunEarned(v3, "stone", 99)) Fail("v3: runEarned üzerine yazıldı");

        Debug.Log("Kayıt migrasyonu testi: hata " + errors);
    }

    // CI icin: hata varsa Unity'yi 1 koduyla kapatir.
    public static void ValidateBatch()
    {
        errors = 0;
        ValidateContent(false);
        TestMigrations(false);
        if (errors > 0) Debug.LogError("BATCH DOĞRULAMA: " + errors + " hata");
        else Debug.Log("BATCH DOĞRULAMA: temiz");
        EditorApplication.Exit(errors > 0 ? 1 : 0);
    }

    // ---- Yardımcılar ----
    static List<T> LoadAll<T>(string folder) where T : ScriptableObject
    {
        var list = new List<T>();
        foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root + "/" + folder }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) list.Add(asset);
        }
        return list;
    }

    static void CheckUniqueIds<T>(List<T> assets, string label) where T : ScriptableObject
    {
        var seen = new HashSet<string>();
        foreach (var a in assets)
        {
            if (a == null) continue;
            var so = new SerializedObject(a);
            var idProp = so.FindProperty("id");
            string id = idProp != null ? idProp.stringValue : a.name;
            if (string.IsNullOrEmpty(id)) { Fail(label + " id boş: " + a.name); continue; }
            if (!seen.Add(id)) Fail("tekrar eden " + label + " id: " + id);
        }
    }

    static bool HasRunEarned(GameState s, string id, double value)
    {
        if (s.runEarned == null) return false;
        foreach (var r in s.runEarned) if (r.id == id) return Mathf.Approximately((float)r.value, (float)value);
        return false;
    }

    static void Fail(string message)
    {
        errors++;
        Debug.LogError("İçerik: " + message);
    }
}
#endif
