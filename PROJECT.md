# Forrest Game — Proje Hafızası

> Bu dosya projenin kalıcı teknik hafızasıdır. Son güncelleme: 16 Temmuz 2026.
> Yeni bir işe başlamadan önce tüm depoyu yeniden taramak yerine önce bu dosya ve `TODO.md` okunur.

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

- `EfeKarakterKontrol.cs`: joystick şiddetini dead-zone sonrasında yürüyüş/koşu hızlarına kademeli dönüştürür; yumuşak hızlanma, dönüş, kararlı yerçekimi ve normalize `Hiz` Animator parametresi üretir.
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
| Ağaç kesme | Yok | Can, hedef seçimi, darbe, düşme ve yeniden doğma sistemi yok. |
| Balta | Görsel/animasyon prototipi | Balta sağ ele bağlı; UI butonu Axe Attack animasyonunu tetikliyor. Ağaç algılama ve hasar henüz yok. |
| Kütük üretme | Asset var, sistem yok | Kesilen ağaçtan kaynak üretimi yok. |
| Sırtta taşıma | Görsel hazırlık | Ahşap sırt sepeti `Spine2` kemiğine bağlı ve gelecekteki odunlar için `LogStackAnchor` var; kapasite, stack ve al/bırak mantığı henüz yok. |
| Kamyona yükleme | Yok | Yük kabul alanı, kapasite ve görsel stack yok. |
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
