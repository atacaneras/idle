using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Assets/Resources/Art/UI, Art/Icons ve kat klasorlerindeki arayuz gorsellerini tek yerden yukler.
// Dosya yoksa null doner; cagiran kod eski duz renkli gorunume duser, yani oyun hicbir zaman bozulmaz.
// Hangi gorselin nerede kullanilacagini degistirmek icin SADECE bu dosyadaki adlari duzelt.
public static class UIArt
{
    // ---- Art/UI ----
    public const string TitlePlaque = "Art/UI/Level_UI";         // ust ortadaki kat adi tabelasi
    public const string TheaterFrame = "Art/UI/Curtain_Frame";   // taraf secim ana menusundeki sahne cercevesi
    public const string ResourcePill = "Art/UI/kazanım_UI";      // sol ustteki kaynak hapi ve sag ustteki Bilgi hapi
    public const string ListPanel = "Art/UI/Panel";              // bina listesinin arkasindaki buyuk dikey parsomen (1424x1668)
    public const string Card = "Art/UI/Panel_UI";                // tek bir bina karti (1174x296)
    public const string ClaimReady = "Art/UI/Claim_Ready";       // AL (alinabilir)
    public const string ClaimUnready = "Art/UI/Claim_Unready";   // AL (yetersiz kaynak)
    public const string TapOuter = "Art/UI/Tap_Button_UI_Out";   // ana buton: dis cerceve
    public const string TapInner = "Art/UI/Tap_Button_UI_In";    // ana buton: ic yuzey
    public const string TapPressed = "Art/UI/Tap_Button_UI_In_Tappin";   // ana buton: basili ic yuzey
    public const string ModeOn = "Art/UI/x_tapping";             // x1/x10/x100/Max secili
    public const string ModeOff = "Art/UI/x_untapping";          // x1/x10/x100/Max secili degil

    // ---- Art/Icons ----
    public const string IconSin = "Art/Icons/Günah_ikon";
    public const string IconToil = "Art/Icons/cefa_ikon";
    public const string IconSoul = "Art/Icons/ruh_ikon";
    public const string IconSettings = "Art/Icons/ayarlar_ikon";
    public const string SettingsButton = "Art/Icons/Settings_Button_S";

    // Gorsellerin uzerinde yazi zaten VARSA true yap; kod ayni yaziyi ikinci kez basmaz.
    public static readonly bool ClaimSpriteHasText = false;
    public static readonly bool ModeSpriteHasText = false;

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    static readonly Dictionary<string, Sprite[]> folders = new Dictionary<string, Sprite[]>();

    public static Sprite Load(string path)
    {
        Sprite s;
        if (cache.TryGetValue(path, out s)) return s;
        s = Resources.Load<Sprite>(path);
        if (s == null) s = LoadFuzzy(path);   // dosya adinda Turkce karakter / buyuk-kucuk harf farki olsa da bul
        cache[path] = s;
        return s;
    }

    static Sprite LoadFuzzy(string path)
    {
        int slash = path.LastIndexOf('/');
        if (slash < 0) return null;
        string folder = path.Substring(0, slash);
        Sprite[] all;
        if (!folders.TryGetValue(folder, out all)) { all = Resources.LoadAll<Sprite>(folder); folders[folder] = all; }
        string want = Normalize(path.Substring(slash + 1));
        foreach (var sp in all) if (Normalize(sp.name) == want) return sp;
        return null;
    }

    // "Günah_ikon" == "gunah_ikon" == "G#U00fcnah_ikon" benzeri adlari eslestirir
    static string Normalize(string n)
    {
        n = n.ToLowerInvariant().Replace("#u00fc", "u").Replace("#u0131", "i").Replace("#u00f6", "o")
            .Replace("#u015f", "s").Replace("#u00e7", "c").Replace("#u011f", "g");
        var sb = new System.Text.StringBuilder();
        foreach (char c in n.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            switch (c)
            {
                case 'ı': sb.Append('i'); break;
                case 'ü': sb.Append('u'); break;
                case 'ö': sb.Append('o'); break;
                case 'ş': sb.Append('s'); break;
                case 'ç': sb.Append('c'); break;
                case 'ğ': sb.Append('g'); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }


    // ---- Kat arka plani ----
    // Sanatci dosyayi "background.png" yerine "Hell_01_background.png" gibi adlandirsa da bulunur.
    static readonly Dictionary<string, Sprite> floorBackgrounds = new Dictionary<string, Sprite>();

    public static Sprite FloorBackground(string key)
    {
        Sprite found;
        if (floorBackgrounds.TryGetValue(key, out found)) return found;

        string folder = "Art/" + key;
        found = Resources.Load<Sprite>(folder + "/background");
        if (found == null)
        {
            float bestScore = 0f;
            foreach (var sp in Resources.LoadAll<Sprite>(folder))
            {
                string n = sp.name.ToLowerInvariant();
                if (n.Contains("boss") || n.Contains("ikon") || n.Contains("icon") || n.Contains("_lv") ||
                    n.Contains("iblis") || n.Contains("spine") || n.Contains("ghost") || n.Contains("export") || n.Contains("atlas"))
                    continue;
                float w = sp.rect.width, h = sp.rect.height;
                if (w < 800f || h < 450f || w / h < 1.2f) continue;   // arka plan genis ve buyuk bir gorsel olmali
                float score = w * h + (n.Contains("background") ? 1e9f : 0f);
                if (score > bestScore) { bestScore = score; found = sp; }
            }
        }
        floorBackgrounds[key] = found;
        return found;
    }

    public static bool Has(string path) { return Load(path) != null; }

    // Kaynak kimligine gore ikon (kaynak ikonu yoksa null)
    public static Sprite ResourceIcon(ResourceData r)
    {
        if (r == null) return null;
        if (r.icon != null) return r.icon;
        switch (r.id)
        {
            case "sin": return Load(IconSin);
            case "toil": case "cefa": return Load(IconToil);
            case "soul": case "ruh": return Load(IconSoul);
        }
        return null;
    }

    // hell_1 bina ikonlari: bina kimligi -> klasordeki dosya. Yeni kat ikonlari buraya eklenir.
    static readonly Dictionary<string, string> buildingIcons = new Dictionary<string, string>
    {
        { "hell_1_korna",    "Art/hell_1/vesvese_ikon" },
        { "hell_1_bant",     "Art/hell_1/itaat_ikon" },
        { "hell_1_fotokopi", "Art/hell_1/gunah_ikon" },
        { "hell_1_asansor",  "Art/hell_1/bekci_ikon" },
        { "hell_1_tabela",   "Art/hell_1/iskence_ikon" },
    };

    // Binanin elle atanmis ikonu varsa o, yoksa tablodaki, yoksa null
    public static Sprite BuildingIcon(BuildingData b)
    {
        if (b == null) return null;
        if (b.icon != null) return b.icon;
        string path;
        return buildingIcons.TryGetValue(b.id, out path) ? Load(path) : null;
    }

    // Sprite varsa goster (renk beyaz = orijinal renkler), yoksa verilen yedek renkle duz kutu birak.
    public static bool Skin(Image img, string path, Color fallback)
    {
        if (img == null) return false;
        var s = Load(path);
        if (s == null) { img.color = fallback; return false; }
        img.sprite = s;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = Color.white;
        return true;
    }

    // Sprite varsa Image ve Button geçişlerini sprite'a göre kurar (Button renk tonu: basinca hafif koyulasir).
    public static void SkinButton(Button btn, string path, Color fallback)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        bool ok = Skin(img, path, fallback);
        if (ok)
        {
            var c = btn.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1f, 1f, 1f, 1f);
            c.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            c.disabledColor = Color.white;   // pasif gorunum kendi sprite'inda (Claim_Unready gibi)
            btn.colors = c;
        }
    }

    static readonly Dictionary<string, Sprite> sliced = new Dictionary<string, Sprite>();

    // Ayni dokudan 9-slice kenarli kopya. Buton gibi farkli boyutlarda cizilen cerceveler bozulmadan buyur.
    public static Sprite Sliced(string path, float border)
    {
        string key = path + "|" + border;
        Sprite ns;
        if (sliced.TryGetValue(key, out ns)) return ns;
        var s = Load(path);
        if (s != null)
            ns = Sprite.Create(s.texture, s.rect, new Vector2(0.5f, 0.5f), s.pixelsPerUnit, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sliced[key] = ns;
        return ns;
    }

    // Image'a 9-slice sprite uygular. srcBorder = gorseldeki kose kalinligi (piksel), corner = ekranda istenen kose (piksel).
    public static bool ApplySliced(Image img, string path, float srcBorder, float corner, Color fallback)
    {
        if (img == null) return false;
        var sp = Sliced(path, srcBorder);
        if (sp == null) { img.color = fallback; return false; }
        img.sprite = sp;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = Mathf.Max(0.1f, srcBorder / corner);
        img.color = Color.white;
        return true;
    }

    static readonly Color Cream = new Color(0.98f, 0.92f, 0.8f, 1f);
    public static Color ButtonText { get { return Cream; } }

    // Tum ikincil butonlar icin ortak gorunum: x_untapping cercevesi + krem yazi. Sprite yoksa duz renk.
    public static bool StyleButton(Button btn, TMPro.TMP_Text label, float corner = 14f)
    {
        if (btn == null) return false;
        bool ok = ApplySliced(btn.GetComponent<Image>(), ModeOff, 34f, corner, new Color(0.2f, 0.2f, 0.25f, 0.9f));
        if (!ok) return false;
        var c = btn.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(1f, 0.93f, 0.8f, 1f);
        c.pressedColor = new Color(0.72f, 0.7f, 0.68f, 1f);
        c.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        btn.colors = c;
        if (label != null) label.color = Cream;
        return true;
    }

    // Sprite'in en/boy orani (yoksa yedek)
    public static float Aspect(string path, float fallback)
    {
        var s = Load(path);
        if (s == null || s.rect.height < 1f) return fallback;
        return s.rect.width / s.rect.height;
    }
}
