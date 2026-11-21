using Spine.Unity;
using UnityEngine;

// Spine karakterlerini arayuz (Canvas) icinde cizmek icin kucuk yardimci.
// SideSelectScreen'deki calisan kurulumla ayni sirayi kullanir: SkeletonGraphic once, SkeletonAnimation sonra.
public static class SpineUI
{
    // feetAnchor: karakterin AYAKLARININ duracagi nokta (boyutsuz bir RectTransform).
    // Karakter bu noktaya alt-orta kenari oturacak sekilde, targetHeight yuksekliginde cizilir.
    static SkeletonDataAsset[] allData;

    // Dosya adinda Turkce karakter / "#U015f" gibi kodlu harf farki olabilir; bu yuzden adin ASCII parcasiyla aranir.
    public static SkeletonDataAsset Find(string nameFragment)
    {
        if (allData == null) allData = Resources.LoadAll<SkeletonDataAsset>("Art");
        string want = nameFragment.ToLowerInvariant();
        foreach (var a in allData) if (a != null && a.name.ToLowerInvariant().Contains(want)) return a;
        return null;
    }

    public static GameObject CreateGraphic(RectTransform feetAnchor, SkeletonDataAsset data, string skin, string animation, float targetHeight)
    {
        if (data == null || feetAnchor == null) return null;

        var go = new GameObject("Spine", typeof(RectTransform));
        go.transform.SetParent(feetAnchor, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        var g = go.AddComponent<SkeletonGraphic>();
        g.skeletonDataAsset = data;
        g.raycastTarget = false;
        g.allowMultipleCanvasRenderers = true;
        if (!string.IsNullOrEmpty(skin)) g.initialSkinName = skin;

        var straightMat = Resources.Load<Material>("Materials/UI-StraightAlphaTex/SkeletonGraphicDefault-Straight");
        if (straightMat != null) g.material = straightMat;
        else
        {
            var shader = Shader.Find("Spine/SkeletonGraphic");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
                g.material = mat;
            }
        }

        // Animasyon bileseni once eklenir; yoksa karakter tek kare cizilip donar.
        var anim = go.AddComponent<SkeletonAnimation>();
        anim.skeletonDataAsset = data;
        anim.unscaledTime = true;
        anim.Initialize(false);

        if (!string.IsNullOrEmpty(skin) && g.Skeleton != null)
        {
            var sk = g.Skeleton;
            if (sk.Data.FindSkin(skin) != null)
            {
                sk.SetSkin(skin);
                sk.SetupPoseSlots();
            }
        }

        var sd = data.GetSkeletonData(true);
        if (sd != null && anim.AnimationState != null)
        {
            Spine.Animation a = string.IsNullOrEmpty(animation) ? null : sd.FindAnimation(animation);
            if (a == null && sd.Animations.Count > 0) a = sd.Animations.Items[0];
            if (a != null) anim.AnimationState.SetAnimation(0, a, true);
        }

        if (g.MatchRectTransformWithBounds() && rt.rect.height > 1f)
        {
            float s = targetHeight / rt.rect.height;
            rt.localScale = new Vector3(s, s, 1f);
            Vector3 bottomCenter = feetAnchor.InverseTransformPoint(rt.TransformPoint(new Vector3(rt.rect.center.x, rt.rect.yMin, 0f)));
            rt.anchoredPosition -= new Vector2(bottomCenter.x, bottomCenter.y);
        }
        return go;
    }
}
