# Forrest Game — Proje Hafızası

> Bu dosya projenin kalıcı teknik hafızasıdır. Son güncelleme: 17 Temmuz 2026.
> Yeni bir işe başlamadan önce tüm depoyu yeniden taramak yerine önce bu dosya ve `TODO.md` okunur.
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
