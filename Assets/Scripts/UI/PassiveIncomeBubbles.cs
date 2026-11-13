using System.Collections.Generic;
using UnityEngine;

// Binalarin uzerinde belli araliklarla kucuk "+0.5" baloncuklari gosterir.
// Gosterilen miktar, o aralikta bina grubunun gercekten urettigi miktardir.
public class PassiveIncomeBubbles : MonoBehaviour
{
    [SerializeField] bool bubblesEnabled = true;
    [SerializeField] float interval = 2.5f;                  // her bina icin ortalama balon araligi (sn)
    [SerializeField] float scale = 0.55f;                    // tiklama yazilarindan kucuk
    [SerializeField] float riseMultiplier = 0.6f;
    [SerializeField] Color color = new Color(0.85f, 1f, 0.85f);
    [SerializeField] int maxActiveTexts = 25;                // ekran dolarsa balon cikarmaz, tiklama yazilarina yer birakir

    FloatingTextSpawner texts;
    SceneBuildingSpawner scene;
    Camera cam;
    readonly Dictionary<string, float> timers = new Dictionary<string, float>();

    public void Init(FloatingTextSpawner t, SceneBuildingSpawner s)
    {
        texts = t;
        scene = s;
    }

    void Update()
    {
        if (!bubblesEnabled || texts == null || scene == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentAge == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float dt = Time.unscaledDeltaTime;
        foreach (var b in gm.CurrentAge.buildings)
        {
            int n = gm.GetCount(b);
            if (n <= 0) continue;

            float timer;
            if (!timers.TryGetValue(b.id, out timer)) timer = Random.Range(0f, interval);   // baslangicta dagit
            timer -= dt;

            if (timer <= 0f)
            {
                timer = interval * Random.Range(0.85f, 1.15f);
                Emit(gm, b, n);
            }
            timers[b.id] = timer;
        }
    }

    void Emit(GameManager gm, BuildingData b, int n)
    {
        if (texts.ActiveCount >= maxActiveTexts) return;
        Vector3 world;
        if (!scene.TryGetTopPosition(b, out world)) return;

        double amount = gm.GetBuildingProductionPerSecond(b) * interval;
        Vector2 screen = cam.WorldToScreenPoint(world);
        texts.Spawn("+" + NumberFormatter.Format(amount), screen, color, scale, riseMultiplier);
    }
}
