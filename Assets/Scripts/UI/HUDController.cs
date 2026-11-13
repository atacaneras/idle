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

    readonly List<UIBuildingRow> rows = new List<UIBuildingRow>();
    readonly StringBuilder sb = new StringBuilder();

    void Start()
    {
        var gm = GameManager.Instance;
        gm.OnAgeChanged += RebuildRows;
        gm.OnStateChanged += Refresh;
        gatherButton.onClick.AddListener(gm.Gather);
        advanceButton.onClick.AddListener(gm.AdvanceAge);

        RebuildRows();
        Refresh();

        if (offlineText != null)
        {
            offlineText.text = gm.OfflineSecondsApplied > 1
                ? "Sen yokken " + Mathf.RoundToInt((float)gm.OfflineSecondsApplied / 60f) + " dk ilerleme kazandın"
                : "";
        }
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
