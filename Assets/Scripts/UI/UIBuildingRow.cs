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
        float textLeft = icon != null && d.icon != null ? 124f : 24f;
        const float rightColumn = 226f;

        // Keep the text column between the optional icon and the buy button.
        SetRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(textLeft, -52f), new Vector2(-rightColumn, -14f), new Vector2(0f, 1f));
        SetRect(infoText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(textLeft, -130f), new Vector2(-rightColumn, -60f), new Vector2(0f, 1f));
        SetRect(costText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(textLeft, 4f), new Vector2(-rightColumn, 38f), new Vector2(0f, 0f));

        RectTransform buttonRt = buyButton.GetComponent<RectTransform>();
        buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(1f, 0.5f);
        buttonRt.pivot = new Vector2(1f, 0.5f);
        buttonRt.anchoredPosition = new Vector2(-16f, 0f);
        buttonRt.sizeDelta = new Vector2(190f, 76f);

        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 22f;
        nameText.fontSizeMax = 30f;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.fontStyle = FontStyles.Bold;
        infoText.enableAutoSizing = true;
        infoText.fontSizeMin = 18f;
        infoText.fontSizeMax = 22f;
        infoText.textWrappingMode = TextWrappingModes.Normal;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.color = new Color(0.29f, 0.25f, 0.22f);
        costText.enableAutoSizing = true;
        costText.fontSizeMin = 18f;
        costText.fontSizeMax = 22f;
        costText.textWrappingMode = TextWrappingModes.NoWrap;
        costText.overflowMode = TextOverflowModes.Ellipsis;
        costText.alignment = TextAlignmentOptions.MidlineLeft;
        costText.fontStyle = FontStyles.Bold;
        costText.color = new Color(0.36f, 0.25f, 0.16f);

        var background = GetComponent<Image>();
        if (background != null) background.color = new Color(1f, 0.96f, 0.88f, 0.82f);
        if (icon != null)
        {
            icon.sprite = d.icon;
            icon.enabled = d.icon != null;   // sprite yokken beyaz kutu gorunmesin
            RectTransform iconRt = icon.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(16f, 0f);
            iconRt.sizeDelta = new Vector2(92f, 92f);
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
            buyLabel.fontSizeMin = 18f;
            buyLabel.fontSizeMax = 24f;
        }
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => GameManager.Instance.TryBuyCurrent(data));
        Refresh();
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
