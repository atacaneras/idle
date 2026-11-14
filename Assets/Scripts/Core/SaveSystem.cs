using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    const string FileName = "save.json";

    static string FilePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }
    static string BackupPath { get { return FilePath + ".bak"; } }
    static string TempPath { get { return FilePath + ".tmp"; } }

    // Ana kayit bozuksa yedekten yuklendiginde true olur. HUD oyuncuya haber verir.
    public static bool LastLoadUsedBackup { get; private set; }

    public static void Save(GameState state)
    {
        try
        {
            string json = JsonUtility.ToJson(state);
            File.WriteAllText(TempPath, json);

            if (File.Exists(FilePath))
            {
                // Once eskisini yedege al, sonra yeniyi yerine koy. Yazma sirasinda cokersek
                // hem eski kayit hem yedek saglam kalir.
                try { File.Replace(TempPath, FilePath, BackupPath); }
                catch (Exception)
                {
                    File.Copy(FilePath, BackupPath, true);
                    File.Delete(FilePath);
                    File.Move(TempPath, FilePath);
                }
            }
            else
            {
                File.Move(TempPath, FilePath);
            }
        }
        catch (Exception e) { Debug.LogError("Save failed: " + e); }
    }

    public static GameState Load()
    {
        LastLoadUsedBackup = false;

        var main = TryLoad(FilePath);
        if (IsValid(main)) return main;

        if (File.Exists(BackupPath))
        {
            var backup = TryLoad(BackupPath);
            if (IsValid(backup))
            {
                LastLoadUsedBackup = true;
                Debug.LogWarning("Ana kayit okunamadi, yedekten yuklendi: " + BackupPath);
                return backup;
            }
        }

        if (main != null || File.Exists(FilePath))
            Debug.LogError("Kayit dosyasi gecersiz ve kullanilabilir yedek yok: " + FilePath);
        return null;
    }

    public static void Delete()
    {
        SafeDelete(FilePath);
        SafeDelete(BackupPath);
        SafeDelete(TempPath);
    }

    // Destek icin: kaydi zaman damgali bir dosyaya kopyalar, tam yolu dondurur.
    public static string ExportCopy()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            string name = "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            string target = Path.Combine(Application.persistentDataPath, name);
            File.Copy(FilePath, target, true);
            return target;
        }
        catch (Exception e)
        {
            Debug.LogError("Save export failed: " + e);
            return null;
        }
    }

    public static string SaveFolder { get { return Application.persistentDataPath; } }

    static GameState TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonUtility.FromJson<GameState>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed (" + path + "): " + e.Message);
            return null;
        }
    }

    // Bozuk/yarim kayitlarda JsonUtility bazen alanlari bos birakir; temel alanlari kontrol et.
    static bool IsValid(GameState s)
    {
        if (s == null) return false;
        if (s.version < 1) return false;
        if (string.IsNullOrEmpty(s.ageId)) return false;
        if (s.resources == null || s.buildings == null || s.upgrades == null || s.permanents == null) return false;
        if (double.IsNaN(s.knowledge) || double.IsInfinity(s.knowledge)) return false;
        if (double.IsNaN(s.lifetimeKnowledge) || double.IsInfinity(s.lifetimeKnowledge)) return false;
        return true;
    }

    static void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception e) { Debug.LogWarning("Could not delete " + path + ": " + e.Message); }
    }
}
