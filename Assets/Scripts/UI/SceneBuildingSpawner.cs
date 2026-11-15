using System.Collections.Generic;
using UnityEngine;

// Sahnedeki binalari ve zemini cizer. Sprite atanmamissa renkli kutu (placeholder) kullanir.
// Bina satin alinca animasyon oynar:
//   - Ilk bina     : zeminden "insa edilir" (0'dan buyuyup hafif tasar) + toz
//   - Normal alim  : kisa squash & stretch ziplamasi + toz
//   - Seviye atlama: buyuk ziplama + parlama + altin kivilcimlar
public class SceneBuildingSpawner : MonoBehaviour
{
    [Header("Satin alma animasyonu")]
    [SerializeField] bool enableAnimations = true;
    [SerializeField] float buildDuration = 0.55f;
    [SerializeField] float bounceDuration = 0.4f;
    [SerializeField] float levelUpDuration = 0.75f;
    [SerializeField] int dustCount = 8;
    [SerializeField] int maxParticles = 150;

    [Header("Kademe animasyonu")]
    [Tooltip("Bina 26/51/101 adette gorunum degistirir; her kademede sallanma hizi ve parlamasi artar.")]
    [SerializeField] bool enableIdleAnimations = true;

    [Header("Cag gecisi")]
    [SerializeField] float colorFadeDuration = 1.2f;   // gokyuzu ve zemin eski renkten yeniye bu surede gecer

    enum AnimKind { Build, Bounce, LevelUp }

    class View
    {
        public BuildingData data;
        public SpriteRenderer sr;
        public SpriteRenderer glow;
        public Transform glowT;
        public Vector3 basePos;
        public Vector3 baseScale = Vector3.one;
        public Color baseColor = Color.white;
        public float width = 1f;    // dunya biriminde, toz/kivilcim konumu icin
        public float height = 1f;
        public float t = -1f;       // 0..1 arasi animasyon ilerlemesi, -1 = animasyon yok
        public float duration = 0.4f;
        public AnimKind kind;
        public int tier;
        public float phase;         // her bina farkli fazda sallansin
        public float flash;         // kademe atlayinca parlama
        public float ambientTimer;
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
    readonly Dictionary<string, float> stageX = new Dictionary<string, float>();   // bina id -> sahnedeki (yerel) x
    int lastScreenW, lastScreenH;
    Canvas hudCanvas;
    readonly List<Particle> particles = new List<Particle>();
    readonly Stack<Particle> particlePool = new Stack<Particle>();
    Transform fxRoot;
    Camera cam;
    SpriteRenderer groundSr;
    bool hasColors;
    float colorT = 1f;
    Color curSky, curGround, fromSky, fromGround, toSky, toGround;
    Color dustColor = new Color(0.9f, 0.85f, 0.78f);
    Color glowColor = new Color(1f, 0.55f, 0.2f);
    static Sprite whiteSprite;
    static Sprite glowSprite;
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
        glowColor = age.side == Side.Hell
            ? new Color(1f, 0.42f, 0.12f)
            : new Color(1f, 0.88f, 0.48f);

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

        ComputeStage(age);
        foreach (var b in age.buildings)
        {
            var go = new GameObject("bld_" + b.id);
            go.transform.SetParent(transform, false);
            Vector3 placed = Placed(b);
            go.transform.localPosition = placed;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = b.sortingOrder;

            // Kademe 2+ icin yumusak isik halesi (ayni yerde, binanin arkasinda)
            var glowGo = new GameObject("glow_" + b.id);
            glowGo.transform.SetParent(transform, false);
            var gsr = glowGo.AddComponent<SpriteRenderer>();
            gsr.sprite = GlowSprite();
            gsr.sortingOrder = b.sortingOrder - 1;
            gsr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);

            views[b.id] = new View
            {
                data = b,
                sr = sr,
                glow = gsr,
                glowT = glowGo.transform,
                basePos = placed,
                phase = (b.id.GetHashCode() & 0x7fffffff) % 1000 * 0.00628f,
                ambientTimer = Random.Range(0.3f, 1.6f)
            };
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
            if (n <= 0)
            {
                sr.enabled = false;
                if (v.glow != null) v.glow.enabled = false;
                v.t = -1f;
                continue;
            }

            int level = BuildingTiers.LevelFor(n);
            v.tier = level;
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
                // Sprite'larin arasinda bosluk kalsin; yuksek kademeler komsu binalara tasmasin.
                float grow = 1f + 0.06f * level;
                sr.sprite = BuildingSprite(b, level);
                v.baseColor = Color.white; // Renkler sprite'in icine zaten uygulandi.
                v.baseScale = new Vector3(b.placeholderSize.x * grow, b.placeholderSize.y * grow, 1f);
                v.width = v.baseScale.x;
                v.height = v.baseScale.y;
            }

            // Isik halesi yalnizca 2. kademeden sonra gorunur.
            if (v.glow != null)
            {
                v.glow.enabled = level >= 1;
                v.glowT.localPosition = new Vector3(v.basePos.x, v.basePos.y + v.height * 0.5f, 0.01f);
                v.glowT.localScale = new Vector3(v.width * 1.9f, v.height * 1.2f, 1f);
            }

            Apply(v);
        }
    }

    // ---- Satin alma animasyonu ----
    void OnBought(BuildingData b, int qty)
    {
        View v;
        if (!enableAnimations || !views.TryGetValue(b.id, out v)) return;

        int n = GameManager.Instance.GetCount(b);
        int before = n - qty;
        bool first = before <= 0;
        bool levelUp = !first && BuildingTiers.LevelFor(n) > BuildingTiers.LevelFor(before);

        v.kind = first ? AnimKind.Build : (levelUp ? AnimKind.LevelUp : AnimKind.Bounce);
        v.duration = first ? buildDuration : (levelUp ? levelUpDuration : bounceDuration);
        v.t = 0f;
        if (levelUp)
        {
            v.flash = 1f;
            v.ambientTimer = 0.15f;
        }
        Apply(v);

        bool big = first || levelUp || qty >= 10;   // toplu alimda daha cok toz
        SpawnDust(v, big ? dustCount * 2 : dustCount);
        if (levelUp)
        {
            SpawnSparkles(v, 14 + v.tier * 6);
            SoundManager.Play(SoundId.LevelUp);
        }
    }

    // Pasif uretim baloncuklari icin: binanin tepesinin dunya konumu
    public bool TryGetTopPosition(BuildingData b, out Vector3 world)
    {
        View v;
        if (views.TryGetValue(b.id, out v) && v.sr.enabled)
        {
            world = transform.TransformPoint(new Vector3(v.basePos.x, v.basePos.y + v.height + 0.1f, 0f));
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

        // Yuksek kademelerde cok hafif renk nabzi ve kademe atlama parlamasi
        if (v.tier >= 2 || v.flash > 0f)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (1.3f + v.tier * 0.35f) + v.phase);
            float tint = (v.tier >= 2 ? (0.025f + 0.02f * (v.tier - 2)) * pulse : 0f) + v.flash * 0.4f;
            c = Color.Lerp(c, glowColor, Mathf.Clamp01(tint));
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

    // Binalar panellerin arasindaki bos orta sahneye sigsin diye x konumlari yeniden hesaplanir.
    // Sol/sag sutun genislikleri HudLayout'tan gelir; en/boy orani degisse de dogru kalir.
    void ComputeStage(AgeData age)
    {
        stageX.Clear();
        var list = age.buildings;
        if (list == null || list.Length == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (var b in list) { minX = Mathf.Min(minX, b.scenePosition.x); maxX = Mathf.Max(maxX, b.scenePosition.x); }

        float left, right;
        if (!TryGetStageRange(out left, out right))
        {
            foreach (var b in list) stageX[b.id] = b.scenePosition.x;   // kamera/canvas yoksa eski konum
            return;
        }

        const float halfBuilding = 1.0f;   // bina yarim genisligi + pay (dunya birimi)
        float avail = Mathf.Max(0.5f, (right - left) - 2f * halfBuilding);
        float span = maxX - minX;
        float scale = (span > 0.001f && span > avail) ? avail / span : 1f;   // sadece sigmiyorsa sik
        float center = (left + right) * 0.5f;
        float mid = (minX + maxX) * 0.5f;
        foreach (var b in list) stageX[b.id] = center + (b.scenePosition.x - mid) * scale;
    }

    // Sahne alani = sol kenar boslugu ile sag sutun (bina listesi) arasi, spawner'in yerel x'i olarak
    bool TryGetStageRange(out float left, out float right)
    {
        left = right = 0f;
        if (cam == null) return false;
        if (hudCanvas == null)
        {
            var hud = FindAnyObjectByType<HUDController>();
            if (hud != null) { var c = hud.GetComponentInParent<Canvas>(); if (c != null) hudCanvas = c.rootCanvas; }
        }
        float w = HudLayout.RefWidth;
        if (hudCanvas != null)
        {
            float cw = ((RectTransform)hudCanvas.transform).rect.width;
            if (cw > 1f) w = cw;
        }
        float lf = (HudLayout.Margin + 8f) / w;
        float rf = (w - HudLayout.RightW - HudLayout.Margin - 16f) / w;
        if (rf <= lf + 0.1f) return false;
        float z = Mathf.Abs(cam.transform.position.z);
        left = transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(lf, 0.5f, z))).x;
        right = transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(rf, 0.5f, z))).x;
        return right > left;
    }

    Vector3 Placed(BuildingData b)
    {
        float x;
        if (!stageX.TryGetValue(b.id, out x)) x = b.scenePosition.x;
        return new Vector3(x, b.scenePosition.y, 0f);
    }

    void RelayoutIfScreenChanged()
    {
        if (Screen.width == lastScreenW && Screen.height == lastScreenH) return;
        lastScreenW = Screen.width; lastScreenH = Screen.height;
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentAge == null) return;
        ComputeStage(gm.CurrentAge);
        foreach (var kv in views) kv.Value.basePos = Placed(kv.Value.data);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        RelayoutIfScreenChanged();

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
            if (v.t >= 0f)
            {
                v.t += dt / v.duration;
                if (v.t >= 1f) v.t = -1f;
                Apply(v);
            }
            AnimateIdle(v, dt);
        }

        UpdateParticles(dt);
    }

    // Her bina surekli hafifce hareket eder. Kademe yukseldikce salinim hizlanir,
    // isik halesi belirginlesir ve cevreye kivilcim sacar.
    void AnimateIdle(View v, float dt)
    {
        if (!v.sr.enabled) return;
        if (v.flash > 0f) v.flash = Mathf.Max(0f, v.flash - dt * 2.6f);

        if (!enableIdleAnimations)
        {
            v.sr.transform.localPosition = v.basePos;
            v.sr.transform.localRotation = Quaternion.identity;
            if (v.glow != null) v.glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            return;
        }

        float tier = v.tier;
        float speed = 1.05f + tier * 0.55f;
        float t = Time.time * speed + v.phase;
        float bob = Mathf.Sin(t) * (0.012f + 0.014f * tier);
        float sway = Mathf.Sin(t * 0.72f + 1.1f) * (0.8f + 1.25f * tier);
        if (tier >= 2) sway += Mathf.Sin(t * 2.7f) * (0.35f * (tier - 1));   // ust kademeler daha canli

        v.sr.transform.localPosition = v.basePos + new Vector3(0f, bob, 0f);
        v.sr.transform.localRotation = Quaternion.Euler(0f, 0f, sway);
        float breath = 1f + Mathf.Sin(t * 1.5f + 0.7f) * (0.006f + 0.004f * tier);
        Vector3 sc = v.sr.transform.localScale;
        v.sr.transform.localScale = new Vector3(sc.x * breath, sc.y * breath, sc.z);

        if (v.glow == null) return;
        if (v.tier >= 1)
        {
            v.glow.enabled = true;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 1.6f + v.phase * 0.7f);
            float alpha = (0.06f + 0.035f * v.tier) * (0.72f + 0.28f * pulse) + v.flash * 0.35f;
            v.glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Clamp01(alpha));

            v.ambientTimer -= dt;
            if (v.ambientTimer <= 0f)
            {
                v.ambientTimer = Random.Range(0.9f, 2.0f);
                SpawnAmbient(v);
            }
        }
    }

    // ---- Parcaciklar (toz ve kivilcim). Ek asset gerekmez: beyaz kare kullanilir. ----
    void SpawnDust(View v, int count)
    {
        float baseY = v.basePos.y;
        float cx = v.basePos.x;
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
        float baseY = v.basePos.y;
        float cx = v.basePos.x;
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

    // Yuksek kademeli binalardan ara ara yukselen kor / isik parcaciklari
    void SpawnAmbient(View v)
    {
        var p = GetParticle();
        if (p == null) return;
        float half = Mathf.Max(0.3f, v.width * 0.4f);
        p.pos = new Vector2(v.basePos.x + Random.Range(-half, half),
            v.basePos.y + Random.Range(0.1f, v.height * 0.9f));
        p.vel = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.3f, 0.7f));
        p.gravity = 0.06f;
        p.size = Random.Range(0.04f, 0.1f);
        p.grow = -0.45f;
        p.life = Random.Range(0.9f, 1.7f);
        p.color = glowColor;
        p.age = 0f;
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

    // Yumusak isik halesi: merkezi parlak, kenarlara dogru seffaf.
    static Sprite GlowSprite()
    {
        if (glowSprite == null)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = a * a;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }
        return glowSprite;
    }

    // Sprite atanmamis binalari duz kare yerine renkli, kademeye gore buyuyen bir yapi siluetiyle goster.
    // Kademe 1: kucuk kulube, 2: iki katli yapi, 3: kule, 4: hisar (altin suslu).
    static Sprite BuildingSprite(BuildingData data, int tier)
    {
        tier = Mathf.Clamp(tier, 0, BuildingTiers.Count - 1);
        string key = data.id + "#" + tier;
        Sprite cached;
        if (buildingSprites.TryGetValue(key, out cached)) return cached;

        const int w = 32, h = 32;
        var pixels = new Color32[w * h];

        Color baseCol = Color.Lerp(data.placeholderColor, Color.white, 0.06f * tier);
        Color.RGBToHSV(baseCol, out float hue, out float saturation, out float value);
        Color palette = Color.HSVToRGB(hue, Mathf.Min(saturation, 0.62f) * 0.72f,
            Mathf.Clamp(value * 0.8f + 0.12f + 0.03f * tier, 0f, 0.92f));
        Color32 wall = palette;
        Color32 shade = Color.Lerp(palette, Color.black, 0.36f);
        Color32 roof = Color.Lerp(palette, Color.white, 0.24f);
        Color32 window = new Color32(255, 226, 132, 255);
        Color32 windowDark = new Color32(74, 54, 48, 255);
        Color32 gold = new Color32(236, 186, 84, 255);

        int seed = data.id.GetHashCode() & 0x7fffffff;
        int inset = Mathf.Max(3, 10 - tier * 2) + seed % 2;   // kademe buyudukce daha genis
        int left = inset;
        int right = w - 1 - inset - (seed / 2) % 2;
        int top = 16 + tier * 3;                             // govde yuksekligi
        int center = (left + right) / 2;

        // Temel / zemin
        for (int x = Mathf.Max(0, left - 1); x <= Mathf.Min(w - 1, right + 1); x++)
        {
            Put(pixels, w, h, x, 1, shade);
            Put(pixels, w, h, x, 2, Color.Lerp(palette, Color.black, 0.18f));
        }

        // Govde
        for (int y = 2; y <= top; y++)
        for (int x = left; x <= right; x++)
        {
            Color32 c = wall;
            if (x == left || x == right) c = shade;
            if (y == top) c = Color.Lerp(palette, Color.white, 0.08f);
            Put(pixels, w, h, x, y, c);
        }

        // Catı / mazgallar
        if (tier <= 1)
        {
            int roofH = 3 + tier;
            for (int i = 0; i < roofH; i++)
            for (int x = left + i; x <= right - i; x++)
                Put(pixels, w, h, x, top + 1 + i, i == roofH - 1 ? gold : roof);
        }
        else
        {
            for (int x = left; x <= right; x++)
                Put(pixels, w, h, x, top + 1, gold);
            for (int x = left; x <= right; x += 3)
            for (int y = top + 2; y <= top + 3; y++)
                Put(pixels, w, h, x, y, roof);
        }

        // 3. ve 4. kademede yan kuleler
        if (tier >= 2)
        {
            int towerTop = top + 3;
            for (int side = -1; side <= 1; side += 2)
            {
                int tx = side < 0 ? Mathf.Max(0, left - 2) : Mathf.Min(w - 2, right + 1);
                for (int y = 2; y <= towerTop; y++)
                for (int x = tx; x <= tx + 1; x++)
                {
                    Color32 c = x == tx && side < 0 ? shade : wall;
                    if (y == towerTop) c = roof;
                    Put(pixels, w, h, x, y, c);
                }
                Put(pixels, w, h, tx, towerTop + 1, gold);
                Put(pixels, w, h, tx + 1, towerTop + 1, gold);
            }
        }

        // 4. kademede ortada sivri kule ve altin tepe
        if (tier >= 3)
        {
            int spireTop = Mathf.Min(h - 2, top + 6);
            for (int y = top + 2; y <= spireTop; y++)
            for (int x = center - 1; x <= center + 1; x++)
                Put(pixels, w, h, x, y, y == spireTop ? gold : roof);
            Put(pixels, w, h, center, spireTop + 1, gold);
            Put(pixels, w, h, center, spireTop + 2, gold);
        }

        // Pencereler: kademe arttikca daha cok pencere yanar
        int cols = 2 + Mathf.Min(2, tier);
        int rows = 1 + (tier >= 2 ? 1 : 0);
        int usable = right - left - 2;
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < cols; col++)
        {
            int wx = left + 2 + Mathf.RoundToInt((col + 0.5f) / cols * usable) - 1;
            int wy = top - 4 - row * 4;
            bool lit = ((seed + col * 7 + row * 13) % 5) != 0 || tier >= 2;
            Put(pixels, w, h, wx, wy, lit ? window : windowDark);
            Put(pixels, w, h, wx, wy + 1, lit ? window : windowDark);
            if (tier >= 2)
            {
                Put(pixels, w, h, wx + 1, wy, lit ? window : windowDark);
                Put(pixels, w, h, wx + 1, wy + 1, lit ? window : windowDark);
            }
        }

        // Kapi
        int doorH = 4 + tier / 2;
        for (int y = 2; y <= 2 + doorH; y++)
        for (int x = center - 1; x <= center + 1; x++)
            Put(pixels, w, h, x, y, shade);
        Put(pixels, w, h, center, 3, gold);

        // 3. kademe ve uzeri altin kusak
        if (tier >= 2)
            for (int x = left + 1; x <= right - 1; x++)
                Put(pixels, w, h, x, 7, gold);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "generated_" + key;
        tex.filterMode = FilterMode.Point;
        tex.SetPixels32(pixels);
        tex.Apply();
        cached = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32f);
        buildingSprites[key] = cached;
        return cached;
    }

    static void Put(Color32[] px, int w, int h, int x, int y, Color32 c)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return;
        px[y * w + x] = c;
    }
}
