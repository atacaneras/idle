using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu: Evrim > Sanatçı Klasörlerini Hazırla
// 15 kat icin Assets/Resources/Art/<kat>/ klasorlerini acar ve SANATCI_REHBERI.md yazar:
// hangi klasore hangi dosya adiyla ne atilacak. Icerik Uret calisinca bu da otomatik calisir.
public static class ArtGuide
{
    const string Root = "Assets/Resources/Art";

    [MenuItem("Evrim/Sanatçı Klasörlerini Hazırla")]
    public static void Generate()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Art");

        var ages = new Dictionary<string, AgeData>();
        foreach (var guid in AssetDatabase.FindAssets("t:AgeData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<AgeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null && !string.IsNullOrEmpty(a.id)) ages[a.id] = a;
        }

        var keys = new List<string> { "earth" };
        for (int i = 1; i <= 7; i++) keys.Add("hell_" + i);
        for (int i = 1; i <= 7; i++) keys.Add("heaven_" + i);

        var sb = new StringBuilder();
        sb.AppendLine("# Sanatçı Rehberi");
        sb.AppendLine();
        sb.AppendLine("PNG dosyasını ilgili kat klasörüne at, Unity'de hiçbir ayara dokunma. Oyun Play'e basınca dosyayı kendi bulur.");
        sb.AppendLine("Dosya adları **birebir** aşağıdaki gibi olmalı (küçük harf, boşluksuz). Dosya yoksa oyun placeholder çizer.");
        sb.AppendLine();
        sb.AppendLine("- Kat arka planı: 1920x1080 önerilir.");
        sb.AppendLine("- **Arayüz panelleri arka planın üstüne biner.** Ana çizimi (kapı, tezgâh, taht vb.) ortadaki alana koy: yaklaşık x 600–1150, y 170–720. Sol ~%30 (yükseltme paneli), sağ ~%40 (bina listesi), üst ~%14 ve alt ~%25 (butonlar) kısmı süs/dolgu olsun.");
        sb.AppendLine("- `boss.png` bu orta alanın ortasına konur (ekranda yaklaşık 430 px yükseklik). Aynı yerde HUD çubuğu üstte, binalar altta kalır.");
        sb.AppendLine("- Bina: yaklaşık 140x180 px (placeholder 1.4x1.8 birim). `_lv1` / `_lv2` / `_lv3` seviye görünümleri, sadece `_lv1` yeterli.");
        sb.AppendLine("- Hepsi şeffaf zeminli PNG olabilir.");
        sb.AppendLine("- Binaların tabanı ekranın altından yaklaşık 200 px yukarıda durur (1080 yükseklikte y=880). Arka plandaki zemin çizgisini oraya çiz. Bina PNG'sinin alt-orta noktası bu çizgiye oturur.");
        sb.AppendLine();

        EnsureFolder(Root, "UI");
        sb.AppendLine("## Ana Menü (taraf seçimi)  —  `Assets/Resources/Art/UI/`");
        sb.AppendLine();
        sb.AppendLine("Hepsi opsiyonel: dosya yoksa oyun placeholder çizer. Fare imleci gizlenir, yerine hayalet gelir.");
        sb.AppendLine();
        sb.AppendLine("| Dosya | Ne |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| `menu_bg.png` | Tam ekran arka plan (solda cehennem, ortada köprü, sağda cennet), 1920x1080 |");
        sb.AppendLine("| `Curtain_Frame.png` | Perde çerçevesi, tam ekran, ortası şeffaf |");
        sb.AppendLine("| `menu_title.png` | Üstteki tabela (oyun adı kodla üstüne yazılır) |");
        sb.AppendLine("| `menu_plaque_hell.png` | Sol alttaki CEHENNEM levhası (yazı kodla yazılır) |");
        sb.AppendLine("| `menu_plaque_heaven.png` | Sağ alttaki CENNET levhası (yazı kodla yazılır) |");
        sb.AppendLine("| `ghost.png` | Sade hayalet, imleç (ekranın ortasında / köprüde), yaklaşık 150x190 |");
        sb.AppendLine("| `ghost_angel.png` | Cennet tarafına gelince: kanatlı, haleli melek hayalet |");
        sb.AppendLine("| `ghost_demon.png` | Cehennem tarafına gelince: boynuzlu zebani hayalet |");
        sb.AppendLine("| `../Icons/Settings_Button_S.png` | Altta ortadaki ayarlar butonu |");
        sb.AppendLine();
        sb.AppendLine("Hayaletin üç hâli aynı boyutta ve aynı merkezde çizilirse geçiş düzgün görünür. İmleç noktası hayaletin merkezidir.");
        sb.AppendLine();

        foreach (var key in keys)
        {
            EnsureFolder(Root, key);
            AgeData age;
            ages.TryGetValue(key, out age);
            string title = key == "earth" ? "Yer Yüzü" : (age != null ? age.displayName : key + " (içerik henüz yok)");
            sb.AppendLine("## " + title + "  —  `Assets/Resources/Art/" + key + "/`");
            sb.AppendLine();
            sb.AppendLine("| Dosya | Ne |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| `background.png` | Kat arka planı |");
            if (key != "earth") sb.AppendLine("| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |");
            if (age != null && age.buildings != null)
                foreach (var b in age.buildings)
                    sb.AppendLine("| `" + b.id + "_lv1.png` | " + b.displayName + " |");
            sb.AppendLine();
        }

        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Root + "/SANATCI_REHBERI.md"), sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log("Sanatçı klasörleri ve rehber hazır: " + Root + "/SANATCI_REHBERI.md");
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }
}
