using System.Collections.Generic;
using UnityEngine;

// Sahnedeki binalari ve zemini cizer. Sprite atanmamissa renkli kutu (placeholder) kullanir.
// Bina satin alinca animasyon oynar:
//   - Ilk bina     : zeminden "insa edilir" (0'dan buyuyup hafif tasar) + toz
//   - Normal alim  : kisa squash & stretch ziplamasi + toz
//   - Seviye atlama: buyuk ziplama + parlama + altin kivilcimlar
public class SceneBuildingSpawner : MonoBehaviour
{
    [Tooltip("Bina adedi bu esiklere ulasinca gorunum lv1, lv2, lv3 olur.")]
    [SerializeField] int[] levelThresholds = { 1, 10, 25 };

    [Header("Satin alma animasyonu")]
    [SerializeField] bool enableAnimations = true;
    [SerializeField] float buildDuration = 0.55f;
    [SerializeField] float bounceDuration = 0.4f;
    [SerializeField] float levelUpDuration = 0.75f;
    [SerializeField] int dustCount = 8;
    [SerializeField] int maxParticles = 150;

    [Header("Cag gecisi")]
    [SerializeField] float colorFadeDuration = 1.2f;   // gokyuzu ve zemin eski renkten yeniye bu surede gecer

    enum AnimKind { Build, Bounce, LevelUp }

    class View
    {
        public BuildingData data;
        public SpriteRenderer sr;
        public Vector3 baseScale = Vector3.one;
        public Color baseColor = Color.white;
        public float width = 1f;    // dunya biriminde, toz/kivilcim konumu icin
        public float height = 1f;
        public float t = -1f;       // 0..1 arasi animasyon ilerlemesi, -1 = animasyon yok
        public float duration = 0.4f;
        public AnimKind kind;
    }

    class Particle
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 pos, vel;
        public float age, life, size, gravity, grow;
        public Color color;
    }

    readonly Dictionary<string, View> views = new Dictionary<string, View>();
    readonly List<Particle> particles = new List<Particle>();
    readonly Stack<Particle> particlePool = new Stack<Particle>();
    Transform fxRoot;
    Camera cam;
    SpriteRenderer groundSr;
    bool hasColors;
    float colorT = 1f;
    Color curSky, curGround, fromSky, fromGround, toSky, toGround;
    Color dustColor = new Color(0.9f, 0.85f, 0.78f);
    static Sprite whiteSprite;
    static readonly Dictionary<string, Sprite> buildingSprites = new Dictionary<string, Sprite>();

    void Start()
    {
        fxRoot = new GameObject("fx").transform;
        fxRoot.SetParent(transform, false);

        var gm = GameManager.Instance;
        gm.OnAgeChanged += Rebuild;
        gm.OnStateChanged += Refresh;
        gm.OnBuildingBought += OnBought;
        Rebuild();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnAgeChanged -= Rebuild;
        gm.OnStateChanged -= Refresh;
        gm.OnBuildingBought -= OnBought;
    }

    void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child == fxRoot) continue;   // parcacik kabini koru
            Destroy(child.gameObject);
        }
        views.Clear();

        var age = GameManager.Instance.CurrentAge;
        dustColor = Color.Lerp(age.groundColor, Color.white, 0.55f);

        cam = Camera.main;
        if (cam != null) cam.clearFlags = CameraClearFlags.SolidColor;

        // Ilk kurulumda renkler aninda, cag atlayinca eski renkten yeniye yumusak gecis
        toSky = age.skyColor;
        toGround = age.groundColor;
        if (hasColors && colorFadeDuration > 0f)
        {
            fromSky = curSky;
            fromGround = curGround;
            colorT = 0f;
        }
        else
        {
            curSky = toSky;
            curGround = toGround;
            colorT = 1f;
        }
        hasColors = true;

        // Zemin (kamera ortho size 5.4 => ekran yuksekligi 10.8 birim)
        var ground = new GameObject("ground");
        ground.transform.SetParent(transform, false);
        ground.transform.localPosition = new Vector3(0, -5.4f, 0);
        ground.transform.localScale = new Vector3(40f, 2.4f, 1f);
        groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite = WhiteSprite();
        groundSr.sortingOrder = -10;
        ApplyColors();

        foreach (var b in age.buildings)
        {
            var go = new GameObject("bld_" + b.id);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = b.scenePosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = b.sortingOrder;
            views[b.id] = new View { data = b, sr = sr };
        }
        Refresh();
    }

    // Saniyede ~10 kez calisir. Olcek/renk dogrudan yazilmaz, Apply uzerinden gecer;
    // boylece calisan animasyon bu yenilemeyle ezilmez.
    void Refresh()
    {
        var gm = GameManager.Instance;
        foreach (var b in gm.CurrentAge.buildings)
        {
            View v;
            if (!views.TryGetValue(b.id, out v)) continue;
            var sr = v.sr;

            int n = gm.GetCount(b);
            if (n <= 0) { sr.enabled = false; v.t = -1f; continue; }

            int level = LevelFor(n);
            sr.enabled = true;

            var sprite = b.GetSprite(level);
            if (sprite != null)
            {
                sr.sprite = sprite;
                v.baseColor = Color.white;
                v.baseScale = Vector3.one;
                v.width = sprite.bounds.size.x;
                v.height = sprite.bounds.size.y;
            }
            else
            {
                // Sprite'larin arasinda bosluk kalsin; yuksek milestone seviyeleri komsu binalara tasmasin.
                float grow = 1f + 0.07f * level;
                sr.sprite = BuildingSprite(b);
                v.baseColor = Color.white; // Renkler sprite'in icine zaten uygulandi.
                v.baseScale = new Vector3(b.placeholderSize.x * grow, b.placeholderSize.y * grow, 1f);
                v.width = v.baseScale.x;
                v.height = v.baseScale.y;
            }
            Apply(v);
        }
    }

    int LevelFor(int count)
    {
        int level = 0;
        for (int i = 0; i < levelThresholds.Length; i++) if (count >= levelThresholds[i]) level = i;
        return level;
    }

    // ---- Satin alma animasyonu ----
    void OnBought(BuildingData b, int qty)
    {
        View v;
        if (!enableAnimations || !views.TryGetValue(b.id, out v)) return;

        int n = GameManager.Instance.GetCount(b);
        int before = n - qty;
        bool first = before <= 0;
        bool levelUp = !first && LevelFor(n) > LevelFor(before);

        v.kind = first ? AnimKind.Build : (levelUp ? AnimKind.LevelUp : AnimKind.Bounce);
        v.duration = first ? buildDuration : (levelUp ? levelUpDuration : bounceDuration);
        v.t = 0f;
        Apply(v);

        bool big = first || levelUp || qty >= 10;   // toplu alimda daha cok toz
        SpawnDust(v, big ? dustCount * 2 : dustCount);
        if (levelUp)
        {
            SpawnSparkles(v, 14);
            SoundManager.Play(SoundId.LevelUp);
        }
    }

    // Pasif uretim baloncuklari icin: binanin tepesinin dunya konumu
    public bool TryGetTopPosition(BuildingData b, out Vector3 world)
    {
        View v;
        if (views.TryGetValue(b.id, out v) && v.sr.enabled)
        {
            world = transform.TransformPoint(new Vector3(b.scenePosition.x, b.scenePosition.y + v.height + 0.1f, 0f));
            return true;
        }
        world = Vector3.zero;
        return false;
    }

    void ApplyColors()
    {
        if (cam != null) cam.backgroundColor = curSky;
        if (groundSr != null) groundSr.color = curGround;
    }

    void Apply(View v)
    {
        Vector3 s = v.baseScale;
        Color c = v.baseColor;

        if (v.t >= 0f)
        {
            float k = Mathf.Clamp01(v.t);
            if (v.kind == AnimKind.Build)
            {
                float p = EaseOutBack(k);   // 0'dan buyur, hafif tasip yerine oturur
                s = new Vector3(v.baseScale.x * p, v.baseScale.y * p, v.baseScale.z);
            }
            else
            {
                // Sonumlenen squash & stretch: y uzar, x daralir
                float amp = v.kind == AnimKind.LevelUp ? 0.35f : 0.16f;
                float sq = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * amp;
                s = new Vector3(v.baseScale.x * (1f - sq * 0.6f), v.baseScale.y * (1f + sq), v.baseScale.z);
                if (v.kind == AnimKind.LevelUp) c = Color.Lerp(c, Color.white, (1f - k) * 0.7f);   // parlama
            }
        }

        v.sr.transform.localScale = s;
        v.sr.color = c;
    }

    static float EaseOutBack(float k)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = k - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (colorT < 1f)
        {
            colorT = Mathf.Min(1f, colorT + dt / colorFadeDuration);
            float k = Mathf.SmoothStep(0f, 1f, colorT);
            curSky = Color.Lerp(fromSky, toSky, k);
            curGround = Color.Lerp(fromGround, toGround, k);
            ApplyColors();
        }

        foreach (var v in views.Values)
        {
            if (v.t < 0f) continue;
            v.t += dt / v.duration;
            if (v.t >= 1f) v.t = -1f;
            Apply(v);
        }

        UpdateParticles(dt);
    }

    // ---- Parcaciklar (toz ve kivilcim). Ek asset gerekmez: beyaz kare kullanilir. ----
    void SpawnDust(View v, int count)
    {
        float baseY = v.data.scenePosition.y;
        float cx = v.data.scenePosition.x;
        float half = Mathf.Max(0.3f, v.width * 0.5f);

        for (int i = 0; i < count; i++)
        {
            float x = cx + Random.Range(-half, half);
            float dir = x >= cx ? 1f : -1f;
            var p = GetParticle();
            if (p == null) return;
            p.pos = new Vector2(x, baseY + 0.05f);
            p.vel = new Vector2(dir * Random.Range(0.4f, 1.8f), Random.Range(0.3f, 1.2f));
            p.gravity = -0.8f;
            p.size = Random.Range(0.12f, 0.24f);
            p.grow = 0.9f;   // toz buyuyerek dagilir
            p.life = Random.Range(0.45f, 0.8f);
            p.color = dustColor;
            p.age = 0f;
        }
    }

    void SpawnSparkles(View v, int count)
    {
        float baseY = v.data.scenePosition.y;
        float cx = v.data.scenePosition.x;
        float half = Mathf.Max(0.3f, v.width * 0.5f);

        for (int i = 0; i < count; i++)
        {
            var p = GetParticle();
            if (p == null) return;
            p.pos = new Vector2(cx + Random.Range(-half, half), baseY + v.height * Random.Range(0.3f, 1f));
            p.vel = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(2f, 4f));
            p.gravity = -4f;
            p.size = Random.Range(0.12f, 0.2f);
            p.grow = -0.6f;   // kivilcim kuculerek sonmeye gider
            p.life = Random.Range(0.7f, 1.1f);
            p.color = new Color(1f, 0.85f, 0.25f);
            p.age = 0f;
        }
    }

    Particle GetParticle()
    {
        if (particles.Count >= maxParticles) return null;

        Particle p;
        if (particlePool.Count > 0)
        {
            p = particlePool.Pop();
            p.t.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("p");
            go.transform.SetParent(fxRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.sortingOrder = 100;   // binalarin onunde
            p = new Particle { t = go.transform, sr = sr };
        }
        particles.Add(p);
        return p;
    }

    void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.age += dt;
            float k = p.age / p.life;
            if (k >= 1f)
            {
                p.t.gameObject.SetActive(false);
                particlePool.Push(p);
                particles.RemoveAt(i);
                continue;
            }

            p.vel.y += p.gravity * dt;
            p.pos += p.vel * dt;
            p.t.localPosition = new Vector3(p.pos.x, p.pos.y, 0f);

            float sz = p.size * Mathf.Max(0.05f, 1f + p.grow * k);
            p.t.localScale = new Vector3(sz, sz, 1f);

            var c = p.color;
            c.a = p.color.a * (1f - k * k);
            p.sr.color = c;
        }
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

    // Sprite atanmamis binalari duz kare yerine renkli, kucuk bir yapi siluetiyle goster.
    static Sprite BuildingSprite(BuildingData data)
    {
        Sprite cached;
        if (buildingSprites.TryGetValue(data.id, out cached)) return cached;

        const int w = 24, h = 24;
        var pixels = new Color32[w * h];
        Color.RGBToHSV(data.placeholderColor, out float hue, out float saturation, out float value);
        Color palette = Color.HSVToRGB(hue, Mathf.Min(saturation, 0.62f) * 0.72f, Mathf.Clamp(value * 0.82f + 0.12f, 0f, 0.9f));
        Color32 wall = palette;
        Color32 shade = Color.Lerp(palette, Color.black, 0.34f);
        Color32 roof = Color.Lerp(palette, Color.white, 0.2f);
        Color32 window = new Color32(255, 224, 139, 255);
        int seed = Mathf.Abs(data.id.GetHashCode());
        int left = 4 + seed % 3, right = 20 - seed % 3;
        int roofTop = 14 + seed % 5;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Color32 c = new Color32(0, 0, 0, 0);
            bool body = x >= left && x <= right && y >= 2 && y <= roofTop;
            int roofHeight = Mathf.Max(1, roofTop / 3);
            bool roofPixel = y > roofTop && y <= roofTop + roofHeight &&
                x >= left - (y - roofTop) && x <= right + (y - roofTop);
            if (body) c = x == left || x == right || y == 2 ? shade : wall;
            if (roofPixel) c = roof;
            if (body && y >= 7 && y <= 10 && (x == left + 3 || x == right - 3)) c = window;
            if (body && x >= (left + right) / 2 - 1 && x <= (left + right) / 2 + 1 && y >= 2 && y <= 7) c = shade;
            pixels[y * w + x] = c;
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "generated_" + data.id;
        tex.filterMode = FilterMode.Point;
        tex.SetPixels32(pixels);
        tex.Apply();
        cached = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 24f);
        buildingSprites[data.id] = cached;
        return cached;
    }
}
