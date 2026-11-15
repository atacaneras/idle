using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bina listesinin altinda (sag alt) x1 / x10 / x100 / Max butonlarini kodla olusturur.
// Secim PlayerPrefs'te saklanir.
public class BuyModeSelector : MonoBehaviour
{
    const string PrefKey = "evrim_buymode";
    static readonly string[] Labels = { "x1", "x10", "x100", "Max" };
    static readonly Color OnColor = new Color(0.95f, 0.75f, 0.25f, 1f);
    static readonly Color OffColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);

    readonly Image[] images = new Image[4];
    readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[4];

    void Start()
    {
        var canvas = GetComponentInParent<Canvas>().rootCanvas;

        var bar = new GameObject("BuyModeBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var rt = (RectTransform)bar.transform;
        rt.SetParent(canvas.transform, false);
        rt.SetAsFirstSibling();   // diger arayuzun arkasinda kalsin, yuzen yazilari engellemesin
        // Alt barin sag tarafi: [x1 x10 x100 Max] solda, Ses butonu en sagda
        const float muteW = 216f;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-(HudLayout.Margin + muteW + HudLayout.Gap), (HudLayout.BottomBarH - 80f) * 0.5f);
        rt.sizeDelta = new Vector2(HudLayout.RightW - muteW - HudLayout.Gap, 80f);

        var layout = bar.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;

        for (int i = 0; i < Labels.Length; i++)
        {
            int mode = i;
            TextMeshProUGUI lbl;
            var btn = UIFactory.CreateButton(bar.transform, "Buy" + Labels[i], Labels[i], 34f, out lbl);
            btn.onClick.AddListener(() => Select(mode));
            images[i] = btn.GetComponent<Image>();
            labels[i] = lbl;
        }

        var gm = GameManager.Instance;
        gm.OnBuyModeChanged += Highlight;
        gm.SetBuyMode((BuyMode)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, Labels.Length - 1));
        Highlight();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.OnBuyModeChanged -= Highlight;
    }

    void Select(int mode)
    {
        GameManager.Instance.SetBuyMode((BuyMode)mode);
        PlayerPrefs.SetInt(PrefKey, mode);
    }

    void Highlight()
    {
        int current = (int)GameManager.Instance.CurrentBuyMode;
        for (int i = 0; i < images.Length; i++)
        {
            bool on = i == current;
            images[i].color = on ? OnColor : OffColor;
            labels[i].color = on ? new Color(0.2f, 0.12f, 0.02f) : Color.white;
        }
    }
}
