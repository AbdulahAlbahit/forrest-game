# Forrest Game — Proje Hafızası

> Bu dosya projenin kalıcı teknik hafızasıdır. Son güncelleme: 23 Temmuz 2026.
> Yeni bir işe başlamadan önce tüm depoyu yeniden taramak yerine önce bu dosya ve `TODO.md` okunur.
> Mevsim kıyafetleri: Efe karakterinde CC0 lisanslı Quaternius Ultimate Modular Men paketinin resmi Humanoid Rig modelleri kullanılır: İlkbahar `Farmer`, Yaz `Casual2`, Sonbahar `Adventurer`, Kış `Swat`. Her kıyafetin kendi geçerli Humanoid Avatar/Animator'ı vardır ve `EfeKiyafetAnimatorEsleyici` ana `Ch36` Animator'ının state, süre, parametre ve katman ağırlıklarını aktif kıyafete eşler. Karakter kimliği dört mevsimde de sabittir: sakalsız, açık tenli `Casual2_Head` baş mesh'i ile aynı göz/kaş/saç/ten malzemeleri her kıyafetin kendi iskeletine bağlanır; yalnızca kıyafet değişir. Eski `Ch36` renderer silinmeden kapalı tutulur; ana Animator Controller, hareket kodları, balta ve sepet özgün kemiklerinde korunur. Inspector'daki başlangıç mevsimi Play dışında anında önizlenir; ekip mevsim sistemi `EfeMevsimKiyafetleri.MevsimiUygula(int|string)` üzerinden bağlanabilir.
> Son karakter güncellemesi: Balta, `Hiz` ve locomotion animasyon döngüsüyle senkronize salınır; yürüyüşte hafif, koşuda daha belirgindir ve saldırı sırasında vuruş animasyonuna bırakılır.
> Hareket senkronu: Yürüyüş `1,75 m/sn`, koşuya geçiş `2,4 m/sn` ve tam koşu `4,6 m/sn` değerindedir. Hızlanma `12`, joystick bırakıldığındaki durma sertliği `40`, keskin yön değişimi sertliği `45`tir. Karakter yaklaşık `0,1 sn` içinde net durur; karşı yöne ani dönüşte eski yatay hız hızla kırılarak yan kayma azaltılır.
> Kol animasyonu düzeltmesi: `Upper Body Attack` katmanı locomotion sırasında ağırlık `0`, yalnızca saldırı süresince `1` olur. Böylece Walk/Run kliplerinin kol salınımı ve ele bağlı baltanın doğal hareketi korunur.
> Saldırı alt gövde koruması: `Efe_UpperBody` maskesinde Root, baş, iki bacak ve iki Foot IK kanalı kapalı; yalnızca gövde, kollar ve parmaklar açıktır. Eski prosedürel ayak taşıma IK’sı kaldırıldı; saldırıda alt gövde tamamen Base Layer Idle/Walk/Run pozunu korur.
> Dengeli saldırı duruşu: Üst gövde maskesi korunurken Base Layer ayak IK’sı yalnızca saldırı boyunca devreye girer; sağ ayak `0,17 m` öne, sol ayak `0,11 m` geriye ve iki ayak `0,11 m` yana açılarak Terrain zemine basar. Böylece düz paralel duruş kırılırken bacak animasyonu saldırı klibinden etkilenmez.
> Odun taşıma prototipi: Yalnızca `Efe_CharacterSandbox` içinde odunların yerde kalma süresi `60 sn` oldu. `EfeOdunTasima` yakındaki `Pool_WoodLog_*` odunlarını otomatik toplar, en fazla `6` odunu sepette gösterir ve `Zone_Truck_Wood_Loading` alanında mevcut `TruckRouteController.AddWoodToCargo` API’sine aktarır.
> Sepet görünürlüğü: Taşınan odunlar sepet duvarlarının içinde kalmaması için `LogStackAnchor` üzerinde ikili yatay katlar olarak yerleştirilir; ölçek `0,74` ve kapasiteye göre görünürlük 1–6 arasında açılır.
> Odun toplama geri bildirimi: Toplanan havuz odunu güvenle kapatılır; karşılık gelen sepet görseli yerden başlayıp `0,45 sn` içinde `1,3 m` yüksekliğinde kavis ve hafif dönüşle hareketli `LogStackAnchor` yuvasına oturur. Kapasite ve kamyona aktarım davranışı değişmez.
> Kamyon yükleme geri bildirimi: Yükleme alanına girince sepetteki odunlar `0,5 sn` kavisli uçuşla tek tek kamyon kasasına gider; her odun vardığında mevcut `TruckRouteController.AddWoodToCargo(1)` API’si çağrılır. Kamyon prefabı değiştirilmeden runtime'da oluşturulan kasa odunları `currentLoadedWood / requiredWoodToDepart` oranına göre kademeli görünür ve araçla birlikte hareket eder.
> Kamyon kasa yerleşimi: Gerçek `truck.fbx` mesh ölçümünde geniş kasa yüzeyi yaklaşık `Y=0,60`, ölçeklenmiş odun kalınlığı `0,09` bulundu; runtime odun kökü yükseklik oranı `0,42` yapılarak merkezler yaklaşık `Y=0,63` seviyesine indirildi. İkinci kat aralığı `0,10` olduğundan yük kasa duvarları içinde sıkı istiflenir.
> Kamyon kasa dolum sırası: Oyuncunun 6 odunluk taşıma kapasitesinde ilk slotların en arka sıraya düşmesi engellendi. Satırlar artık merkezden dışarı `2, 1, 3, 0, 4` sırasıyla dolar; ilk 3 odun kasa merkezine, sonraki 3 odun merkezin hemen arkasına yerleşir.
> Kamyon bounds güvenliği: Kasa merkezinin araçtan uzakta hesaplanmasının kök nedeni, `VFX_Duman` üzerindeki `ParticleSystemRenderer` bounds’unun araç ölçümüne katılmasıydı. `KamyonYerelSinirlariniHesapla` artık yalnızca `MeshRenderer` ve `SkinnedMeshRenderer` gövde parçalarını kapsar; particle/trail/line efektleri kasa hesabını etkileyemez.

## 1. Oyun vizyonu

İzometrik kamera kullanan, mobil öncelikli 3D hyper-casual/idle işleme oyunu:

1. Oyuncu evden çıkar.
2. Ormana gider ve ağacı baltayla keser.
3. Ortaya çıkan kütükleri sırtında taşır.
4. Kütükleri kamyonun yük alanına bırakır.
5. Kamyon dolunca atölyeye gider.
6. Atölye odunu işler ve satılabilir ürüne/oyun parasına dönüştürür.
7. Oyuncu; taşıma kapasitesi, balta, hareket, kamyon ve atölyeyi geliştirir.
8. Daha hızlı üretim ve yeni alanlarla döngü tekrar eder.

Temel hedef, önce tek bir ağacın kesilmesinden ilk gelirin alınmasına kadar eksiksiz ve anlaşılır bir dikey dilim oluşturmaktır. İçerik ve seviye sayısı bundan sonra artırılır.

## 2. Çalışma sınırı ve ekip kuralı

- Ekip 4 kişidir.
- Codex yalnızca kullanıcının görev dağılımında açıkça kendisine ait olduğu belirtilen alanlarda kod veya asset değişikliği yapar.
- Başka ekip üyesinin alanında hata görülürse doğrudan değiştirilmez; `TODO.md` içinde sahibi belirtilerek raporlanır.
- Kullanıcının rolü ve kesin sorumlulukları henüz paylaşılmadı. Bu bilgi gelene kadar uygulama kodunda yeni özellik geliştirilmeyecek.
- Ekip görev dağılımı ve teknik doküman geldiğinde bu bölüm; sahiplik tablosu, klasör sınırları ve entegrasyon sözleşmeleriyle güncellenecek.

### Geçici sahiplik tablosu

| Alan | Mevcut işaret | Kesin sahip |
|---|---|---|
| Karakter, kamera, mobil joystick | `karakter-kontrol`, `Efe_Scene`, Efe adlı scriptler | Belirlenecek |
| Çevre, bina, atölye görselleri, kamyon rota/görseli | `feature/mert-art-design` ve Mert commitleri | Belirlenecek |
| Firebase, veri, ekonomi, upgrade | `backend-veri-modelleri` | Belirlenecek |
| UI/UX ve animasyon | README'de rol olarak geçiyor | Belirlenecek |

## 3. Teknik temel

| Konu | Durum |
|---|---|
| Motor | Unity 6000.5.2f1 (Unity 6.5) |
| Render | Universal Render Pipeline 17.5.0; PC ve Mobile URP assetleri var |
| Input | Unity Input System 1.19.0 kurulu; mevcut karakter kodu özel `VirtualJoystick` kullanıyor |
| Navigasyon | AI Navigation 2.0.13 kurulu |
| UI | Unity UI 2.5.0 |
| Hedef | README ve kontroller mobil önceliğe işaret ediyor; kesin platform dokümanla netleşecek |
| Backend | Firebase çalışması ayrı branch'te; mevcut branch'e entegre değil |
| Test | Unity Test Framework kurulu, proje testleri yok |
| Kaynak kontrol | GitHub; özellik branch'i -> `develop` -> PR akışı hedeflenmiş |

## 4. Mevcut depo durumu

### Git özeti

- Aktif branch: `karakter-kontrol`; güncel `develop` entegrasyon merge commit'i `449c84e`.
- `origin/develop`: `7a08e2f`; Mert'in düzeltilmiş art/kamyon branch'i bu commit ile birleştirildi.
- Yerel branch'in eski ve yanlış `origin/main` upstream ayarı entegrasyon sonunda `origin/karakter-kontrol` olarak düzeltildi.
- `origin/main`: `9378949`; geliştirmelerin gerisinde.
- `origin/feature/mert-art-design`: `7823ca5`; `develop` sonrasında kamyonet, rota, teker ve VFX işi içeriyor.
- `origin/backend-veri-modelleri`: `82b23a4`; Firebase, veri modeli, ekonomi ve upgrade çalışmaları içeriyor.
- Branch'ler henüz tek bir entegrasyon branch'inde birleşmiş değil.
- Remote URL içinde düz metin GitHub anahtarı tespit edildi ve URL kimlik bilgisiz hale getirildi. Açığa çıkan anahtar GitHub üzerinden iptal edilmelidir.

### Kaydedilmemiş yerel değişiklikler

İnceleme anında kullanıcıya ait değişiklikler vardı ve korunmuştur:

- `Assets/Art/Environments/mat_water.mat`
- `Assets/Art/Trees/Materials/leafsGreen 1.mat`
- `Assets/Art/Trees/Materials/leafsGreen 2.mat`
- `ProjectSettings/GraphicsSettings.asset` (`m_LightsUseLinearIntensity: 1 -> 0`)
- Takipte kalmış bazı `UserSettings` dosyaları
- Takip edilmeyen `ProjectSettings/ProjectSettings.asset`

Bu dosyalar temizlenmeden branch değiştirmek veya merge/rebase yapmak risklidir.

### Sahne ve içerik özeti

| Dosya | Gerçek durum |
|---|---|
| `Assets/Scenes/Efe_Scene.unity` | Terrain, nehir, çit, joystick, kamera ve karakter bağlantıları var. Karakter/kamera prototip sahnesi. |
| `Assets/Scenes/Efe_CharacterSandbox.unity` | Mert'in dünya sahnesinin test kopyası ile Efe'nin karakter, takip kamerası, Canvas/joystick ve EventSystem nesnelerini birleştiren kişisel geliştirme sahnesi. Harita öğeleri bu sahnede değiştirilmez. |
| `Assets/Scenes/Mert_Artscene.unity` | Terrain, ev, duvar/zemin, çevre yerleşimi ve baked lighting verileri var. |
| `Assets/Scenes/SampleScene.unity` | Boş URP örnek sahnesi; şu an Build Settings'teki tek sahne. |

Önemli: Asıl oyun sahneleri Build Settings'e ekli değil. Mevcut build açılırsa yalnızca `SampleScene` yüklenir.

### Mevcut kod

- `EfeOynanisEventleri.cs`: Ses/VFX entegrasyonu için Efe sistemlerine ait merkezi C# event sözleşmesidir. Joystick basma/bırakma; hareket/koşu başlama-bitiş; balta sallama/ağaca temas; odun toplama, sepet dolma/boşalma, kamyon yükleme başlama-bitiş ve tekil odun yükleme; suya girme/çıkma/kurtarılma olaylarını yayınlar. Olaylar kaynak `Transform` ve gerektiğinde mevcut/azami miktar bilgisini taşır. Her dinleyici ayrı hata izolasyonuyla çağrılır; ses kodundaki bir exception oynanış akışını veya diğer dinleyicileri durdurmaz. Dinleyiciler `OnEnable` içinde abone olup `OnDisable` içinde abonelikten çıkmalıdır.
- `EfeKarakterKontrol.cs`: joystick şiddetini dead-zone sonrasında yürüyüş/koşu hızlarına kademeli dönüştürür; yumuşak hızlanma, dönüş, kararlı yerçekimi ve normalize `Hiz` Animator parametresi üretir. `water_river` tetikleyicisindeyken karakteri göğüs seviyesine kadar suya batırır; suda kesintisiz 5 saniye kalırsa başlangıç pozisyonuna geri taşır.
- `EfeBaltaKontrol.cs`: UI butonundan gelen saldırı isteğini cooldown ile sınırlar ve Animator `Saldir` trigger'ını çalıştırır; henüz hasar üretmez.
- `KameraTakip.cs`: başlangıç ofsetini koruyan yumuşak kamera takibi.
- `VirtualJoystick.cs`: pointer/drag tabanlı joystick.
- `PlayerMovement.cs`: boş Unity şablonu; kullanılmıyor.
- Kamyon rota kodu ve kamyon prefabları yalnızca `origin/feature/mert-art-design` branch'inde.
- Firebase/Auth/CloudSave/Economy/Upgrade kodları yalnızca `origin/backend-veri-modelleri` branch'inde.

`Assembly-CSharp.csproj` derlemesi 0 hata ile tamamlandı. Unity tarafından üretilen proje hedefi ile bazı editor bağımlılıkları arasında .NET Framework 4.7.1/4.7.2 kaynaklı 7 uyarı var. Bu sonuç Unity Editor Play Mode ve cihaz build testi yerine geçmez.

## 5. Oynanış sistemlerinin hazır olma durumu

| Sistem | Durum | Not |
|---|---|---|
| Karakter hareketi | Prototip | Mobil joystick çalışması var; dayanıklılık, zemin eğimi ve kamera göreli hareket yok. |
| Kamera | Prototip | Sabit açılı yumuşak takip var. |
| Animasyon | Kısmi | Idle/Walk/Run, `Hiz` kontrollü 1D Blend Tree ile bağlı ve loop ayarlı; Axe Attack animasyonu UI butonuna bağlı, hasar anı henüz bağlı değil. |
| Ağaç kesme | Prototip | Balta butonu 3 metre içindeki en yakın Terrain ağacını seçer; karakter ağaca döner ve animasyon temas anında `TreeManager` hasar alır. Ağaç 3 vuruşta devrilir, güdük/odun görselleri çıkar ve 10 saniye sonra yeniden doğar. |
| Balta | Etkileşim prototipi | Balta sağ ele bağlı; UI butonu Axe Attack animasyonunu ve yakın Terrain ağacına 1 hasarı tetikliyor. Vuruş menzili 3 m, temas gecikmesi 0,45 sn. |
| Kütük üretme | Prototip | `TreeManager`, kesilen ağaçtan havuzlanmış üç `Pool_WoodLog_*` odunu çıkarır; Efe sandbox içinde yerde kalma süresi 60 saniyedir. |
| Sırtta taşıma | Prototip | `EfeOdunTasima`, 1,15 m içindeki odunları otomatik toplar; kapasite 4’tür ve `LogStackAnchor` üzerinde dört görsel odunla gösterilir. |
| Kamyona yükleme | Entegrasyon prototipi | Efe karakteri `Zone_Truck_Wood_Loading` alanına girince sepetindeki odunları mevcut kamyonun açık kargo API’sine aktarır; Mert’in zone/truck kodu değiştirilmemiştir. |
| Kamyon yolculuğu | Ayrı branch'te prototip | Waypoint rota ve kamyon seviyeleri mevcut; ana döngüye bağlı değil. |
| Atölye | Görsel temel var | Girdi kuyruğu, işleme süresi ve çıktı yok. |
| Ekonomi/upgrade | Ayrı branch'te | Backend branch'inde yöneticiler var; oynanışla entegre değil. |
| Kayıt/Auth | Ayrı branch'te | Firebase kurulumu ve cloud save var; güvenlik/yapılandırma incelemesi gerekli. |
| UI/geri bildirim | Yok/Kısmi | Joystick dışında kapasite, para, ilerleme, yönlendirme yok. |
| Ses/VFX | Çok sınırlı | Kamyon egzoz VFX ayrı branch'te; kesme/taşıma/işleme feedback'i yok. |
| Test/build | Yok | Otomatik test, cihaz build doğrulaması ve performans bütçesi yok. |

## 6. Önerilen mimari ve entegrasyon sözleşmeleri

Sistemler birbirinin somut sınıflarına bağlanmak yerine küçük olaylar ve açık veri sözleşmeleriyle haberleşmeli:

```text
PlayerInteraction
  -> TreeHarvestable.TakeHit(toolPower)
  -> ResourcePickup/WoodUnit
  -> PlayerCarryInventory.TryAdd(resource)
  -> TruckCargo.TryDeposit(resource, amount)
  -> TruckDispatch (capacity reached / player command)
  -> WorkshopProcessor.Enqueue(wood)
  -> Economy.AddCurrency(processedValue)
  -> UpgradeService applies configuration values
```

Önerilen klasörleme:

```text
Assets/_Game/
  Art/
  Audio/
  Data/               ScriptableObject ayarları
  Prefabs/
  Scenes/
  Scripts/
    Character/
    Interaction/
    Harvesting/
    Carrying/
    Vehicles/
    Workshop/
    Economy/
    UI/
  Tests/
    EditMode/
    PlayMode/
```

Klasör taşıma işlemi şu an yapılmamalı; aktif branch'lerde zaten büyük asset taşıma/silme farkları var. Önce branch entegrasyonu ve sahiplik netleşmeli.

## 7. MVP kabul ölçütü

İlk dikey dilim şu koşulların tamamını sağlamalı:

- Oyuncu build açıldığında evin yanında doğar ve joystick ile hareket eder.
- Bir ağaca yaklaşınca anlaşılır bir etkileşim geri bildirimi alır.
- Balta vuruşu animasyonla senkron hasar verir; ağaç belirli sayıda vuruşta kesilir.
- Kütük oluşur, oyuncu kapasitesi kadar kütüğü sırtında görsel olarak taşır.
- Kamyon yük alanına girince kütükler kontrollü biçimde aktarılır ve sayaç güncellenir.
- Kamyon dolunca atölyeye gider; rota sonunda yükü teslim eder ve geri dönebilir.
- Atölye odunu işler; oyuncuya para kazandırır.
- En az bir upgrade satın alınabilir ve etkisi oyun içinde gözlenir.
- Oyun kapanıp açıldığında kritik ilerleme korunur (MVP'de önce local save; Firebase gereksinimi dokümana göre netleşir).
- Android hedef cihazda kritik hata olmadan çalışır ve belirlenen FPS/bellek bütçesini karşılar.

## 8. Çalışma yöntemi

1. Her oturumda önce `PROJECT.md` ve `TODO.md` okunur.
2. Kullanıcının görev sınırı kontrol edilir.
3. Başlamadan önce ilgili branch güncellenir; kirli çalışma ağacı korunur.
4. Her özellik küçük, tek amaçlı commitlere ayrılır.
5. Kod değişikliğinde mümkünse EditMode/PlayMode testi eklenir.
6. Unity sahne/prefab değişiklikleri ayrı commit tutulur; aynı sahnede eşzamanlı çalışma yapılmaz.
7. Bitirilen iş `TODO.md` içinde işaretlenir; bu dosyanın mevcut durum bölümü güncellenir.
8. PR hedefi `develop` olur; `main` yalnızca kararlı sürüm için kullanılır.

## 9. Bilinen riskler

- Aktif branch'lerde yüzlerce asset silme/taşıma farkı var; plansız merge içerik kaybı yaratabilir.
- Aynı `Mert_Artscene.unity` birden fazla branch'te değişiyor; Unity YAML merge çatışması olası.
- `Assets/Prefabs` ve `Assets/Prefabs 1` kopya yapısı mevcut; GUID ve referans kaybı riski var.
- `UserSettings` ignore edilmiş olsa da geçmişten takip edilen dosyalar değişiklik ve conflict üretiyor.
- Build Settings yanlış/eksik.
- Ana oynanış sistemleri henüz yok; görsel ilerleme, gerçek oynanabilir ilerlemeden daha önde.
- Firebase yapılandırma dosyalarının repoda tutulması, ortam ayrımı ve yetki kuralları teknik dokümana göre gözden geçirilmeli.
- Otomatik test ve CI yok.

### Mert branch entegrasyonu — 2026-07-16

`origin/feature/mert-art-design` (`7823ca5`) branch'i `origin/develop` (`cf24b75`) üzerine temiz bir geçici worktree'de merge provasıyla kontrol edildi.

- Git metin merge'i conflict üretmedi.
- Unity GUID taramasında merge sonrasında yeni bir kayıp asset GUID'si bulundu: `c3ed598915d129d488949ff08cbe6ef0`.
- `Mert_Artscene.unity` içinde bu GUID'ye 331 referans var. Referanslar silinen `Assets/Prefabs 1/Fence_group_6x.prefab` dosyasına ait.
- Korunan eş prefab `Assets/Prefabs/Fence_group_6x.prefab` farklı GUID (`de747b78d7287af49b21bf9dc389a7cf`) taşıyor. İki prefab içeriği karşılaştırıldığında aynıdır; dolayısıyla sahne referanslarının korunan GUID'ye geçirilmesi güvenli düzeltme adayıdır.
- Branch ayrıca kişisel `UserSettings/Layouts/default-6000.dwlt` değişikliğini içeriyor; bu dosya özellik commit'inden çıkarılmalıdır.
- Commit açıklaması 10 kamyon seviyesi dese de branch'te `truckLv1`–`truckLv5` olmak üzere 5 kamyon prefabı bulunuyor; beklenti Mert tarafından netleştirilmelidir.
- Mert düzeltmeyi `39698d2` commit'iyle kendi branch'ine pushladı. Eski kırık GUID sahneden tamamen kaldırıldı ve çitler korunan prefab GUID'sine bağlandı.
- Tekrar yapılan merge provasında yeni kayıp GUID ve duplicate GUID oluşmadı.
- Unity 6000.5.2f1 temiz import testinde 4.076 asset içe aktarıldı; C# derlemesi tamamlandı ve batchmode 0 hata koduyla kapandı.
- Test edilmiş sonuç `7a08e2f` ile `develop` branch'ine merge edilip GitHub'a pushlandı.
- Güncel `develop`, `449c84e` ile `karakter-kontrol` branch'ine merge edildi. Kullanıcının yerel material, graphics, proje dokümanı ve Unity kişisel ayar değişiklikleri yedeklenip geri yüklendi.
- Kopya prefab temizliği sonrasında `Efe_Scene.unity` içinde kalan 65 eski çit GUID referansı, aynı içeriğe sahip korunan `Assets/Prefabs/Fence_group_6x.prefab` GUID'sine geçirildi.
- `UserSettings` artık Git takibinde değildir; dosyalar yerelde korunur ve ignore edilir.

## 10. Güncelleme günlüğü

- 2026-07-23: `Efe_CharacterSandbox` başlangıcında evin içinde bulunan Efe karakteri önce `(95.447, 20.070, 152.009)` noktasına çıkarıldı; bu noktanın evin arka tarafında kaldığı görülünce araç tarafındaki gerçek ön alana, `(91.559, 20.070, 138.198)` konumuna taşındı. Yeni nokta ev renderer sınırından `2,155 m`, araç kökünden `3,872 m` uzakta; Terrain yüksekliği `19,999`, ayak/zemin payı `0,071 m` ve kapsül çarpışma kontrolü temizdir. Karakter araç yönüne bakar. `EfeKarakterKontrol` Play başlangıcında bu transformu kaydettiği için su kurtarma dönüş noktası da otomatik olarak yeni ev önü konumu oldu. Kamera, hareket, balta, sepet, kıyafet ve ekip sahneleri değiştirilmedi; önceki sürümler `C:\Users\90505\forrest-game-spawn-backup-20260723-102933` ve `C:\Users\90505\forrest-game-front-spawn-backup-20260723-104809` altında korunuyor.
- 2026-07-23: Karakter evin araç tarafına taşındıktan sonra `KameraTakip` bileşeninin yanlış uzak dünya ofseti kaydetmemesi için `Efe_CharacterSandbox/Main Camera`, karakterle aynı taşıma farkı `(-3.888, +0.000, -13.811)` kadar kaydırıldı. Kamera konumu `(84.115, 24.500, 131.098)` oldu; mevcut arka-üst bakış rotasyonu, karakter hedefi ve takip yumuşaklığı aynen korundu. Yalnızca Efe sahnesindeki kamera başlangıç konumu değişti; yedek `C:\Users\90505\forrest-game-camera-backup-20260723-105656` altında tutuluyor.
- 2026-07-23: Ev önü kamera takibi doğrulandıktan sonra arka-üst bakış ofseti aynı doğrultuda `%18` küçültülerek kamera karaktere biraz yaklaştırıldı. `Main Camera` konumu `(85.455, 23.702, 132.376)` oldu; rotasyon, hedef, yumuşaklık ve tüm oynanış sistemleri değişmedi. Önceki kamera mesafesi `C:\Users\90505\forrest-game-camera-distance-backup-20260723-110424` altında korunuyor.
- 2026-07-23: Atölyedeki otomatik paket üretimini ilerideki ürün seçim UI'ına hazırlamak için geriye uyumlu seçim kapısı kuruldu. `TruckRouteController`, atölyeye varınca `OnWorkshopSelectionRequested` event'ini dinleyen bir sistem kamyonu devralırsa `isWorkshopProcessDone` tamamlanana kadar bekler; dinleyici yoksa Mert'in mevcut `WorkshopProductionManager.StartProductionCycle` akışı aynen çalışır. Efe'ye ait `EfeWorkshopSelectionController`, yalnızca `Efe_CharacterSandbox/Efe_WorkshopSelection` üzerinde Mert'in görsel üretimine bağlandı; şimdilik Game penceresi odaktayken `1` mevcut `sandalye` tarifini (`10 odun/adet`), `2` Efe test `koltuk` tarifini (`15 odun/adet`) seçer. 30 odunluk Play Mode doğrulamasında event kapalı eski otomatik üretim, `1 -> 3 sandalye` ve `2 -> 2 koltuk` döngülerinin üçü de paketi yükleyip kamyonu serbest bıraktı. Seçim sistemi Firebase/`WorkshopManager` kullanmaz; gerçek UI ve Abdullah'ın kalıcı ham odun/`TryStartProduction` bağlantısı sonraki entegrasyondur. `WorkshopProductionManager.cs`, `Mert_Artscene`, karakter/kamera/odun sistemleri değiştirilmedi; yedek `C:\Users\90505\forrest-game-workshop-selection-backup-20260723-114246` altında tutuluyor.
- 2026-07-23: `Efe_CharacterSandbox` baltasının ilk avuç ayarının görünmeyen kaynak `Ch36` iskeletine uygulandığı, ekrandaki mevsimlik karakterlerin ise ayrı Humanoid iskeletler kullandığı tespit edildi. `Efe_AxeInHand` artık Efe'ye ait `EfeBaltaGorunurElBaglayici` ile editörde ve Play Mode'da aktif görünen kıyafetin gerçek `RightHand/Hand.R` kemiğine bağlanır; mevsim değişince otomatik olarak yeni görünür ele geçer. Görünür kıyafet elinin iskelet ölçeği kaynak elden yaklaşık 100 kat büyük olduğu için ilk aktarımda oluşan dev balta giderildi: bağlayıcı artık yerel ölçeği kopyalamak yerine eski baltanın gerçek dünya boyutunu korur ve hedef ele göre yerel ölçeği otomatik hesaplar (`0,82` kaynak karşılığı görünür elde yaklaşık `0,008106`). Sapın dipten `0,08 m` ilerideki tutuş noktası parmak köklerinden hesaplanan avuç merkezine oturtulur. İlkbahar, yaz, sonbahar, kış ve saldırı anını kapsayan Play Mode testinde balta dünya ölçeği her modelde `0,492`, görünür model uzunluğu yaklaşık `0,38–0,46 m`, en büyük tutuş sapması `0,000075 m` ve doğru el ebeveyni olarak doğrulandı. Dünya yönü ile saldırı/hasar kodları korundu; yalnızca Efe sahnesi ile Efe bağlayıcı scripti değişti. Mert sahnesi ve ortak saldırı kodu değiştirilmedi. İşlem öncesi yedek `C:\Users\90505\forrest-game-axe-grip-backup-20260723-134209` altında korunuyor.
- 2026-07-23: Görünür ele geçişten sonra yürüyüş/koşuda baltanın avuçtan ayrılmasının, `EfeBaltaKontrol` içindeki eski kaynak iskelet ölçeğine göre hazırlanmış ek yerel salınım ofsetlerinin yaklaşık 100 kat büyük görünür el iskeletinde uygulanmasından kaynaklandığı doğrulandı. Ortak saldırı/hareket kodu değiştirilmeden `EfeBaltaGorunurElBaglayici` yürüyüş, koşu ve saldırı hesaplarından sonra çalışacak şekilde düzenlendi; her `LateUpdate` sonunda balta aktif görünür avuç pozuna geri sabitlenir. Kol ve el animasyonu baltayı doğal olarak taşımaya devam ederken yapay havada sallanma engellendi. Otomatik Play Mode testinde 1,4 sn yürüyüş, 1,4 sn koşu ve 1,1 sn koşarken saldırı boyunca her kare ölçüldü; üç durumda da en büyük avuç sapması `0,000000 m` ve doğru el ebeveyni doğrulandı. `EfeBaltaKontrol`, karakter hareketi, saldırı/hasar, Mert sahnesi ve ekip dosyaları değiştirilmedi.
- 2026-07-23: Ekip çalışmalarının birleştiği güncel `origin/develop` (`f44df83`), `karakter-kontrol` dalına çakışmasız fast-forward ile alındı. Mevsim/kar VFX ve shader değişiklikleri, Yasir'in ses sistemi ve varlıkları, Mert'in güncel sahnesi ile backend/atölye/dükkan geliştirmeleri dahil 32 commit yerelde bulunuyor. İşlem öncesindeki 11 yerel dosya `C:\Users\90505\forrest-game-local-backup-20260723-094723` ve `safety-before-develop-sync-2026-07-23-094723` stash'iyle korundu. Metin tabanlı iki `leafsGreen` materyali develop shader ayarları ile yerel renk hassasiyetini otomatik birleştirdi; ikili `Assets/New Terrain.asset` için aktif projede güncel develop sürümü bırakıldı, önceki yerel sürüm dış yedek ve stash'te tutuldu. `mat_water.mat` yeni `Assets/Art/Environments/Materials/` yoluna yerel değişikliğiyle taşındı; diğer yerel dosyalar içerik/CRLF karşılaştırmasıyla korundu. Bu senkron henüz `origin/karakter-kontrol` dalına pushlanmadı.
- 2026-07-23: Güncel `Mert_Artscene`, `Efe_CharacterSandbox` içine güvenli grup klonlama yöntemiyle aktarıldı. Efe'ye ait `Main Camera`, `Ch36_nonPBR`, `Canvas` ve `EventSystem` kökleri aynı nesneler olarak korundu; Mert'in diğer 10 dünya kökü (`Directional Light`, `Terrain`, `truckLv1`, `TreeManager`, `Workshop_Production`, `LoadingRingCanvas`, `CameraShakeManager`, `Map`, `AxeEnvanterManager`, `GameManager`) güncel sürümleriyle değiştirildi. Mert'in yeni `VFX_Winter` kamera çocuğu ve `CameraCullingManager` bileşeni Efe kamerasına eklenirken mevcut `KameraTakip` ayarları korundu. Unity denetiminde iki sahne de 14 kök, `Map` 569 nesne, güncel `GameManager` 3 nesne, Efe karakteri 372 nesne/dört mevsim kıyafeti, tek kamera, tek EventSystem, bağlı `BaltaSalla` ve sıfır eksik script ile geçti. `Mert_Artscene` Git blobu `f20d83d0ad1b2efe5391f507bcec4f333471c96c` olarak değişmeden kaldı; önceki Efe sahnesi `C:\Users\90505\forrest-game-scene-backup-20260723-100306` altında korundu ve geçici senkron araçları kaldırıldı.
- 2026-07-23: Güncel develop importunda `Assets/Audio/Ambience.meta`, `Assets/Audio/SFX/Gameplay.meta` ve bunlarla yanlış conflict içeriği paylaşan iki Efe belge metasının Git conflict işaretleriyle commit edildiği tespit edildi. Yasir'in ses dosyalarına müdahale edilmedi; yalnızca referanssız Efe `How To Use.txt.meta` ve `License.txt.meta` dosyaları temiz `TextScriptImporter` yapısı ve benzersiz GUID'lerle düzeltildi. Ses klasörü metalarındaki upstream conflict işaretleri ekip sahibinin çözmesi gereken ayrı bir develop sorunu olarak bırakıldı.
- 2026-07-22: Mevsim değiştirildiğinde yüzün/sakalın da değişmesi giderildi. Sakalsız ve açık tenli Yaz `Casual2_Head` mesh'i ile ona ait 5 göz/kaş/saç/ten malzemesi İlkbahar, Yaz, Sonbahar ve Kış modellerinin kendi 49 baş kemiğine eşlendi. Böylece dört mevsimde aynı erkek karakter korunurken yalnızca kıyafet değişir. Animator, yürüyüş, koşu, balta, sepet, zemin ayarı ve mevsim API'si değiştirilmedi; dört sette ortak mesh/malzeme ve doğru kemik sayısı Unity üzerinden doğrulandı. Mert'in kaynak sahnesi SHA-256 karşılaştırmasında değişmeden kaldı.
- 2026-07-22: Mevsim kıyafetlerinin ilk modüler iskelet denemesinde oluşan görünmez karakter/dikey çubuk, yan açılan sol ayak ve kıyafetlerin görünmemesi sorunları kalıcı olarak giderildi. Paket sahibinin resmi `Humanoid Rig` FBX'leri kullanıldı: İlkbahar `Farmer`, Yaz `Casual2`, Sonbahar `Adventurer`, Kış `Swat`; her biri bağımsız geçerli Humanoid Avatar ve Animator ile ana `Ch36` hareket durumuna `EfeKiyafetAnimatorEsleyici` üzerinden eşlenir. Koşu klibi ve oynanış değerleri değiştirilmedi. Dört mevsim için Play Mode'da renderer görünürlüğü, yürüyüş state hash'i ve normalize zaman eşleşmesi doğrulandı; tüm setlerde ayak alt sınırı `Y=20,0985`, karakter kökü `Y=20,1654` ve `grounded=true` ölçüldü. Balta özgün `mixamorig1:RightHand`, sepet özgün `mixamorig1:Spine2` kemiğinde kaldı; Mert'in sahne SHA-256 değeri değişmedi.
- 2026-07-22: İlk mevsim kıyafeti bağlantısında ana `Ch36_nonPBR` Animator'ının Quaternius iskeletine geçirilmesi Play Mode'da karakteri görünmez, kıyafet iskeletini dikey çubuk biçiminde gösterdiği ve baltayı elden ayırdığı için düzeltildi. Ana karakter `Ch36_nonPBRAvatar` Avatar'ına geri döndürüldü; balta tam eski `mixamorig1:RightHand` yerel değerleriyle, sepet tam eski `mixamorig1:Spine2` yerel değerleriyle yeniden bağlandı. Kıyafet görseline bağımsız geçerli Humanoid Animator verildi ve `EfeKiyafetAnimatorEsleyici` ile ana Animator'ın parametre/state/katman ağırlıkları eşlendi. Gerçek Game Camera Play Mode görüntüsünde karakterin tekrar görünür olduğu ve dikey çubuğun kaybolduğu doğrulandı; Base Layer ve balta saldırı katmanlarında kaynak/hedef state hash ve ağırlıkları birebir eşleşti. Mert'in kaynak sahnesi değişmedi.
- 2026-07-22: Efe karakterinin dört mevsim kıyafetleri için yapılan ilk ortak-avatar/modüler parça bağlantısı denendi; daha sonra yukarıdaki resmi Humanoid Rig çözümüyle değiştirildi. Eski `Ch36` gövdesi silinmeden devre dışı bırakıldı ve ekip kaynaklarına dokunulmadı.
- 2026-07-22: PR #28'in `afe468e` ile, Mert'in mevsim/harita geliştirmelerini içeren PR #29'un `e1c0f7a` ile `develop` dalına merge edildiği doğrulandı. `karakter-kontrol` dalı çakışmasız fast-forward ile güncel `e1c0f7a` commitine getirildi; Mert'in `EfeBaltaKontrol` içine eklediği yaz/kış/mevsim balta hasarı entegrasyonu korunarak alındı. On yerel dosya işlem öncesinde `C:\Users\90505\forrest-game-local-backup-20260722-115150` ve `safety-before-develop-sync-2026-07-22-115150` stash'iyle korundu, ardından SHA-256 karşılaştırmasıyla 10/10 geri yüklendi.
- 2026-07-22: Güncel `Mert_Artscene`, Efe'nin `Main Camera`, `Ch36_nonPBR`, `Canvas` ve `EventSystem` kökleri aynen korunarak `Efe_CharacterSandbox` içine yeniden senkronlandı. Mert'in yeni `GameManager` kökü dahil güncel dünya kökleri alındı. Unity 6000.5.2f1 doğrulamasında 14 kök, tek kamera, tek EventSystem, kamera/joystick/`BaltaSalla` bağlantıları, Efe karakter bileşenleri, 42/42 `FadeableObstacle` bağlantısı ve sıfır eksik script doğrulandı. `Mert_Artscene` SHA-256 karşılaştırmasıyla değişmeden kaldı; önceki Efe sahnesi `C:\Users\90505\forrest-game-scene-backup-20260722-115430` altında korundu, 604 meta dosyasında duplicate GUID bulunmadı ve geçici senkron aracı kaldırıldı.
- 2026-07-22: Balta saldırısındaki yapay sağ/sol/üst varyasyonlar profesyonel görünmediği için yalnızca vuruş sunumu yenilendi. Adobe Mixamo'nun hazır animasyonu karakter üzerinde önizleme ve hız/arm-spacing ayarıyla kullanma yaklaşımı incelendi; projede zaten bulunan profesyonel `Standing Melee Attack Downward` klibinin kendi prosedürel gövde dönüşü, sağ-el IK'si ve ek balta dönüşleriyle bozulduğu belirlendi. Mert'in ortak FBX/meta dosyası değiştirilmeden `Assets/Efe/Animations/Efe_Ch36_AxeChop.fbx` Efe kopyası oluşturuldu ve `Ch36_nonPBRAvatar` ile import edildi. Controller yalnızca bu non-loop `Efe Axe Chop` klibine bağlandı; 2,2667 sn kaynak klip `2,40×` hızla yaklaşık `0,94 sn` oynar, giriş offset'i `0,08`, temas zamanı `0,42 sn`, cooldown `0,95 sn` ve çıkış zamanı normalize `0,85`tir. Yapay saldırı çeşitliliği/sağ-el IK kodları kaldırıldı; dengeli ayak IK'si, yürüyüş/koşu sırasında balta salınımı, hasar `1`, ağaç algılama ve event'ler korundu. Unity 6000.5.2f1 import/controller/C# doğrulaması hata kodu `0`, eksik script sayısı `0` ile tamamlandı; işlem öncesi yedek `C:\Users\90505\forrest-game-efe-backup-20260722-professional-axe-chop` altında bulunuyor.
- 2026-07-22: Koşunun kaynak FBX önizlemesinden farklı ve kayıyor görünmesi yeniden incelendi. Controller'ın ortak `Running` klibini yüzde 100 kullandığı doğrulandı; ancak ortak klip kendi ürettiği Avatar ile retarget ediliyordu. Mert'in ortak FBX/meta dosyası değiştirilmeden, aynı FBX'in `Assets/Efe/Animations/Efe_Ch36_Running.fbx` kopyası oluşturuldu ve yalnızca Efe kopyası karakterin `Ch36_nonPBRAvatar` Avatar'ını kullanacak şekilde import edildi. Kopya `Running` klibi 0,70 sn, loop açık ve doğal ileri hızı `4,23 m/sn` olarak doğrulandı; `PlayerAnimator.controller` içindeki yalnızca koşu motion'ı bu kopyaya bağlandı. Önceki tempo-parametresi yaklaşımı kaldırıldı ve koşu dünya hızı klibin doğal `4,23 m/sn` hızına sabitlendi; böylece koşu klibi 1× saf hızda oynarken ayak kayması azaltıldı. Walking klibi, joystick, balta katmanı ve ekip kaynakları değiştirilmedi. Unity 6000.5.2f1 import/controller/C# kontrolleri hata kodu `0` ile tamamlandı; işlem öncesi yedek `C:\Users\90505\forrest-game-efe-backup-20260722-run-foot-sync` altında korunuyor.
- 2026-07-22: Efe karakterinin hareket mesafesi, hızları, joystick eşikleri ve geçiş yumuşaklığı korunarak yalnızca yürüyüş/koşu görsel seçimi düzeltildi. `PlayerAnimator.controller` içinde Mert'in eklediği `Ch36_nonPBR@Walking` ve `Ch36_nonPBR@Running` kliplerinin zaten doğru bağlı olduğu doğrulandı; kötü görünümün `EfeKarakterKontrol` tarafından klipler arasında sürekli ara poz üretilmesinden kaynaklandığı belirlendi. Yürüme bölgesinde Blend Tree değeri doğrudan `0.5` (Walking), koşu bölgesinde doğrudan `1.0` (Running) olacak şekilde yalnızca Efe kodu değiştirildi; Idle `0` olarak kaldı. İşlem öncesi script/controller yedeği `C:\Users\90505\forrest-game-efe-backup-20260722-locomotion-clips` altında korunuyor.
- 2026-07-22: Ekip çalışmalarının birleştiği güncel `origin/develop` (`37efe3a`), `karakter-kontrol` dalına çakışmasız fast-forward ile alındı. Efe'nin `e471175` çalışmasının PR #26 ile, Mert'in son `987a8e2` harita/prefab düzeltmesinin PR #27 ile develop içinde bulunduğu doğrulandı. İşlem öncesindeki 11 yerel dosya `C:\Users\90505\forrest-game-local-backup-20260722-093550` ve `safety-before-develop-sync-2026-07-22-093550` stash'iyle korundu; güncel Terrain çalışma kopyasında bırakılırken diğer 10 dosya SHA-256 karşılaştırmasıyla 10/10 birebir geri yüklendi. Bu sabahki senkronizasyon henüz `origin/karakter-kontrol` dalına pushlanmadı.
- 2026-07-22: Güncel `Mert_Artscene`, yalnızca `Efe_CharacterSandbox` hedefi değiştirilerek yeniden senkronlandı. Efe'ye ait `Main Camera`, `Ch36_nonPBR`, `Canvas` ve `EventSystem` aynen korunurken Mert'in güncel harita kökleri ve yeni `AxeEnvanterManager` kökü alındı. Kaynak sahnedeki eski `AxeManager` script GUID'si yalnızca Efe sahne kopyasında mevcut `AxeVisualManager` bileşenine uyarlandı; `Mert_Artscene` SHA-256 karşılaştırmasıyla değişmeden kaldı. Unity 6000.5.2f1 doğrulamasında 13 kök, tek kamera, tek EventSystem, bağlı joystick ve `BaltaSalla`, Efe karakter bileşenleri, 42/42 `FadeableObstacle` bağlantısı ve sıfır eksik script doğrulandı. Önceki Efe sahnesi `C:\Users\90505\forrest-game-scene-backup-20260722-094430` altında korunuyor; geçici Editor aracı kaldırıldı ve 604 meta dosyasında duplicate GUID bulunmadı.
- 2026-07-21: Önceki balta çeşitlilik yaklaşımının ekranda görünmemesinin kök nedeni giderildi. Sağ el IK'si Base Layer'da uygulanırken sonraki `Upper Body Attack` katmanı kol pozunu tekrar tek klibe yazdığı için yön farkı kayboluyordu. Efe'ye ait `PlayerAnimator.controller` içindeki yalnızca `Upper Body Attack` katmanında IK Pass açıldı; ayak IK'si Base Layer'da bırakıldı ve sağ el/balta vuruş yolu saldırı katmanının sonunda uygulandı. Sol/sağ vuruşlarda sağ el `0.38 m` yana, üst vuruşta sağ omuzun `0.55 m` üstüne hedeflenir; sıra yakın ağaçtan bağımsız olduğu için boşta da `sol → sağ → üst` çalışır. Otomatik Play Mode ölçümünde sol/sağ yolları arasında `0.576 m`, üst vuruş ile yan vuruşlar arasında `0.390 m` yükseklik farkı doğrulandı; su hareket çarpanı da `0.30` olarak doğrulandı. Hasar, temas zamanı, ayak değerleri, ağaç sistemi ve ekip dosyaları değiştirilmedi. İşlem öncesi kod/controller yedeği `C:\Users\90505\forrest-game-code-backup-20260721-axe-variety-ik-layer` altında bulunuyor.
- 2026-07-21: İlk balta çeşitlilik ayarı oyun içinde yeterince belirgin görünmediği için güçlendirildi. Sol/sağ vuruşlarda üst gövde dünya ekseninde `24°` dönüş ve `10°` yatış, üstten vuruşta `16°` eğilme kullanır; baltaya ayrıca `14°` yanal ve `18°` üst yön vurgusu uygulanır. Saldırı çeşidi yakın ağaç kontrolünden bağımsız seçildiği için hem ağaca vururken hem boşta sallarken `sol → sağ → üst` sırasıyla çalışır. Aynı değişiklikte su hareket çarpanı kullanıcı geri bildirimiyle `0.45`ten `0.30`a indirildi. Unity 6000.5.2f1 doğrulamasında saldırı süresi `0.65 sn`, temas anı `0.30 sn`, ayak duruşu `0.17/0.11/0.11`, kara hızları `1.75/4.6`, sudaki azami süre `5 sn` ve `BaltaSalla` bağlantısı korundu. Animator Controller, sahneler, ağaç sistemi ve ekip dosyaları değiştirilmedi; işlem öncesi iki kod yedeği `C:\Users\90505\forrest-game-code-backup-20260721-axe-variety-v2-water` altında bulunuyor.
- 2026-07-21: Balta saldırısının art arda aynı görünmesini azaltmak için `EfeBaltaKontrol` içine yalnızca üst gövdeyi etkileyen üçlü görsel vuruş sırası eklendi: hafif soldan, hafif sağdan ve üstten. Mevcut Humanoid göğüs kemiğine saldırının orta anında yumuşak yön/yatış vurgusu uygulanır; ayak IK'si ve alt gövde etkilenmez. Unity 6000.5.2f1 doğrulamasında yön değerleri `10/5/8 derece`, saldırı süresi `0.65 sn`, temas anı `0.30 sn`, hasar `1`, ayak duruşu `0.17/0.11/0.11` ve buton bağlantısı `BaltaSalla` olarak korundu. Ağaç sistemi, Animator Controller, sahneler ve ekip dosyaları değiştirilmedi; işlem öncesi kod yedeği `C:\Users\90505\forrest-game-code-backup-20260721-axe-variety` altında bulunuyor.
- 2026-07-21: Efe karakterinin nehir içindeki yatay hareketi daha ağır ve kısıtlı hissettirmek için `EfeKarakterKontrol` içine `0.45` su hareket hızı çarpanı eklendi. Bu çarpan yalnızca gerçek su modu aktifken uygulanır ve suya giriş anındaki fazla yatay hız da güvenli biçimde sınırlandırılır. Unity 6000.5.2f1 derleme/doğrulamasında su çarpanının `0.45`, kara yürüyüş/koşu hızlarının `1.75/4.6`, sudaki azami sürenin `5 saniye` ve su derinliğinin `1.25` olarak korunduğu doğrulandı. Başka kod veya ekip dosyası değiştirilmedi; işlem öncesi kod yedeği `C:\Users\90505\forrest-game-code-backup-20260721-water-speed` altında bulunuyor.
- 2026-07-21: `Efe_CharacterSandbox` içindeki yuvarlak balta butonu daha rahat görülebilmesi için `132×132` boyutundan `156×156` boyutuna getirildi. Unity 6000.5.2f1 doğrulamasında butonun ekran konumu, anchor değerleri, balta sprite'ı ve tek kalıcı `EfeBaltaKontrol.BaltaSalla` bağlantısının aynen korunduğu doğrulandı. Başka bir oynanış veya ekip dosyası değiştirilmedi. İşlem öncesi sahne yedeği `C:\Users\90505\forrest-game-scene-backup-20260721-axe-button-resize` altında korunuyor.
- 2026-07-21: `Efe_CharacterSandbox` hareket joystick'inin yalnızca görsel sunumu yenilendi. Dış kontrol alanı Unity'nin yuvarlak UI sprite'ıyla açık gri ve `%34` şeffaf, iç tutamaç koyu gri ve `%78` şeffaf olarak ayarlandı; iki parçaya hafif gölge eklendi ve iç tutamaç `82×82` boyutuna getirildi. Unity 6000.5.2f1 doğrulamasında dış hareket alanı ile konumunun `260×260` ve önceki değerlerde kaldığı, `VirtualJoystick` bileşeni ile kontrol kodunun değişmediği doğrulandı. Mert'in sahnesi ve diğer ekip dosyaları değiştirilmedi. İşlem öncesi sahne yedeği `C:\Users\90505\forrest-game-scene-backup-20260721-joystick-visual` altında korunuyor.
- 2026-07-21: `Efe_CharacterSandbox` içindeki mevcut balta saldırı butonunun yalnızca görseli yenilendi. Eski dikdörtgen/yazılı görünüm yerine Efe'ye ait `Assets/Efe/UI/Efe_AxeButton.png` şeffaf sprite'ı kullanılarak 132×132 yuvarlak, ahşap/bronze çerçeveli ve balta ikonlu mobil oyun butonu oluşturuldu; eski `BALTA` yazısı gizlendi. Unity 6000.5.2f1 doğrulamasında butonun tek kalıcı tıklama bağlantısının `EfeBaltaKontrol.BaltaSalla` olarak aynen korunduğu doğrulandı. Saldırı kodu, Mert'in sahnesi ve diğer ekip dosyaları değiştirilmedi. İşlem öncesi sahne yedeği `C:\Users\90505\forrest-game-scene-backup-20260721-axe-button` altında korunuyor.
- 2026-07-21: Mert'in güncel haritasındaki duvar/çatı saydamlık sistemi `Efe_CharacterSandbox` içinde etkinleştirildi. Harita aktarımında korunan Efe karakterinin etiketi `Untagged` olduğu için 42 adet `FadeableObstacle` bileşeninin `player` alanı boş kalmıştı; bu alanların tamamı doğrudan Efe'nin `Ch36_nonPBR` karakterine bağlandı. Unity 6000.5.2f1 batch doğrulaması 42/42 doğru bağlantıyla tamamlandı. İki nesnedeki boş saydam materyal alanının (`Map/Crafter/WallsWorkshop/wall-window-shutters (1)` ve `Map/Home`) kaynak `Mert_Artscene` içinde de aynı olduğu doğrulandı; aktarım kaynaklı kayıp değildir. `FadeableObstacle.cs`, `Mert_Artscene` ve diğer ekip dosyaları değiştirilmedi. İşlem öncesi sahne yedeği `C:\Users\90505\forrest-game-scene-backup-20260721-occlusion` altında korunuyor.
- 2026-07-21: Yalnızca Efe'ye ait oynanış kodları için ses/VFX entegrasyon sözleşmesi eklendi. `EfeOynanisEventleri` üzerinden toplam 17 olay; `EfeKarakterKontrol`, `EfeBaltaKontrol`, `EfeOdunTasima` ve `VirtualJoystick` içindeki gerçekleşmiş oynanış noktalarından yayınlanır. Mevcut hareket, hasar, toplama ve yükleme hesapları değiştirilmedi. Unity 6000.5.2f1 C# derlemesi sıfır hatayla geçti; otomatik sözleşme testinde tüm event'lerin varlığı, odun miktar verileri ve hata veren bir dinleyicinin sonraki dinleyiciyi/oynanışı engellemediği doğrulandı. Mert ve diğer ekip üyelerinin scriptleri değiştirilmedi; işlem öncesi Efe script yedeği `C:\Users\90505\forrest-game-efe-events-backup-20260721-102413` altında korundu.
- 2026-07-21: Güncel `Mert_Artscene`, `Efe_CharacterSandbox` içine yeniden senkronlandı. Mert'in `CameraShakeManager`, `Directional Light`, `LoadingRingCanvas`, `Map`, `Terrain`, `TreeManager`, `truckLv1` ve `Workshop_Production` kökleri alınırken Efe'ye ait `Main Camera`, `Ch36_nonPBR`, `Canvas` ve `EventSystem` kökleri aynen korundu. Unity doğrulamasında toplam 12 kök, tek kamera, tek EventSystem, kameranın Efe karakterine bağlı takip hedefi, CharacterController/karakter kontrolü/balta/odun taşıma bileşenleri ve sıfır eksik script doğrulandı. `Mert_Artscene` değiştirilmedi; önceki Efe sahnesi `C:\Users\90505\forrest-game-scene-backup-20260721-093857` altında korundu ve işlem için kullanılan geçici Editor aracı kaldırıldı.
- 2026-07-21: Mert'in `feature/mert-art-design` çalışmasının PR #19 ile `450a4d2` üzerinden, Efe'nin PR #18 çalışmasının da ardından `82d2bec` üzerinden `develop` branch'ine zaten merge edildiği doğrulandı. Güncel `develop` ayrı worktree'de 524 meta dosyasıyla tarandı; duplicate GUID bulunmadı ve Unity 6000.5.2f1 tam import/C# derlemesi sıfır hatayla tamamlandı. `karakter-kontrol` branch'i conflict oluşturmayan fast-forward ile `82d2bec` commitine getirildi ve GitHub'a pushlandı. On bir yerel dosya işlem öncesinde dış yedeğe ve isimli stash'e alındı; Terrain dışındaki on dosya SHA-256 karşılaştırmasıyla birebir geri yüklendi. Çalışma kopyasında Mert'in güncel `New Terrain.asset` sürümü kullanılırken önceki yerel Terrain sürümü `C:\Users\90505\forrest-game-local-backup-20260721-093113` ve güvenlik stash'inde korundu.
- 2026-07-16: İlk kapsamlı depo incelemesi yapıldı; vizyon, branch durumu, mevcut sistemler, riskler ve MVP ölçütü kaydedildi.
- 2026-07-16: Git remote URL'sindeki düz metin erişim anahtarı kaldırıldı.
- 2026-07-16: Mert branch'i için merge provası yapıldı; 331 adet kayıp prefab GUID referansı nedeniyle entegrasyon durduruldu.
- 2026-07-16: Mert'in `39698d2` düzeltmesi doğrulandı; Unity temiz import/derleme testi geçti, branch `develop`'a ve ardından `karakter-kontrol` branch'ine güvenli biçimde alındı.
- 2026-07-16: Entegrasyon sonrası `Efe_Scene` içindeki 65 eski çit prefab referansı kayıp oluşturmayacak biçimde güncellendi.
- 2026-07-16: `Efe_CharacterSandbox` temiz Unity otomasyonunda oluşturuldu; tek kamera, kamera hedefi ve joystick bağlantıları doğrulandı. Kaynak Mert/Efe sahneleri değiştirilmedi.
- 2026-07-16: Karakter Idle/Walk/Run animasyonları `Hiz` (0/0.5/1) 1D Blend Tree ile bağlandı; klipler loop yapıldı ve joystick hareket/animasyon geçişleri gerçekçi hızlanma ile düzenlendi. Unity Animator doğrulaması ve C# derlemesi geçti.
- 2026-07-16: Balta karakterin sağ el kemiğine bağlandı, sağ altta `BALTA` butonu oluşturuldu ve `Saldir` trigger'lı Axe Attack state'i eklendi. Buton bağlantısı/Animator Unity otomasyonunda doğrulandı, C# derlemesi geçti; hasar sistemi eklenmedi.
- 2026-07-16: Üstü açık ahşap sırt sepeti prefabı oluşturuldu, karakterin `Spine2` kemiğine bağlandı ve gelecekte odun dizmek için `LogStackAnchor` eklendi. Taşıma mantığı eklenmedi; Unity prefab/sahne doğrulaması geçti.
- 2026-07-17: Mert'in güncel haritasını içeren `develop` (`1fd81d0`), yerel kullanıcı dosyaları korunarak `karakter-kontrol` branch'ine fast-forward ile alındı ve pushlandı. `Efe_CharacterSandbox`, Mert'in 12 güncel harita kökü ile Efe'ye ait mevcut karakter/kamera/Canvas/EventSystem kökleri birleştirilerek yenilendi; `Mert_Artscene` değiştirilmedi. Unity batch doğrulamasında 16 kök, tek kamera/joystick, karakter-kamera-balta-sepet bağlantıları, `TreeManager` ve sıfır eksik script doğrulandı.
- 2026-07-17: `EfeBaltaKontrol`, 3 metre içindeki en yakın Terrain ağacını seçip karakteri hedefe döndürecek ve balta temasında `TreeManager.HitTreeAt` çağıracak şekilde bağlandı. Yalnızca `Efe_CharacterSandbox` içindeki kırık TreeManager prefab referansları mevcut pine/cone/default fallen, stump ve wood-log prefablarına düzeltildi; Mert'in kaynak sahnesi, scriptleri ve prefabları değiştirilmedi. Unity editör derlemesi hatasız tamamlandı.
- 2026-07-17: Balta saldırısının uzun hazırlığı kısaltıldı: Axe Attack state hızı 2.0, başlangıç ofseti 0.20, giriş geçişi 0.03 sn ve hasar temas gecikmesi 0.30 sn olarak ayarlandı. Unity günlüğünde üç vuruş ve ağaç devrilme akışı doğrulandı.
- 2026-07-17: Balta saldırısı Base Layer'dan çıkarılıp root/bacak/ayak IK kanalları kapalı `Efe_UpperBody` maskeli `Upper Body Attack` katmanına taşındı. Idle/Walk/Run alt gövdeyi ve ayak temasını korurken saldırı yalnızca üst gövdede çalışacak şekilde geçici Unity projesinde oluşturulup doğrulandı.
- 2026-07-17: Saldırı sırasında donuk alt gövdeyi düzeltmek için ayak IK duruşu eklendi; sağ ayak 0,24 m ileri, sol ayak 0,18 m geri alınır, 0,08 sn'de duruşa girip 0,14 sn'de çıkar ve iki ayağın yüksekliği aktif Terrain üzerinden zemine sabitlenir. Üst gövde hızı ve hasar zamanlaması değiştirilmedi; minimal Unity projesinde C# derlemesi geçti.
- 2026-07-20: Mert'in `feature/mert-art-design` PR'ı ekip tarafından `ca04e03` ile `develop` branch'ine birleştirildi. Güncel `develop`, ayrı worktree'de Git merge, sıfır duplicate GUID ve Unity 6000.5.2f1 temiz import/C# derleme testiyle doğrulandıktan sonra `karakter-kontrol` branch'ine alındı. Efe'nin kaydedilmemiş yerel dosyaları işlem öncesinde dış yedek ve Git stash ile korundu; çalışma kopyasında Mert'in güncel `New Terrain.asset` sürümü kullanıldı, önceki yerel Terrain sürümü yedekte tutuldu. Mert'in kaynak branch'i, sahnesi ve `develop` üzerinde bu işlem sırasında doğrudan değişiklik yapılmadı.
- 2026-07-20: `Efe_CharacterSandbox`, güncel `Mert_Artscene` harita kökleri temel alınarak yenilendi. Efe'ye ait `Main Camera`, `Ch36_nonPBR`, `Canvas` ve `EventSystem` kökleri aynen korundu; Mert sahnesindeki aynı adlı karakter/UI kökleri hedefe taşınmadı. Sonuç Unity 6000.5.2f1'de 19 kök, tek kamera, tek EventSystem, bağlı joystick, balta/odun bileşenleri, TreeManager ve sıfır eksik script ile doğrulandı. Kaynak `Mert_Artscene` değiştirilmedi ve eski Efe sahnesi dış yedekte korundu.
- 2026-07-20: `EfeOdunTasima` toplama geri bildirimi geliştirildi. Sepet slotu görseli odunun dünya konumundan hareketli `LogStackAnchor` hedefine cubic ease-out, kavis, dönüş ve hafif ölçek vurgusuyla uçar; eşzamanlı toplama ve animasyon sırasında kamyona boşaltma sürüm kontrolüyle güvenli tutulur. TreeManager havuzu, kapasite, sahne ve prefablar değiştirilmedi; Unity 6000.5.2f1 izole derleme testi geçti.
- 2026-07-20: Sepetten kamyona yükleme, odunların tek tek kavis çizerek kasaya uçtuğu sıralı animasyona dönüştürüldü. Kamyon yük sayacı yalnızca uçuş tamamlanınca artar; runtime kasa görselleri yük oranına göre 0–30 arasında açılır. İzole Unity testinde `0/30 → 0`, `15/30 → 15`, `30/30 → 30` görünür odun ve kamyona bağlı görsel kökü doğrulandı. `TruckRouteController`, TreeManager, kamyon prefabları ve Mert sahnesi değiştirilmedi.
- 2026-07-20: Karakterin tek koşuda aldığı fazla mesafeyi azaltmak için tam koşu hızı `5,5`ten `4,6 m/sn`ye indirildi. Joystick bırakıldığında hızlanmadan bağımsız `40` durma sertliği kullanılır ve küçük artık yatay hız sıfırlanır; yürüyüş, joystick eşikleri ve Animator `Hiz` aralığı değiştirilmedi.
- 2026-07-20: Ani sağ/sol ve ters yön girişlerinde transform yeni yöne dönerken eski yatay hızın sürmesinden oluşan kaymayı azaltmak için yön vektörleri arasındaki dot değeri `0,25`in altındaysa `45` sertliğinde ayrı hız değişimi uygulanır. Normal hızlanma, durma, joystick eşikleri ve animasyon sistemi korunur.
- 2026-07-20: Karakterin nehirde dibe çökmesini önlemek için yalnızca `water_river` tetikleyicisini tanıyan su yürüyüşü eklendi. Su yüzeyi tetikleyici sınırının üstünden hesaplanır; karakter ayakları yüzeyin `0,65 m` altında, bacak seviyesi suda kalacak biçimde saniyede `5 m` hızla dengelenir. Nehir nesnesi, Mert sahnesi ve çevre varlıkları değiştirilmedi; Unity 6000.5.2f1 izole C# derlemesi geçti.
- 2026-07-20: Nehir derinliği karakterin göğüs hizasına gelmesi için `1,25 m` olarak ayarlandı. Karakter suda kesintisiz `5 saniye` kalırsa oyun başındaki konumuna ve yönüne geri döner; yatay/dikey hızı ile Animator hareket değeri sıfırlanır. Sudan süre dolmadan çıkılırsa sayaç sıfırlanır. Yalnızca Efe karakter kontrolü ve `Efe_CharacterSandbox` ayarı değiştirildi; Unity 6000.5.2f1 izole C# derlemesi hatasız geçti.
- 2026-07-20: `water_river` trigger alanı karakterin başlangıç konumunu da geometrik olarak kapsadığı için 5 saniyelik sayaç karadayken başlayabiliyordu. Su modu artık önce karakterin karada olduğunu doğrular; yalnızca trigger içindeyken zemin temasını kaybedip su yüzeyine indiğinde etkinleşir. Böylece başlangıçta geri ışınlanma engellendi; su içindeki 5 saniyelik geri dönüş korundu. Nehir ve Mert sahnesi değiştirilmedi, izole Unity derlemesi geçti.
