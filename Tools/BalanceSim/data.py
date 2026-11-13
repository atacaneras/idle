# Tek kaynak: hem simulasyon hem C# uretici bunu kullanir.
# bina: id, ad, uretir, maliyetKaynak, baseCost, baseProd, (x,y,order), renk(r,g,b)
STONE = dict(
  id="stone", name="Taş Çağı", click="food", advance="stone", advanceCost=3000, knowledge=3,
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
  id="farm", name="Tarım Çağı", click="wheat", advance="wheat", advanceCost=2000000, knowledge=0,
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
        ups.append(dict(id=b[0]+"_a",name=an,kind='bld',value=2,res=b[3],cost=b[4]*12,desc=ad,pre=None,unlock=(b[0],5),target=b[0]))
        ups.append(dict(id=b[0]+"_b",name=bn,kind='bld',value=2,res=b[3],cost=b[4]*180,desc=bd,pre=b[0]+"_a",unlock=(b[0],20),target=b[0]))
    for j,(n,d,v,c,ub,uc) in enumerate(age['global_upgrades']):
        ups.append(dict(id=age['id']+"_global%d"%(j+1),name=n,kind='glob',value=v,res=age['advance'] if False else age['click'],cost=c,desc=d,pre=None,unlock=(ub,uc),target=None))
    return ups
