using System;
using System.Collections.Generic;
using UnityEngine;

// Anahtar = Turkce metnin kendisi. Ceviriler Resources/Localization/en.txt dosyasinda:
//   Turkce metin|English text
// Ceviri bulunamazsa Turkce metin aynen gosterilir; boylece eksik ceviri oyunu bozmaz.
public static class Loc
{
    public const string Turkish = "tr";
    public const string English = "en";

    const string PrefKey = "evrim_lang";
    const string ResourcePath = "Localization/en";

    static Dictionary<string, string> english;
    static bool initialized;
    static string language;

    public static event Action OnLanguageChanged;

    public static string Language
    {
        get { EnsureInit(); return language; }
    }

    public static bool IsEnglish { get { return Language == English; } }

    public static void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        language = PlayerPrefs.GetString(PrefKey, DefaultLanguage());
        if (language != English) language = Turkish;
        LoadEnglish();
    }

    static string DefaultLanguage()
    {
        // Turkce sistem disinda Ingilizce baslar. Steam'de cogu oyuncu Ingilizce bekler.
        return Application.systemLanguage == SystemLanguage.Turkish ? Turkish : English;
    }

    static void LoadEnglish()
    {
        english = new Dictionary<string, string>();
        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("Loc: ceviri dosyasi bulunamadi (" + ResourcePath + "). Turkce kullanilacak.");
            return;
        }

        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            int i = line.IndexOf('|');
            if (i <= 0 || i >= line.Length - 1) continue;
            // Dosyada satir sonu "\n" ile yazilir; burada gercek satir sonuna cevrilir.
            string key = Unescape(line.Substring(0, i).Trim());
            string value = Unescape(line.Substring(i + 1).Trim());
            if (key.Length == 0 || value.Length == 0) continue;
            english[key] = value;
        }
    }

    static string Unescape(string s)
    {
        return s.IndexOf('\\') < 0 ? s : s.Replace("\\n", "\n").Replace("\\t", "\t");
    }

    public static string T(string turkish)
    {
        if (string.IsNullOrEmpty(turkish)) return "";
        EnsureInit();
        if (language != English) return turkish;
        string value;
        return english.TryGetValue(turkish, out value) ? value : turkish;
    }

    // T + string.Format: yer tutucu iceren metinler icin.
    public static string F(string turkish, params object[] args)
    {
        string pattern = T(turkish);
        try { return string.Format(pattern, args); }
        catch (FormatException) { return pattern; }
    }

    public static void SetLanguage(string lang)
    {
        EnsureInit();
        lang = lang == English ? English : Turkish;
        if (lang == language) return;
        language = lang;
        PlayerPrefs.SetString(PrefKey, lang);
        PlayerPrefs.Save();
        if (OnLanguageChanged != null) OnLanguageChanged();
    }

    public static void Toggle() { SetLanguage(IsEnglish ? Turkish : English); }

}
