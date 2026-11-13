using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBuildingRow : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text infoText;
    [SerializeField] TMP_Text costText;
    [SerializeField] Image icon;
    [SerializeField] Button buyButton;

    BuildingData data;

    public void Setup(BuildingData d)
    {
        data = d;
        if (icon != null && d.icon != null) icon.sprite = d.icon;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => GameManager.Instance.TryBuy(data));
        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        nameText.text = data.displayName + "  x" + gm.GetCount(data);
        infoText.text = "+" + NumberFormatter.Format(data.baseProduction * gm.Multiplier) + "/sn " + data.produces.displayName;
        costText.text = NumberFormatter.Format(gm.GetCost(data)) + " " + data.costResource.displayName;
        buyButton.interactable = gm.CanAfford(data);
    }
}
