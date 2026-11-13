using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Tiklayinca cikan yuzen "+1" yazilari. Canvas ile ayni objede durur (HUDController yoksa kendisi ekler).
// Yazilar havuzdan alinir, tek bir Update dongusuyle animasyonlanir: otomatik tiklayici gibi hizli
// olaylarda bile cop (GC) uretmez.
public class FloatingTextSpawner : MonoBehaviour
{
    [Header("Gorunum")]
    [SerializeField] float fontSize = 56f;
    [SerializeField] Color textColor = new Color(1f, 0.93f, 0.55f);
    [SerializeField] Color outlineColor = new Color(0.25f, 0.12f, 0.02f, 1f);

    [Header("Hareket")]
    [SerializeField] float duration = 0.9f;
    [SerializeField] float riseDistance = 140f;      // Canvas birimi (1920x1080 referansi)
    [SerializeField] float horizontalJitter = 45f;   // her yazi rastgele saga/sola kayar
    [SerializeField] int maxActive = 40;

    class Popup
    {
        public RectTransform rt;
        public TextMeshProUGUI text;
        public Vector2 start;
        public float drift;
        public float age;
        public float life;
        public Color color;
        public float scale = 1f;
        public float rise = 1f;
    }

    readonly List<Popup> active = new List<Popup>();
    readonly Stack<Popup> pool = new Stack<Popup>();
    RectTransform container;
    Canvas rootCanvas;

    void Awake()
    {
        var canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
        if (rootCanvas == null) { Debug.LogError("FloatingTextSpawner: Canvas bulunamadi."); enabled = false; return; }

        // Tum ekrani kaplayan, raycast almayan kap. Yazilar hep arayuzun ustunde kalir.
        var go = new GameObject("FloatingTexts", typeof(RectTransform));
        container = (RectTransform)go.transform;
        container.SetParent(rootCanvas.transform, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        container.pivot = new Vector2(0.5f, 0.5f);
        container.SetAsLastSibling();
    }

    public int ActiveCount { get { return active.Count; } }

    // Ekran pikseli konumunda yazi cikarir (fare / dokunma konumu).
    public void Spawn(string message, Vector2 screenPosition)
    {
        Spawn(message, screenPosition, textColor, 1f, 1f);
    }

    // Renk, boyut carpani ve yukselme carpani ile (pasif uretim baloncuklari icin)
    public void Spawn(string message, Vector2 screenPosition, Color color, float scale, float riseMultiplier)
    {
        if (container == null || active.Count >= maxActive) return;

        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPosition, cam, out local)) return;

        var p = pool.Count > 0 ? pool.Pop() : CreatePopup();
        p.start = local + new Vector2(0f, 30f);   // imlecin biraz ustunden basla
        p.drift = Random.Range(-horizontalJitter, horizontalJitter);
        p.age = 0f;
        p.life = duration;
        p.color = color;
        p.scale = scale;
        p.rise = riseMultiplier;
        p.text.text = message;
        p.text.color = color;
        p.rt.anchoredPosition = p.start;
        p.rt.localScale = Vector3.one * 0.5f * scale;
        p.rt.gameObject.SetActive(true);
        p.rt.SetAsLastSibling();
        active.Add(p);
    }

    // Bir UI elemaninin (ornegin butonun) ortasinda yazi cikarir.
    public void SpawnAt(string message, RectTransform target)
    {
        if (target == null) return;
        Vector3 world = target.TransformPoint(target.rect.center);
        Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
        Spawn(message, RectTransformUtility.WorldToScreenPoint(cam, world));
    }

    Popup CreatePopup()
    {
        var go = new GameObject("Popup", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(container, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 90f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;   // tiklamalari engellemesin
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = outlineColor;

        return new Popup { rt = rt, text = tmp };
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var p = active[i];
            p.age += dt;
            float k = p.age / p.life;
            if (k >= 1f)
            {
                p.rt.gameObject.SetActive(false);
                pool.Push(p);
                active.RemoveAt(i);
                continue;
            }

            float rise = 1f - (1f - k) * (1f - k);   // yavaslayarak yuksel
            p.rt.anchoredPosition = p.start + new Vector2(p.drift * k, riseDistance * p.rise * rise);

            // Hizlica buyuyup (pop) yerine oturur
            float scale = k < 0.15f
                ? Mathf.Lerp(0.5f, 1.25f, k / 0.15f)
                : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - 0.15f) / 0.15f));
            p.rt.localScale = new Vector3(scale * p.scale, scale * p.scale, 1f);

            // Ilk %60 tam gorunur, sonra solar
            float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            var c = p.color;
            c.a = a;
            p.text.color = c;
        }
    }
}
