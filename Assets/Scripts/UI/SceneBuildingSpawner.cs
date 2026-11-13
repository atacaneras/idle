using System.Collections.Generic;
using UnityEngine;

// Sahnedeki binalari ve zemini cizer. Sprite atanmamissa renkli kutu (placeholder) kullanir.
public class SceneBuildingSpawner : MonoBehaviour
{
    [Tooltip("Bina adedi bu esiklere ulasinca gorunum lv1, lv2, lv3 olur.")]
    [SerializeField] int[] levelThresholds = { 1, 10, 25 };

    readonly Dictionary<string, SpriteRenderer> views = new Dictionary<string, SpriteRenderer>();
    static Sprite whiteSprite;

    void Start()
    {
        var gm = GameManager.Instance;
        gm.OnAgeChanged += Rebuild;
        gm.OnStateChanged += Refresh;
        Rebuild();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnAgeChanged -= Rebuild;
        gm.OnStateChanged -= Refresh;
    }

    void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        views.Clear();

        var age = GameManager.Instance.CurrentAge;

        var cam = Camera.main;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = age.skyColor; }

        // Zemin (kamera ortho size 5.4 => ekran yuksekligi 10.8 birim)
        var ground = new GameObject("ground");
        ground.transform.SetParent(transform, false);
        ground.transform.localPosition = new Vector3(0, -5.4f, 0);
        ground.transform.localScale = new Vector3(40f, 2.4f, 1f);
        var gsr = ground.AddComponent<SpriteRenderer>();
        gsr.sprite = WhiteSprite();
        gsr.color = age.groundColor;
        gsr.sortingOrder = -10;

        foreach (var b in age.buildings)
        {
            var go = new GameObject("bld_" + b.id);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = b.scenePosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = b.sortingOrder;
            views[b.id] = sr;
        }
        Refresh();
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        foreach (var b in gm.CurrentAge.buildings)
        {
            SpriteRenderer sr;
            if (!views.TryGetValue(b.id, out sr)) continue;

            int n = gm.GetCount(b);
            if (n <= 0) { sr.enabled = false; continue; }

            int level = LevelFor(n);
            sr.enabled = true;

            var sprite = b.GetSprite(level);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                sr.transform.localScale = Vector3.one;
            }
            else
            {
                float grow = 1f + 0.35f * level;   // seviye artinca kutu buyur
                sr.sprite = WhiteSprite();
                sr.color = b.placeholderColor;
                sr.transform.localScale = new Vector3(b.placeholderSize.x * grow, b.placeholderSize.y * grow, 1f);
            }
        }
    }

    int LevelFor(int count)
    {
        int level = 0;
        for (int i = 0; i < levelThresholds.Length; i++) if (count >= levelThresholds[i]) level = i;
        return level;
    }

    // 1x1 birimlik, pivotu alt-orta beyaz kare (cizerle anlastigimiz pivot kuraliyla ayni)
    static Sprite WhiteSprite()
    {
        if (whiteSprite == null)
        {
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 4f);
        }
        return whiteSprite;
    }
}
