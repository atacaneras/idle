using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public const int CurrentVersion = 4;

    // v1: ilk surum (knowledge = tek Bilgi sayaci)
    // v2: yukseltmeler, kalici yukseltmeler, Zaman Makinesi. knowledge artik harcanabilir bakiye,
    //     lifetimeKnowledge toplam kazanilan (v1'den gelen kayitta knowledge'a esitlenir).
    // v3: runEarned = bu turda toplam kazanilan kaynak. Cag atlama ve Zaman Makinesi eldeki
    //     bakiyeye degil bu toplama bakar; boylece bina almak ilerlemeyi cezalandirmaz.
    // v4: istatistikler (tiklama, alinan bina/yukseltme, oynama suresi, gecilen cag) ve basarimlar.
    public int version = CurrentVersion;
    public long lastSaveUnix;
    public string ageId;
    public double knowledge;           // harcanabilir Bilgi
    public double lifetimeKnowledge;   // bugune kadar kazanilan toplam Bilgi (pasif carpan buna bagli)
    public int timeLoops;              // kac kez Zaman Makinesi kullanildi
    public List<NamedDouble> resources = new List<NamedDouble>();
    public List<NamedDouble> runEarned = new List<NamedDouble>();   // v3: bu turda toplam kazanilan
    public List<NamedInt> buildings = new List<NamedInt>();
    public List<string> upgrades = new List<string>();          // bu turda alinan cag ici yukseltmeler
    public List<NamedInt> permanents = new List<NamedInt>();    // kalici yukseltme seviyeleri

    // v4: istatistikler ve basarimlar
    public double totalClicks;
    public double totalBuildingsBought;
    public double totalUpgradesBought;
    public double playSeconds;
    public int agesAdvanced;
    public List<string> achievements = new List<string>();

    // v5 (surum numarasi artmadi: eksik alanlar 0 gelir): taraf ve kat
    public int side;    // 0 = secilmedi, 1 = Cehennem, 2 = Cennet
    public int floor;   // -7..-1 cehennem, 0 yeryuzu, 1..7 cennet
    public double runClicks;                                      // bu turdaki tiklama (gorevler icin)
    public List<string> claimedQuests = new List<string>();       // bu turda odulu alinan gorevler
}

public enum Side { None = 0, Hell = 1, Heaven = 2 }

[Serializable] public struct NamedDouble { public string id; public double value; }
[Serializable] public struct NamedInt { public string id; public int value; }
