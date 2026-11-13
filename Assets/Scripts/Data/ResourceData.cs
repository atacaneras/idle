using UnityEngine;

[CreateAssetMenu(menuName = "Evrim/Resource", fileName = "res_")]
public class ResourceData : ScriptableObject
{
    public string id;            // ornek: food, wood, stone
    public string displayName;   // Yiyecek
    public Sprite icon;          // stone_icon_food

    void OnValidate()
    {
        if (id != null) id = id.Trim();   // yanlislikla yazilan bosluk/tab kayit sistemini bozmasin
    }
}
