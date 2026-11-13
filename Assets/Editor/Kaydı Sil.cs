#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ResetSaveMenu
{
    [MenuItem("Evrim/Kaydı Sil")]
    static void DeleteSave()
    {
        SaveSystem.Delete();
        PlayerPrefs.DeleteAll();
        Debug.Log("Kayıt silindi.");
    }
}
#endif