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

    const float RowHeight = 140f;   // Referanstaki geniş kartlar, beşi tek parşömene sığar.
    const float RightColumn = 176f;
    Image buyImage;
    TMP_Text buyLabel;

    public void Setup(BuildingData d)
    {
        data = d;
        var iconSprite = UIArt.BuildingIcon(d);
        float textLeft = icon != null && iconSprite != null ? 162f : 36f;

        // Kart yuksekligi: ekranda 5 kart gorunsun (mockup)
        var le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = RowHeight;
        le.preferredHeight = RowHeight;

        // Metin sutunu ikon ile AL butonu arasinda kalir.
        SetRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(textLeft, -48f), new Vector2(-RightColumn, -4f), new Vector2(0f, 1f));
        SetRect(infoText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(textLeft, -138f), new Vector2(-RightColumn, -48f), new Vector2(0f, 1f));

        // AL butonu kartin saginda, maliyet hemen altinda.
        RectTransform buttonRt = buyButton.GetComponent<RectTransform>();
        buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(1f, 0.5f);
        buttonRt.pivot = new Vector2(1f, 0.5f);
        buttonRt.anchoredPosition = new Vector2(-28f, 10f);
        buttonRt.sizeDelta = new Vector2(150f, 56f);
        SetRect(costText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
            Vector2.zero, Vector2.zero, new Vector2(1f, 0f));
        costText.rectTransform.anchoredPosition = new Vector2(-18f, 5f);
        costText.rectTransform.sizeDelta = new Vector2(164f, 24f);

        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 28f;
        nameText.fontSizeMax = 42f;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.22f, 0.12f, 0.07f);
        infoText.enableAutoSizing = true;
        infoText.fontSizeMin = 20f;
        infoText.fontSizeMax = 28f;
        infoText.textWrappingMode = TextWrappingModes.Normal;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.color = new Color(0.29f, 0.2f, 0.14f);
        costText.enableAutoSizing = true;
        costText.fontSizeMin = 11f;
        costText.fontSizeMax = 14f;
        costText.textWrappingMode = TextWrappingModes.NoWrap;
        costText.overflowMode = TextOverflowModes.Ellipsis;
        costText.alignment = TextAlignmentOptions.Top;
        costText.fontStyle = FontStyles.Bold;
        costText.color = new Color(0.36f, 0.2f, 0.1f);

        // Kart arka plani: Art/UI/Panel varsa o, yoksa eski acik renk
        var background = GetComponent<Image>();
        if (background != null) UIArt.Skin(background, UIArt.Card, new Color(1f, 0.96f, 0.88f, 0.82f));

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;   // sprite yokken beyaz kutu gorunmesin
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRt = icon.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(28f, 0f);
            iconRt.sizeDelta = new Vector2(128f, 128f);
        }

        buyImage = buyButton.GetComponent<Image>();
        buyLabel = buyButton.GetComponentInChildren<TMP_Text>();
        if (buyLabel != null)
        {
            buyLabel.text = UIArt.ClaimSpriteHasText && UIArt.Has(UIArt.ClaimReady) ? "" : Loc.T("AL");
            buyLabel.fontStyle = FontStyles.Bold;
            buyLabel.enableAutoSizing = true;
            buyLabel.fontSizeMin = 18f;
            buyLabel.fontSizeMax = 30f;
        }
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => GameManager.Instance.TryBuyCurrent(data));
        Refresh();
    }

    // AL butonu: alinabiliyorsa Claim_Ready (sari), degilse Claim_Unready (soluk); sprite yoksa duz renk
    void SkinBuyButton(bool canBuy)
    {
        if (buyImage == null) return;
        var skin = UIArt.Load(canBuy ? UIArt.ClaimReady : UIArt.ClaimUnready);
        if (skin != null)
        {
            buyImage.sprite = skin;
            buyImage.type = Image.Type.Simple;
            buyImage.preserveAspect = false;
            buyImage.color = Color.white;
            var c = buyButton.colors;
            c.normalColor = c.highlightedColor = c.disabledColor = Color.white;
            c.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            buyButton.colors = c;
        }
        else buyImage.color = canBuy ? new Color(0.92f, 0.69f, 0.3f, 1f) : new Color(0.66f, 0.58f, 0.53f, 1f);
        if (buyLabel != null)
            buyLabel.color = canBuy ? new Color(0.19f, 0.13f, 0.08f) : new Color(0.4f, 0.33f, 0.28f);
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
        int owned = gm.GetCount(data);
        nameText.text = Loc.T(data.displayName) + "  x" + owned;

        int next = gm.GetNextMilestone(data);
        string info = Loc.F("+{0}/sn {1}", NumberFormatter.Format(gm.GetUnitProduction(data)), Loc.T(data.produces.displayName));
        info += "\n" + Loc.F("Toplam: {0}/sn", NumberFormatter.Format(gm.GetBuildingProductionPerSecond(data)));
        if (next > 0) info += "\n" + Loc.F("Sonraki x{0}: {1} adet", NumberFormatter.Format(gm.MilestoneMultiplier), next);
        else info += "\n" + Loc.T("MAKS SEVİYE");
        infoText.text = info;

        // x10 / x100 / Max modunda toplam maliyet ve adet gosterilir
        int qty = gm.GetBuyQuantity(data);
        if (qty <= 0)
        {
            costText.text = Loc.T("MAKS SEVİYE");
            buyButton.interactable = false;
            SkinBuyButton(false);
            return;
        }
        costText.text = NumberFormatter.Format(gm.GetCostFor(data, qty)) + " " + Loc.T(data.costResource.displayName)
            + (qty > 1 ? Loc.F("  (x{0})", qty) : "");
        bool canBuy = gm.CanBuyCurrent(data);
        buyButton.interactable = canBuy;
        SkinBuyButton(canBuy);
    }
}
