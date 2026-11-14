using UnityEngine;

// Sahneye elle bir sey eklemeden: oyun acilinca giris ekranini ve kuleyi kurar.
// Taraf kayitliysa direkt kuleye gecer, degilse once taraf secimi gelir.
public class TowerBootstrap : MonoBehaviour
{
    TowerView tower;
    Side side;

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
        tower = new GameObject("TowerView", typeof(RectTransform)).AddComponent<TowerView>();
        tower.transform.SetParent(canvas.transform, false);
        tower.Build(canvas);
        gm.OnAgeChanged += OnAgeChanged;
        FloorBar.Create(canvas);
        QuestPanel.Create(canvas);

        if (gm.CurrentSide == Side.None)
        {
            var sel = SideSelectScreen.Create(canvas);
            sel.onChosen = s =>
            {
                gm.ChooseSide(s);
                side = s;
                Destroy(sel.gameObject);
                tower.PlayIntro(s, null);
            };
        }
        else { side = gm.CurrentSide; tower.SnapTo(TowerView.FloorIndex(side, FloorOfAge())); }
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
