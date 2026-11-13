using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    static string FilePath { get { return Path.Combine(Application.persistentDataPath, "save.json"); } }

    public static void Save(GameState state)
    {
        try
        {
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(state));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);   // yazma sirasinda cokerse eski kayit bozulmaz
        }
        catch (Exception e) { Debug.LogError("Save failed: " + e); }
    }

    public static GameState Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            return JsonUtility.FromJson<GameState>(File.ReadAllText(FilePath));
        }
        catch (Exception e) { Debug.LogError("Load failed: " + e); return null; }
    }

    public static void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}
