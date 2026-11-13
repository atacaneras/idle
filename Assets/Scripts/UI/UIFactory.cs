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

    // Renkli, istege bagli raycast alan bir dikdortgen (panel / arka plan)
    public static RectTransform CreatePanel(Transform parent, string name, Color color, bool blocksRaycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = blocksRaycast;
        return rt;
    }

    // Verilen anchor/offset ile metin olusturur. autoSize: kutuya sigmazsa kuculur.
    public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
        TextAlignmentOptions align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        bool bold = false, bool autoSize = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold;
        if (autoSize)
        {
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(10f, size * 0.55f);
        }
        return t;
    }
}
