using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sahneye elle bir sey eklemeden: oyun acilinca giris ekranini ve kuleyi kurar.
// Taraf kayitliysa direkt kuleye gecer, degilse once taraf secimi gelir.
public class TowerBootstrap : MonoBehaviour
{
    TowerView tower;
    Side side;
    Canvas rootCanvas;
    Transform sideMenuRoot;
    CanvasGroup hudGroup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (GameManager.Instance == null) return;
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        canvas = canvas.rootCanvas;
        canvas.gameObject.AddComponent<TowerBootstrap>().Begin(canvas);
    }

    void Begin(Canvas canvas)
    {
        var gm = GameManager.Instance;
        rootCanvas = canvas;
        hudGroup = canvas.GetComponent<CanvasGroup>();
        if (hudGroup == null) hudGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        if (gm.CurrentSide == Side.None) SetHudVisible(false);
        tower = new GameObject("TowerView", typeof(RectTransform)).AddComponent<TowerView>();
        tower.transform.SetParent(canvas.transform, false);
        tower.Build(canvas);
        gm.OnAgeChanged += OnAgeChanged;
        FloorBar.Create(canvas);
        QuestPanel.Create(canvas);

        if (gm.CurrentSide == Side.None)
        {
            SetHudVisible(false);
            var sel = SideSelectScreen.Create(canvas);
            sideMenuRoot = sel.transform;
            HideGameplayUI();
            sel.onChosen = s =>
            {
                gm.ChooseSide(s);
                side = s;
                sel.gameObject.SetActive(false);
                Destroy(sel.gameObject);
                sideMenuRoot = null;
                HideGameplayUI();
                tower.PlayIntro(s, () => StartCoroutine(FadeHudIn()));
            };
        }
        else
        {
            side = gm.CurrentSide;
            SetHudVisible(true);
            tower.SnapTo(TowerView.FloorIndex(side, FloorOfAge()));
        }
    }

    void SetHudVisible(bool visible)
    {
        if (hudGroup == null) return;
        hudGroup.alpha = visible ? 1f : 0f;
        hudGroup.interactable = visible;
        hudGroup.blocksRaycasts = visible;
    }

    void LateUpdate()
    {
        // Scene UI can create nested override-sorting canvases after bootstrap.
        // Keep those hidden while the side-selection menu is active as well.
        if (side == Side.None && sideMenuRoot != null) HideGameplayUI();
    }

    void HideGameplayUI()
    {
        SetHudVisible(false);
        if (rootCanvas == null) return;

        var canvases = rootCanvas.GetComponentsInChildren<Canvas>(true);
        foreach (var childCanvas in canvases)
        {
            if (childCanvas == rootCanvas) continue;
            if (sideMenuRoot != null && childCanvas.transform.IsChildOf(sideMenuRoot)) continue;
            if (childCanvas.name == "SettingsOverlay") continue;

            var group = childCanvas.GetComponent<CanvasGroup>();
            if (group == null) group = childCanvas.gameObject.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            childCanvas.enabled = false;
        }
    }

    IEnumerator FadeHudIn()
    {
        var groups = new List<CanvasGroup>();
        var canvases = rootCanvas != null ? rootCanvas.GetComponentsInChildren<Canvas>(true) : new Canvas[0];
        foreach (var childCanvas in canvases)
        {
            if (childCanvas == rootCanvas) continue;
            if (childCanvas.name == "SettingsOverlay") continue;
            var group = childCanvas.GetComponent<CanvasGroup>();
            if (group == null) group = childCanvas.gameObject.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            childCanvas.enabled = true;
            groups.Add(group);
        }

        SetHudVisible(false);
        const float fadeDuration = 0.8f;
        for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
            hudGroup.alpha = t;
            for (int i = 0; i < groups.Count; i++) groups[i].alpha = t;
            yield return null;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            groups[i].alpha = 1f;
            groups[i].interactable = true;
            groups[i].blocksRaycasts = true;
        }
        SetHudVisible(true);
    }

    // Gecici eslesme: mevcut cag sirasi = kat numarasi (1..7). Yeni icerikte kat dogrudan GameManager'dan gelecek.
    int FloorOfAge()
    {
        var gm = GameManager.Instance;
        return Mathf.Clamp(gm.FloorNumber, 1, TowerView.MaxFloor);
    }

    void OnAgeChanged()
    {
        if (side == Side.None || tower.Busy) return;
        tower.GoToFloor(TowerView.FloorIndex(side, FloorOfAge()));
    }

    void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.OnAgeChanged -= OnAgeChanged; }
}
