# Sıvıları açıklıkla ayır, arka planı sustur

Bir renk sıralama bulmacasında renkleri birbirinden ayıran şey ton (hue) değil, **açıklık (OKLCH L) ve mavi–sarı eksenidir**. Kırmızı–yeşil renk körlüğünde (protanopia/deuteranopia) kırmızı–yeşil ekseni neredeyse tamamen silinir; oyuncunun elinde yalnızca bu iki boyut kalır. Projenin bugünkü 10 renklik paleti normal görüşte çok iyi: en yakın çift 0.142 ΔEOK. Simülasyonda ise üç çift, Red/Brown, Blue/Purple ve Green/Orange, **0.032–0.033 ΔEOK'a** iniyor. Bu, renk körlüğüne dayanıklı kabul edilen Okabe-Ito paletinin en kötü çiftinin (0.076) yarısından az. Hesaplanmış 12 renklik bir set (#EA464A, #1770FA, #F1D01B, #3DCDCF, #F6F3E9, #C75CAA, #7E462D, #F98A21, #7B4EAE, #61BD69, #BEF686, #9D968E) açıklığı 0.46–0.96 aralığına yayıyor. Normal görüşte ≥0.108, dört görme koşulunun en kötüsünde ≥0.076 tutuyor. Yine de yalnızca renge dayalı tanıma için pratik tavan **7–9 renk**; 10. renkten itibaren sembol modu gerekiyor.

Arka plan sıvılardan açıklıkla ayrılmalı: OKLCH L 0.19–0.27, kroma ≤0.05, mavi-mora çalan koyu bir nötr. Bugünkü `Bg.png` açıklık olarak doğru, ama merkezde **C 0.146** ile önerilenin üç katı doygun bir mor. Cam, seçim, mantar, arayüz ve efektler ton yerine parlaklık, şekil ve hareketle konuşmalı. Mevcut altın seçim çizgisi (#FFD94D) sarı sıvıya karşı 1.11:1'e düşüp kayboluyor.

Türün liderleri (Magic Sort, IEC Water Sort Puzzle) paletlerini yayınlamıyor ve hiçbir erişilebilirlik özelliği beyan etmiyor. İsteğe bağlı bir sembol modu bu yüzden hem kılavuzlara uyum hem de rakiplerden farklılaşma sağlar. Unity tarafında post-processing'in kapalı olması doğru. Ancak `Liquid.shader`'daki 1.2 doygunluk artışı, önerilen setin en kötü çiftini 0.076'dan 0.058'e indiriyor ve kapatılmalı.

## Türün liderleri paleti değil, kaplamayı ve temayı satıyor

**Türün en çok kazanan oyunları renk kararlarını hiçbir yerde belgelemiyor.**

- **Magic Sort (Grand Games):** ABD App Store'da 695 bin değerlendirmeyle 4.7 puanda. Görselliğini canlı sıçramalar ve renk cümbüşü üzerinden pazarlıyor, ama palet, renk sayısı ya da hex değeri yayınlamıyor. Sürüm notlarının tamamı "yeni 100 bölüm ve hata düzeltmeleri"nden ibaret. Erişilebilirlik bölümünde geliştirici hiçbir özellik beyan etmemiş ([App Store: Magic Sort!](https://apps.apple.com/us/app/magic-sort/id6499209744)).
- **Ticari ağırlık:** Grand Games Ocak 2025'te Balderton liderliğinde **30 milyon dolarlık A serisi** yatırım aldı. Magic Sort o sırada Apple'ın en çok kazanan casual listelerine girmişti ([TechCrunch](https://techcrunch.com/2025/01/09/grand-games-the-turkish-gaming-startup-behind-pouring-game-magic-sort-drinks-up-30m/)).
- **IEC Water Sort Puzzle:** 324 bin değerlendirmeyle 4.7'de, erişilebilirlik bölümü yine boş ([App Store: IEC](https://apps.apple.com/us/app/water-sort-puzzle/id1514542157)).

**Liderlerin paletleri bu raporda doğrulanmış değil.** Bu araştırmada mağaza ekran görüntüleri piksel olarak okunamadığı için hiçbir oyundan hex örneklenemedi. Türde sık anılan "koyu zemin üzerinde 10–12 düz, doygun ton" kalıbı hâlâ bir hipotez.

Belgelenen şey, katman katman bir kaplama (skin) sistemi:

- **Get Color (Tripledot):** Oyuncu yorumlarına göre sıvı renklerinin kendisi bile şişe ve duvar kâğıdıyla birlikte açılabilen bir kozmetik ([App Store yorumları: Get Color](https://apps.apple.com/us/app/water-sort-puzzle-get-color/id1569776448?see-all=reviews)).
- **IEC:** Arka planlar ve şişeler coin ile satılıyor ([App Store yorumları: IEC](https://apps.apple.com/us/app/water-sort-puzzle/id1514542157?see-all=reviews)).
- **Water Sort Quest (MOBIRIX):** Tüp, tıpa ve arka plan özelleştirmesi sunuyor. 1.0.5 sürümünde tıpalara bir "tamamlanma efekti" ekledi ([App Store: Water Sort Quest](https://apps.apple.com/app/id6462847997)).

Bunun üç pratik sonucu var:

- **Varsayılan palet tek bir zeminde değil, birçok kaplama kombinasyonunda okunmalı.** Bu da arka planın sessiz kalmasını ve tüplerin kendi ayrımlarını (bir cam kenarı) taşımasını zorunlu kılıyor.
- **Çözülen tüpü kapatmak türün yerleşik "bitti" sinyali.** Projedeki mantar bu sözleşmeye uyuyor.
- **Gizli katmanlar ayrı bir görsel durum ister.** Magic Sort ve IEC'de alt katmanların gizlendiği bölümler var ([App Store yorumları: Magic Sort!](https://apps.apple.com/us/app/magic-sort/id6499209744?see-all=reviews)). Bu "bilinmeyen" durumun hiçbir sıvı rengiyle karıştırılmaması gerekiyor.

**Oyuncu şikâyetleri hep aynı türden çiftleri işaret ediyor.**

- **IEC, 2021:** Bir yorum yeşil ile açık mavinin karıştığını söylüyor ve renk körleri için oyunun oynanamaz olabileceği uyarısını yapıyor.
- **IEC, 2022:** Başka bir yorum, en düşük ekran parlaklığında renklerin ayırt edilemediğini anlatıyor ve renk körlüğü seçeneği istiyor ([App Store yorumları: IEC](https://apps.apple.com/us/app/water-sort-puzzle/id1514542157?see-all=reviews)).
- **Açık kaynak bir klon (sidhant947):** Turuncu ile sarı-turuncu karışıyor. Oyunda semboller olmasına rağmen bildiren kişi "you primarily identify them by their shade" diyor. Geliştiricinin çözümü varsayılan paleti düzeltmek değil, renk özelleştirmesini oyuncuya bırakmak oldu ([GitHub issue #50](https://github.com/sidhant947/water-sort/issues/50)).

Belgelenen bütün karışan çiftler renk çemberinde komşu ve **benzer açıklıkta**. Çözüm ton eklemek değil, açıklığı ayırmak. Bu yorumlar ayrıca sembollerin paletin yerine geçemeyeceğini, yalnızca bir yedek olduğunu gösteriyor.

**Renk körlüğü desteği yalnızca küçük oyunlarda var.** Water Sort Quest bir kırmızı–yeşil renk körlüğü modu duyuruyor ama neyi değiştirdiğini açıklamıyor ([App Store: Water Sort Quest](https://apps.apple.com/app/id6462847997)). En çok kazanan oyunların hiçbiri böyle bir şey sunmuyor. Bu rapordaki kaynaklar arasında bu, en ucuz farklılaşma fırsatı.

## Mevcut palet renk körlüğü simülasyonunda 0.032 ΔEOK'a çöküyor

**"Bir bakışta açıkça farklı" için yayımlanmış bir eşik yok.**

- **JND (fark edilebilir en küçük fark):** OKLab'de ΔEOK ≈ **0.02** (yaklaşık 1 ΔE00). Bu değer CSS Color 4'ün gamut eşlemesinde kullanılan bir mühendislik sabiti ([W3C CSS Color 4](https://www.w3.org/TR/css-color-4/)).
- **Kategoriler için çok küçük:** Bu eşik, oyuncunun bir bakışta kategori eşleştirmesi için çok küçük.
- **Eşikler tasarım tercihi:** Renk körlüğü simülasyonunu optimizasyon döngüsüne koyan `palettecore` bile eşiklerinin "erişilebilirlik sınırı değil, ayarlanabilir tasarım kuralı" olduğunu açıkça söylüyor ([palettecore](https://cran.rstudio.com/web/packages/palettecore/index.html)).

Bu yüzden hedefler, uzmanların renk körlüğüne dayanıklı saydığı paletlerin en kötü çiftlerinden geriye doğru hesaplandı:

| Durum | Hedef |
|---|---|
| Normal görüş | Her çift arasında **≥0.10–0.12 ΔEOK** (ΔE00 ≳ 15) |
| Her simüle renk körlüğü tipi | **≥0.075–0.08** (ΔE00 ≳ 8–10) |
| Pratikte çöken çiftler | 0.04'ün altı |

**Küçük alanlar hedefin üstünde pay ister.**

- Szafir'in modeli, ayırt edilebilirliğin işaretin türüne ve boyutuna göre belirgin biçimde değiştiğini gösteriyor ([Szafir 2018, IEEE TVCG](https://pubmed.ncbi.nlm.nih.gov/28866544/)).
- Çok küçük foveal alanlar normal gözde bile tritan benzeri, mavi–sarı karışıklığı üretiyor ([Williams vd. 1981](https://aria.cvs.rochester.edu/papers/williams-etal_VR1981b.pdf)).
- Telefondaki sıvı bantları yaklaşık 1–2° görüş açısı kaplıyor. Mavi–yeşil, sarı–beyaz ve sarı–limon çiftlerinde ek pay bırakılmalı.

**Tasarım uzayı OKLCH olmalı, HSL değil.**

- OKLab, CIELAB'ın özellikle mavilerde tonu yanlış tahmin etmesini düzeltmek için tasarlandı ([Ottosson](https://bottosson.github.io/posts/oklab/)).
- HSL'nin "lightness" değeri tonlar arasında tutarlı değil ([Evil Martians](https://evilmartians.com/chronicles/oklch-in-css-why-quit-rgb-hsl)).

Hangi çiftlerin çöktüğü tipten tipe değişiyor:

- **Protan/deutan:** Kırmızı, turuncu, kahverengi, zeytin, sarı-yeşil ve yeşil tek bir sarı-kahve ailesine çöküyor; aralarında yalnızca açıklık farkı kalıyor. Mavi ile mor, pembe ile gri/camgöbeği de birbirine çöküyor.
- **Tritan:** Mavi ile yeşil/teal, sarı ile beyaz/pembe ve turuncu ile pembe/macenta karışıyor.

Kılavuzlar da aynı yerde buluşuyor:

- **Xbox:** Kırmızı–yeşil eşleşmesinin yaygın renk körlüğünde başarısız olduğunu yazıyor ([Xbox XAG 102](https://devdocs.xbox.com/build/game-principles/accessibility/xag-deep-dives/xag-102-contrast.md)).
- **Apple HIG:** Kırmızı–yeşile ek olarak mavi–turuncuyu da riskli çift sayıyor ([Apple HIG](https://developer.apple.com/design/human-interface-guidelines/accessibility)).
- **GAG:** Kırmızının renk körlerine daha koyu göründüğünü not ediyor. İki takım için turuncu–mavi artı bir parlaklık farkı öneriyor ([Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/)).

Aşağıdaki tablo, Machado 2009 matrisleriyle (%100 şiddet, doğrusal sRGB) yayımlanmış paletler ve projenin mevcut paleti için hesaplanmış en kötü çift mesafelerini gösteriyor. Hex değerleri: Okabe-Ito ([NYU Siegal lab](https://siegal.bio.nyu.edu/color-palette/)), Tol ([khroma](https://www.stats.bris.ac.uk/R/web/packages/khroma/vignettes/tol.html)), Tableau ([d3](https://raw.githubusercontent.com/d3/d3-scale-chromatic/main/src/categorical/Tableau10.js)), Kelly ([gist](https://gist.github.com/ollieglass/f6ddd781eeae1d24e391265432297538)). Simülasyon matrisleri: [colorspacious](https://raw.githubusercontent.com/njsmith/colorspacious/master/colorspacious/cvd.py).

| Palet | Normal | Protan | Deutan | Tritan* | Dört koşulda ≥0.08 ile birlikte kullanılabilen |
|---|---|---|---|---|---|
| Okabe-Ito (7) | 0.156 | 0.096 | **0.076** | 0.085 | 6/7 |
| Tol vibrant (7) | 0.129 | 0.085 | 0.062 | 0.061 | 5/7 |
| Tol muted (10) | 0.120 | 0.081 | 0.052 | 0.073 | 8/10 |
| Tableau 10 | 0.083 | **0.011** | **0.007** | 0.051 | 5/10 |
| Kelly 20 kromatik | 0.051 | **0.004** | **0.009** | 0.026 | 8/20 |
| Proje, mevcut 10 renk | 0.142 | **0.033** | **0.032** | 0.055 | 6/10 |

*Tritan değerleri yaklaşıktır. DaltonLens'e göre tritanopia için tek geçerli model Brettel 1997; Machado tritanda zayıf kalıyor ([DaltonLens](https://daltonlens.org/opensource-cvd-simulation/)).

**Projenin paleti yüksek kromaya rağmen çöküyor.**

- **Kaynak:** Palet `Assets/HeronCaseRepo/ScriptableObjects/WaterColorPalette.asset` içinde. Renklerin çoğu çok canlı: kroma 0.18–0.26.
- **Çöken çiftler:**
  - Red #ED1C2E / Brown #8C4C1F protanda 0.033'e iniyor.
  - Blue #1A61FF / Purple #9429EB deutanda 0.032'ye iniyor.
  - Green #29CC38 / Orange #FF7300 deutanda 0.032'ye iniyor.
- **Sembolsüz kullanılabilir alt küme:** On rengin yalnızca altısı (Blue, Green, Yellow, Pink, Brown, White) aynı anda ≥0.08 en kötü durum mesafesinde kullanılabiliyor.
- **Ders:** Kromayı kırmızı–yeşil ekseni boyunca artırmak, renk körü oyuncuya hiçbir şey kazandırmıyor.

**Yalnız renkle tanıma için pratik tavan 7–9 renk.**

- **Yayımlanmış tavan:** Hiçbir yayımlanmış palet 12 canlı ve renk körlüğüne dayanıklı renk vermiyor. Tol kendi setleri için sınırı 7 (bright, vibrant) ve 9 (muted) koyuyor ve setlerin genişletilmemesini, aradeğerlenmemesini istiyor ([khroma](https://www.stats.bris.ac.uk/R/web/packages/khroma/vignettes/tol.html)).
- **Simülasyonun söylediği:** Simülasyondan çıkan "birlikte kullanılabilir" sayıları da aynı tavanı gösteriyor.
- **Koyu zeminin maliyeti:** Koyu zemin, açıklık ekseninin alt üçte birini kullanılamaz kılıyor. L ≈ 0.45'in altındaki sıvılar L 0.25'lik bir zemine karşı 2:1'in altına düşüyor; lacivert, bordo ve koyu yeşil zayıf kategorilere dönüşüyor.
- **Sonuç:** Kullanılabilir bant kabaca L 0.45–0.97. On iki rengin bu yarım birimlik aralığı paylaşması gerekiyor.

## Önerilen 12 renk Okabe-Ito'nun 0.076'lık tabanını tutuyor

**Set nasıl üretildi?**

- **Yöntem:** Adlandırılmış ton bantları içinde OKLCH üzerinde simulated annealing çalıştırıldı. Amaç, normal, protan, deutan ve tritan koşullarında en kötü çift mesafesini, yani min(ΔEOK, ΔE00/100)'ü büyütmekti. Bu, Glasbey'in "maximin" seçimi ve `palettecore`'un renk körlüğü simülasyonlu optimizasyonuyla aynı ilke ([Glasbey vd. 2007](https://strathprints.strath.ac.uk/30312/); [palettecore](https://cran.rstudio.com/web/packages/palettecore/index.html)).
- **Sabit yuvalar:** `WaterColor.cs`'teki on enum yuvası korundu, üstüne iki yuva eklendi.
- **Eklenen iki yuva: Lime ve Gray.**
  - Teal + Maroon kombinasyonu biraz daha yüksek puan aldı (0.081). Ama Maroon ve Navy L 0.40–0.53'e düşüyor. Bu renkler koyu zeminde kötü okunuyor ve gizli sıvı rengine yaklaşıyor.
  - Sky eklemek üç mavi sıvı yaratıyor, bu da tritan ve küçük alan riski demek.
- **Uyarı:** Değerler hesaplanmış; kullanıcı testinden geçmedi.

| Sıra | Ad | Önerilen hex | OKLCH (L C h) | WCAG, #1D2035 zemine | Mevcut hex |
|---|---|---|---|---|---|
| 1 | Red | **#EA464A** | 0.631 0.201 23.8 | 4.19 | #ED1C2E |
| 2 | Blue | **#1770FA** | 0.581 0.221 260.0 | 3.62 | #1A61FF |
| 3 | Yellow | **#F1D01B** | 0.860 0.173 97.1 | 10.54 | #FFDB00 |
| 4 | Cyan (turkuaz) | **#3DCDCF** | 0.776 0.118 196.4 | 8.28 | #00C7ED |
| 5 | White (krem) | **#F6F3E9** | 0.964 0.014 93.0 | 14.47 | #F2F2F7 |
| 6 | Pink (macenta-pembe) | **#C75CAA** | 0.631 0.164 339.0 | 4.24 | #FF409E |
| 7 | Brown | **#7E462D** | 0.456 0.086 44.2 | 2.14 | #8C4C1F |
| 8 | Orange | **#F98A21** | 0.740 0.170 56.7 | 6.64 | #FF7300 |
| 9 | Purple | **#7B4EAE** | 0.519 0.150 303.2 | 2.71 | #9429EB |
| 10 | Green | **#61BD69** | 0.721 0.148 145.6 | 6.88 | #29CC38 |
| 11 | Lime | **#BEF686** | 0.910 0.153 130.7 | 12.78 | — |
| 12 | Gray (sıcak) | **#9D968E** | 0.677 0.014 71.3 | 5.50 | — |

**Tüm set için koşul bazında en küçük mesafeler:**

| Koşul | En küçük ΔEOK | Çift |
|---|---|---|
| Normal görüş | **0.108** | Yellow/Lime |
| Protan | 0.088 | Orange/Green |
| Deutan | 0.076 | Yellow/Lime |
| Tritan (yaklaşık) | 0.076 | Green/Cyan |

Mevcut paletin karşılığı 0.032–0.033.

**Bedeli dürüstçe söylemek gerekiyor.**

- **Normal görüşte en yakın çift kötüleşiyor:** 0.142'den 0.108'e iniyor. Bu yine de hedefin üstünde.
- **Bazı renkler daha yumuşak görünecek:**

  | Renk | Mevcut kroma | Önerilen kroma |
  |---|---|---|
  | Red | 0.234 | 0.201 |
  | Purple | 0.262 | 0.150 |
  | Pink | 0.237 | 0.164 |
  | Green | 0.226 | 0.148 |

- **Karşı görüş:** Canlı mevcut paleti tutup sembol modunu zorunlu kılmak savunulabilir.
- **Bu raporun tercihi:** Dayanıklı seti varsayılan yapmak. Çünkü oyuncular sembol olsa bile önce renk tonuna bakıyor (sidhant947 issue #50). Ayrıca kaybedilen kroma büyük ölçüde renk körü oyuncunun zaten göremediği kırmızı–yeşil ekseninde.

**Optimizasyonun bulduğu taktikler kendi başına kural olarak kullanılabilir:**

- **Mavi–mor:** Mor, maviyle aynı açıklıkta bir mavi-menekşe olmamalı. Daha kırmızıya çekilmeli (h ≈ 303).
- **Kırmızı–turuncu–yeşil:**
  - Kırmızı orta açıklıkta (0.63) duruyor, turuncu daha açık (0.74).
  - Yeşil turuncuyla benzer açıklıkta ama mavi–sarı ekseninde ondan ayrılıyor (h 146).
  - Kırmızı ile yeşil ise ≈0.09'luk açıklık farkına dayanıyor.
- **Sarı–limon–beyaz (koyu zeminde en sıkışık üçlü):**
  - Sarı L 0.86'da.
  - Limon L 0.91'de ve yeşile kaydırılmış (h 131).
  - Beyaz L 0.96'da, kroması sıfıra yakın bir krem.
- **Gri:** Pembe ve kırmızıyla açıklıkta üst üste binmesin diye L 0.68'de.
- **Okabe-Ito'dan alınan ders:** Yeşil maviye, kırmızı turuncu-vermilyona itildiğinde çiftler simülasyonda hayatta kalıyor.

**Renklerin oyuna giriş sırası da tasarlanmalı.**

- **Önerilen sıra:** Red, Blue, Yellow, Cyan, White, Pink, Brown, Orange, Purple, Green, Lime, Gray. Her adımda zaten oyundaki renklerden en uzak olan renk ekleniyor (greedy maximin). Bu, Kelly ve Trubetskoy listelerindeki "her önek kendi başına kullanılabilir olsun" mantığıyla aynı ([Polychrome](https://rdrr.io/pkg/Polychrome/man/palettes.html); [sashamaps](https://sashamaps.net/docs/resources/20-colors/)).
- **Bu sırayla en kötü çift mesafesi:**

  | Oyundaki renk sayısı | En kötü çift |
  |---|---|
  | 4'e kadar | ≥0.18 |
  | 9'a kadar | ≥0.09 |
  | 10 ve üstü | 0.076 |

- **Yeşil neden geç geliyor:** Yeşil üç tehlikeli çiftin merkezinde.
- **"Tanıdık önce" alternatifi:** Red, Blue, Yellow, Green ile başlamak, Red/Green yüzünden 4. renkte 0.079'a düşüyor. Bu sıra yalnızca sembollerin baştan açık olduğu bir oyunda mantıklı.
- **Seviye üretecinin bilmesi gereken tehlikeli çiftler** (en kötü durum < 0.09):

  | Çift | En kötü durum |
  |---|---|
  | Yellow/Lime | 0.076 |
  | Green/Cyan | 0.076 |
  | Pink/Gray | 0.076 |
  | Green/Gray | 0.077 |
  | Red/Green | 0.079 |
  | White/Lime | 0.079 |
  | Orange/Green | 0.080 |
  | Red/Gray | 0.088 |

- **Seviye kuralları:**
  - **2–5 renkli seviyeler:** Yalnızca ≥0.12 çiftler.
  - **6–9 renkli seviyeler:** ≥0.09.
  - **10. renk geldiğinde:** Renk körlüğü sembolleri seçeneği önerilmeli ya da varsayılan olarak açılmalı.

**Renk bu oyunda mekaniğin kendisi; bu yüzden sembol modu kılavuzların doğrudan karşılığı.**

- **GAG:** Temel seviye kural olarak hiçbir temel bilginin yalnızca renkle verilmemesini istiyor. Renk kaçınılmazsa oyuncuya renk seçimi ya da protan/deutan/tritan hazır ayarları sunulmasını öneriyor. Tüm ekranı filtrelemeyi "kaba bir araç" sayıyor. Tasarımın %100 şiddete göre yapılmasını söylüyor ve bulmaca örneği olarak Two Dots'u gösteriyor. Kırmızı–yeşil güçlüğü erkeklerin yaklaşık **%8–10**'unu etkiliyor ([GAG](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/)).
- **Xbox XAG 103:** Rengin asla tek kanal olmamasını istiyor ([Xbox XAG 103](https://devdocs.xbox.com/gaming/accessibility/xbox-accessibility-guidelines/103)).
- **Apple:** Renge ek olarak şekil ve ikon öneriyor ([Apple HIG](https://developer.apple.com/design/human-interface-guidelines/accessibility)).
- **Two Dots:** Renk körlüğü modunda her renge ayrı bir sembol veriyor ([iPhone Life](https://iphonelife.com/node/275277)).

Projeye uygulanışı şöyle olmalı:

- **Ayar:** Kalıcı, isteğe bağlı bir "Renk körlüğü sembolleri" ayarı.
- **Sembolün çizildiği yerler:** Her sıvı bandının üstü. Döküm akışı ve mantar renkle kodlanıyorsa onlar da.
- **Silüetle ayırt edilen 12 şekil:** Daire, üçgen, kare, eşkenar dörtgen, yıldız, kalp, artı, hilal, damla, altıgen, şimşek, halka.
- **Okunurluk:** Bantlar L 0.45 ile 0.97 arasında yayıldığı için semboller banda göre açık-koyu dış çizgili olmalı.
- **Gizli katman:** Kendi "?" işaretini taşımalı.

## Arka plan L 0.19–0.27 ve C ≤0.05 bandında kalmalı

**Sanat yönetimi kaynakları ilkede hemfikir ama sayı vermiyor.**

- **Odak ve değer aralığı:** Oyun parçaları en yüksek kontrastı almalı; arka plan dar bir değer aralığında kalıp dikkat çekmemeli.
- **Gri tonlama testi:** Ekran görüntüsü renksizleştirildiğinde sahne bir bakışta okunmalı. DIGHARD örneği bu testi zemin, düşman ve yürüme yüzeyleri aynı gri tonda olduğu için geçememiş ([Game Developer](https://gamedeveloper.com/design/how-to-reduce-visual-confusion-in-your-game)).
- **Ölçek ihtiyatı:** Supersonic, kontrast içeren bir arayüz yeniden tasarımının tek bir oyunda D7 tutundurmayı %20 artırdığını aktarıyor. Ama bu, rengin tek başına etkisi değil, tüm yeniden tasarımın etkisi ([Supersonic](https://supersonic.com/learn/blog/improve-the-clarity-of-your-hyper-casual-prototype-with-these-3-art-design-tips/)).

**Sayılar arayüz standartlarından ödünç alınabilir.**

- **WCAG 1.4.11:** İçeriği anlamak için gereken grafik parçalarının komşu renge karşı **en az 3:1** olmasını istiyor. Yuvarlama yok, yani 2.999:1 geçmiyor ([W3C 1.4.11](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html)).
- **Material HCT:** Tonlar (CIELAB L*) arasında **40'lık fark ≥3:1**, **50'lik fark ≥4.5:1** garanti ediyor ([Material HCT kaynak kodu](https://developer.android.com/agents/skills/xr/display-glasses-with-jetpack-compose-glimmer/references/material-hct-source)).

Canlı bir 10 renklik örnek palet üzerinde yapılan hesaplar zemin seçeneklerini açıkça ayırıyor:

| Zemin | Hex | Sonuç |
|---|---|---|
| Koyu çivit, oklch(0.22 0.04 280) | #17182D | 10 tonun 8'i 4:1'i geçiyor; yalnızca doğası gereği koyu mor (2.48) ve kahverengi (2.29) geride kalıyor |
| Orta gri | #808080 | En kötü çift 1.05:1; en iyi ton bile 2.83:1 |
| Açık pastel | #DCEAF8 | Sarıyı 1.14:1'e indiriyor |
| Kırık beyaz | #F8F5EE | Sarıyı 1.28:1'e, limonu 1.64:1'e indiriyor |
| Doygun koyu mor, oklch(0.30 0.07 300) | #33244B | Moru 2.00'ye, kahveyi 1.85'e düşürüyor; kırmızı, mavi ve pembeyi 4.5:1'in altına çekiyor |

**Sonuç: kazanan, koyu, düşük kromalı, soğuk bir nötr.**

- **Açık zemin neden olmuyor:** NHS'in kategorik palet kılavuzu açık zemini önerse de kendi paletini beyaz için tasarlamış ([Turas](https://www.turasdesignsystem.nhs.scot/design/data-visualisation/categorical-palette/)). Parlak sarı ve limona ihtiyaç duyan bir sort oyununa bu öneri taşınmıyor.
- **Ton seçimi:** Tüm renk çemberine yayılan bir paletin tamamlayıcısı olmadığından, zemin tonu paletin en az hassas olduğu mavi-menekşe bölgede (h 260–285) seçilmeli. Sıcak koyu zeminler kahve, kırmızı, turuncu ve pembe sıvılarla çatışıyor ve camı kirli gösteriyor.
- **Eşzamanlı kontrast:** Doygun bir zemin komşu renkleri karşıt tona boyar ([Contrast effect](https://en.wikipedia.org/wiki/Contrast_effect)). Mor bir alan, yanındaki camı ve beyaz parlamaları sarı-yeşile iter ve mavi/mor sıvıları soldurur.
- **Helmholtz–Kohlrausch etkisi:** Doygun kırmızı, pembe ve maviler koyu zeminde WCAG oranının söylediğinden parlak görünür; sarı ve yeşil bu etkiden daha az payını alır ([Helmholtz–Kohlrausch effect](https://en.wikipedia.org/wiki/Helmholtz%E2%80%93Kohlrausch_effect)).

**Projenin bugünkü arka planı açıklıkta doğru, kromada yanlış.** `Assets/HeronCaseRepo/Art/Sprites/Bg.png` bu rapor için piksel piksel ölçüldü:

| Ölçüm noktası | Hex | OKLCH |
|---|---|---|
| Radyal gradyan, merkez | #1D0C69 | 0.266 0.146 278 |
| Köşeler | #040127 | 0.134 0.079 275 |
| Üst ve alt orta | #090332 | ≈0.16 0.09 278 |
| Kameranın clear rengi | #1F1660 | 0.270 0.124 280 |

- **Kontrast değişmiyor:** Merkez açıklığı varsayılan referans zeminle (L 0.25) neredeyse aynı. Önerilen paletin kontrastları burada da korunuyor (Brown 2.14, Purple 2.71, Blue 3.62).
- **Sorun 1, kroma:** 0.146, önerilen üst sınırın (0.05) yaklaşık üç katı. Bu doygun mor, mavi ve mor sıvılarla tonda yarışıyor.
- **Sorun 2, açıklık farkı:** Merkezle kenar arasındaki fark ΔL 0.13. Bu, önerilen 0.04–0.08'in yaklaşık iki katı. Kenardaki tüpler merkezdekilerden farklı bir kontrast zemininde duruyor.

**Önerilen arka plan:**

- **Merkez:** oklch(0.25 0.04 278) = **#1D2035**.
- **Kenarlar:** oklch(0.19 0.035 278) = **#101223** (ΔL 0.06).
- **Spot ışığının konumu:** Ekranın değil tüp kümesinin merkezine oturmalı. Yarıçapı hiçbir tüp geçiş bölgesinde kalmayacak kadar geniş olmalı.
- **Gradyanın kenarı:** Tamamen yumuşatılmalı ([Adobe Lightroom](https://www.adobe.com/learn/lightroom-cc/web/photo-vignette)).
- **Bantlaşma:** Koyu gradyanlar 8-bit'te bantlaşır. Zemin shader'ına ±0.5–1 LSB dither eklenmeli.
- **Saf siyahtan kaçınılmalı.**

Zemin ne kadar koyuysa koyu sıvılar o kadar iyi okunuyor:

| Zemin | Brown | Purple | Blue |
|---|---|---|---|
| L 0.29 (#262942) | 1.89 | 2.40 | 3.20 |
| L 0.25 (#1D2035) | 2.14 | 2.70 | 3.61 |
| L 0.19 (#101223) | 2.47 | 3.13 | 4.18 |

Brown ve Purple hiçbir makul zeminde 3:1'i geçmiyor. Bu yüzden 3:1'lik kenarı sıvı değil, açık renkli cam kenarı ve menisküs çizgisi taşımalı.

**Gizli sıvı rengi bugün bir delik gibi okunuyor.**

- **Mevcut durum:** `GameSettings.cs`'teki `_hiddenLiquidColor` (≈#0E0E0E) mevcut zemin merkezine karşı **1.20:1**. Gizli bir katman "bilinmeyen sıvı" değil, boş bir delik gibi görünüyor.
- **Öneri:** oklch(0.367 0.015 286) = **#3E3E47** dolgu ve üstünde #B9BDCE renginde bir "?" (dolguya karşı 5.66:1).
- **Neden daha açık bir gri değil:** #4A4A52 (L 0.41) renk körlüğü simülasyonunda Brown'a 0.066'ya kadar yaklaşıyor. #3E3E47 ise 0.085'i koruyor.
- **İlke:** Gizli durum düşük kromalı olmalı, Brown'dan koyu durmalı ve bir sembol taşımalı. Böylece sıvı uzayının dışında kalır.

## Cam, mantar, arayüz ve efektler tonla değil, parlaklık ve şekille konuşmalı

**Camın kendine ait bir rengi yoktur; onu kenarları ve parlamaları tanımlar.** Bir illüstrasyon tarifi ([Clip Studio Tips](https://tips.clip-studio.com/zh-tw/articles/3432?org=1)) şunları öneriyor:

- Gövde düşük opaklıkta açık renk (%18).
- Kalın bölgelerde %30 Multiply koyu mavi gölge.
- Forma uyan sert kenarlı uzun parlama çizgileri.
- Saf siyah ve saf beyazdan kaçınma.

Koyu zemine uyarlanmış önerilen değerler:

| Öğe | Önerilen değer |
|---|---|
| Gövde | %6–15 opaklık |
| Kenar | oklch(0.92 0.02 278) = **#E1E4F2**, %50–80 opaklık, telefonda 2–4 px. Tam opaklıkta #1D2035'e karşı 12.65:1; kompozit sonuç ≥3:1 olmalı |
| Parlama | Tüp genişliğinin %8–15'i kadar dar dikey beyaz şerit, %60–90 opaklık |
| İç gölge | Zemin renginde %20–30 Multiply |
| Sıvı üstü | İnce, açık bir menisküs çizgisi. Benzer açıklıktaki komşu katmanları ayırır |

**Doğrusal (linear) renk uzayında cam, Photoshop maketinden çok daha fazla yıkar.**

- **Neden:** Unity Linear projede alfa harmanlama doğrusal ışık değerleri üzerinde yapılıyor ([Unity Manual](https://docs.unity3d.com/6000.2/Documentation/Manual/differences-linear-gamma-color-space.html)).
- **Sayısal örnek:** #E63946 kırmızı üstüne %25 beyaz:
  - Photoshop'un gamma harmanlaması #EC6A74 veriyor.
  - Unity #ED9094 veriyor.
  - Aynı parlaklığı tutturmak için Unity'de **≈0.117** alfa gerekiyor.
  - Koyu zemin üstünde bu oran 0.080'e iniyor.
  - Koyu gölge katmanları ise ≈1.8 kat alfa istiyor.
- **Sort paleti için en kötü durum:** Açık bir cam katmanı her kategoriyi pastele çeker ve kategoriler arası kroma farkını küçültür.
- **Seçenek 1:** Açık camı sıvı gövdesinin üstünden çekmek, yalnızca kenar ve parlamada tutmak.
- **Seçenek 2:** "Cam arkasındaki sıvı" rengini sıvı shader'ı içinde birleştirmek. Böylece her kategori için son renk doğrudan tasarlanıp ölçülebilir.
- **Maketler için:** Photoshop'ta "Blend RGB Colors Using Gamma: 1.00" ayarı kullanılabilir ([PetaPixel](https://petapixel.com/2015/03/20/an-explanation-of-how-computers-handle-color)). Photoshop 26.10'da bu ayarla ilgili hatalar bildirilmiş ([Adobe Community](https://community.adobe.com/t5/photoshop-ecosystem-bugs/p-photoshop-26-10-incorrect-alpha-blending-on-canvas-for-gamma-1-00-at-different-zoom-levels/m-p/15495532)).

**Tek renkli hiçbir seçim çizgisi 12 renklik palette güvenli değil.**

- **Mevcut altın (`_outlineColor` ≈ #FFD94D):** Önerilen Yellow'a karşı **1.11:1** (renk körlüğü simülasyonunda 0.023 ΔEOK), Lime'a karşı 1.09:1. Sarı ya da limon üst katmanın üzerinde kayboluyor.
- **Beyaz alternatif (#F2F4FF):** Bu kez White sıvıya karşı 1.01:1'e düşüyor.
- **Çözüm:** Seçimi önce hareket (tüpün kalkması), sonra çift çizgi taşımalı:
  - Zeminle buluşan açık bir dış çizgi (#F0F1F9, zemine karşı ≥14:1).
  - Sıvıyla arasına giren koyu bir iç çizgi (#1D2035).
- **Dinlenme hâli:** Cam kenarı nötr kalmalı ki seçim net bir değişim olarak okunsun.
- **Ton ailesi:** Altın, ödül ve CTA vurgusu olarak ayrılırsa seçimle aynı ton ailesini paylaşmamış olur.

**Mantarın rengi doğru; kimliğini ise şekil taşımalı.**

- **Mevcut renk:** `Cap.png` ortalaması #A5815C = oklch(0.629 0.068 67). Zemine karşı 4.5:1, düşük kromalı ve iyi bir malzeme rengi.
- **Risk:** Renk körlüğü simülasyonunda Red'e **0.034**, Gray'e 0.067 yaklaşıyor. Protan bir oyuncu kırmızı bir tüpün mantarını "bir kırmızı bant daha" sanabilir.
- **Mantarın şekil kimliği:**
  - Mantarın alt kenarı ile en üst sıvı yüzeyi arasında görünür bir cam ağzı ya da boşluk kalmalı.
  - Mantarın daha koyu bir dış çizgisi, daha açık bir üst yüzü ve gözenek dokusu olmalı.
  - Kroması ≤0.07'de tutulmalı ki 13. bir sıvıya dönüşmesin.
- **Oturma animasyonu:** Zaten renk dışı bir işaret. Water Sort Quest'in tıpa tamamlanma efektiyle aynı sözleşmeye uyuyor ([App Store: Water Sort Quest](https://apps.apple.com/app/id6462847997)).

**Çözülmüş tüp parlaması:**

- Tüpün kendi sıvı rengini kullanmalı.
- %25–50 opaklıkta ve yumuşak olmalı.
- Beyaz bir çekirdeği olmalı. Böylece komşu tüplere taşıp onların algılanan tonunu değiştirmez.

**Efektlerde Riot'un VFX kuralları işe yarıyor.**

- **Kurallar:** Okunması gereken birincil efekt en geniş değer aralığını, güçlü kontrastı ve yüksek opaklığı alır. İkincil öğeler daha düşük değerde kalır. %100 ve %0 doygunluktan kaçınılır ([VFX Apprentice, Riot VFX kılavuzu özeti](https://www.vfxapprentice.com/blog/10-league-of-legends-vfx-design-tips)).
- **Döküm sıçraması:** Dökülen sıvının rengi, tepede +0.05–0.1 L açılmış hâli ve beyaz parlama noktaları. Başka ton yok, çünkü sıçrama hangi sıvının hareket ettiğinin bilgisi.
- **Kıvılcımlar:** Beyazdan soluk altına, oklch(0.95 0.06 90) = **#FEEEC1**. Tonla değil parlaklıkla okunurlar, hiçbir sıvıyla eşleşmezler.
- **Konfeti:** Altın, beyaz ve bir zemin tonu; ya da yalnızca bölümün kendi sıvı renkleri. Tahta görünürken gökkuşağı konfeti her sıvı tonunu gürültü olarak geri getirir. Kazanma paneli altında %40–60'lık bir karartma katmanı kullanılmalı.
- **Kıvılcım mı, konfeti mi:** Bir geliştirici ödül ekranında konfetiyi parıltılı parçacıklarla değiştirmenin çok daha iyi çalıştığını aktarıyor ([Picking Pies devlog](https://pickingpies.itch.io/archeonimals-tap-dig-collect/devlog/113518/how-to-improve-a-new-creature-screen)).
- **Bütçe:** Aynı anda en fazla bir tam ekran efekt. Parçacık doygunluğu sıvıların altında kalmalı.

**Arayüzde WCAG tabandır.**

- **Metin:** Normal metin ≥4.5:1. Büyük metin (18 pt ya da 14 pt kalın) ≥3:1 ([W3C 1.4.3](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html)).
- **Bileşen sınırları:** ≥3:1.

**60-30-10 kuralı bu oyuna şöyle oturuyor** ([UX Planet](https://uxplanet.org/the-60-30-10-rule-a-foolproof-way-to-choose-colors-for-your-ui-design-d15625e56d25)):

- **%60, arka plan:** Görsel "sessizlik".
- **%30, nötr yapı:** Cam, HUD ve paneller. Zemin tonunun 0.08–0.15 L açılmış hâli; yeni bir doygun ton getirmez.
- **%10, doygun renk:** Bunun neredeyse tamamını sıvılar kullanıyor. Arayüz vurgusuna kalan bütçe tek bir renk.
- **Menüler:** Supersonic menülerde 1–2 renk kullanmayı ve başlat düğmesini en büyük, en parlak öğe yapmayı öneriyor ([Supersonic](https://supersonic.com/learn/blog/improve-the-clarity-of-your-hyper-casual-prototype-with-these-3-art-design-tips/)).
- **Uyum:** Gurney'nin gamut maskeleme yaklaşımına göre uyum renkleri dışarıda bırakarak kurulur ([Lines and Colors](https://linesandcolors.com/2011/09/19/james-gurney-on-gamut-masking/)). Sahne dar bir soğuk maske, tek bir sıcak altın vurgu ve bu maskenin istisnası olan tam doygun sıvılardan oluşmalı.

Önerilen renk değerleri:

| Rol | Hex | OKLCH | Not |
|---|---|---|---|
| Zemin merkezi | #1D2035 | 0.25 0.04 278 | Spot ışığı tüp kümesinde |
| Zemin kenarı | #101223 | 0.19 0.035 278 | ΔL 0.06 |
| Panel / HUD | #2D314A | 0.32 0.045 278 | Zemine karşı yalnızca 1.26:1, bu yüzden açık bir kenar çizgisi gerekir |
| Cam kenarı | #E1E4F2 | 0.92 0.02 278 | %50–80 opaklık |
| Birincil metin | #F0F1F9 | 0.96 0.01 278 | Zemine 14.24:1, panele 11.31:1 |
| İkincil metin | #B9BDCE | 0.80 0.025 278 | Zemine 8.57:1 |
| CTA altını | #F7C243 | 0.84 0.15 85 | Etiket #1D2035 ile 9.73:1 |
| Kıvılcım | #FEEEC1 | 0.95 0.06 90 | Yalnızca başarı anında |
| Gizli sıvı | #3E3E47 | 0.367 0.015 286 | Üstünde #B9BDCE "?" |

**CTA altını konusunda iki uyarı:**

- **Etiket koyu olmalı:** Beyaz yazı daha koyu bir altın üstünde bile (#DF911A) yalnızca **2.56:1** veriyor.
- **Sarı sıvıyla karışmamalı:** CTA altını sarı sıvıya yalnızca ≈0.04 ΔEOK uzak. Malzemeyle ayrılmalı: #DF911A'ya inen dikey gradyan, koyu dış çizgi ve ikon. Ayrıca tahtanın dışında durmalı.

## Unity hattında hex'i sRGB gir, shader'ın doygunluk artışını kapat

**Linear projede tasarımcının sRGB hex'i olduğu gibi girilmeli.** Unity'nin bu değeri yükleme sırasında kendisi doğrusallaştırdığı yerler:

- Inspector renk alanları.
- `MaterialPropertyBlock.SetColor` ([Unity API](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/MaterialPropertyBlock.SetColor.html)).
- `SpriteRenderer.color`.
- Shader Graph'ın Default modundaki Color'ı ([Unity Manual](https://docs.unity3d.com/6000.2/Documentation/Manual/linear-color-space.html)).

**Ham kanallar dönüştürülmez.** `SetVector`, `SetVectorArray`, `SetColorArray` ([Unity API](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Material.SetColorArray.html)), elle yazılan mesh vertex renkleri ve HLSL sabitleri dönüştürülmüyor. Bunlar için `Color.linear` gerekiyor.

**Proje bu ayrımı zaten doğru yapıyor.** `TubeLiquid.cs`, `SetVectorArray`'den önce `.linear` uyguluyor; `PourStreamView.cs` vertex renklerine `color.linear` yazıyor.

**İki ek kural:**

- **`Material.SetColor`:** 6000.2 belgesindeki çelişkili ifade nedeniyle bir kez test edilmeli ([Unity API](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Material.SetColor.html)).
- **`[HDR]` özellikleri:** Palet renkleri bu özelliklerden uzak tutulmalı.

**Asıl sürpriz `Liquid.shader`'ın `Grade()` fonksiyonunda.**

- **Ayarlar:** `Liquid.mat` ve `PourStream.mat` `_Saturation` değerini **1.2**'ye çekiyor. Shader'ın varsayılan `_Brightness` değeri **1.05**. Fonksiyon sonucu `saturate()` ile kırpıyor.
- **Sonuç:** Önerilen paletin bu işlemden geçmiş hâli hesaplandığında en kötü çift **0.076'dan 0.058'e** iniyor (deutanda Yellow/Lime).
  - Lime #BBFF6C'ye, Yellow #FCD400'e kırpılıyor.
  - Red #FC222C'ye kayıyor.
- **Kırpma olmadan:** `_Saturation` 1.0'da (parlaklık 1.05 kalsa bile) en kötü çift 0.077 ve korunuyor.
- **Öneri:** Doygunluk 1.0'a çekilmeli ve palet asset'i tek doğruluk kaynağı olmalı. Ya da artış hex'lere önceden işlenip kontrol yeniden çalıştırılmalı.
- **Luma ağırlıkları:** `Grade()` doğrusal değerler üzerinde Rec.601 gamma ağırlıklarını (0.299/0.587/0.114) kullanıyor. Fiziksel olarak uygun takım Rec.709 (0.2126/0.7152/0.0722). Ama hesaba göre yalnızca ağırlıkları değiştirmek sorunu çözmüyor (0.054); asıl suçlu kırpma.
- **Kenar karartmaları:** `_SideDarken` 0.3 ve `_BottomDarken` 0.16 da kenarlarda açıklığı düşürüyor. Bu yüzden doğrulama yakalanmış pikseller üzerinde yapılmalı.

**Post-processing kapalı kalmalı.** Bir gün açılırsa:

- **Tonemapping None olmalı.** İki tonemapper da paleti bozuyor:
  - **Neutral:** 1.0'daki her kanalı #D0'a indiriyor; #FFFFFF #D0D0D0 oluyor.
  - **ACES:** Tonu büküyor (#00FF00 → #5AD030) ve kontrast ekliyor (#808080 → #696969) ([URP Tonemapping](https://docs.unity3d.com/6000.2/Documentation/Manual/urp/post-processing-tonemapping.html); [Color.hlsl](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl)).
- **Bloom:** Varsayılan eşik gamma uzayında 0.9. Parlak sarı ve turuncu sıvılar parlayıp tonlarını komşu tüplere taşır; eşik 1'in üstüne çekilmeli ([URP Bloom](https://docs.unity3d.com/6000.2/Documentation/Manual/urp/post-processing-bloom.html)).
- **Vignette:** Oyun alanının üstüne konmamalı, çünkü aynı kategoriyi merkezde ve kenarda farklı gösterir.
- **2D ışıklar:** Renkli bir Global Light her kategoriyi aynı tonla çarpar. Palet açısından kritik shader'lar unlit kalmalı ([URP 2D lights](https://docs.unity3d.com/6000.2/Documentation/Manual/urp/2DLightProperties.html)).

**Palet hattı:**

1. **Tasarım:** OKLCH'de yapılır.
2. **Gamut eşleme:** CSS Color 4 yöntemiyle, yani kroma üzerinde ikili arama ve 0.02 JND ile yapılır. Araç olarak color.js `toGamut()` kullanılabilir ([Color.js](https://colorjs.io/docs/gamut-mapping)). Kanal kanal kırpma kullanılmamalı, çünkü ton kayması yaratır ([Ottosson](https://bottosson.github.io/posts/gamutclipping/)).
3. **Saklama:** Sonuç sRGB hex olarak `WaterColorPalette.asset`'te saklanır.

**Doğrulama dört adımda yapılmalı:**

1. **Referans sahne:** Unlit kareler, post kapalı. Her renk hedef hex'in ±1/255 içinde olmalı.
2. **Üretim sahnesi:** Cam arkasındaki sıvılar yakalanır ve kategori başına ΔEOK ölçülür. 0.02–0.04'ü aşan sapmalar işaretlenir; kategoriler arası en küçük mesafenin korunduğu kontrol edilir.
3. **Renk körlüğü simülasyonu:** Yakalanan görüntüler Viénot/Brettel tabanlı araçlardan geçirilir (Color Oracle, DaltonLens). Coblis V1 asla kullanılmamalı ([DaltonLens](https://daltonlens.org/opensource-cvd-simulation/)). Bu raporun tritan sayıları Machado ile hesaplandığı için özellikle tritan, Brettel ile yeniden kontrol edilmeli.
4. **Cihaz:** Hem Samsung "Natural" hem "Vivid" modunda bakılmalı. DisplayMate, Natural modun sRGB'ye doğru oturduğunu ölçmüş ([DisplayMate](https://www.displaymate.com/Gamut_47S.html)). Vivid modda ise palet olduğundan doygun görünecek. IEC şikâyeti nedeniyle en düşük parlaklıkta ve gece modu açıkken de test edilmeli. GAG'in önerdiği gibi birkaç gerçek renk körü oyuncuyla da denenmeli.

## Sonuç

Bu türde renk sorunu aslında bir **açıklık bütçesi** sorunu.

- **Bütçe neden dar:** Koyu zemin L 0.45'in altını yiyor. Beyaz, sarı ve limon tepeye sıkışıyor. On iki sıvı kabaca yarım birimlik bir açıklık aralığını paylaşmak zorunda.
- **Doğrudan sonuçları:** Yalnız renkle tanımanın tavanı 9 civarında kalıyor. Sıvı olmayan her öğe sıvıların kapladığı uzayın dışına itilmeli:
  - Zemin aşağıya ve kromasızlığa.
  - Cam ve kıvılcımlar ince ve beyaza yakın tarafa.
  - Seçim ve mantar renge değil şekle ve harekete.
  - Tek altın vurgu tahtanın dışına.
- **"Canlılık" takası:** Paleti renk körlüğüne dayanıklı yapmak için verilen kroma, çoğunlukla renk körü oyuncunun zaten göremediği kırmızı–yeşil ekseninden geliyor. Normal görüşte en yakın çift hâlâ hedefin üstünde kalıyor.
- **Bedavaya gelen kazanç:** Gizli sıvı rengi, seçim çizgisi ve shader doygunluğu gibi bugünkü kusurlar küçük parametre değişiklikleri. Bunlar paleti değiştirmeden bile okunurluğu artırır.

**Stratejik olarak durum net.** Türün en çok kazanan oyunları hiçbir erişilebilirlik özelliği beyan etmiyor. Oyuncular ise yıllardır yeşil–açık mavi gibi benzer açıklıktaki komşu tonlardan şikâyet ediyor. Dayanıklı bir varsayılan palet ve 10. renkle birlikte devreye giren bir sembol modu, düşük maliyetli ve savunulabilir bir fark yaratır.

**Belirsizlikler açıkça not edilmeli:**

- Önerilen palet ve giriş sırası hesaplanmış, oyuncuyla test edilmemiş.
- Tritan sonuçları doğruluğu tartışmalı bir modele dayanıyor.
- Rakiplerin gerçek hex değerleri örneklenemedi.
- Zemin gradyanı, cam opaklığı ve efekt oranları yayımlanmış oyun standartlarından değil, bu raporun sentezinden geliyor.

Bu değerler kesin hüküm olarak değil, cihazda ölçülerek ayarlanacak başlangıç noktaları olarak kullanılmalı.
