using System;
using UnityEngine;

public enum QuestKind { Click, Building, Earn, Upgrades }

// Kat gorevi: bu turda hedefe ulasinca odulu alinir. Tur sifirlaninca (kat atlama / yeniden dogus) yeniden baslar.
[Serializable]
public class QuestDef
{
    public string id;
    public string text;               // esprili gorev metni
    public QuestKind kind;
    public BuildingData building;     // sadece Building turu icin
    public double amount;             // hedef
    public double rewardFlat;         // sabit kaynak odulu
    public double rewardSeconds;      // + bu kadar saniyelik uretim
    public double rewardKnowledge;    // + Bilgi
}

[CreateAssetMenu(menuName = "Cennet ve Cehennem/Kat", fileName = "kat_")]
public class AgeData : ScriptableObject
{
    public string id;            // stone, farm, ...
    public string displayName;   // Taş Çağı

    [Header("Taraf / Kat")]
    public Side side;            // Cehennem ya da Cennet
    public string clickLabel;    // tıklama butonu yazısı (CEZALANDIR / İYİLİK YAP)
    public string goalTitle;
    public QuestDef[] quests;     // kat hedefi: boss adı ya da huzur çubuğu başlığı

    [Header("Icerik")]
    public ResourceData[] resources;
    public BuildingData[] buildings;
    public UpgradeData[] upgrades;       // cag ici tek seferlik yukseltmeler
    public ResourceData clickResource;   // tiklayinca kazanilan kaynak

    [Header("Cag atlama")]
    public ResourceData advanceResource;
    public double advanceCost = 300;
    public double knowledgeReward = 1;   // kalici carpan puani

    [Header("Placeholder renk paleti")]
    public Color skyColor = new Color(0.98f, 0.8f, 0.65f);
    public Color groundColor = new Color(0.45f, 0.3f, 0.2f);

    void OnValidate()
    {
        if (id != null) id = id.Trim();   // yanlislikla yazilan bosluk/tab kayit sistemini bozmasin
    }
}
