using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public const int CurrentVersion = 2;

    // v1: ilk surum (knowledge = tek Bilgi sayaci)
    // v2: yukseltmeler, kalici yukseltmeler, Zaman Makinesi. knowledge artik harcanabilir bakiye,
    //     lifetimeKnowledge toplam kazanilan (v1'den gelen kayitta knowledge'a esitlenir).
    public int version = CurrentVersion;
    public long lastSaveUnix;
    public string ageId;
    public double knowledge;           // harcanabilir Bilgi
    public double lifetimeKnowledge;   // bugune kadar kazanilan toplam Bilgi (pasif carpan buna bagli)
    public int timeLoops;              // kac kez Zaman Makinesi kullanildi
    public List<NamedDouble> resources = new List<NamedDouble>();
    public List<NamedInt> buildings = new List<NamedInt>();
    public List<string> upgrades = new List<string>();          // bu turda alinan cag ici yukseltmeler
    public List<NamedInt> permanents = new List<NamedInt>();    // kalici yukseltme seviyeleri
}

[Serializable] public struct NamedDouble { public string id; public double value; }
[Serializable] public struct NamedInt { public string id; public int value; }
