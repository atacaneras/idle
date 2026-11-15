# Sanatçı Rehberi

PNG dosyasını ilgili kat klasörüne at, Unity'de hiçbir ayara dokunma. Oyun Play'e basınca dosyayı kendi bulur.
Dosya adları **birebir** aşağıdaki gibi olmalı (küçük harf, boşluksuz). Dosya yoksa oyun placeholder çizer.

- Kat arka planı: 1920x1080 önerilir.
- **Arayüz panelleri arka planın üstüne biner.** Ana çizimi (kapı, tezgâh, taht vb.) ortadaki alana koy: yaklaşık x 600–1150, y 170–720. Sol ~%30 (yükseltme paneli), sağ ~%40 (bina listesi), üst ~%14 ve alt ~%25 (butonlar) kısmı süs/dolgu olsun.
- `boss.png` bu orta alanın ortasına konur (ekranda yaklaşık 430 px yükseklik). Aynı yerde HUD çubuğu üstte, binalar altta kalır.
- Bina: yaklaşık 140x180 px (placeholder 1.4x1.8 birim). `_lv1` / `_lv2` / `_lv3` seviye görünümleri, sadece `_lv1` yeterli.
- Hepsi şeffaf zeminli PNG olabilir.
- Binaların tabanı ekranın altından yaklaşık 200 px yukarıda durur (1080 yükseklikte y=880). Arka plandaki zemin çizgisini oraya çiz. Bina PNG'sinin alt-orta noktası bu çizgiye oturur.

## Ana Menü (taraf seçimi)  —  `Assets/Resources/Art/UI/`

Hepsi opsiyonel: dosya yoksa oyun placeholder çizer. Fare imleci gizlenir, yerine hayalet gelir.

| Dosya | Ne |
|---|---|
| `menu_bg.png` | Tam ekran arka plan (solda cehennem, ortada köprü, sağda cennet), 1920x1080 |
| `Curtain_Frame.png` | Perde çerçevesi, tam ekran, ortası şeffaf |
| `menu_title.png` | Üstteki tabela (oyun adı kodla üstüne yazılır) |
| `menu_plaque_hell.png` | Sol alttaki CEHENNEM levhası (yazı kodla yazılır) |
| `menu_plaque_heaven.png` | Sağ alttaki CENNET levhası (yazı kodla yazılır) |
| `ghost.png` | Sade hayalet, imleç (ekranın ortasında / köprüde), yaklaşık 150x190 |
| `ghost_angel.png` | Cennet tarafına gelince: kanatlı, haleli melek hayalet |
| `ghost_demon.png` | Cehennem tarafına gelince: boynuzlu zebani hayalet |
| `../Icons/Settings_Button_S.png` | Altta ortadaki ayarlar butonu |

Hayaletin üç hâli aynı boyutta ve aynı merkezde çizilirse geçiş düzgün görünür. İmleç noktası hayaletin merkezidir.

## Yer Yüzü  —  `Assets/Resources/Art/earth/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |

## Erebus  —  `Assets/Resources/Art/hell_1/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |
| `hell_1_korna_lv1.png` | Vesvese |
| `hell_1_bant_lv1.png` | İtaat |
| `hell_1_fotokopi_lv1.png` | Günah |
| `hell_1_asansor_lv1.png` | Bekçilik |
| `hell_1_tabela_lv1.png` | İşkence |

## Cehennem 2. Kat: Şikâyet Departmanı  —  `Assets/Resources/Art/hell_2/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |
| `hell_2_damga_lv1.png` | Damga Makinesi (Hep REDDEDİLDİ) |
| `hell_2_telefon_lv1.png` | Telefon Bekletme Hattı |
| `hell_2_gise_lv1.png` | Gişe Camı (Kapalı) |
| `hell_2_dosya_lv1.png` | Şikâyet Dosyası Dağı |
| `hell_2_yarin_lv1.png` | “Yarın Gel” Panosu |

## hell_3 (içerik henüz yok)  —  `Assets/Resources/Art/hell_3/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## hell_4 (içerik henüz yok)  —  `Assets/Resources/Art/hell_4/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## hell_5 (içerik henüz yok)  —  `Assets/Resources/Art/hell_5/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## hell_6 (içerik henüz yok)  —  `Assets/Resources/Art/hell_6/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## hell_7 (içerik henüz yok)  —  `Assets/Resources/Art/hell_7/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## Cennet 1. Kat: Bulut Kafe  —  `Assets/Resources/Art/heaven_1/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |
| `heaven_1_teyze_lv1.png` | Teyzeye Yol Gösterme Tezgâhı |
| `heaven_1_barista_lv1.png` | Bulut Kafe Baristası |
| `heaven_1_corba_lv1.png` | Komşuya Çorba Servisi |
| `heaven_1_arp_lv1.png` | Arp Çalma Kursu |
| `heaven_1_sarilma_lv1.png` | Ücretsiz Sarılma Standı |

## Cennet 2. Kat: Teyze Danışma Masası  —  `Assets/Resources/Art/heaven_2/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |
| `heaven_2_levha_lv1.png` | Yol Tarifi Levhası |
| `heaven_2_demlik_lv1.png` | Çay Servisi (İnce Belli) |
| `heaven_2_kayip_lv1.png` | Kayıp Eşya Bürosu |
| `heaven_2_torun_lv1.png` | Torun Fotoğrafı Galerisi |
| `heaven_2_tavsiye_lv1.png` | Teyze Tavsiye Hattı |

## heaven_3 (içerik henüz yok)  —  `Assets/Resources/Art/heaven_3/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## heaven_4 (içerik henüz yok)  —  `Assets/Resources/Art/heaven_4/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## heaven_5 (içerik henüz yok)  —  `Assets/Resources/Art/heaven_5/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## heaven_6 (içerik henüz yok)  —  `Assets/Resources/Art/heaven_6/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

## heaven_7 (içerik henüz yok)  —  `Assets/Resources/Art/heaven_7/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |

