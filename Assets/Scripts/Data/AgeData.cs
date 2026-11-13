using UnityEngine;

[CreateAssetMenu(menuName = "Evrim/Age", fileName = "age_")]
public class AgeData : ScriptableObject
{
    public string id;            // stone, farm, ...
    public string displayName;   // Taş Çağı

    [Header("Icerik")]
    public ResourceData[] resources;
    public BuildingData[] buildings;
    public ResourceData clickResource;   // tiklayinca kazanilan kaynak

    [Header("Cag atlama")]
    public ResourceData advanceResource;
    public double advanceCost = 300;
    public double knowledgeReward = 1;   // kalici carpan puani

    [Header("Placeholder renk paleti")]
    public Color skyColor = new Color(0.98f, 0.8f, 0.65f);
    public Color groundColor = new Color(0.45f, 0.3f, 0.2f);
}
