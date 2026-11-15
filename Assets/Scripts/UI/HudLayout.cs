using System;
using UnityEngine;
using UnityEngine.UI;

// Ekran yerlesiminin TEK kaynagi. Referans cozunurluk 1920x1080 (CanvasScaler ile olceklenir).
// Her panel konumunu kendi icinde sabit yazmak yerine buradaki olculeri kullanir; boylece
// paneller birbirinin ustune binmez ve sahne (binalar) bos orta alana oturur.
//
//  +------------------------------------------------------------------------------+
//  | [Gunah]            |   KAT ADI TABELASI (asili)   |                  [Bilgi]    |
//  | [Cefa]             |   kat cubugu (boss cani)     +-----------------------------+
//  | [Ruh]              |   ipucu bandi                |   BINA LISTESI              |
//  | [Yukseltmeler][Bilgi Agaci]                       |   (kartlar, AL butonlari)   |
//  | [Is emirleri]      |   ORTA SAHNE                 |                             |
//  |                    |                              +-----------------------------+
//  | [Basarimlar][Ayarlar]    [   CEZALANDIR   ]       |   [x1 x10 x100 Max]         |
//  |                                                   |                  [Ses]      |
//  +------------------------------------------------------------------------------+
// Kat gecisi OTOMATIK: kat hedefi dolunca oyun bir sonraki kata kendisi gecer, kat atla butonu yoktur.
public static class HudLayout
{
    public const float RefWidth = 1920f;

    public const float Margin = 24f;       // sag kenar bosluğu
    public const float LeftInset = 64f;    // sol kenar (perde cercevesinin icinde kalmak icin)
    public const float TopInset = 92f;     // ust kenar (perde saçağının altı)
    public const float Gap = 12f;

    // Ust / alt bolge yuksekligi: sag sutun ve orta sahne bu bolgelerin arasinda kalir.
    public const float TopBarH = 168f;
    public const float BottomBarH = 176f;

    public const float LeftW = 520f;     // yukseltmeler + is emirleri acilir listeleri
    public const float RightW = 640f;    // bina listesi + satin alma modu + ses

    public const float ColumnTop = TopBarH;                 // sag sutun (bina listesi) ust bardan basliyor
    public const float ColumnBottom = BottomBarH + Gap;     // ... ve alt bolgenin ustunde bitiyor

    // Sol sutun: ust uste kaynak hapleri, sonra [Yukseltmeler][Bilgi Agaci] satiri, sonra Is Emirleri
    public const float PillW = 330f;   // kazanim_UI orani ~5.1
    public const float PillH = 64f;
    public const float PillGap = 8f;
    public const float LeftButtonsTop = TopInset + 3f * (PillH + PillGap) + 14f;   // 3 kaynak yeri ayrilir
    public const float LeftButtonH = 52f;
    public const float UpgradeHeaderW = 340f;
    public const float TreeButtonW = 240f;

    // Orta sutun = sol ve sag sutun arasindaki bosluk
    public const float CenterLeft = LeftInset + LeftW + Gap;
    public const float CenterRightInset = RightW + Margin;
    public const float CenterWidth = RefWidth - CenterLeft - CenterRightInset;
    // Ekran ortasina (anchor 0.5) gore orta sutunun merkezi; her en/boy oraninda dogru kalir.
    public const float CenterOffsetX = (CenterLeft - CenterRightInset) * 0.5f;

    // Orta ust: asili kat adi tabelasi, altinda kat cubugu (boss cani), onun altinda ipucu bandi
    public const float TitleH = 210f;
    public const float CenterTop = TitleH + 6f;
    public const float FloorBarH = 70f;
    public const float HintH = 84f;

    // Alt orta: tek buyuk buton (CEZALANDIR / IYILIK YAP)
    public const float GatherW = 480f;
    public const float GatherH = 132f;
    public const float GatherY = 36f;

    // Alt sol: Basarimlar | Ayarlar | (editorde) test
    public const float CornerBtnW = 200f;
    public const float CornerBtnH = 56f;
    public const float CornerBtnGap = 10f;
    public const float CornerBtnY = 40f;

    // Kat gecisinde ust kenarda ince bir sinir cizgisi (cehennemde katlar benzer renkte oldugu icin)
    public const float FloorEdgeH = 24f;

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

    // Alt soldaki kose butonlari: slot 0 en solda (Basarimlar), 1 (Ayarlar), 2 (editor test butonu)
    public static void PlaceCornerButton(RectTransform rt, int slot)
    {
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(LeftInset + slot * (CornerBtnW + CornerBtnGap), CornerBtnY);
        rt.sizeDelta = new Vector2(CornerBtnW, CornerBtnH);
    }

    // Sol sutundaki [Yukseltmeler][Bilgi Agaci] satirinin Bilgi Agaci tarafi
    public static void PlaceTreeButton(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(LeftInset + UpgradeHeaderW + Gap, -LeftButtonsTop);
        rt.sizeDelta = new Vector2(TreeButtonW, LeftButtonH);
    }

    // Perde cercevesi (Art/UI/Curtain_Frame) varsa tum ekrani kaplar ve ust/alt bar cizilmez.
    // Yoksa eski koyu ust/alt barlar yedek olarak cizilir.
    // Kule canvas'inda (HUD'un arkasinda) cizilir, boylece siralama sorunu olmaz.
    public static void BuildBars(Transform parent)
    {
        var frame = UIArt.Load(UIArt.Frame);
        if (frame != null)
        {
            var fr = UIFactory.CreatePanel(parent, "CurtainFrame", Color.white, false);
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            var fi = fr.GetComponent<Image>();
            fi.sprite = frame; fi.type = Image.Type.Simple; fi.preserveAspect = false;
            return;
        }

        const float fallbackTop = 96f, fallbackBottom = 148f;
        var top = UIFactory.CreatePanel(parent, "TopBar", Panel, false);
        top.anchorMin = new Vector2(0f, 1f); top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.offsetMin = new Vector2(0f, -fallbackTop); top.offsetMax = Vector2.zero;
        var topLine = UIFactory.CreatePanel(top, "AccentLine", Accent, false);
        topLine.anchorMin = new Vector2(0f, 0f); topLine.anchorMax = new Vector2(1f, 0f);
        topLine.offsetMin = Vector2.zero; topLine.offsetMax = new Vector2(0f, 3f);

        var bottom = UIFactory.CreatePanel(parent, "BottomBar", Panel, false);
        bottom.anchorMin = new Vector2(0f, 0f); bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.offsetMin = Vector2.zero; bottom.offsetMax = new Vector2(0f, fallbackBottom);
        var bottomLine = UIFactory.CreatePanel(bottom, "AccentLine", Accent, false);
        bottomLine.anchorMin = new Vector2(0f, 1f); bottomLine.anchorMax = new Vector2(1f, 1f);
        bottomLine.offsetMin = new Vector2(0f, -3f); bottomLine.offsetMax = Vector2.zero;
    }
}
