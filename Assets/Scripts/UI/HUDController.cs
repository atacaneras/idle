using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Metinler")]
    [SerializeField] TMP_Text resourcesText;
    [SerializeField] TMP_Text ageText;
    [SerializeField] TMP_Text knowledgeText;
    [SerializeField] TMP_Text offlineText;

    [Header("Butonlar")]
    [SerializeField] Button gatherButton;
    [SerializeField] Button advanceButton;
    [SerializeField] TMP_Text advanceButtonLabel;

    [Header("Bina listesi")]
    [SerializeField] Transform rowParent;
    [SerializeField] UIBuildingRow rowPrefab;

    [Header("Geri bildirim (opsiyonel, bos birakilirsa otomatik eklenir)")]
    [SerializeField] FloatingTextSpawner floatingText;
    [SerializeField] float offlineMessageSeconds = 8f;

    readonly List<UIBuildingRow> rows = new List<UIBuildingRow>();
    readonly StringBuilder sb = new StringBuilder();

    void Start()
    {
        var gm = GameManager.Instance;
        gm.OnAgeChanged += RebuildRows;
        gm.OnStateChanged += Refresh;
        gatherButton.onClick.AddListener(gm.Gather);
        advanceButton.onClick.AddListener(gm.AdvanceAge);

        // Yuzen "+X" yazilari ve buton punch animasyonu: sahneyi elle duzenlemeye gerek yok.
        if (floatingText == null) floatingText = GetComponent<FloatingTextSpawner>();
        if (floatingText == null) floatingText = gameObject.AddComponent<FloatingTextSpawner>();
        var feedback = gatherButton.GetComponent<GatherButtonFeedback>();
        if (feedback == null) feedback = gatherButton.gameObject.AddComponent<GatherButtonFeedback>();
        feedback.Init(floatingText);

        // Toplu satin alma butonlari, sesler, cag gecisi ve pasif uretim baloncuklari (hepsi kodla kurulur)
        GetOrAdd<BuyModeSelector>();
        GetOrAdd<SoundManager>();
        GetOrAdd<AgeTransition>();
        GetOrAdd<PassiveIncomeBubbles>().Init(floatingText, FindFirstObjectByType<SceneBuildingSpawner>());

        RebuildRows();
        Refresh();

        if (offlineText != null)
        {
            if (gm.OfflineSecondsApplied > 1)
            {
                offlineText.text = "Sen yokken " + FormatDuration(gm.OfflineSecondsApplied) + " ilerleme kazandın";
                Invoke(nameof(HideOfflineText), offlineMessageSeconds);   // sonsuza kadar ekranda kalmasin
            }
            else offlineText.text = "";
        }
    }

    T GetOrAdd<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }

    void HideOfflineText() { if (offlineText != null) offlineText.text = ""; }

    static string FormatDuration(double seconds)
    {
        int s = Mathf.RoundToInt((float)seconds);
        if (s < 90) return s + " sn";
        int m = s / 60;
        if (m < 90) return m + " dk";
        return (m / 60) + " sa " + (m % 60) + " dk";
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnAgeChanged -= RebuildRows;
        gm.OnStateChanged -= Refresh;
    }

    void RebuildRows()
    {
        foreach (var r in rows) Destroy(r.gameObject);
        rows.Clear();
        foreach (var b in GameManager.Instance.CurrentAge.buildings)
        {
            var row = Instantiate(rowPrefab, rowParent);
            row.Setup(b);
            rows.Add(row);
        }
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        var age = gm.CurrentAge;

        ageText.text = age.displayName;
        knowledgeText.text = "Bilgi: " + NumberFormatter.Format(gm.Knowledge)
            + "  (x" + gm.Multiplier.ToString("0.0", CultureInfo.InvariantCulture) + ")";

        sb.Length = 0;
        foreach (var r in age.resources)
        {
            sb.Append(r.displayName).Append(": ")
              .Append(NumberFormatter.Format(gm.GetResource(r)))
              .Append("  (+").Append(NumberFormatter.Format(gm.GetProductionPerSecond(r))).Append("/sn)\n");
        }
        resourcesText.text = sb.ToString();

        foreach (var row in rows) row.Refresh();

        var next = gm.NextAge;
        advanceButton.interactable = gm.CanAdvance;
        if (advanceButtonLabel != null)
        {
            advanceButtonLabel.text = next != null
                ? next.displayName + "'na Geç (" + NumberFormatter.Format(age.advanceCost) + " " + age.advanceResource.displayName + ")"
                : "Son Çağ";
        }
    }
}
