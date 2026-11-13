#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Evrim > Baslangic Icerigini Olustur
// Tas ve Tarim caglarini 8'er binaya cikarir, cag ici yukseltmeleri ve Bilgi Agaci'ni (kalici yukseltmeler) olusturur.
// Tekrar tekrar calistirilabilir: ayni id'li asset varsa uzerine yazar, yenisini uretmez.
// DIKKAT: bina ekonomisi (maliyet/uretim), sahne konumu ve placeholder renkleri bu verilere gore yeniden yazilir.
// Sprite / ikon alanlarina dokunmaz.
// Yeni cag eklerken bu dosyayi kopyalayip ayni deseni kullanabilirsin ya da asset'leri elle olusturabilirsin.
public static class StarterContentGenerator
{
    const string Root = "Assets/GameData";

    static readonly Dictionary<string, ResourceData> resources = new Dictionary<string, ResourceData>();
    static readonly Dictionary<string, BuildingData> buildings = new Dictionary<string, BuildingData>();
    static readonly Dictionary<string, UpgradeData> upgrades = new Dictionary<string, UpgradeData>();
    static readonly Dictionary<string, PermanentUpgradeData> perms = new Dictionary<string, PermanentUpgradeData>();
    static readonly List<KeyValuePair<string, string>> pendingPrereq = new List<KeyValuePair<string, string>>();
    static readonly List<KeyValuePair<string, string>> pendingPermReq = new List<KeyValuePair<string, string>>();

    [MenuItem("Evrim/Başlangıç İçeriğini Oluştur")]
    public static void Generate()
    {
        resources.Clear(); buildings.Clear(); upgrades.Clear(); perms.Clear();
        pendingPrereq.Clear(); pendingPermReq.Clear();

        // Mevcut kaynaklar
        ExistingResource("food", "res_food"); ExistingResource("wood", "res_wood");
        ExistingResource("stone", "res_stone"); ExistingResource("wheat", "res_wheat");

        // Yeni kaynaklar
        Resource("milk", "Süt", "res_milk");
        Resource("flour", "Un", "res_flour");
        Resource("bronze", "Bronz", "res_bronze");
        Resource("coin", "Sikke", "res_coin");
        Resource("knowledge", "Bilgelik", "res_wisdom");

        // Binalar
        Building("stone_gatherer", "bld_gatherer", "Toplayıcı Alanı", "food", "food", 15.0, 0.5, -8f, -3.4f, 1, 0f, 1f, 0f);
        Building("stone_woodcamp", "bld_woodcamp", "Odun Kampı", "wood", "food", 100.0, 1.0, -6.5f, -2.7f, 0, 0.545f, 0.27f, 0.075f);
        Building("stone_hunter", "bld_hunter", "Avcı Kampı", "food", "wood", 300.0, 8.0, -5f, -3.4f, 1, 1f, 0f, 0f);
        Building("stone_quarry", "bld_quarry", "Taş Ocağı", "stone", "wood", 1100.0, 1.0, -3.5f, -2.7f, 0, 0.3f, 0.3f, 0.3f);
        Building("stone_axeshop", "bld_stone_axeshop", "Balta Atölyesi", "wood", "stone", 300.0, 30.0, -2f, -3.4f, 1, 0.6f, 0.45f, 0.3f);
        Building("stone_firepit", "bld_stone_firepit", "Ateş Çukuru", "food", "stone", 1500.0, 120.0, -0.5f, -2.7f, 0, 1f, 0.5f, 0f);
        Building("stone_mammoth", "bld_stone_mammoth", "Mamut Taşıyıcılar", "stone", "food", 15000.0, 40.0, 1f, -3.4f, 1, 0.45f, 0.3f, 0.25f);
        Building("stone_gallery", "bld_stone_gallery", "Mağara Galerisi", "food", "stone", 9000.0, 600.0, 2.5f, -2.7f, 0, 0.7f, 0.4f, 0.8f);
        Building("farm_field", "bld_farm", "Buğday Tarlası", "wheat", "wheat", 10.0, 1.0, -8f, -3.4f, 1, 1f, 0.92f, 0.016f);
        Building("farm_cows", "bld_farm_cows", "İnek Ahırı", "milk", "wheat", 120.0, 2.0, -6.5f, -2.7f, 0, 0.9f, 0.9f, 0.9f);
        Building("farm_mill", "bld_farm_mill", "Yel Değirmeni", "flour", "wheat", 1200.0, 6.0, -5f, -3.4f, 1, 0.8f, 0.7f, 0.5f);
        Building("farm_scarecrow", "bld_farm_scarecrow", "Korkuluk Birliği", "wheat", "milk", 900.0, 40.0, -3.5f, -2.7f, 0, 0.8f, 0.6f, 0.2f);
        Building("farm_market", "bld_farm_market", "Pazar Tezgâhı", "milk", "flour", 9000.0, 90.0, -2f, -3.4f, 1, 0.3f, 0.6f, 0.9f);
        Building("farm_bakery", "bld_farm_bakery", "Köy Fırını", "flour", "milk", 40000.0, 250.0, -0.5f, -2.7f, 0, 0.85f, 0.45f, 0.2f);
        Building("farm_canal", "bld_farm_canal", "Sulama Kanalı", "wheat", "flour", 300000.0, 2500.0, 1f, -3.4f, 1, 0.2f, 0.5f, 0.8f);
        Building("farm_square", "bld_farm_square", "Dedikodu Meydanı", "flour", "milk", 2500000.0, 5000.0, 2.5f, -2.7f, 0, 0.6f, 0.6f, 0.3f);
        Building("bronze_smelter", "bld_bronze_smelter", "Bronz Dökümhanesi", "bronze", "wheat", 20e6, 18000, -8f, -3.4f, 1, 0.72f, 0.35f, 0.18f);
        Building("bronze_mint", "bld_bronze_mint", "Sikke Darphanesi", "coin", "bronze", 90e6, 7000, -6f, -2.7f, 0, 0.86f, 0.68f, 0.28f);
        Building("bronze_library", "bld_bronze_library", "Kil Tablet Arşivi", "knowledge", "coin", 300e6, 2500, -4f, -3.4f, 1, 0.55f, 0.38f, 0.22f);
        Building("bronze_cart", "bld_bronze_cart", "Tekerlekli Kervan", "coin", "bronze", 900e6, 80000, -2f, -2.7f, 0, 0.48f, 0.3f, 0.16f);
        Building("bronze_aqueduct", "bld_bronze_aqueduct", "Su Kemeri", "knowledge", "coin", 5e9, 24000, 0f, -3.4f, 1, 0.42f, 0.67f, 0.72f);
        Building("bronze_workshop", "bld_bronze_workshop", "Zanaat Atölyesi", "bronze", "knowledge", 18e9, 350000, 2f, -2.7f, 0, 0.72f, 0.48f, 0.25f);
        Building("bronze_observatory", "bld_bronze_observatory", "Gözlemevi", "knowledge", "bronze", 70e9, 180000, 4f, -3.4f, 1, 0.24f, 0.32f, 0.55f);
        Building("bronze_forum", "bld_bronze_forum", "Şehir Forumu", "coin", "knowledge", 250e9, 1800000, 6f, -2.7f, 0, 0.78f, 0.58f, 0.32f);

        // Cag ici yukseltmeler
        Upgrade("stone_click1", "Daha Sivri Taş", "Sivri olan daha iyi toplar. Bu bilim.", UpgradeKind.ClickMultiplier, 2.0, null, "food", 100.0, null, 0, null);
        Upgrade("stone_click2", "Çift Elle Toplama", "İki el, iki kat verim. Matematik zor ama doğru.", UpgradeKind.ClickMultiplier, 2.0, null, "food", 1500.0, null, 0, "stone_click1");
        Upgrade("stone_click3", "Toplama Teknikleri", "Ustalar işi böyle yapar. Kimse ustaların kim olduğunu bilmiyor.", UpgradeKind.ClickProductionShare, 0.01, null, "food", 12000.0, null, 0, "stone_click2");
        Upgrade("stone_click4", "Kas Hafızası", "Kollar düşünmeden topluyor. Beyin tatilde.", UpgradeKind.ClickMultiplier, 3.0, null, "food", 90000.0, null, 0, "stone_click3");
        Upgrade("stone_gatherer_a", "Sepet Örmek", "İlk sepet. Kimse nasıl yapıldığını hatırlamıyor ama herkes çok gururlu.", UpgradeKind.BuildingMultiplier, 2.0, "stone_gatherer", "food", 180.0, "stone_gatherer", 5, null);
        Upgrade("stone_gatherer_b", "Meyve Haritası", "Hangi çalıdan ne çıktığını duvara çizdik. Duvar artık bizim arama motoru.", UpgradeKind.BuildingMultiplier, 2.0, "stone_gatherer", "food", 2700.0, "stone_gatherer", 20, "stone_gatherer_a");
        Upgrade("stone_woodcamp_a", "Sivri Sopa", "Her sopa bir ağaç söker. Teoride.", UpgradeKind.BuildingMultiplier, 2.0, "stone_woodcamp", "food", 1200.0, "stone_woodcamp", 5, null);
        Upgrade("stone_woodcamp_b", "Odun Sendikası", "Mesai saati icat edildi. Kimse ne olduğunu anlamadı.", UpgradeKind.BuildingMultiplier, 2.0, "stone_woodcamp", "food", 18000.0, "stone_woodcamp", 20, "stone_woodcamp_a");
        Upgrade("stone_hunter_a", "Mızrak Ucu", "Eskiden elimizdeki sopaya mızrak derdik. Şimdi ucu var, çok profesyonel.", UpgradeKind.BuildingMultiplier, 2.0, "stone_hunter", "wood", 3600.0, "stone_hunter", 5, null);
        Upgrade("stone_hunter_b", "Av Stratejisi", "Önce bağırmayı, sonra koşmayı öğrendik. Sıra önemli.", UpgradeKind.BuildingMultiplier, 2.0, "stone_hunter", "wood", 54000.0, "stone_hunter", 20, "stone_hunter_a");
        Upgrade("stone_quarry_a", "Daha İyi Kazma", "Taşla taş kazmak mantıklı mı? Çalışıyor, soru sorma.", UpgradeKind.BuildingMultiplier, 2.0, "stone_quarry", "wood", 13000.0, "stone_quarry", 5, null);
        Upgrade("stone_quarry_b", "Taşa Yazı Yazmak", "Kimse okuyamıyor ama çok şık duruyor.", UpgradeKind.BuildingMultiplier, 2.0, "stone_quarry", "wood", 200000.0, "stone_quarry", 20, "stone_quarry_a");
        Upgrade("stone_axeshop_a", "Bileme Taşı", "Taşı bilemek için taş. Evrenin ironisi.", UpgradeKind.BuildingMultiplier, 2.0, "stone_axeshop", "stone", 3600.0, "stone_axeshop", 5, null);
        Upgrade("stone_axeshop_b", "Çırak Alımı", "Çırak bedava çalışıyor. Buna 'tecrübe' diyoruz.", UpgradeKind.BuildingMultiplier, 2.0, "stone_axeshop", "stone", 54000.0, "stone_axeshop", 20, "stone_axeshop_a");
        Upgrade("stone_firepit_a", "Kuru Odun Kuralı", "Islak odun yanmaz. Üç kişi yanarak öğrendi.", UpgradeKind.BuildingMultiplier, 2.0, "stone_firepit", "stone", 18000.0, "stone_firepit", 5, null);
        Upgrade("stone_firepit_b", "Baharat Keşfi", "Bir ot ateşe düştü, koku harika çıktı. Mutfak doğdu.", UpgradeKind.BuildingMultiplier, 2.0, "stone_firepit", "stone", 270000.0, "stone_firepit", 20, "stone_firepit_a");
        Upgrade("stone_mammoth_a", "Mamut Eğitimi", "Mamutlar 'otur' komutunu öğrenmedi ama taşıyorlar.", UpgradeKind.BuildingMultiplier, 2.0, "stone_mammoth", "food", 180000.0, "stone_mammoth", 5, null);
        Upgrade("stone_mammoth_b", "Mamut Sendikası", "Mamutlar zam istiyor. Yaprakla değil, gerçek zam.", UpgradeKind.BuildingMultiplier, 2.0, "stone_mammoth", "food", 2700000.0, "stone_mammoth", 20, "stone_mammoth_a");
        Upgrade("stone_gallery_a", "Parmak Boyası", "İlk eleştirmen: 'Benim çocuğum da yapar.'", UpgradeKind.BuildingMultiplier, 2.0, "stone_gallery", "stone", 110000.0, "stone_gallery", 5, null);
        Upgrade("stone_gallery_b", "Sanat Turu", "Komşu kabileden turistler geliyor. Rehber: 'Yiyecek ayrıca.'", UpgradeKind.BuildingMultiplier, 2.0, "stone_gallery", "stone", 1600000.0, "stone_gallery", 20, "stone_gallery_a");
        Upgrade("stone_global1", "Ateşi Evcilleştirmek", "Artık yakmıyor da ısıtıyor. Büyük ilerleme.", UpgradeKind.GlobalMultiplier, 1.5, null, "food", 2500.0, "stone_hunter", 10, null);
        Upgrade("stone_global2", "Tekerlek Fikri", "Henüz tekerlek yok ama fikir var. Fikri bile ağır.", UpgradeKind.GlobalMultiplier, 2.0, null, "food", 40000.0, "stone_quarry", 15, null);
        Upgrade("farm_click1", "Daha İyi Orak", "Orak keskin. Parmaklar da.", UpgradeKind.ClickMultiplier, 2.0, null, "wheat", 150.0, null, 0, null);
        Upgrade("farm_click2", "Çift Orak", "Biri yeterliydi ama iki tane daha havalı.", UpgradeKind.ClickMultiplier, 2.0, null, "wheat", 4000.0, null, 0, "farm_click1");
        Upgrade("farm_click3", "Bereket Duası", "Dua ediyoruz. Sonuç: biraz daha fazla buğday.", UpgradeKind.ClickProductionShare, 0.01, null, "wheat", 50000.0, null, 0, "farm_click2");
        Upgrade("farm_click4", "Sabah Antrenmanı", "Horoz ötmeden kalkıyoruz. Horoz küstü.", UpgradeKind.ClickMultiplier, 3.0, null, "wheat", 700000.0, null, 0, "farm_click3");
        Upgrade("farm_field_a", "Demir Saban", "Tahta saban kırılıyordu. Demir diye bir şey keşfettik.", UpgradeKind.BuildingMultiplier, 2.0, "farm_field", "wheat", 120.0, "farm_field", 5, null);
        Upgrade("farm_field_b", "Ürün Rotasyonu", "Aynı tarlaya aynı şeyi ekme. Ya da ek, bakalım.", UpgradeKind.BuildingMultiplier, 2.0, "farm_field", "wheat", 1800.0, "farm_field", 20, "farm_field_a");
        Upgrade("farm_cows_a", "Yonca Menüsü", "İnekler artık yonca istiyor. 'Ot' demek hakaret sayılıyor.", UpgradeKind.BuildingMultiplier, 2.0, "farm_cows", "wheat", 1400.0, "farm_cows", 5, null);
        Upgrade("farm_cows_b", "Sabah Sağımı", "İnekler sabah insanı değil ama idare ediyor.", UpgradeKind.BuildingMultiplier, 2.0, "farm_cows", "wheat", 22000.0, "farm_cows", 20, "farm_cows_a");
        Upgrade("farm_mill_a", "Daha Büyük Kanatlar", "Rüzgâr çıkınca çalışıyor. Çıkmayınca toplantı yapıyoruz.", UpgradeKind.BuildingMultiplier, 2.0, "farm_mill", "wheat", 14000.0, "farm_mill", 5, null);
        Upgrade("farm_mill_b", "Değirmen Ustası", "'Ben bilirim' deyip her şeyi bozdu. Sonra tamir etti.", UpgradeKind.BuildingMultiplier, 2.0, "farm_mill", "wheat", 220000.0, "farm_mill", 20, "farm_mill_a");
        Upgrade("farm_scarecrow_a", "Ürkütücü Yüzler", "Kargalar bile ürküyor. Komşu da.", UpgradeKind.BuildingMultiplier, 2.0, "farm_scarecrow", "milk", 11000.0, "farm_scarecrow", 5, null);
        Upgrade("farm_scarecrow_b", "Korkuluk Tatbikatı", "Düşman yok, tatbikat var. Ama hazırlıklıyız.", UpgradeKind.BuildingMultiplier, 2.0, "farm_scarecrow", "milk", 160000.0, "farm_scarecrow", 20, "farm_scarecrow_a");
        Upgrade("farm_market_a", "Pazarlık Sanatı", "'Son fiyat' ilk fiyatın yarısı. Herkes biliyor ama oynuyor.", UpgradeKind.BuildingMultiplier, 2.0, "farm_market", "flour", 110000.0, "farm_market", 5, null);
        Upgrade("farm_market_b", "Bağırarak Satış", "En yüksek ses en çok satar. Bilimsel olarak kanıtlandı: biri bağırdı.", UpgradeKind.BuildingMultiplier, 2.0, "farm_market", "flour", 1600000.0, "farm_market", 20, "farm_market_a");
        Upgrade("farm_bakery_a", "Taş Fırın", "Ekmek kabarıyor. Neden kabarıyor? Kimse bilmiyor.", UpgradeKind.BuildingMultiplier, 2.0, "farm_bakery", "milk", 480000.0, "farm_bakery", 5, null);
        Upgrade("farm_bakery_b", "Maya Bekçisi", "Mayanın bakımını biri yapıyor. Maya ona teşekkür etmiyor.", UpgradeKind.BuildingMultiplier, 2.0, "farm_bakery", "milk", 7200000.0, "farm_bakery", 20, "farm_bakery_a");
        Upgrade("farm_canal_a", "Daha Derin Hendek", "Su akıyor. Neredeyse istediğimiz yere.", UpgradeKind.BuildingMultiplier, 2.0, "farm_canal", "flour", 3600000.0, "farm_canal", 5, null);
        Upgrade("farm_canal_b", "Kanal Bekçisi", "Biri suyu çalıyor. Komşu mu? Evet, komşu.", UpgradeKind.BuildingMultiplier, 2.0, "farm_canal", "flour", 54000000.0, "farm_canal", 20, "farm_canal_a");
        Upgrade("farm_square_a", "Çeşme Başı", "Su alırken konuşuyoruz. Konu: başkaları.", UpgradeKind.BuildingMultiplier, 2.0, "farm_square", "milk", 30000000.0, "farm_square", 5, null);
        Upgrade("farm_square_b", "Meydan Muhabbeti", "Haberler sözlü. Çoğu yanlış. Hepsi eğlenceli.", UpgradeKind.BuildingMultiplier, 2.0, "farm_square", "milk", 450000000.0, "farm_square", 20, "farm_square_a");
        Upgrade("farm_global1", "Takvim İcadı", "Ne zaman ekeceğimizi biliyoruz. Çiftçiler 'zaten biliyorduk' diyor.", UpgradeKind.GlobalMultiplier, 1.5, null, "wheat", 25000.0, "farm_mill", 10, null);
        Upgrade("farm_global2", "Çit Çekmek", "Çit koyduk. Komşunun ineği 'sınır nerede' diye soruyor.", UpgradeKind.GlobalMultiplier, 2.0, null, "wheat", 600000.0, "farm_canal", 10, null);
        Upgrade("bronze_click1", "Döküm Kalıbı", "Aynı baltayı tekrar tekrar dökmek artık mümkün.", UpgradeKind.ClickMultiplier, 2, null, "bronze", 25000, null, 0, null);
        Upgrade("bronze_click2", "Usta Çekiç", "Çekiç daha ağır. Başparmak daha dikkatli.", UpgradeKind.ClickMultiplier, 3, null, "bronze", 900000, null, 0, "bronze_click1");
        Upgrade("bronze_click3", "Yazılı Tarifler", "Tablete yazdık. Tablet ağır, tarif kısa.", UpgradeKind.ClickProductionShare, .015, null, "knowledge", 250000, null, 0, "bronze_click2");
        Upgrade("bronze_click4", "Atölye Nöbeti", "Vardiya sistemi icat edildi. Usta hâlâ uyumuyor.", UpgradeKind.ClickMultiplier, 4, null, "coin", 8000000, null, 0, "bronze_click3");
        string[] bb = { "bronze_smelter", "bronze_mint", "bronze_library", "bronze_cart", "bronze_aqueduct", "bronze_workshop", "bronze_observatory", "bronze_forum" };
        string[] bn = { "Fırın Tuğlaları", "Düzenli Sikke", "Tablet Kataloğu", "Tekerlek Gresi", "Çift Kemer", "Usta Çıraklığı", "Yıldız Cetveli", "Açık Forum" };
        string[] br = { "bronze", "coin", "knowledge", "coin", "knowledge", "bronze", "knowledge", "coin" };
        double[] bc = { 100000, 450000, 1500000, 4500000, 15000000, 45000000, 150000000, 450000000 };
        for (int i = 0; i < bb.Length; i++)
        {
            Upgrade("bronze_" + bb[i] + "_a", bn[i], "Daha iyi araçlar, daha düzenli üretim.", UpgradeKind.BuildingMultiplier, 2, bb[i], br[i], bc[i], bb[i], 5, null);
            Upgrade("bronze_" + bb[i] + "_b", bn[i] + " II", "İlk yöntem işe yaradı. Biraz daha iyisini yaptık.", UpgradeKind.BuildingMultiplier, 2, bb[i], br[i], bc[i] * 100, bb[i], 20, "bronze_" + bb[i] + "_a");
        }
        Upgrade("bronze_global1", "Standart Ölçüler", "Herkes aynı ölçü kabını kullanıyor. Neredeyse.", UpgradeKind.GlobalMultiplier, 1.5, null, "coin", 3000000, "bronze_mint", 10, null);
        Upgrade("bronze_global2", "Yazılı Kanunlar", "Kurallar tablete yazıldı; artık kimse okuyamıyor.", UpgradeKind.GlobalMultiplier, 2, null, "knowledge", 100000000, "bronze_forum", 10, null);
        foreach (var kv in pendingPrereq) upgrades[kv.Key].prerequisite = upgrades[kv.Value];

        // Kalici yukseltmeler (Bilgi Agaci)
        Perm("perm_global", "Atalardan Notlar", "Kimse okuyamıyor ama duvarda çok şık duruyor.", PermanentKind.GlobalProduction, 0.1, 10, 3.0, 1.55, null);
        Perm("perm_click", "Güçlü Bilekler", "Ellerin hafızası var. Zaman makinesi bunu silemedi.", PermanentKind.ClickPower, 0.25, 8, 2.0, 1.5, null);
        Perm("perm_cost", "Pazarlık Ruhu", "Tüccarlar 'bu sefer fiyatı sen belirle' dedi. İlk kez.", PermanentKind.CostReduction, 0.03, 10, 5.0, 1.6, "perm_global");
        Perm("perm_advance", "Kestirme Yol", "Haritada yok ama bir keçi yolu biliyoruz.", PermanentKind.AdvanceDiscount, 0.05, 8, 4.0, 1.6, "perm_click");
        Perm("perm_knowledge", "Not Alma Alışkanlığı", "Zaman yolculuğundan her seferinde daha çok şey hatırlıyoruz.", PermanentKind.KnowledgeGain, 0.2, 5, 8.0, 2.0, "perm_global");
        Perm("perm_offline", "Gözü Açık Uyku", "Sen yokken köy çalışıyor. Sana sormadan.", PermanentKind.OfflineHours, 1.0, 8, 3.0, 1.5, "perm_click");
        Perm("perm_global2", "Paradoks Verimi", "Geçmişteki biz, gelecekteki bizi kıskanıyor.", PermanentKind.GlobalProduction, 0.25, 5, 25.0, 2.2, "perm_cost");
        foreach (var kv in pendingPermReq) perms[kv.Key].requires = perms[kv.Value];

        // Caglar
        ConfigureAge("age_stone", new[] { "food", "wood", "stone" }, new[] { "stone_gatherer", "stone_woodcamp", "stone_hunter", "stone_quarry", "stone_axeshop", "stone_firepit", "stone_mammoth", "stone_gallery" }, new[] { "stone_click1", "stone_click2", "stone_click3", "stone_click4", "stone_gatherer_a", "stone_gatherer_b", "stone_woodcamp_a", "stone_woodcamp_b", "stone_hunter_a", "stone_hunter_b", "stone_quarry_a", "stone_quarry_b", "stone_axeshop_a", "stone_axeshop_b", "stone_firepit_a", "stone_firepit_b", "stone_mammoth_a", "stone_mammoth_b", "stone_gallery_a", "stone_gallery_b", "stone_global1", "stone_global2" }, 3800000.0, 3.0);
        ConfigureAge("age_farm", new[] { "wheat", "milk", "flour" }, new[] { "farm_field", "farm_cows", "farm_mill", "farm_scarecrow", "farm_market", "farm_bakery", "farm_canal", "farm_square" }, new[] { "farm_click1", "farm_click2", "farm_click3", "farm_click4", "farm_field_a", "farm_field_b", "farm_cows_a", "farm_cows_b", "farm_mill_a", "farm_mill_b", "farm_scarecrow_a", "farm_scarecrow_b", "farm_market_a", "farm_market_b", "farm_bakery_a", "farm_bakery_b", "farm_canal_a", "farm_canal_b", "farm_square_a", "farm_square_b", "farm_global1", "farm_global2" }, 290000000.0, 0.0);
        var bronzeUpgrades = new List<string> { "bronze_click1", "bronze_click2", "bronze_click3", "bronze_click4", "bronze_global1", "bronze_global2" };
        foreach (var b in bb) { bronzeUpgrades.Add("bronze_" + b + "_a"); bronzeUpgrades.Add("bronze_" + b + "_b"); }
        ConfigureAge("age_bronze", new[] { "bronze", "coin", "knowledge" }, bb, bronzeUpgrades.ToArray(), 1000000000000.0, 0);

        AssignPermanentsToScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Evrim: baslangic icerigi olusturuldu. Sahneyi kaydet (Ctrl+S).");
    }

    // ---- Yardimcilar ----
    static T LoadOrCreate<T>(string folder, string file) where T : ScriptableObject
    {
        string dir = Root + "/" + folder;
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Root, folder);
        string path = dir + "/" + file + ".asset";
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null)
        {
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
        }
        return a;
    }

    static void ExistingResource(string id, string file)
    {
        var r = AssetDatabase.LoadAssetAtPath<ResourceData>(Root + "/Kaynaklar/" + file + ".asset");
        if (r == null) { Debug.LogError("Kaynak bulunamadi: " + file); return; }
        resources[id] = r;
    }

    static void Resource(string id, string displayName, string file)
    {
        var r = LoadOrCreate<ResourceData>("Kaynaklar", file);
        r.id = id; r.displayName = displayName;
        EditorUtility.SetDirty(r);
        resources[id] = r;
    }

    static void Building(string id, string file, string displayName, string produces, string costRes,
        double baseCost, double baseProd, float x, float y, int order, float cr, float cg, float cb)
    {
        var b = LoadOrCreate<BuildingData>("Binalar", file);
        b.id = id; b.displayName = displayName;
        b.produces = resources[produces]; b.costResource = resources[costRes];
        b.baseCost = baseCost; b.costGrowth = 1.15; b.baseProduction = baseProd;
        b.scenePosition = new Vector2(x, y); b.sortingOrder = order;
        b.placeholderColor = new Color(cr, cg, cb, 1f);
        if (b.placeholderSize == Vector2.zero) b.placeholderSize = new Vector2(1.2f, 1.2f);
        EditorUtility.SetDirty(b);
        buildings[id] = b;
    }

    static void Upgrade(string id, string displayName, string desc, UpgradeKind kind, double value, string target,
        string costRes, double cost, string unlockBuilding, int unlockCount, string prereq)
    {
        var u = LoadOrCreate<UpgradeData>("Yukseltmeler", "upg_" + id);
        u.id = id; u.displayName = displayName; u.description = desc; u.kind = kind; u.value = value;
        u.targetBuilding = target != null ? buildings[target] : null;
        u.costResource = resources[costRes]; u.cost = cost;
        u.unlockBuilding = unlockBuilding != null ? buildings[unlockBuilding] : null;
        u.unlockCount = unlockCount;
        u.prerequisite = null;
        if (prereq != null) pendingPrereq.Add(new KeyValuePair<string, string>(id, prereq));
        EditorUtility.SetDirty(u);
        upgrades[id] = u;
    }

    static void Perm(string id, string displayName, string desc, PermanentKind kind, double valuePerLevel, int maxLevel,
        double baseCost, double growth, string requires)
    {
        var p = LoadOrCreate<PermanentUpgradeData>("Kalici", id);
        p.id = id; p.displayName = displayName; p.description = desc; p.kind = kind;
        p.valuePerLevel = valuePerLevel; p.maxLevel = maxLevel; p.baseCost = baseCost; p.costGrowth = growth;
        p.requires = null;
        if (requires != null) pendingPermReq.Add(new KeyValuePair<string, string>(id, requires));
        EditorUtility.SetDirty(p);
        perms[id] = p;
    }

    static void ConfigureAge(string file, string[] res, string[] blds, string[] upgs, double advanceCost, double knowledge)
    {
        var age = AssetDatabase.LoadAssetAtPath<AgeData>(Root + "/Caglar/" + file + ".asset");
        if (age == null) { Debug.LogError("Cag bulunamadi: " + file); return; }
        age.resources = System.Array.ConvertAll(res, s => resources[s]);
        age.buildings = System.Array.ConvertAll(blds, s => buildings[s]);
        age.upgrades = System.Array.ConvertAll(upgs, s => upgrades[s]);
        age.advanceCost = advanceCost;
        if (age.clickResource == null) age.clickResource = resources[res[0]];
        if (age.advanceResource == null) age.advanceResource = resources[res[0]];
        if (knowledge > 0) age.knowledgeReward = knowledge;
        EditorUtility.SetDirty(age);
    }

    // Sahnedeki GameManager'a kalici yukseltme listesini atar
    static void AssignPermanentsToScene()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(scenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
                gm = Object.FindFirstObjectByType<GameManager>();
            }
        }
        if (gm == null)
        {
            Debug.LogWarning("GameManager bulunamadi. Sahneyi acip GameManager > Permanent Upgrades listesine Assets/GameData/Kalici icindeki asset'leri elle ata.");
            return;
        }

        var list = new List<PermanentUpgradeData>(perms.Values);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        var so = new SerializedObject(gm);
        var prop = so.FindProperty("permanentUpgrades");
        prop.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
    }
}
#endif
