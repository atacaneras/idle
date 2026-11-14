# Tek kaynak: hem simulasyon hem C# uretici bunu kullanir.
# bina: id, ad, uretir, maliyetKaynak, baseCost, baseProd, (x,y,order), renk(r,g,b)
STONE = dict(
  id="stone", name="Taş Çağı", click="food", advance="stone", advanceCost=3800000, knowledge=3,
  buildings=[
    ("stone_gatherer","Toplayıcı Alanı","food","food",15,0.5,(-8,-3.4,1),(0,1,0)),
    ("stone_woodcamp","Odun Kampı","wood","food",100,1.0,(-6.5,-2.7,0),(0.545,0.27,0.075)),
    ("stone_hunter","Avcı Kampı","food","wood",300,8,(-5,-3.4,1),(1,0,0)),
    ("stone_quarry","Taş Ocağı","stone","wood",1100,1.0,(-3.5,-2.7,0),(0.3,0.3,0.3)),
    ("stone_axeshop","Balta Atölyesi","wood","stone",300,30,(-2,-3.4,1),(0.6,0.45,0.3)),
    ("stone_firepit","Ateş Çukuru","food","stone",1500,120,(-0.5,-2.7,0),(1,0.5,0)),
    ("stone_mammoth","Mamut Taşıyıcılar","stone","food",15000,40,(1,-3.4,1),(0.45,0.3,0.25)),
    ("stone_gallery","Mağara Galerisi","food","stone",9000,600,(2.5,-2.7,0),(0.7,0.4,0.8)),
  ],
  click_upgrades=[  # id, ad, tur, deger, maliyet, aciklama, onkosul(idx)
    ("stone_click1","Daha Sivri Taş","click",2,100,"Sivri olan daha iyi toplar. Bu bilim.",None),
    ("stone_click2","Çift Elle Toplama","click",2,1500,"İki el, iki kat verim. Matematik zor ama doğru.",0),
    ("stone_click3","Toplama Teknikleri","share",0.01,12000,"Ustalar işi böyle yapar. Kimse ustaların kim olduğunu bilmiyor.",1),
    ("stone_click4","Kas Hafızası","click",3,90000,"Kollar düşünmeden topluyor. Beyin tatilde.",2),
  ],
  building_upgrades={  # bina id -> [(ad, aciklama), (ad, aciklama)]
    "stone_gatherer":[("Sepet Örmek","İlk sepet. Kimse nasıl yapıldığını hatırlamıyor ama herkes çok gururlu."),("Meyve Haritası","Hangi çalıdan ne çıktığını duvara çizdik. Duvar artık bizim arama motoru.")],
    "stone_woodcamp":[("Sivri Sopa","Her sopa bir ağaç söker. Teoride.") ,("Odun Sendikası","Mesai saati icat edildi. Kimse ne olduğunu anlamadı.")],
    "stone_hunter":[("Mızrak Ucu","Eskiden elimizdeki sopaya mızrak derdik. Şimdi ucu var, çok profesyonel."),("Av Stratejisi","Önce bağırmayı, sonra koşmayı öğrendik. Sıra önemli.")],
    "stone_quarry":[("Daha İyi Kazma","Taşla taş kazmak mantıklı mı? Çalışıyor, soru sorma."),("Taşa Yazı Yazmak","Kimse okuyamıyor ama çok şık duruyor.")],
    "stone_axeshop":[("Bileme Taşı","Taşı bilemek için taş. Evrenin ironisi."),("Çırak Alımı","Çırak bedava çalışıyor. Buna 'tecrübe' diyoruz.")],
    "stone_firepit":[("Kuru Odun Kuralı","Islak odun yanmaz. Üç kişi yanarak öğrendi."),("Baharat Keşfi","Bir ot ateşe düştü, koku harika çıktı. Mutfak doğdu.")],
    "stone_mammoth":[("Mamut Eğitimi","Mamutlar 'otur' komutunu öğrenmedi ama taşıyorlar."),("Mamut Sendikası","Mamutlar zam istiyor. Yaprakla değil, gerçek zam.")],
    "stone_gallery":[("Parmak Boyası","İlk eleştirmen: 'Benim çocuğum da yapar.'"),("Sanat Turu","Komşu kabileden turistler geliyor. Rehber: 'Yiyecek ayrıca.'")],
  },
  global_upgrades=[  # ad, aciklama, deger, maliyet, onKosulBina, onKosulAdet
    ("Ateşi Evcilleştirmek","Artık yakmıyor da ısıtıyor. Büyük ilerleme.",1.5,2500,"stone_hunter",10),
    ("Tekerlek Fikri","Henüz tekerlek yok ama fikir var. Fikri bile ağır.",2,40000,"stone_quarry",15),
  ],
)

FARM = dict(
  id="farm", name="Tarım Çağı", click="wheat", advance="wheat", advanceCost=290000000, knowledge=0,
  new_resources=[("res_milk","milk","Süt"),("res_flour","flour","Un")],
  buildings=[
    ("farm_field","Buğday Tarlası","wheat","wheat",10,1,(-8,-3.4,1),(1,0.92,0.016)),
    ("farm_cows","İnek Ahırı","milk","wheat",120,2,(-6.5,-2.7,0),(0.9,0.9,0.9)),
    ("farm_mill","Yel Değirmeni","flour","wheat",1200,6,(-5,-3.4,1),(0.8,0.7,0.5)),
    ("farm_scarecrow","Korkuluk Birliği","wheat","milk",900,40,(-3.5,-2.7,0),(0.8,0.6,0.2)),
    ("farm_market","Pazar Tezgâhı","milk","flour",9000,90,(-2,-3.4,1),(0.3,0.6,0.9)),
    ("farm_bakery","Köy Fırını","flour","milk",40000,250,(-0.5,-2.7,0),(0.85,0.45,0.2)),
    ("farm_canal","Sulama Kanalı","wheat","flour",300000,2500,(1,-3.4,1),(0.2,0.5,0.8)),
    ("farm_square","Dedikodu Meydanı","flour","milk",2500000,5000,(2.5,-2.7,0),(0.6,0.6,0.3)),
  ],
  click_upgrades=[
    ("farm_click1","Daha İyi Orak","click",2,150,"Orak keskin. Parmaklar da.",None),
    ("farm_click2","Çift Orak","click",2,4000,"Biri yeterliydi ama iki tane daha havalı.",0),
    ("farm_click3","Bereket Duası","share",0.01,50000,"Dua ediyoruz. Sonuç: biraz daha fazla buğday.",1),
    ("farm_click4","Sabah Antrenmanı","click",3,700000,"Horoz ötmeden kalkıyoruz. Horoz küstü.",2),
  ],
  building_upgrades={
    "farm_field":[("Demir Saban","Tahta saban kırılıyordu. Demir diye bir şey keşfettik."),("Ürün Rotasyonu","Aynı tarlaya aynı şeyi ekme. Ya da ek, bakalım.")],
    "farm_cows":[("Yonca Menüsü","İnekler artık yonca istiyor. 'Ot' demek hakaret sayılıyor."),("Sabah Sağımı","İnekler sabah insanı değil ama idare ediyor.")],
    "farm_mill":[("Daha Büyük Kanatlar","Rüzgâr çıkınca çalışıyor. Çıkmayınca toplantı yapıyoruz."),("Değirmen Ustası","'Ben bilirim' deyip her şeyi bozdu. Sonra tamir etti.")],
    "farm_scarecrow":[("Ürkütücü Yüzler","Kargalar bile ürküyor. Komşu da."),("Korkuluk Tatbikatı","Düşman yok, tatbikat var. Ama hazırlıklıyız.")],
    "farm_market":[("Pazarlık Sanatı","'Son fiyat' ilk fiyatın yarısı. Herkes biliyor ama oynuyor."),("Bağırarak Satış","En yüksek ses en çok satar. Bilimsel olarak kanıtlandı: biri bağırdı.")],
    "farm_bakery":[("Taş Fırın","Ekmek kabarıyor. Neden kabarıyor? Kimse bilmiyor."),("Maya Bekçisi","Mayanın bakımını biri yapıyor. Maya ona teşekkür etmiyor.")],
    "farm_canal":[("Daha Derin Hendek","Su akıyor. Neredeyse istediğimiz yere."),("Kanal Bekçisi","Biri suyu çalıyor. Komşu mu? Evet, komşu.")],
    "farm_square":[("Çeşme Başı","Su alırken konuşuyoruz. Konu: başkaları."),("Meydan Muhabbeti","Haberler sözlü. Çoğu yanlış. Hepsi eğlenceli.")],
  },
  global_upgrades=[
    ("Takvim İcadı","Ne zaman ekeceğimizi biliyoruz. Çiftçiler 'zaten biliyorduk' diyor.",1.5,25000,"farm_mill",10),
    ("Çit Çekmek","Çit koyduk. Komşunun ineği 'sınır nerede' diye soruyor.",2,600000,"farm_canal",10),
  ],
)

BRONZE = dict(
  id="bronze", name="Bronz Çağı", click="bronze", advance="bronze", advanceCost=1000000000000, knowledge=0,
  buildings=[
    ("bronze_smelter","Bronz Dökümhanesi","bronze","bronze",100,18000,(-8,-3.4,1),(0.72,0.35,0.18)),
    ("bronze_mint","Sikke Darphanesi","coin","bronze",90000000,7000,(-6,-2.7,0),(0.86,0.68,0.28)),
    ("bronze_library","Kil Tablet Arşivi","knowledge","coin",300000000,2500,(-4,-3.4,1),(0.55,0.38,0.22)),
    ("bronze_cart","Tekerlekli Kervan","coin","bronze",900000000,80000,(-2,-2.7,0),(0.48,0.3,0.16)),
    ("bronze_aqueduct","Su Kemeri","knowledge","coin",5000000000,24000,(0,-3.4,1),(0.42,0.67,0.72)),
    ("bronze_workshop","Zanaat Atölyesi","bronze","knowledge",18000000000,350000,(2,-2.7,0),(0.72,0.48,0.25)),
    ("bronze_observatory","Gözlemevi","knowledge","bronze",70000000000,180000,(4,-3.4,1),(0.24,0.32,0.55)),
    ("bronze_forum","Şehir Forumu","coin","knowledge",250000000000,1800000,(6,-2.7,0),(0.78,0.58,0.32)),
  ],
  click_upgrades=[
    ("bronze_click1","Döküm Kalıbı","click",2,25000,"Aynı baltayı tekrar tekrar dökmek artık mümkün.",None),
    ("bronze_click2","Usta Çekiç","click",3,900000,"Çekiç daha ağır. Başparmak daha dikkatli.",0),
    ("bronze_click3","Yazılı Tarifler","share",0.015,250000,"Tablete yazdık. Tablet ağır, tarif kısa.",1),
    ("bronze_click4","Atölye Nöbeti","click",4,8000000,"Vardiya sistemi icat edildi. Usta hâlâ uyumuyor.",2),
  ],
  building_upgrades={
    "bronze_smelter":[("Fırın Tuğlaları","Daha iyi araçlar, daha düzenli üretim."),("Fırın Tuğlaları II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
    "bronze_mint":[("Düzenli Sikke","Daha iyi araçlar, daha düzenli üretim."),("Düzenli Sikke II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
    "bronze_library":[("Tablet Kataloğu","Daha iyi araçlar, daha düzenli üretim."),("Tablet Kataloğu II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
    "bronze_cart":[("Tekerlek Gresi","Daha iyi araçlar, daha düzenli üretim."),("Tekerlek Gresi II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
    "bronze_aqueduct":[("Çift Kemer","Daha iyi araçlar, daha düzenli üretim."),("Çift Kemer II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
    "bronze_workshop":[("Usta Çıraklığı","Daha iyi araçlar, daha düzenli üretim."),("Usta Çıraklığı II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
    "bronze_observatory":[("Yıldız Cetveli","Daha iyi araçlar, daha düzenli üretim."),("Yıldız Cetveli II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
    "bronze_forum":[("Açık Forum","Daha iyi araçlar, daha düzenli üretim."),("Açık Forum II","İlk yöntem işe yaradı. Biraz daha iyisini yaptık.")],
  },
  upgrade_costs={"bronze_smelter":(100000,10000000),"bronze_mint":(450000,45000000),"bronze_library":(1500000,150000000),"bronze_cart":(4500000,450000000),"bronze_aqueduct":(15000000,1500000000),"bronze_workshop":(45000000,4500000000),"bronze_observatory":(150000000,15000000000),"bronze_forum":(450000000,45000000000)},
  upgrade_resources={"bronze_smelter":"bronze","bronze_mint":"coin","bronze_library":"knowledge","bronze_cart":"coin","bronze_aqueduct":"knowledge","bronze_workshop":"bronze","bronze_observatory":"knowledge","bronze_forum":"coin"},
  global_upgrades=[
    ("Standart Ölçüler","Herkes aynı ölçü kabını kullanıyor. Neredeyse.",1.5,3000000,"bronze_mint",10,"coin"),
    ("Yazılı Kanunlar","Kurallar tablete yazıldı; artık kimse okuyamıyor.",2,100000000,"bronze_forum",10,"knowledge"),
  ],
)

ANCIENT = dict(
  id="antique", name="Antik Çağ", click="papyrus", advance="drachma", advanceCost=8000000000000, knowledge=1,
  buildings=[
    ("antique_field","Papirüs Tarlası","papyrus","papyrus",10,1,(-8,-3.4,1),(0.56,0.67,0.35)),
    ("antique_masonry","Mermer Ocağı","marble","papyrus",120,2,(-6,-2.7,0),(0.78,0.75,0.65)),
    ("antique_scribes","Kâtipler Birliği","knowledge","papyrus",450,5,(-4,-3.4,1),(0.75,0.63,0.42)),
    ("antique_market","Agora Pazarı","drachma","papyrus",1800,16,(-2,-2.7,0),(0.75,0.48,0.28)),
    ("antique_academy","Felsefe Akademisi","knowledge","drachma",14000,90,(0,-3.4,1),(0.42,0.55,0.72)),
    ("antique_theater","Amfitiyatro","drachma","marble",50000,450,(2,-2.7,0),(0.68,0.32,0.25)),
    ("antique_aqueduct","Su Kemeri","papyrus","marble",350000,3200,(4,-3.4,1),(0.37,0.58,0.66)),
    ("antique_lighthouse","İskenderiye Feneri","knowledge","drachma",3000000,10000,(6,-2.7,0),(0.92,0.73,0.38)),
  ],
  click_upgrades=[
    ("antique_click1","Keskin Kamış Kalem","click",2,250,"Yazı hızlandı. Okunabilirlik aynı kaldı.",None),
    ("antique_click2","Çift Taraflı Tablet","click",2,8000,"Önü dolunca arkasına yazdık. Deha böyle bir şey.",0),
    ("antique_click3","Kamusal Tartışma","share",0.015,120000,"Birisi konuşuyor, herkes üretimi bırakıp dinliyor.",1),
    ("antique_click4","Retorik Dersi","click",3,1800000,"Aynı şeyi daha uzun anlatınca daha ikna edici oluyor.",2),
  ],
  building_upgrades={
    "antique_field":[("Daha Verimli Kamışlık","Üretim arttı. Toplantı tutanağı kayıp."),("Daha Verimli Kamışlık II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
    "antique_masonry":[("Düzgün Kesilmiş Mermer","Üretim arttı. Toplantı tutanağı kayıp."),("Düzgün Kesilmiş Mermer II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
    "antique_scribes":[("Kopya Kâtipler","Üretim arttı. Toplantı tutanağı kayıp."),("Kopya Kâtipler II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
    "antique_market":[("Agora İndirimi","Üretim arttı. Toplantı tutanağı kayıp."),("Agora İndirimi II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
    "antique_academy":[("Düşünce Molası","Üretim arttı. Toplantı tutanağı kayıp."),("Düşünce Molası II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
    "antique_theater":[("Ucuz Koltuklar","Üretim arttı. Toplantı tutanağı kayıp."),("Ucuz Koltuklar II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
    "antique_aqueduct":[("İkinci Su Hattı","Üretim arttı. Toplantı tutanağı kayıp."),("İkinci Su Hattı II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
    "antique_lighthouse":[("Daha Güçlü Ayna","Üretim arttı. Toplantı tutanağı kayıp."),("Daha Güçlü Ayna II","İlk yöntemi geliştirdik. Komite hâlâ toplanıyor.")],
  },
  global_upgrades=[
    ("Standart Drahmi","Artık herkes aynı parayı tartışıyor.",1.5,25000,"antique_market",10,"drachma"),
    ("Büyük Kütüphane","Bilginin merkezi. Yangın söndürücü bütçesi yok.",2,8000000,"antique_academy",10,"knowledge"),
  ],
)

AGES=[STONE,FARM,BRONZE,ANCIENT]

PERMS=[ # id, ad, aciklama, tur, deger/seviye, maxSeviye, baseCost, growth, requires(idx)
  ("perm_global","Atalardan Notlar","Kimse okuyamıyor ama duvarda çok şık duruyor.","GlobalProduction",0.10,10,3,1.55,None),
  ("perm_click","Güçlü Bilekler","Ellerin hafızası var. Zaman makinesi bunu silemedi.","ClickPower",0.25,8,2,1.5,None),
  ("perm_cost","Pazarlık Ruhu","Tüccarlar 'bu sefer fiyatı sen belirle' dedi. İlk kez.","CostReduction",0.03,10,5,1.6,0),
  ("perm_advance","Kestirme Yol","Haritada yok ama bir keçi yolu biliyoruz.","AdvanceDiscount",0.05,8,4,1.6,1),
  ("perm_knowledge","Not Alma Alışkanlığı","Zaman yolculuğundan her seferinde daha çok şey hatırlıyoruz.","KnowledgeGain",0.20,5,8,2.0,0),
  ("perm_offline","Gözü Açık Uyku","Sen yokken köy çalışıyor. Sana sormadan.","OfflineHours",1,8,3,1.5,1),
  ("perm_global2","Paradoks Verimi","Geçmişteki biz, gelecekteki bizi kıskanıyor.","GlobalProduction",0.25,5,25,2.2,2),
]

def upgrades_for(age):
    """Simulasyon icin duz yukseltme listesi (C# uretici de ayni kurali kullanir)."""
    B={b[0]:b for b in age['buildings']}
    ups=[]
    for i,u in enumerate(age['click_upgrades']):
        ups.append(dict(id=u[0],name=u[1],kind=u[2],value=u[3],res=age['click'],cost=u[4],desc=u[5],
                        pre=age['click_upgrades'][u[6]][0] if u[6] is not None else None,unlock=None,target=None))
    for b in age['buildings']:
        (an,ad),(bn,bd)=age['building_upgrades'][b[0]]
        cost_a,cost_b=age.get('upgrade_costs',{}).get(b[0],(b[4]*12,b[4]*180))
        cost_resource=age.get('upgrade_resources',{}).get(b[0],b[3])
        ups.append(dict(id=b[0]+"_a",name=an,kind='bld',value=2,res=cost_resource,cost=cost_a,desc=ad,pre=None,unlock=(b[0],5),target=b[0]))
        ups.append(dict(id=b[0]+"_b",name=bn,kind='bld',value=2,res=cost_resource,cost=cost_b,desc=bd,pre=b[0]+"_a",unlock=(b[0],20),target=b[0]))
    for j,g in enumerate(age['global_upgrades']):
        n,d,v,c,ub,uc=g[:6]
        cost_resource=g[6] if len(g)>6 else age['click']
        ups.append(dict(id=age['id']+"_global%d"%(j+1),name=n,kind='glob',value=v,res=cost_resource,cost=c,desc=d,pre=None,unlock=(ub,uc),target=None))
    return ups
