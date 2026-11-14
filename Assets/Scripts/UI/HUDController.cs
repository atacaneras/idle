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
#if UNITY_EDITOR || DEBUG
    Button fastTestButton;
    TMP_Text fastTestButtonLabel;
#endif

    [Header("Bina listesi")]
    [SerializeField] Transform rowParent;
    [SerializeField] UIBuildingRow rowPrefab;

    [Header("Geri bildirim (opsiyonel, bos birakilirsa otomatik eklenir)")]
    [SerializeField] FloatingTextSpawner floatingText;
    [SerializeField] float offlineMessageSeconds = 8f;

    SceneBuildingSpawner sceneSpawner;
    readonly List<UIBuildingRow> rows = new List<UIBuildingRow>();
    readonly StringBuilder sb = new StringBuilder();

    void Start()
    {
        var gm = GameManager.Instance;
        PolishHud();
        BuildFastTestButton();
        gm.OnAgeChanged += RebuildRows;
        gm.OnStateChanged += Refresh;
        gatherButton.onClick.AddListener(gm.Gather);
        advanceButton.onClick.AddListener(OnAdvanceClicked);

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
        sceneSpawner = FindAnyObjectByType<SceneBuildingSpawner>();
        gm.OnMilestoneReached += OnMilestone;
        GetOrAdd<PassiveIncomeBubbles>().Init(floatingText, sceneSpawner);
        GetOrAdd<UpgradePanel>();
        GetOrAdd<TimeMachinePanel>();
        GetOrAdd<SettingsPanel>();
        GetOrAdd<AchievementPanel>();
        GetOrAdd<Hotkeys>();
        GetOrAdd<TutorialHints>().Init(floatingText);
        GetOrAdd<AchievementSystem>().Init(floatingText);

        // Steam Deck / gamepad: ilk secili oge Topla olsun; yon tuslariyla gezilebilsin.
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null && gatherButton != null) es.SetSelectedGameObject(gatherButton.gameObject);

        RebuildRows();
        Refresh();

        if (offlineText != null)
        {
            if (gm.SaveRecovered)
            {
                offlineText.text = Loc.T("Kayıt dosyası bozuktu, yedekten kurtarıldı.");
                Invoke(nameof(HideOfflineText), offlineMessageSeconds * 2f);
            }
            else if (gm.OfflineSecondsApplied > 1)
            {
                offlineText.text = Loc.F("Sen yokken {0} ilerleme kazandın", FormatDuration(gm.OfflineSecondsApplied));
                Invoke(nameof(HideOfflineText), offlineMessageSeconds);   // sonsuza kadar ekranda kalmasin
            }
            else offlineText.text = "";
        }
    }

    // Son cagda bu buton Zaman Makinesi panelini acar (panel onay ve odul onizlemesi gosterir)
    void OnAdvanceClicked()
    {
        var gm = GameManager.Instance;
        if (gm.IsFinalAge) { var tm = GetComponent<TimeMachinePanel>(); if (tm != null) tm.Open(); }
        else gm.AdvanceAge();
    }

    T GetOrAdd<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }

    void BuildFastTestButton()
    {
        // Test modu sadece editorde ve development build'lerde gorunur; oyuncu build'inde yok.
#if UNITY_EDITOR || DEBUG
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        fastTestButton = UIFactory.CreateButton(canvas.rootCanvas.transform, "FastTestToggle", "", 24f, out var label);
        fastTestButtonLabel = label;
        var rt = (RectTransform)fastTestButton.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(365f, 150f);
        rt.sizeDelta = new Vector2(240f, 76f);
        fastTestButton.onClick.AddListener(GameManager.Instance.ToggleFastTestMode);
        RefreshFastTestButton();
#endif
    }

    void RefreshFastTestButton()
    {
#if UNITY_EDITOR || DEBUG
        if (fastTestButton == null || GameManager.Instance == null) return;
        bool enabled = GameManager.Instance.FastTestMode;
        fastTestButtonLabel.text = enabled ? "TEST MODU AÇIK\nTık x1M • Üret x1K" : "HIZLI TEST: KAPALI";
        fastTestButtonLabel.fontSize = 20f;
        fastTestButtonLabel.fontSizeMin = 17f;
        fastTestButtonLabel.fontSizeMax = 22f;
        fastTestButtonLabel.enableAutoSizing = true;
        fastTestButtonLabel.color = enabled ? new Color(0.2f, 0.14f, 0.07f) : Color.white;
        fastTestButton.GetComponent<Image>().color = enabled
            ? new Color(0.96f, 0.73f, 0.28f)
            : new Color(0.25f, 0.2f, 0.18f, 0.96f);
#endif
    }

    void PolishHud()
    {
        SetTopText(resourcesText, new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(620f, 150f), TextAlignmentOptions.TopLeft);
        SetTopText(ageText, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(780f, 88f), TextAlignmentOptions.Midline);
        SetTopText(knowledgeText, new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(500f, 56f), TextAlignmentOptions.MidlineRight);

        if (resourcesText != null)
        {
            resourcesText.enableAutoSizing = true;
            resourcesText.fontSizeMin = 26f;
            resourcesText.fontSizeMax = 36f;
            resourcesText.color = new Color(0.16f, 0.13f, 0.12f);
        }
        if (ageText != null)
        {
            ageText.enableAutoSizing = true;
            ageText.fontSizeMin = 38f;
            ageText.fontSizeMax = 54f;
            ageText.color = new Color(0.17f, 0.13f, 0.11f);
        }
        if (knowledgeText != null)
        {
            knowledgeText.enableAutoSizing = true;
            knowledgeText.fontSizeMin = 22f;
            knowledgeText.fontSizeMax = 30f;
            knowledgeText.color = new Color(0.22f, 0.16f, 0.12f);
            knowledgeText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        var gatherImage = gatherButton != null ? gatherButton.GetComponent<Image>() : null;
        if (gatherImage != null) gatherImage.color = new Color(0.96f, 0.73f, 0.28f);
        var gatherLabel = gatherButton != null ? gatherButton.GetComponentInChildren<TMP_Text>() : null;
        if (gatherLabel != null)
        {
            gatherLabel.text = Loc.T("TOPLA");
            gatherLabel.fontStyle = FontStyles.Bold;
            gatherLabel.fontSize = 28f;
            gatherLabel.color = new Color(0.24f, 0.17f, 0.1f);
        }
        if (advanceButtonLabel != null)
        {
            advanceButtonLabel.enableAutoSizing = true;
            advanceButtonLabel.fontSizeMin = 18f;
            advanceButtonLabel.fontSizeMax = 24f;
            advanceButtonLabel.fontStyle = FontStyles.Bold;
        }
    }

    static void SetTopText(TMP_Text text, Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    // Kilometre tasi esigi gecilince binanin ustunde "x2!" cikar
    void OnMilestone(BuildingData b, int count)
    {
        Vector3 world;
        var cam = Camera.main;
        if (sceneSpawner == null || cam == null || !sceneSpawner.TryGetTopPosition(b, out world)) return;
        string msg = "x" + NumberFormatter.Format(GameManager.Instance.MilestoneMultiplier) + "!";
        floatingText.Spawn(msg, cam.WorldToScreenPoint(world), new Color(1f, 0.85f, 0.25f), 1.4f, 1.2f);
        SoundManager.Play(SoundId.LevelUp, 1.1f, 1f);
    }

    void HideOfflineText() { if (offlineText != null) offlineText.text = ""; }

    static string FormatDuration(double seconds)
    {
        int s = Mathf.RoundToInt((float)seconds);
        if (s < 90) return s + " " + Loc.T("sn");
        int m = s / 60;
        if (m < 90) return m + " " + Loc.T("dk");
        return (m / 60) + " " + Loc.T("sa") + " " + (m % 60) + " " + Loc.T("dk");
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnAgeChanged -= RebuildRows;
        gm.OnStateChanged -= Refresh;
        gm.OnMilestoneReached -= OnMilestone;
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
#if UNITY_EDITOR || DEBUG
        RefreshFastTestButton();
#endif

        ageText.text = Loc.T(age.displayName);
        knowledgeText.text = Loc.F("Bilgi: {0}  (x{1})", NumberFormatter.Format(gm.Knowledge),
                gm.Multiplier.ToString("0.0", CultureInfo.InvariantCulture))
            + (gm.TimeLoops > 0 ? Loc.F("  •  Döngü {0}", gm.TimeLoops) : "");

        sb.Length = 0;
        foreach (var r in age.resources)
        {
            sb.Append(Loc.F("{0}: {1}  (+{2}/sn)", Loc.T(r.displayName),
                    NumberFormatter.Format(gm.GetResource(r)),
                    NumberFormatter.Format(gm.GetProductionPerSecond(r))))
              .Append('\n');
        }
        resourcesText.text = sb.ToString();

        foreach (var row in rows) row.Refresh();

        var next = gm.NextAge;
        if (next != null)
        {
            advanceButton.interactable = gm.CanAdvance;
            if (advanceButtonLabel != null)
                advanceButtonLabel.text = Loc.F("{0}'na Geç", Loc.T(next.displayName)) + "\n"
                    + NumberFormatter.Format(gm.GetRunEarned(age.advanceResource)) + " / "
                    + NumberFormatter.Format(gm.AdvanceCost) + " " + Loc.T(age.advanceResource.displayName);
        }
        else
        {
            // Son cag: cag atlama yerine Zaman Makinesi
            advanceButton.interactable = gm.CanUseTimeMachine;
            if (advanceButtonLabel != null)
                advanceButtonLabel.text = gm.CanUseTimeMachine
                    ? Loc.T("Zaman Makinesi") + "\n+" + NumberFormatter.Format(gm.TimeMachineReward) + " " + Loc.T("Bilgi")
                    : Loc.T("Zaman Makinesi") + "\n" + NumberFormatter.Format(gm.GetRunEarned(age.advanceResource)) + " / "
                        + NumberFormatter.Format(gm.AdvanceCost) + " " + Loc.T(age.advanceResource.displayName);
        }

        bool canProgress = advanceButton.interactable;
        var advanceImage = advanceButton.GetComponent<Image>();
        if (advanceImage != null)
            advanceImage.color = canProgress ? new Color(0.91f, 0.63f, 0.28f) : new Color(0.66f, 0.58f, 0.53f);
        if (advanceButtonLabel != null)
            advanceButtonLabel.color = canProgress ? new Color(0.25f, 0.17f, 0.1f) : new Color(0.32f, 0.28f, 0.26f);
    }
}
