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
//  | [             Is Emirleri              ]         |                             |
//  |                    |                              +-----------------------------+
//  | [Basarimlar][Ayarlar]    [   CEZALANDIR   ]       |   [x1 x10 x100 Max]         |
//  |                                                   |                  [Ses]      |
//  +------------------------------------------------------------------------------+
// Kat gecisi OTOMATIK: kat hedefi dolunca oyun bir sonraki kata kendisi gecer, kat atla butonu yoktur.
public static class HudLayout
{
    public const float RefWidth = 1920f;

    public const float Margin = 24f;       // sag kenar bosluğu
    public const float LeftInset = 48f;    // sol kenar (perde cercevesinin icinde kalmak icin)
    public const float TopInset = 92f;     // ust kenar (perde saçağının altı)
    public const float Gap = 20f;

    // Ust / alt bolge yuksekligi: sag sutun ve orta sahne bu bolgelerin arasinda kalir.
    public const float TopBarH = 168f;
    public const float LeftW = 430f;     // sol gezinme ve acilir listeler
    public const float RightW = 680f;    // daraltilmis bina parşömeni
    public const float PanelRightShift = 48f;

    public const float ColumnTop = 170f;
    public const float ColumnBottom = 80f;
    // Parsomen gorselinin seffaf kenar payi: gorunen cerceve panel dikdortgeninden bu kadar iceridedir.
    public const float PanelVisibleInset = 72f;
    // x1/x10/x100/Max cubugu: gorunen parsomenin TAM altinda, ayni genislikte (ekran disina tasmaz).
    public const float BuyModeHeight = 64f;
    public const float BuyModeBottom = 106f;
    public const float BuyModeRightInset = Margin - PanelRightShift + PanelVisibleInset;   // ekranin sag kenarindan
    public const float BuyModeWidth = RightW - 2f * PanelVisibleInset;

    // Sol sutun: kaynak hapleri, [Yukseltmeler][Bilgi Agaci] ve altinda genis Is Emirleri sekmesi
    public const float PillW = 330f;   // kazanim_UI orani ~5.1
    public const float PillH = 64f;
    public const float PillGap = 8f;
    public const float LeftButtonsTop = TopInset + 3f * (PillH + PillGap) + 14f;   // 3 kaynak yeri ayrilir
    public const float LeftButtonH = 52f;
    public const float UpgradeHeaderW = 250f;
    public const float TreeButtonW = 160f;
    public const float QuestHeaderW = UpgradeHeaderW + Gap + TreeButtonW;
    public const float QuestHeaderY = LeftButtonsTop + LeftButtonH + 10f;

    // Orta sutun = sol ve sag sutun arasindaki bosluk
    public const float CenterLeft = LeftInset + LeftW + Gap;
    public const float CenterRightInset = RightW + Margin;
    public const float CenterWidth = RefWidth - CenterLeft - CenterRightInset;
    // Ekran ortasina (anchor 0.5) gore orta sutunun merkezi; her en/boy oraninda dogru kalir.
    // Ana oyun ekseni ekranda kalir; yan paneller bu ekseni saga/sola cekmez.
    public const float CenterOffsetX = 0f;

    // Orta ust: asili kat adi tabelasi, altinda kat cubugu (boss cani), onun altinda ipucu bandi
    public const float TitleH = 170f;
    public const float CenterTop = TitleH + 6f;
    public const float FloorBarH = 70f;
    public const float HintH = 84f;

    // Alt orta: tek buyuk buton (CEZALANDIR / IYILIK YAP)
    public const float GatherW = 440f;
    public const float GatherH = 132f;
    public const float GatherY = 36f;

    // Alt sol: Ayarlar | (editorde) test
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

    // Alt soldaki kose butonlari: slot 0 Ayarlar, slot 1 editor test butonu
    public static void PlaceCornerButton(RectTransform rt, int slot)
    {
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(LeftInset + slot * (CornerBtnW + CornerBtnGap), CornerBtnY);
        rt.sizeDelta = new Vector2(CornerBtnW, CornerBtnH);
    }

    // Sol sutundaki gezinme satiri
    public static void PlaceTreeButton(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(LeftInset + UpgradeHeaderW + Gap, -LeftButtonsTop);
        rt.sizeDelta = new Vector2(TreeButtonW, LeftButtonH);
    }

}
