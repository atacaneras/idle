using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sahneye elle buton koymadan, kodla buton uretmek icin kucuk yardimci.
public static class UIFactory
{
    public static Button CreateButton(Transform parent, string name, string label, float fontSize, out TextMeshProUGUI labelText)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lgo = new GameObject("Label", typeof(RectTransform));
        var lrt = (RectTransform)lgo.transform;
        lrt.SetParent(go.transform, false);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        labelText = lgo.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = fontSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.raycastTarget = false;
        return btn;
    }
}
