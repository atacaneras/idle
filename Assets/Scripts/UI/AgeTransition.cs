using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cag atlayinca: kisa bir beyaz parlama + ortada yeni cagin adi (buyuyup solar).
// Gokyuzu/zemin renk gecisi SceneBuildingSpawner'da yapilir.
public class AgeTransition : MonoBehaviour
{
    [SerializeField] float totalDuration = 2.2f;
    [SerializeField] float flashDuration = 0.6f;
    [SerializeField] float flashStrength = 0.55f;

    Image flash;
    TextMeshProUGUI banner;
    RectTransform bannerRt;
    float t = -1f;

    void Start()
    {
        var canvas = GetComponentInParent<Canvas>().rootCanvas;

        var fgo = new GameObject("AgeFlash", typeof(RectTransform), typeof(Image));
        var frt = (RectTransform)fgo.transform;
        frt.SetParent(canvas.transform, false);
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        flash = fgo.GetComponent<Image>();
        flash.color = new Color(1f, 1f, 1f, 0f);
        flash.raycastTarget = false;
        fgo.SetActive(false);

        var bgo = new GameObject("AgeBanner", typeof(RectTransform));
        bannerRt = (RectTransform)bgo.transform;
        bannerRt.SetParent(canvas.transform, false);
        bannerRt.anchorMin = bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRt.pivot = new Vector2(0.5f, 0.5f);
        bannerRt.anchoredPosition = new Vector2(0f, 120f);
        bannerRt.sizeDelta = new Vector2(1200f, 200f);
        banner = bgo.AddComponent<TextMeshProUGUI>();
        banner.fontSize = 120f;
        banner.fontStyle = FontStyles.Bold;
        banner.alignment = TextAlignmentOptions.Center;
        banner.color = new Color(1f, 0.9f, 0.45f);
        banner.raycastTarget = false;
        banner.outlineWidth = 0.3f;
        banner.outlineColor = new Color32(60, 30, 5, 255);
        bgo.SetActive(false);

        GameManager.Instance.OnAgeChanged += Play;
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.OnAgeChanged -= Play;
    }

    void Play()
    {
        banner.text = Loc.T(GameManager.Instance.CurrentAge.displayName);
        t = 0f;
        flash.gameObject.SetActive(true);
        bannerRt.gameObject.SetActive(true);
        flash.transform.SetAsLastSibling();
        bannerRt.SetAsLastSibling();
    }

    void Update()
    {
        if (t < 0f) return;
        t += Time.unscaledDeltaTime;

        if (t >= totalDuration)
        {
            t = -1f;
            flash.gameObject.SetActive(false);
            bannerRt.gameObject.SetActive(false);
            return;
        }

        // Parlama: hemen en parlak, sonra solar
        float fk = Mathf.Clamp01(t / flashDuration);
        flash.color = new Color(1f, 1f, 1f, flashStrength * (1f - fk) * (1f - fk));

        // Baslik: ilk 0.4 sn'de buyuyerek gelir, son 0.7 sn'de solar
        float inK = Mathf.Clamp01(t / 0.4f);
        float x = inK - 1f;
        float back = 1f + 2.70158f * x * x * x + 1.70158f * x * x;   // easeOutBack
        float s = Mathf.Lerp(0.5f, 1f, back);
        bannerRt.localScale = new Vector3(s, s, 1f);

        float fadeStart = totalDuration - 0.7f;
        float a = t < fadeStart ? 1f : 1f - (t - fadeStart) / 0.7f;
        var c = banner.color;
        c.a = Mathf.Clamp01(a);
        banner.color = c;
    }
}
