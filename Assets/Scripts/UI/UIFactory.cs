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
        go.AddComponent<UIButtonMotion>();

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
        UIArt.StyleButton(btn, labelText);
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

    // ---- Kodla uretilen, dosya gerektirmeyen yumusak sekiller (menu / parlama) ----

    static Sprite rounded, radial, vignette, verticalFade;

    // Kose yaricapı 9-slice kenarlikli beyaz kutu. Image.type = Sliced ile kullanilir.
    public static Sprite RoundedSprite()
    {
        if (rounded != null) return rounded;
        const int n = 64, r = 18;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float px0 = x + 0.5f, py0 = y + 0.5f;
            float qx = Mathf.Abs(px0 - n * 0.5f) - (n * 0.5f - r);
            float qy = Mathf.Abs(py0 - n * 0.5f) - (n * 0.5f - r);
            float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            float a = Mathf.Clamp01(0.5f - d);
            px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return rounded;
    }

    // Merkezi parlak, kenarlara dogru sonen yuvarlak (isik / hale).
    public static Sprite RadialSprite()
    {
        if (radial != null) return radial;
        radial = BuildRadial(false);
        return radial;
    }

    // Kenarlari koyulastiran tam ekran vinyet.
    public static Sprite VignetteSprite()
    {
        if (vignette != null) return vignette;
        vignette = BuildRadial(true);
        return vignette;
    }

    // Alttan yukariya dogru seffaflasan beyaz gecis (Image rengiyle boyanir).
    public static Sprite VerticalFadeSprite()
    {
        if (verticalFade != null) return verticalFade;
        const int n = 64;
        var tex = new Texture2D(4, n, TextureFormat.RGBA32, false);
        var px = new Color32[4 * n];
        for (int y = 0; y < n; y++)
        {
            byte a = (byte)(255f * (1f - Mathf.SmoothStep(0f, 1f, y / (float)(n - 1))));
            for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        verticalFade = Sprite.Create(tex, new Rect(0, 0, 4, n), new Vector2(0.5f, 0.5f), 1f);
        return verticalFade;
    }

    static Sprite BuildRadial(bool edges)
    {
        const int n = 96;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f;
            float dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = edges
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1.05f, r))
                : Mathf.Clamp01(1f - r);
            if (!edges) a *= a;
            px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // Yuvarlak koseli panel (menu kartlari / buton cerceveleri).
    public static Image CreateRoundedPanel(Transform parent, string name, Color color, bool blocksRaycast)
    {
        var rt = CreatePanel(parent, name, color, blocksRaycast);
        var img = rt.GetComponent<Image>();
        img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        return img;
    }

    // Var olan bir butonu / paneli yuvarlak koseli yapar.
    public static void MakeRounded(Image img)
    {
        if (img == null) return;
        img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
    }
}
