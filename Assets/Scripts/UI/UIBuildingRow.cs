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

    // Kart yuksekligi HUDController tarafindan listenin gorunen alanina gore hesaplanir (5 kart tam sigar).
    public static float RowHeight = 142f;
    const float PadTop = 12f;      // kart cercevesinin ust/alt suslemesi icin ic bosluk
    const float PadBottom = 16f;
    const float IconPad = 24f;
    const float RightColumn = 168f;
    Image buyImage;
    TMP_Text buyLabel;

    public void Setup(BuildingData d)
    {
        data = d;
        var iconSprite = UIArt.BuildingIcon(d);
        float h = RowHeight;
        float usable = h - PadTop - PadBottom;
        float iconSize = Mathf.Min(usable, 112f);
        float textLeft = icon != null && iconSprite != null ? IconPad + iconSize + 12f : 36f;
        const float nameH = 36f;

        var le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = h;
        le.preferredHeight = h;

        // Ad: ust satir. Bilgi: altindaki 3 satir. Ikisi de ikon ile sag sutun arasinda kalir ve karttan tasmaz.
        SetRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(textLeft, -(PadTop + nameH)), new Vector2(-RightColumn, -PadTop), new Vector2(0f, 1f));
        SetRect(infoText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(textLeft, -(h - PadBottom)), new Vector2(-RightColumn, -(PadTop + nameH)), new Vector2(0f, 1f));

        // Sag sutun: AL butonu ve hemen altinda maliyet. Ikisi de sutunun ortasinda, karta sigar.
        float colCenterX = -(RightColumn * 0.5f + 8f);
        float btnH = Mathf.Min(52f, usable * 0.46f);
        float btnY = (PadBottom - PadTop) * 0.5f + usable * 0.12f;
        RectTransform buttonRt = buyButton.GetComponent<RectTransform>();
        buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(1f, 0.5f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = new Vector2(colCenterX, btnY);
        buttonRt.sizeDelta = new Vector2(RightColumn - 36f, btnH);
        SetRect(costText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));
        costText.rectTransform.anchoredPosition = new Vector2(colCenterX, btnY - btnH * 0.5f - 3f);
        costText.rectTransform.sizeDelta = new Vector2(RightColumn - 12f, 26f);

        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 22f;
        nameText.fontSizeMax = 36f;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.22f, 0.12f, 0.07f);
        infoText.enableAutoSizing = true;
        infoText.fontSizeMin = 16f;
        infoText.fontSizeMax = 27f;
        infoText.textWrappingMode = TextWrappingModes.Normal;
        infoText.overflowMode = TextOverflowModes.Ellipsis;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.color = new Color(0.29f, 0.2f, 0.14f);
        costText.enableAutoSizing = true;
        costText.fontSizeMin = 15f;
        costText.fontSizeMax = 23f;
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
            iconRt.anchoredPosition = new Vector2(IconPad, (PadBottom - PadTop) * 0.5f);
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);
        }

        buyImage = buyButton.GetComponent<Image>();
        buyLabel = buyButton.GetComponentInChildren<TMP_Text>();
        if (buyLabel != null)
        {
            buyLabel.text = UIArt.ClaimSpriteHasText && UIArt.Has(UIArt.ClaimReady) ? "" : Loc.T("AL");
            buyLabel.fontStyle = FontStyles.Bold;
            buyLabel.enableAutoSizing = true;
            buyLabel.fontSizeMin = 16f;
            buyLabel.fontSizeMax = 28f;
            buyLabel.rectTransform.offsetMin = new Vector2(8f, 2f);
            buyLabel.rectTransform.offsetMax = new Vector2(-8f, -2f);
        }
        buyButton.onClick.RemoveAllListeners();
        if (buyButton.GetComponent<UIButtonMotion>() == null) buyButton.gameObject.AddComponent<UIButtonMotion>();
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
