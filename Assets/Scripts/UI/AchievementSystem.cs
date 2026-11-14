using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementDef
{
    public string id;            // Steam basarim API adiyla birebir ayni tut (ileride Steamworks baglanti noktasi)
    public string name;          // Turkce; gosterimde Loc.T ile cevrilir
    public string description;
    public Func<GameManager, bool> test;
}

// Basarim kosullarini yaris yarisa kontrol eder, kazaninca ekranda haber verir.
// Durumu GameManager tutar ve kaydeder; burada sadece tanimlar ve kontrol var.
// Yeni basarim eklemek: Build() icine bir satir ekle, en.txt'ye ceviri yaz.
public class AchievementSystem : MonoBehaviour
{
    public static readonly List<AchievementDef> All = Build();

    const float CheckInterval = 0.5f;
    const float ToastStackSeconds = 3f;

    FloatingTextSpawner texts;
    float timer, stackTimer;
    int stackIndex;
    bool silent;   // acilista, eski kayittan zaten hak edilmis basarimlar bildirim yagdirmasin

    public void Init(FloatingTextSpawner floatingText) { texts = floatingText; }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnAchievementUnlocked += OnUnlocked;
        silent = true;
        CheckAll(gm);
        silent = false;
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.OnAchievementUnlocked -= OnUnlocked;
    }

    void Update()
    {
        if (stackTimer > 0f) { stackTimer -= Time.unscaledDeltaTime; if (stackTimer <= 0f) stackIndex = 0; }

        timer += Time.unscaledDeltaTime;
        if (timer < CheckInterval) return;
        timer = 0f;
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentAge != null) CheckAll(gm);
    }

    void CheckAll(GameManager gm)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var a = All[i];
            if (gm.IsAchievementUnlocked(a.id)) continue;
            bool ok;
            try { ok = a.test(gm); } catch (Exception e) { Debug.LogException(e); ok = false; }
            if (ok) gm.UnlockAchievement(a.id);
        }
    }

    void OnUnlocked(string id)
    {
        if (silent || texts == null) return;
        AchievementDef def = Find(id);
        if (def == null) return;

        // Ayni anda birden fazla acilirsa alt alta dizilsinler
        var pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.82f - stackIndex * 64f);
        stackIndex++;
        stackTimer = ToastStackSeconds;
        texts.Spawn(Loc.F("Başarım: {0}", Loc.T(def.name)), pos, new Color(1f, 0.85f, 0.25f), 1.3f, 0.35f);
        SoundManager.Play(SoundId.LevelUp, 0.9f, 1f);
    }

    public static AchievementDef Find(string id)
    {
        for (int i = 0; i < All.Count; i++) if (All[i].id == id) return All[i];
        return null;
    }

    static bool AnyBuildingAtLeast(GameManager gm, int n)
    {
        var age = gm.CurrentAge;
        if (age == null || age.buildings == null) return false;
        foreach (var b in age.buildings) if (b != null && gm.GetCount(b) >= n) return true;
        return false;
    }

    static List<AchievementDef> Build()
    {
        var l = new List<AchievementDef>();
        Func<string, string, string, Func<GameManager, bool>, AchievementDef> d = (id, name, desc, test) =>
            new AchievementDef { id = id, name = name, description = desc, test = test };

        l.Add(d("first_click", "İlk Dokunuş", "Tarihin ilk tıklaması. Alkışlayan olmadı.", g => g.TotalClicks >= 1));
        l.Add(d("clicks_100", "Parmak Isınması", "100 tıklama. Parmaklar henüz şikâyet etmiyor.", g => g.TotalClicks >= 100));
        l.Add(d("clicks_1000", "Tırnak Sorunu", "1.000 tıklama. Taş Devri'nde manikür yok.", g => g.TotalClicks >= 1000));
        l.Add(d("clicks_10000", "Tendon Sendikası", "10.000 tıklama. Bileğin bir avukata ihtiyacı var.", g => g.TotalClicks >= 10000));

        l.Add(d("first_building", "Ev Sahibi", "İlk binan. Kira yok, komşu da yok.", g => g.TotalBuildingsBought >= 1));
        l.Add(d("buildings_100", "Köy Kurucusu", "100 bina. Muhtarlık seçimi yakında.", g => g.TotalBuildingsBought >= 100));
        l.Add(d("buildings_1000", "Şehir Planlayıcı", "1.000 bina. Plan yok ama bina çok.", g => g.TotalBuildingsBought >= 1000));

        l.Add(d("first_upgrade", "Fikir Adamı", "İlk yükseltme. Düşünmek bedava değilmiş.", g => g.TotalUpgradesBought >= 1));
        l.Add(d("upgrades_25", "Ar-Ge Departmanı", "25 yükseltme. Kimse ne yaptığını bilmiyor.", g => g.TotalUpgradesBought >= 25));

        l.Add(d("milestone_25", "Kilometre Taşı", "Tek binadan 25 adet. Üretim ikiye katlandı, gurur üçe.", g => AnyBuildingAtLeast(g, 25)));
        l.Add(d("milestone_100", "Tam Yüz", "Tek binadan 100 adet. Artık hobi değil, takıntı.", g => AnyBuildingAtLeast(g, 100)));

        l.Add(d("age_1", "Çağ Atladım", "İlk çağ geçişi. Geçmiş geride kaldı, mamutlar da.", g => g.AgesAdvanced >= 1));
        l.Add(d("age_3", "Zaman Yolcusu", "3 çağ atladın. Tarih kitabı yazılıyor.", g => g.AgesAdvanced >= 3));
        l.Add(d("final_age", "Son Çağ", "Son çağa vardın. Burası bitiş çizgisi değil, geri dönüş kapısı.", g => g.IsFinalAge));

        l.Add(d("loop_1", "Geri Dönüş", "İlk Zaman Makinesi. Paradoks yok, sadece biraz baş dönmesi.", g => g.TimeLoops >= 1));
        l.Add(d("loop_5", "Döngü Bağımlısı", "5 zaman döngüsü. Aynı mamutu beşinci kez görüyorsun.", g => g.TimeLoops >= 5));

        l.Add(d("knowledge_10", "Bilgi Kurdu", "10 Bilgi. Duvara not almak işe yarıyor.", g => g.LifetimeKnowledge >= 10));
        l.Add(d("knowledge_100", "Ansiklopedi", "100 Bilgi. Kimse okumuyor ama kitaplık dolu.", g => g.LifetimeKnowledge >= 100));
        l.Add(d("perm_1", "Ağaç Dikici", "İlk kalıcı yükseltme. Gölgesi sonra.", g => g.TotalPermanentLevels >= 1));
        l.Add(d("perm_10", "Orman Bakıcısı", "10 kalıcı yükseltme seviyesi. Ağaç artık orman.", g => g.TotalPermanentLevels >= 10));

        l.Add(d("play_1h", "Bir Saat Geçti", "1 saat oynadın. Su içtin mi?", g => g.PlaySeconds >= 3600));
        l.Add(d("play_10h", "Vazgeçmeyen", "10 saat. Dışarıda hava güzelmiş.", g => g.PlaySeconds >= 36000));
        l.Add(d("offline_1h", "Sen Yokken", "Bir saatten uzun çevrimdışı ilerleme. Köy seni beklemeden çalıştı.", g => g.OfflineSecondsApplied >= 3600));
        return l;
    }
}
