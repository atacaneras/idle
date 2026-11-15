using System;
using UnityEngine;

// Ekran yerlesiminin TEK kaynagi. Referans cozunurluk 1920x1080 (CanvasScaler ile olceklenir).
// Her panel konumunu kendi icinde sabit yazmak yerine buradaki olculeri kullanir; boylece
// paneller birbirinin ustune binmez ve sahne (binalar) bos orta alana oturur.
//
//  +------------------------------------------------------------------------------+
//  | UST BAR   kaynaklar        |   kat adi (orta)   |        Bilgi + 3 buton      |
//  +-------------------+---------------------------+------------------------------+
//  | SOL SUTUN         |  ORTA SAHNE               | SAG SUTUN                    |
//  |  Yukseltmeler     |   boss cubugu             |   bina listesi               |
//  |  Is emirleri      |   boss + binalar          |                              |
//  +-------------------+---------------------------+------------------------------+
//  | ALT BAR  [TOPLA] [test] [kat atla]            | [x1 x10 x100 Max]   [Ses]    |
//  +------------------------------------------------------------------------------+
public static class HudLayout
{
    public const float RefWidth = 1920f;

    public const float Margin = 24f;
    public const float Gap = 12f;

    public const float TopBarH = 96f;
    public const float BottomBarH = 148f;

    public const float LeftW = 520f;     // yukseltmeler + is emirleri
    public const float RightW = 640f;    // bina listesi + satin alma modu + ses

    public const float ColumnTop = TopBarH + Gap;        // sutunlar ust bardan bu kadar asagida baslar
    public const float ColumnBottom = BottomBarH + Gap;  // ... ve alt bardan bu kadar yukarida biter

    // Orta sutun = sol ve sag sutun arasindaki bosluk (boss cubugu, ipuclari, kat atla butonu)
    public const float CenterLeft = Margin + LeftW + Gap;
    public const float CenterRightInset = RightW + Margin;
    public const float CenterWidth = RefWidth - CenterLeft - CenterRightInset;
    // Ekran ortasina (anchor 0.5) gore orta sutunun merkezi; her en/boy oraninda dogru kalir.
    public const float CenterOffsetX = (CenterLeft - CenterRightInset) * 0.5f;

    // Sag ustteki dugmeler: Ayarlar | Bilgi Agaci | Basarimlar (slot 0 en sagda)
    public const float TopBtnW = 176f;
    public const float TopBtnH = 44f;
    public const float TopBtnGap = 8f;
    public const float TopBtnY = 46f;

    // ---- Renkler ----
    public static readonly Color Panel = new Color(0.07f, 0.06f, 0.08f, 0.86f);
    public static readonly Color PanelSoft = new Color(0.07f, 0.06f, 0.08f, 0.62f);
    public static readonly Color Accent = new Color(0.96f, 0.73f, 0.28f, 1f);
    public static readonly Color AccentInk = new Color(0.2f, 0.14f, 0.07f, 1f);
    public static readonly Color TextLight = new Color(0.97f, 0.94f, 0.88f, 1f);
    public static readonly Color TextDim = new Color(0.78f, 0.75f, 0.72f, 1f);
    public static readonly Color TextOutline = new Color(0.08f, 0.04f, 0.02f, 1f);

    // Sol sutundaki acilir paneller (yukseltmeler / is emirleri) ayni anda acik kalmasin.
    public static event Action<object> LeftPanelOpened;
    public static void RaiseLeftPanelOpened(object sender) { if (LeftPanelOpened != null) LeftPanelOpened(sender); }

    public static void PlaceTopButton(RectTransform rt, int slotFromRight)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-Margin - slotFromRight * (TopBtnW + TopBtnGap), -TopBtnY);
        rt.sizeDelta = new Vector2(TopBtnW, TopBtnH);
    }

    // Ust ve alt bar: kule canvas'inda (HUD'un arkasinda) cizilir, boylece siralama sorunu olmaz.
    public static void BuildBars(Transform parent)
    {
        var top = UIFactory.CreatePanel(parent, "TopBar", Panel, false);
        top.anchorMin = new Vector2(0f, 1f); top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.offsetMin = new Vector2(0f, -TopBarH); top.offsetMax = Vector2.zero;
        var topLine = UIFactory.CreatePanel(top, "AccentLine", Accent, false);
        topLine.anchorMin = new Vector2(0f, 0f); topLine.anchorMax = new Vector2(1f, 0f);
        topLine.offsetMin = Vector2.zero; topLine.offsetMax = new Vector2(0f, 3f);

        var bottom = UIFactory.CreatePanel(parent, "BottomBar", Panel, false);
        bottom.anchorMin = new Vector2(0f, 0f); bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.offsetMin = Vector2.zero; bottom.offsetMax = new Vector2(0f, BottomBarH);
        var bottomLine = UIFactory.CreatePanel(bottom, "AccentLine", Accent, false);
        bottomLine.anchorMin = new Vector2(0f, 1f); bottomLine.anchorMax = new Vector2(1f, 1f);
        bottomLine.offsetMin = new Vector2(0f, -3f); bottomLine.offsetMax = Vector2.zero;
    }
}
