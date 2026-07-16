# Forrest Game — TODO

> Öncelik: P0 kritik, P1 MVP, P2 iyileştirme, P3 sonraki sürüm.
> Sahibi `BELİRLENECEK` olan uygulama işleri, ekip görev dağılımı gelmeden Codex tarafından yapılmaz.

## Şimdi — projeyi güvenli hale getir

- [ ] **P0 / SEN:** Açığa çıkan GitHub Personal Access Token'ı GitHub Settings > Developer settings üzerinden iptal et; gerekiyorsa yeni token oluştur.
- [ ] **P0 / SEN:** Ekip görev dağılımını ve teknik proje dokümanını gönder.
- [ ] **P0 / SEN:** Şu anki kaydedilmemiş Unity değişikliklerinin hangilerinin korunacağını belirt/commit et.
- [x] **P0 / CODEX:** `karakter-kontrol` branch upstream'ini `origin/karakter-kontrol` olarak düzelt.
- [ ] **P0 / BELİRLENECEK:** `UserSettings` altında takipte kalan dosyaları Git index'inden güvenli biçimde çıkar; kişisel editor düzenlerini commit etme.
- [ ] **P0 / BELİRLENECEK:** Takip edilmeyen `ProjectSettings/ProjectSettings.asset` dosyasının nedenini doğrula ve doğru sürümü commit et.
- [ ] **P0 / EKİP:** Branch entegrasyon sırasını kararlaştır: güncel `develop` tabanı, backend PR'ı ve Mert kamyon PR'ı.
- [ ] **P0 / EKİP:** `feature/mert-art-design` ve `backend-veri-modelleri` branch'lerindeki büyük silme/taşıma farklarını PR öncesi dosya bazında incele.
- [x] **P0 / MERT:** `Mert_Artscene.unity` içindeki silinmiş `Fence_group_6x` prefab GUID'sine ait referansları, korunan `Assets/Prefabs/Fence_group_6x.prefab` ile yeniden bağla.
- [x] **P0 / MERT:** `UserSettings` dosyalarını özellik branch'inden ve Git takibinden çıkar.
- [ ] **P0 / MERT:** Commit açıklamasındaki 10 kamyon seviyesi ile repodaki 5 kamyon prefabı arasındaki farkı doğrula.
- [x] **P0 / CODEX:** Mert düzeltmesi geldikten sonra GUID taramasını ve merge provasını tekrar çalıştır; temizse `develop`'a merge/push et.
- [x] **P0 / CODEX:** Güncel `develop`'ı, yerel değişiklikleri koruyarak `karakter-kontrol` branch'ine merge et.

## Sprint 0 — teknik omurga

- [ ] **P0 / EKİP:** Kesin platformu, ekran yönünü, minimum cihazı, hedef FPS'yi ve offline/online gereksinimini teknik dokümana yaz.
- [ ] **P0 / EKİP:** Tek entegrasyon sahnesini seç; sahne sahipliği kuralını belirle.
- [ ] **P0 / BELİRLENECEK:** Gerçek başlangıç sahnesini Build Settings'e ekle, boş `SampleScene` bağımlılığını kaldır.
- [ ] **P0 / EKİP:** `Assets/Prefabs` ile `Assets/Prefabs 1` tekrarını GUID/referans kaybetmeden çöz.
- [ ] **P1 / BELİRLENECEK:** Oynanış veri ayarlarını ScriptableObject'lere ayır: balta gücü, ağaç canı, taşıma kapasitesi, kamyon kapasitesi, işleme süresi, ürün değeri.
- [ ] **P1 / BELİRLENECEK:** Ortak kaynak tipini tanımla (`Wood` ile başlayan, ileride genişleyebilir yapı).
- [ ] **P1 / BELİRLENECEK:** Sistemler arası arayüz/olay sözleşmelerini netleştir; ekonomi ve Firebase'e doğrudan sahne referansı verme.
- [ ] **P1 / BELİRLENECEK:** EditMode ve PlayMode test klasörlerini/assembly definition dosyalarını kur.
- [ ] **P1 / BELİRLENECEK:** GitHub Actions veya Unity CI ile en azından derleme/test kontrolü oluştur.

## Sprint 1 — karakter ve etkileşim

- [ ] **P1 / BELİRLENECEK:** Karakter hareketini kamera yönüne göre test et; izometrik görünümde joystick yönlerinin sezgisel olduğunu doğrula.
- [x] **P1 / EFE:** CharacterController yerçekimini düşey hız biriktiren kararlı yapıya çevir.
- [ ] **P1 / EFE:** CharacterController eğim ve basamak sınırlarını oyun alanında test edip ayarla.
- [x] **P1 / EFE:** Animator null/parametre doğrulamasını ve Idle/Walk/Run Blend Tree geçişlerini düzenle.
- [ ] **P1 / BELİRLENECEK:** Etkileşim algılama sistemi ekle (trigger veya yakın hedef seçimi).
- [ ] **P1 / BELİRLENECEK:** Oyuncunun hangi hedefle etkileşeceğini görsel olarak belirt.
- [ ] **P2 / BELİRLENECEK:** Mobil joystick'i farklı çözünürlük, safe area ve DPI değerlerinde test et.
- [ ] **P2 / BELİRLENECEK:** Kamera engel/duvar arkasında kalma davranışını belirle.
- [ ] **P2 / BELİRLENECEK:** Boş `PlayerMovement.cs` dosyasını referans kontrolünden sonra kaldır.

## Sprint 2 — ağaç kesme

- [x] **P1 / EFE:** Baltayı sağ el kemiğine bağla ve UI butonuyla Axe Attack animasyonunu tetikle.
- [ ] **P1 / BELİRLENECEK:** `TreeHarvestable` can, kaynak miktarı ve kesilmiş durumunu uygula.
- [ ] **P1 / BELİRLENECEK:** Balta ekipmanı ve vuruş gücü modelini uygula.
- [ ] **P1 / BELİRLENECEK:** Saldırı animasyon event'i ile hasar anını senkronize et.
- [ ] **P1 / BELİRLENECEK:** Aynı vuruşta birden fazla hasarı ve uzaktan kesmeyi engelle.
- [ ] **P1 / BELİRLENECEK:** Kesilince ağaç görseli/collider geçişini ve kütük üretimini uygula.
- [ ] **P1 / BELİRLENECEK:** Ağaç yeniden doğma süresini ve havuzlama (pooling) yaklaşımını uygula.
- [ ] **P2 / BELİRLENECEK:** Vuruş VFX, ses, kamera sarsıntısı ve hasar geri bildirimi ekle.
- [ ] **P2 / BELİRLENECEK:** Farklı ağaç türleri ve odun değerleri için veri odaklı yapı kur.
- [ ] **P1 / BELİRLENECEK:** Ağaç hasarı, tek seferlik kesilme ve doğru drop miktarı için test yaz.

## Sprint 3 — sırt çantası ve kütük taşıma

- [x] **P1 / EFE:** Karakterin sırtına ahşap sepet prefabı bağla ve gelecekteki kütükler için `LogStackAnchor` hazırla.
- [ ] **P1 / BELİRLENECEK:** `PlayerCarryInventory` kapasite ve kaynak sayacı uygula.
- [ ] **P1 / BELİRLENECEK:** Yakındaki kütüğü otomatik veya düğmeyle toplama kararını teknik dokümana göre uygula.
- [ ] **P1 / BELİRLENECEK:** Sırtta kütük stack noktaları ve kapasiteye bağlı görselleştirme ekle.
- [ ] **P1 / BELİRLENECEK:** Kapasite dolu geri bildirimi ekle.
- [ ] **P1 / BELİRLENECEK:** Toplama/bırakma sırasında çoğalma, negatif miktar ve kayıp oluşmasını engelleyen testler yaz.
- [ ] **P2 / BELİRLENECEK:** Taşınan yüke bağlı hareket/animasyon etkisini tasarıma göre ekle.

## Sprint 4 — kamyon yükleme ve taşıma

- [ ] **P1 / BELİRLENECEK:** `TruckCargo` kapasite ve kabul edilen kaynak tiplerini uygula.
- [ ] **P1 / BELİRLENECEK:** Oyuncu kamyon yük alanındayken kontrollü aktarım uygula.
- [ ] **P1 / BELİRLENECEK:** Kamyon üzerindeki kütükleri doluluk oranına göre görselleştir.
- [ ] **P1 / BELİRLENECEK:** Kamyon doluluk sayacı/progress UI ekle.
- [ ] **P1 / BELİRLENECEK:** Mert'in `TruckRouteController` sistemini entegrasyon sözleşmesine bağla; görsel/rota kodunun sahipliğini koru.
- [ ] **P1 / BELİRLENECEK:** Kalkış koşulu seç: otomatik doluluk, oyuncu komutu veya zamanlayıcı.
- [ ] **P1 / BELİRLENECEK:** Atölyeye varış, boşaltma ve dönüş state machine'ini tamamla.
- [ ] **P1 / BELİRLENECEK:** Kamyon yolculuğunda duplicate teslimat ve rota kilitlenmesi testleri yaz.

## Sprint 5 — atölye, ürün ve ekonomi

- [ ] **P1 / BELİRLENECEK:** Atölye giriş deposu ve üretim kuyruğu uygula.
- [ ] **P1 / BELİRLENECEK:** Odun -> işlenmiş ürün dönüşüm tarifi, süre ve değer verilerini tanımla.
- [ ] **P1 / BELİRLENECEK:** Üretim progress görseli ve tamamlanma feedback'i ekle.
- [ ] **P1 / BELİRLENECEK:** İşlenmiş ürünün satılması ve para kazanımı akışını tamamla.
- [ ] **P1 / BELİRLENECEK:** Backend branch'indeki `EconomyManager` ve `UpgradeManager` API'lerini oynanıştan bağımsız incele ve sözleşmeyle entegre et.
- [ ] **P1 / BELİRLENECEK:** En az balta gücü, sırt kapasitesi, kamyon kapasitesi ve atölye hızı upgrade'lerini uygula.
- [ ] **P1 / BELİRLENECEK:** Satın alma maliyeti, yetersiz bakiye ve kalıcı seviye testleri yaz.

## Sprint 6 — kayıt, UI ve oyuncu yönlendirmesi

- [ ] **P1 / EKİP:** MVP kayıt stratejisini seç: local-first mi, zorunlu Firebase mi?
- [ ] **P1 / BELİRLENECEK:** Para, upgrade seviyeleri, kaynaklar ve tutorial adımını kaydet/yükle.
- [ ] **P1 / BELİRLENECEK:** Bozuk/eksik kayıt için varsayılan veri ve migration stratejisi ekle.
- [ ] **P1 / BELİRLENECEK:** İlk 2–3 dakikalık yönlendirme: evden çık, ağacı kes, kütüğü al, kamyona götür.
- [ ] **P1 / BELİRLENECEK:** HUD: para, taşıma kapasitesi, kamyon kapasitesi, atölye progress ve etkileşim feedback'i.
- [ ] **P2 / BELİRLENECEK:** Ayarlar: ses, titreşim, dil ve gizlilik bağlantıları.
- [ ] **P0 / BACKEND SAHİBİ:** Firebase Security Rules, test/prod proje ayrımı ve config yönetimini doğrula.
- [ ] **P1 / BACKEND SAHİBİ:** Auth/CloudSave hata, timeout ve offline senaryolarını test et.

## Sprint 7 — kalite, performans ve yayın

- [ ] **P0 / EKİP:** Hedef Android cihaz listesini ve performans bütçesini belirle.
- [ ] **P1 / BELİRLENECEK:** 400x400 harita için CPU, GPU, draw call, memory ve loading profili çıkar.
- [ ] **P1 / BELİRLENECEK:** Ağaç/kütük/VFX için object pooling uygula veya doğrula.
- [ ] **P1 / BELİRLENECEK:** Collider, static batching, GPU instancing, LOD ve shadow ayarlarını hedef cihazda doğrula.
- [ ] **P1 / BELİRLENECEK:** Input, pause/resume, uygulama kapanması ve düşük bellek senaryolarını test et.
- [ ] **P1 / BELİRLENECEK:** Android development build al ve gerçek cihaz smoke test yap.
- [ ] **P1 / BELİRLENECEK:** Kritik oyun döngüsü için PlayMode test paketi çalıştır.
- [ ] **P2 / EKİP:** İkon, splash, mağaza görselleri, gizlilik politikası ve sürümleme planı hazırla.

## Sonraki sürüm fikirleri — MVP dışı

- [ ] **P3:** Yeni orman bölgeleri ve açılabilir harita.
- [ ] **P3:** Farklı ağaç/odun/ürün türleri.
- [ ] **P3:** Çalışan NPC'ler ve otomasyon.
- [ ] **P3:** Offline kazanç.
- [ ] **P3:** Günlük görevler ve ödüller.
- [ ] **P3:** Kozmetik karakter/balta/kamyon görünümleri.
- [ ] **P3:** Analitik, denge telemetrisi ve uzaktan ayar.

## Codex çalışma kuyruğu

Görev dağılımı gelene kadar:

- [x] Depoyu, branch'leri, sahneleri, kodu ve paketleri incele.
- [x] GitHub remote adresindeki açık anahtarı kaldır.
- [x] `PROJECT.md` proje hafızasını oluştur.
- [x] Öncelikli ve sahiplik kontrollü `TODO.md` oluştur.
- [ ] Kullanıcının görev dağılımını `PROJECT.md` sahiplik tablosuna işle.
- [ ] Teknik dokümanı gereksinimlere ve kabul ölçütlerine dönüştür.
- [ ] Yalnızca kullanıcıya ait ilk P0/P1 görevini uygulamaya başla.
