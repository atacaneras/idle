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
        if (icon != null)
        {
            icon.sprite = d.icon;
            icon.enabled = d.icon != null;   // sprite yokken beyaz kutu gorunmesin
        }
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => GameManager.Instance.TryBuyCurrent(data));
        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        nameText.text = data.displayName + "  x" + gm.GetCount(data);
        int owned = gm.GetCount(data);
        int next = gm.GetNextMilestone(data);
        string info = "+" + NumberFormatter.Format(gm.GetUnitProduction(data)) + "/sn " + data.produces.displayName;
        if (owned > 0) info += "  (toplam " + NumberFormatter.Format(gm.GetBuildingProductionPerSecond(data)) + ")";
        if (next > 0) info += "\nSonraki x" + NumberFormatter.Format(gm.MilestoneMultiplier) + ": " + next + " adet";
        infoText.text = info;

        // x10 / x100 / Max modunda toplam maliyet ve adet gosterilir
        int qty = gm.GetBuyQuantity(data);
        costText.text = NumberFormatter.Format(gm.GetCostFor(data, qty)) + " " + data.costResource.displayName
            + (qty > 1 ? "  (x" + qty + ")" : "");
        buyButton.interactable = gm.CanBuyCurrent(data);
    }
}
