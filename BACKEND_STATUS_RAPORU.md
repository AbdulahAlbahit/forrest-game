# ☁️ Forrest Game — Proje Durum Raporu (Backend & Sistem Mekanikleri Odaklı)

> 📅 Tarih: 26 Temmuz 2026  
> 📁 Proje: `c:\forrest_game\forrest-game`

---

## 📊 Genel Özet

| Kategori | Tamamlanma | Durum |
|----------|------------|-------|
| Firebase Auth (Guest) | ~%50 | 🟡 Kısmen yapılmış (Sadece misafir) |
| Firebase Firestore (Cloud Save) | ~%95 | 🟢 Neredeyse Tamam (Lokal & Çakışma çözümü var) |
| Ekonomi Sistemi | %100 | 🟢 Tamamlandı (Bulut senkronizasyonlu) |
| Upgrade Sistemi | %100 | 🟢 Tamamlandı (Config tabanlı, jenerik) |
| XP & Seviye Sistemi | %100 | 🟢 Tamamlandı (Üstel formüllü, bulut destekli) |
| Atölye & Üretim Sistemi | %100 | 🟢 Tamamlandı (Kuyruk & Fabrika Bandı mantığı var) |
| Mağaza & Sipariş Sistemi | %100 | 🟢 Tamamlandı (Bekleyen sipariş karşılama var) |
| Görev (Quest) Sistemi | %100 | 🟢 Tamamlandı (Otomatik ödüllendirme) |
| Oyuncu Veri Modeli | ~%95 | 🟢 Yeni veri tipleri eklenmiş |

---

## ✅ YAPILANLAR (Mevcut Durum)

### 1. Firebase Authentication & Cloud Save
- [x] **Misafir Girişi (`FirebaseAuthManager`)**: Anonim hesap oluşturma, otomatik bağlanma ve çıkış işlemleri aktif.
- [x] **Bulut & Lokal Kayıt (`CloudSaveManager`)**: 
  - Firestore okuma/yazma işlemleri tam çalışıyor. 
  - Çevrimdışı (Offline) durumlar için `PlayerPrefs` lokal yedekleme devrede.
  - Bulut vs Lokal çatışma çözümü (`LastSaveTime` ile) devrede.
  - 60 saniyede bir otomatik kayıt (Auto-Save) mekanizması yapılmış.
- [x] **Oyuncu Veri Modeli (`PlayerData`)**: Para, XP, zaman değerleri ve `ItemLevels`, `ShopInventory`, `WorkshopQueue` vb. esnek veri sözlükleri (`SerializableDict`) eklenmiş.

### 2. 💰 Ekonomi Sistemi (`EconomyManager`)
- [x] Singleton yapısı ile merkezi para yönetimi sağlandı.
- [x] `CloudSaveManager` ile tam entegre (buluttan para okuma ve harcama).
- [x] `AddMoney(amount)`: `SeasonManager` üzerinden mevsimsel çarpanları dikkate alarak gelir hesaplar.
- [x] `SpendMoney(amount)`: Bakiye kontrolü yapar, yetersizse `NotificationManager` ile bildirim tetikler.
- [x] Oyundan çıkarken güvenli kayıt özelliği aktif.

### 3. ⬆️ Upgrade Sistemi (`UpgradeManager`)
- [x] **Jenerik Altyapı**: Her eşya için ayrı script yazmak yerine, `ItemUpgradeConfig` (ScriptableObject) tabanlı merkezi bir sistem kurulmuş.
- [x] **Hızlı Erişim**: Sistem başlangıcında config'ler Dictionary içine (O(1) erişim hızı) alınarak optimize edilmiş.
- [x] `UpgradeItem(itemId)` metodu:
  - Maksimum seviye kontrolü yapar.
  - Gerekli maliyeti hesaplar ve `EconomyManager`'dan çeker.
  - Seviyeyi artırıp buluta otomatik kayıt atar.
  - Tüm bu olaylar `OnItemUpgraded` eventi ile UI'a bildirilir.

### 4. ⭐ XP ve Seviye Sistemi (`XPManager`)
- [x] **Dinamik XP Eğrisi**: `temelGerekenXP * (seviyeCarpani ^ (seviye - 1))` üstel formülü ile her seviyenin zorluğu dinamik hesaplanıyor.
- [x] `AddXP(amount)` metodu ile XP kazanımı.
- [x] Tek seferde yüksek XP kazanıldığında "Zincirleme Seviye Atlama" (while döngüsü ile) desteği.
- [x] Yeni XP ve seviye anında `CloudSaveManager` üzerinden buluta kaydediliyor.
- [x] UI ilerleme çubuğu için `GetXPProgress()` (0.0 - 1.0 arası) yüzdelik hesaplama metodu hazır.

### 5. 🏭 Atölye & Üretim Sistemi (`WorkshopManager`)
- [x] **Tarif (Recipe) Tabanlı Üretim**: `RecipeConfig` ScriptableObject'leri ile üretim süreçleri tanımlanmış.
- [x] **Üretim Kuyruğu (`WorkshopQueue`)**: Birden fazla ürün sıraya eklenebiliyor. Kapanıp açılsa bile üretim kaldığı yerden devam ediyor (Firebase senkronlu).
- [x] **Fabrika Bandı Mantığı**: Üretimi biten mallar doğrudan oyuncuya geçmez, Kamyon'un alması için `ReadyForTruck` (Kamyon Bekleyenler) havuzuna eklenir.
- [x] **Dinamik Hız & Kapasite**: Üretim hızı ve kuyruk kapasitesi, `UpgradeManager` üzerinden eşzamanlı olarak okunarak dinamik güncellenir. Ayrıca mevsimsel hız çarpanları bulunur.

### 6. 🏪 Mağaza & Sipariş Sistemi (`ShopManager`)
- [x] **Kamyon Entegrasyonu**: Kamyon markete vardığında `TruckRouteController.OnMarketSale` eventi tetiklenir ve `ReadyForTruck` havuzundaki mallar otomatik olarak `ShopInventory` (Stok) kısmına eklenir.
- [x] **Bekleyen Sipariş (Customer Orders)**: Müşteri bir mal sipariş ettiğinde stokta yoksa satışı iptal etmez, `PendingCustomerOrders` içine kaydeder.
- [x] **Otomatik Karşılama**: Kamyon yeni mal getirdiğinde `CheckAndFulfillPendingOrders` devreye girer ve bekleyen siparişleri otomatik olarak karşılayıp oyuncuya parayı verir.
- [x] **Analitik**: Her satılan eşya Firebase Analytics'e `item_sold` eventi olarak düşer.

### 7. 📜 Görev (Quest) Sistemi (`QuestManager`)
- [x] **Otomatik İzleme**: Mağazada her satış yapıldığında `ShopManager.OnItemSold` eventini dinler ve ilgili görevin (Örn: "Masa Sat") ilerlemesini otomatik artırır.
- [x] **Otomatik Ödül**: Görev hedefine ulaşıldığında, oyuncuya anında belirtilen Altın ve XP ödülünü yansıtır (`EconomyManager` ve `XPManager` üzerinden).
- [x] Tekrar yapılmaması için biten görevler `CompletedQuests` listesine kalıcı olarak kaydedilir.

### 8. 🗺️ Bölge & Sepet Sistemleri (`RegionManager` & `BasketManager`)
- [x] **Bölge Açma**: `RegionConfig` üzerinden fiyat çeker. Parası yetiyorsa açar ve `PlayerData.AcilanBolgeler` listesine ekler.
- [x] **Sepet**: Sepet kapasitesini sürekli `UpgradeManager` üzerinden okur. Doluluk, eklenebilir maksimum miktar gibi kontrolleri yapar. Limit aşımlarını engeller.

---

## ❌ YAPILMAYANLAR (Eksikler)

### 1. 🔥 Firebase Auth & Güvenlik
- **Hesap Bağlama (Linking)**: Geçici misafir hesabı oyunu silince yok olur. Bunu kalıcı hale getirecek e-posta veya Google hesap bağlama mekanizması yok.
- **E-posta & Şifre Login**: Sadece anonim giriş var, e-posta ile giriş yapılmıyor.
- **Güvenlik Kuralları**: Firestore'da oyuncuların kendi verileri dışında veri okumasını/yazmasını engelleyecek Security Rules ayarlanmamış.
- **Hile Koruması (Anti-Cheat)**: Para, XP ve Upgrade işlemleri Client'ta yapılıyor, bulut fonksiyonları ile doğrulanmıyor.

---

## 🎯 Yapılması Gereken Sıralı İş Listesi

1. **Hesap Bağlama (Account Linking)**: `FirebaseAuthManager` içine `LinkWithCredential` ekleyerek misafir verisini koruma.
2. **E-posta Girişi**: `SignUpWithEmail` ve `SignInWithEmail` fonksiyonlarının yazılması.
3. **Firestore Security Rules**: Veritabanı okuma/yazma kurallarının güvenli hale getirilmesi.
4. **Google Sign-In**: Mobil oyuncular için tek tuşla hızlı giriş sistemi.
