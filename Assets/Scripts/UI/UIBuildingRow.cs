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
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 16f;
        nameText.fontSizeMax = 26f;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        infoText.enableAutoSizing = true;
        infoText.fontSizeMin = 12f;
        infoText.fontSizeMax = 22f;
        infoText.textWrappingMode = TextWrappingModes.Normal;
        costText.enableAutoSizing = true;
        costText.fontSizeMin = 12f;
        costText.fontSizeMax = 22f;
        costText.textWrappingMode = TextWrappingModes.NoWrap;
        costText.overflowMode = TextOverflowModes.Ellipsis;
        if (icon != null)
        {
            icon.sprite = d.icon;
            icon.enabled = d.icon != null;   // sprite yokken beyaz kutu gorunmesin
        }
        var buyImage = buyButton.GetComponent<Image>();
        if (buyImage != null) buyImage.color = new Color(0.92f, 0.69f, 0.3f, 1f);
        var buyLabel = buyButton.GetComponentInChildren<TMP_Text>();
        if (buyLabel != null)
        {
            buyLabel.text = "AL";
            buyLabel.fontStyle = FontStyles.Bold;
            buyLabel.color = new Color(0.19f, 0.13f, 0.08f);
            buyLabel.enableAutoSizing = true;
            buyLabel.fontSizeMin = 14f;
            buyLabel.fontSizeMax = 22f;
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
        if (owned > 0) info += "\nToplam: " + NumberFormatter.Format(gm.GetBuildingProductionPerSecond(data)) + "/sn";
        if (next > 0) info += "\nSonraki x" + NumberFormatter.Format(gm.MilestoneMultiplier) + ": " + next + " adet";
        infoText.text = info;

        // x10 / x100 / Max modunda toplam maliyet ve adet gosterilir
        int qty = gm.GetBuyQuantity(data);
        costText.text = NumberFormatter.Format(gm.GetCostFor(data, qty)) + " " + data.costResource.displayName
            + (qty > 1 ? "  (x" + qty + ")" : "");
        buyButton.interactable = gm.CanBuyCurrent(data);
    }
}
