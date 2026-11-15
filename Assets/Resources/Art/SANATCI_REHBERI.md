# Sanatçı Rehberi

PNG dosyasını ilgili kat klasörüne at, Unity'de hiçbir ayara dokunma. Oyun Play'e basınca dosyayı kendi bulur.
Dosya adları **birebir** aşağıdaki gibi olmalı (küçük harf, boşluksuz). Dosya yoksa oyun placeholder çizer.

- Kat arka planı: 1920x1080 önerilir.
- **Arayüz panelleri arka planın üstüne biner.** Ana çizimi (kapı, tezgâh, taht vb.) ortadaki alana koy: yaklaşık x 600–1150, y 170–720. Sol ~%30 (yükseltme paneli), sağ ~%40 (bina listesi), üst ~%14 ve alt ~%25 (butonlar) kısmı süs/dolgu olsun.
- `boss.png` bu orta alanın ortasına konur (ekranda yaklaşık 430 px yükseklik). Aynı yerde HUD çubuğu üstte, binalar altta kalır.
- Bina: yaklaşık 140x180 px, alt-orta pivot otomatik ayarlanır. Her bina **4 kademe** görseli kullanır:
  `_lv1` = 1–25 adet, `_lv2` = 26–50, `_lv3` = 51–100, `_lv4` = 101–200. Kademeler aynı binanın belirgin şekilde değişmiş hali olmalı (ör. bağlı adam → dönen adam). Sadece `_lv1` de yeterli; eksik kademe bir öncekinin görselini kullanır.
- Animasyon: oyun binaları kodla hafifçe sallandırır/parlatır, kademe atlayınca ışık ve kıvılcım efekti oynatır. Ek kare animasyonu şimdilik gerekmiyor; tek kare PNG yeterli.
- Hepsi şeffaf zeminli PNG olabilir.
- Binaların tabanı ekranın altından yaklaşık 200 px yukarıda durur (1080 yükseklikte y=880). Arka plandaki zemin çizgisini oraya çiz. Bina PNG'sinin alt-orta noktası bu çizgiye oturur.

## Yer Yüzü  —  `Assets/Resources/Art/earth/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |

## Cehennem 1. Kat: Bekleme Salonu  —  `Assets/Resources/Art/hell_1/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss: Numaratör Nuri, ortada |
| `hell_1_korna_lv1.png` | Kornacı Ceza Kulübesi — 1–25 adet |
| `hell_1_korna_lv2.png` | Kornacı Ceza Kulübesi — 26–50 adet (görsel değişir) |
| `hell_1_korna_lv3.png` | Kornacı Ceza Kulübesi — 51–100 adet (görsel değişir) |
| `hell_1_korna_lv4.png` | Kornacı Ceza Kulübesi — 101–200 adet (son görünüm) |
| `hell_1_bant_lv1.png` | Kırmızı Bant Memuru — 1–25 adet |
| `hell_1_bant_lv2.png` | Kırmızı Bant Memuru — 26–50 adet (görsel değişir) |
| `hell_1_bant_lv3.png` | Kırmızı Bant Memuru — 51–100 adet (görsel değişir) |
| `hell_1_bant_lv4.png` | Kırmızı Bant Memuru — 101–200 adet (son görünüm) |
| `hell_1_fotokopi_lv1.png` | Sonsuz Fotokopi Makinesi — 1–25 adet |
| `hell_1_fotokopi_lv2.png` | Sonsuz Fotokopi Makinesi — 26–50 adet (görsel değişir) |
| `hell_1_fotokopi_lv3.png` | Sonsuz Fotokopi Makinesi — 51–100 adet (görsel değişir) |
| `hell_1_fotokopi_lv4.png` | Sonsuz Fotokopi Makinesi — 101–200 adet (son görünüm) |
| `hell_1_asansor_lv1.png` | Asansör Müziği Hoparlörü — 1–25 adet |
| `hell_1_asansor_lv2.png` | Asansör Müziği Hoparlörü — 26–50 adet (görsel değişir) |
| `hell_1_asansor_lv3.png` | Asansör Müziği Hoparlörü — 51–100 adet (görsel değişir) |
| `hell_1_asansor_lv4.png` | Asansör Müziği Hoparlörü — 101–200 adet (son görünüm) |
| `hell_1_tabela_lv1.png` | “Lütfen Bekleyiniz” Tabelası — 1–25 adet |
| `hell_1_tabela_lv2.png` | “Lütfen Bekleyiniz” Tabelası — 26–50 adet (görsel değişir) |
| `hell_1_tabela_lv3.png` | “Lütfen Bekleyiniz” Tabelası — 51–100 adet (görsel değişir) |
| `hell_1_tabela_lv4.png` | “Lütfen Bekleyiniz” Tabelası — 101–200 adet (son görünüm) |

## Cehennem 2. Kat: Şikâyet Departmanı  —  `Assets/Resources/Art/hell_2/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss: Şikâyet Müdürü Bay Hayır, ortada |
| `hell_2_damga_lv1.png` | Damga Makinesi (Hep REDDEDİLDİ) — 1–25 adet |
| `hell_2_damga_lv2.png` | Damga Makinesi (Hep REDDEDİLDİ) — 26–50 adet (görsel değişir) |
| `hell_2_damga_lv3.png` | Damga Makinesi (Hep REDDEDİLDİ) — 51–100 adet (görsel değişir) |
| `hell_2_damga_lv4.png` | Damga Makinesi (Hep REDDEDİLDİ) — 101–200 adet (son görünüm) |
| `hell_2_telefon_lv1.png` | Telefon Bekletme Hattı — 1–25 adet |
| `hell_2_telefon_lv2.png` | Telefon Bekletme Hattı — 26–50 adet (görsel değişir) |
| `hell_2_telefon_lv3.png` | Telefon Bekletme Hattı — 51–100 adet (görsel değişir) |
| `hell_2_telefon_lv4.png` | Telefon Bekletme Hattı — 101–200 adet (son görünüm) |
| `hell_2_gise_lv1.png` | Gişe Camı (Kapalı) — 1–25 adet |
| `hell_2_gise_lv2.png` | Gişe Camı (Kapalı) — 26–50 adet (görsel değişir) |
| `hell_2_gise_lv3.png` | Gişe Camı (Kapalı) — 51–100 adet (görsel değişir) |
| `hell_2_gise_lv4.png` | Gişe Camı (Kapalı) — 101–200 adet (son görünüm) |
| `hell_2_dosya_lv1.png` | Şikâyet Dosyası Dağı — 1–25 adet |
| `hell_2_dosya_lv2.png` | Şikâyet Dosyası Dağı — 26–50 adet (görsel değişir) |
| `hell_2_dosya_lv3.png` | Şikâyet Dosyası Dağı — 51–100 adet (görsel değişir) |
| `hell_2_dosya_lv4.png` | Şikâyet Dosyası Dağı — 101–200 adet (son görünüm) |
| `hell_2_yarin_lv1.png` | “Yarın Gel” Panosu — 1–25 adet |
| `hell_2_yarin_lv2.png` | “Yarın Gel” Panosu — 26–50 adet (görsel değişir) |
| `hell_2_yarin_lv3.png` | “Yarın Gel” Panosu — 51–100 adet (görsel değişir) |
| `hell_2_yarin_lv4.png` | “Yarın Gel” Panosu — 101–200 adet (son görünüm) |

## Cehennem 3. Kat: Trafik Cezası Şubesi  —  `Assets/Resources/Art/hell_3/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss: Trafik Şube Müdürü Kemal Bey, ortada |
| `hell_3_kamera_lv1.png` | Hız Kamerası (Flaş Bozuk) — 1–25 adet |
| `hell_3_kamera_lv2.png` | Hız Kamerası (Flaş Bozuk) — 26–50 adet (görsel değişir) |
| `hell_3_kamera_lv3.png` | Hız Kamerası (Flaş Bozuk) — 51–100 adet (görsel değişir) |
| `hell_3_kamera_lv4.png` | Hız Kamerası (Flaş Bozuk) — 101–200 adet (son görünüm) |
| `hell_3_park_lv1.png` | Park Yeri Bulamama Alanı — 1–25 adet |
| `hell_3_park_lv2.png` | Park Yeri Bulamama Alanı — 26–50 adet (görsel değişir) |
| `hell_3_park_lv3.png` | Park Yeri Bulamama Alanı — 51–100 adet (görsel değişir) |
| `hell_3_park_lv4.png` | Park Yeri Bulamama Alanı — 101–200 adet (son görünüm) |
| `hell_3_vezne_lv1.png` | Vezne (Kart Kabul Etmiyor) — 1–25 adet |
| `hell_3_vezne_lv2.png` | Vezne (Kart Kabul Etmiyor) — 26–50 adet (görsel değişir) |
| `hell_3_vezne_lv3.png` | Vezne (Kart Kabul Etmiyor) — 51–100 adet (görsel değişir) |
| `hell_3_vezne_lv4.png` | Vezne (Kart Kabul Etmiyor) — 101–200 adet (son görünüm) |
| `hell_3_bariyer_lv1.png` | Otopark Bariyeri (Hep Kapalı) — 1–25 adet |
| `hell_3_bariyer_lv2.png` | Otopark Bariyeri (Hep Kapalı) — 26–50 adet (görsel değişir) |
| `hell_3_bariyer_lv3.png` | Otopark Bariyeri (Hep Kapalı) — 51–100 adet (görsel değişir) |
| `hell_3_bariyer_lv4.png` | Otopark Bariyeri (Hep Kapalı) — 101–200 adet (son görünüm) |
| `hell_3_levha_lv1.png` | Tek Yön Levhası (İki Yön) — 1–25 adet |
| `hell_3_levha_lv2.png` | Tek Yön Levhası (İki Yön) — 26–50 adet (görsel değişir) |
| `hell_3_levha_lv3.png` | Tek Yön Levhası (İki Yön) — 51–100 adet (görsel değişir) |
| `hell_3_levha_lv4.png` | Tek Yön Levhası (İki Yön) — 101–200 adet (son görünüm) |

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
| `heaven_1_teyze_lv1.png` | Teyzeye Yol Gösterme Tezgâhı — 1–25 adet |
| `heaven_1_teyze_lv2.png` | Teyzeye Yol Gösterme Tezgâhı — 26–50 adet (görsel değişir) |
| `heaven_1_teyze_lv3.png` | Teyzeye Yol Gösterme Tezgâhı — 51–100 adet (görsel değişir) |
| `heaven_1_teyze_lv4.png` | Teyzeye Yol Gösterme Tezgâhı — 101–200 adet (son görünüm) |
| `heaven_1_barista_lv1.png` | Bulut Kafe Baristası — 1–25 adet |
| `heaven_1_barista_lv2.png` | Bulut Kafe Baristası — 26–50 adet (görsel değişir) |
| `heaven_1_barista_lv3.png` | Bulut Kafe Baristası — 51–100 adet (görsel değişir) |
| `heaven_1_barista_lv4.png` | Bulut Kafe Baristası — 101–200 adet (son görünüm) |
| `heaven_1_corba_lv1.png` | Komşuya Çorba Servisi — 1–25 adet |
| `heaven_1_corba_lv2.png` | Komşuya Çorba Servisi — 26–50 adet (görsel değişir) |
| `heaven_1_corba_lv3.png` | Komşuya Çorba Servisi — 51–100 adet (görsel değişir) |
| `heaven_1_corba_lv4.png` | Komşuya Çorba Servisi — 101–200 adet (son görünüm) |
| `heaven_1_arp_lv1.png` | Arp Çalma Kursu — 1–25 adet |
| `heaven_1_arp_lv2.png` | Arp Çalma Kursu — 26–50 adet (görsel değişir) |
| `heaven_1_arp_lv3.png` | Arp Çalma Kursu — 51–100 adet (görsel değişir) |
| `heaven_1_arp_lv4.png` | Arp Çalma Kursu — 101–200 adet (son görünüm) |
| `heaven_1_sarilma_lv1.png` | Ücretsiz Sarılma Standı — 1–25 adet |
| `heaven_1_sarilma_lv2.png` | Ücretsiz Sarılma Standı — 26–50 adet (görsel değişir) |
| `heaven_1_sarilma_lv3.png` | Ücretsiz Sarılma Standı — 51–100 adet (görsel değişir) |
| `heaven_1_sarilma_lv4.png` | Ücretsiz Sarılma Standı — 101–200 adet (son görünüm) |

## Cennet 2. Kat: Teyze Danışma Masası  —  `Assets/Resources/Art/heaven_2/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |
| `heaven_2_levha_lv1.png` | Yol Tarifi Levhası — 1–25 adet |
| `heaven_2_levha_lv2.png` | Yol Tarifi Levhası — 26–50 adet (görsel değişir) |
| `heaven_2_levha_lv3.png` | Yol Tarifi Levhası — 51–100 adet (görsel değişir) |
| `heaven_2_levha_lv4.png` | Yol Tarifi Levhası — 101–200 adet (son görünüm) |
| `heaven_2_demlik_lv1.png` | Çay Servisi (İnce Belli) — 1–25 adet |
| `heaven_2_demlik_lv2.png` | Çay Servisi (İnce Belli) — 26–50 adet (görsel değişir) |
| `heaven_2_demlik_lv3.png` | Çay Servisi (İnce Belli) — 51–100 adet (görsel değişir) |
| `heaven_2_demlik_lv4.png` | Çay Servisi (İnce Belli) — 101–200 adet (son görünüm) |
| `heaven_2_kayip_lv1.png` | Kayıp Eşya Bürosu — 1–25 adet |
| `heaven_2_kayip_lv2.png` | Kayıp Eşya Bürosu — 26–50 adet (görsel değişir) |
| `heaven_2_kayip_lv3.png` | Kayıp Eşya Bürosu — 51–100 adet (görsel değişir) |
| `heaven_2_kayip_lv4.png` | Kayıp Eşya Bürosu — 101–200 adet (son görünüm) |
| `heaven_2_torun_lv1.png` | Torun Fotoğrafı Galerisi — 1–25 adet |
| `heaven_2_torun_lv2.png` | Torun Fotoğrafı Galerisi — 26–50 adet (görsel değişir) |
| `heaven_2_torun_lv3.png` | Torun Fotoğrafı Galerisi — 51–100 adet (görsel değişir) |
| `heaven_2_torun_lv4.png` | Torun Fotoğrafı Galerisi — 101–200 adet (son görünüm) |
| `heaven_2_tavsiye_lv1.png` | Teyze Tavsiye Hattı — 1–25 adet |
| `heaven_2_tavsiye_lv2.png` | Teyze Tavsiye Hattı — 26–50 adet (görsel değişir) |
| `heaven_2_tavsiye_lv3.png` | Teyze Tavsiye Hattı — 51–100 adet (görsel değişir) |
| `heaven_2_tavsiye_lv4.png` | Teyze Tavsiye Hattı — 101–200 adet (son görünüm) |

## Cennet 3. Kat: Huzur Mahallesi  —  `Assets/Resources/Art/heaven_3/`

| Dosya | Ne |
|---|---|
| `background.png` | Kat arka planı |
| `boss.png` | Boss (cehennem) / kat karakteri (cennet), ortada |
| `heaven_3_salincak_lv1.png` | Mahalle Salıncağı — 1–25 adet |
| `heaven_3_salincak_lv2.png` | Mahalle Salıncağı — 26–50 adet (görsel değişir) |
| `heaven_3_salincak_lv3.png` | Mahalle Salıncağı — 51–100 adet (görsel değişir) |
| `heaven_3_salincak_lv4.png` | Mahalle Salıncağı — 101–200 adet (son görünüm) |
| `heaven_3_firin_lv1.png` | Komşu Simit Fırını — 1–25 adet |
| `heaven_3_firin_lv2.png` | Komşu Simit Fırını — 26–50 adet (görsel değişir) |
| `heaven_3_firin_lv3.png` | Komşu Simit Fırını — 51–100 adet (görsel değişir) |
| `heaven_3_firin_lv4.png` | Komşu Simit Fırını — 101–200 adet (son görünüm) |
| `heaven_3_bahce_lv1.png` | Ortak Bahçe (Domates Serbest) — 1–25 adet |
| `heaven_3_bahce_lv2.png` | Ortak Bahçe (Domates Serbest) — 26–50 adet (görsel değişir) |
| `heaven_3_bahce_lv3.png` | Ortak Bahçe (Domates Serbest) — 51–100 adet (görsel değişir) |
| `heaven_3_bahce_lv4.png` | Ortak Bahçe (Domates Serbest) — 101–200 adet (son görünüm) |
| `heaven_3_kedi_lv1.png` | Sokak Kedisi Besleme Noktası — 1–25 adet |
| `heaven_3_kedi_lv2.png` | Sokak Kedisi Besleme Noktası — 26–50 adet (görsel değişir) |
| `heaven_3_kedi_lv3.png` | Sokak Kedisi Besleme Noktası — 51–100 adet (görsel değişir) |
| `heaven_3_kedi_lv4.png` | Sokak Kedisi Besleme Noktası — 101–200 adet (son görünüm) |
| `heaven_3_cesme_lv1.png` | Mahalle Çeşmesi (Dedikodu Dahil) — 1–25 adet |
| `heaven_3_cesme_lv2.png` | Mahalle Çeşmesi (Dedikodu Dahil) — 26–50 adet (görsel değişir) |
| `heaven_3_cesme_lv3.png` | Mahalle Çeşmesi (Dedikodu Dahil) — 51–100 adet (görsel değişir) |
| `heaven_3_cesme_lv4.png` | Mahalle Çeşmesi (Dedikodu Dahil) — 101–200 adet (son görünüm) |

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

