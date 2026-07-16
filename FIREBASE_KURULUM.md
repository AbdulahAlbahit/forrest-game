# 🔥 Firebase Kurulum Rehberi — Forrest Game

Bu rehber, Unity projemize Firebase SDK'sını entegre etmek ve Anonim (Misafir) Girişi aktif hale getirmek için adım adım yönergeleri içerir.

---

## 1. Firebase Console'da Proje Oluşturma

1. [Firebase Console](https://console.firebase.google.com/) adresine gidin ve Google hesabınızla giriş yapın.
2. **"Proje Ekle"** (Add Project) butonuna tıklayın.
3. Proje adını girin: `forrest-game` (veya istediğiniz bir isim).
4. Google Analytics adımını isteğe bağlı olarak açıp kapatın, **"Proje Oluştur"** butonuna basın.
5. Proje hazır olduğunda **"Devam Et"** butonuna tıklayın.

---

## 2. Firebase'e Unity Uygulaması Ekleme

1. Firebase Console'da projenize girin.
2. **Proje Ayarları** (⚙️ ikonu) > **Genel** sekmesine gidin.
3. **"Uygulama Ekle"** bölümünden **Unity ikonuna** (🎮) tıklayın.
4. Aşağıdaki bilgileri doldurun:

   | Alan | Değer |
   |------|-------|
   | **Apple bundle ID** (iOS için) | `com.yourcompany.forrestgame` |
   | **Android package name** | `com.yourcompany.forrestgame` |
   | **Uygulama takma adı** | `Forrest Game` |

5. **"Uygulamayı Kaydet"** butonuna tıklayın.
6. `google-services.json` (Android) ve/veya `GoogleService-Info.plist` (iOS) dosyalarını indirin.

---

## 3. İndirilen Yapılandırma Dosyalarını Projeye Ekleme

İndirdiğiniz dosyaları Unity projesinde doğru konumlara yerleştirin:

```
Assets/
├── google-services.json          ← Android yapılandırması
├── GoogleService-Info.plist      ← iOS yapılandırması
├── Scripts/
│   ├── Managers/
│   │   ├── FirebaseAuthManager.cs
│   │   ├── EconomyManager.cs
│   │   └── UpgradeManager.cs
│   └── Data/
│       └── PlayerData.cs
```

> ⚠️ **ÖNEMLİ:** `google-services.json` ve `GoogleService-Info.plist` dosyaları **Assets klasörünün kök dizinine** yerleştirilmelidir. Alt klasörlere koymayın!

---

## 4. Firebase Unity SDK'sını İndirme ve İçe Aktarma

### 4.1 SDK'yı İndirin

1. [Firebase Unity SDK indirme sayfasına](https://firebase.google.com/download/unity) gidin.
2. En güncel `firebase_unity_sdk_*.zip` dosyasını indirin.
3. ZIP dosyasını bilgisayarınızda bir klasöre çıkartın.

### 4.2 Gerekli Paketleri Unity'e Aktarın

ZIP dosyasının içindeki `dotnet4/` klasöründe birçok `.unitypackage` dosyası bulunur. Projemiz için şu paketleri içe aktarmanız **zorunludur**:

| Paket Dosyası | Açıklama |
|---------------|----------|
| `FirebaseAuth.unitypackage` | Kullanıcı kimlik doğrulama (Authentication) |
| `FirebaseFirestore.unitypackage` | Bulut veritabanı (ileride kullanılacak) |
| `FirebaseAnalytics.unitypackage` | Temel Firebase bağımlılığı (zorunlu) |

**İçe aktarma adımları:**

1. Unity'de menüden: **Assets > Import Package > Custom Package...** seçin.
2. İlk olarak `FirebaseAnalytics.unitypackage` dosyasını seçin ve **Import** butonuna basın.
3. Ardından `FirebaseAuth.unitypackage` dosyasını aynı yöntemle içe aktarın.
4. İsteğe bağlı: `FirebaseFirestore.unitypackage` dosyasını da aktarın.

> 💡 **İPUCU:** İçe aktarma sırasında "All" seçili olsun, hiçbir dosyanın işaretini kaldırmayın.

---

## 5. Anonymous Login (Anonim Giriş) Aktif Etme

1. [Firebase Console](https://console.firebase.google.com/) üzerinden projenize gidin.
2. Sol menüden **Build > Authentication** seçeneğine tıklayın.
3. **"Get Started"** (Başlayın) butonuna basın.
4. **"Sign-in method"** (Oturum açma yöntemi) sekmesine geçin.
5. Listeden **"Anonymous"** (Anonim) sağlayıcısını bulun ve tıklayın.
6. Sağ üstteki **"Enable"** (Etkinleştir) anahtarını açık konuma getirin.
7. **"Save"** (Kaydet) butonuna basın.

---

## 6. Unity Sahnesinde Kurulum

Firebase yöneticisini sahnenize eklemeniz gerekmektedir:

1. Unity'de sahnenizi açın.
2. Hiyerarşide sağ tıklayın: **Create Empty** ile boş bir GameObject oluşturun.
3. Adını `FirebaseAuthManager` olarak değiştirin.
4. Inspector'da **Add Component** butonuna basın ve `FirebaseAuthManager` scriptini ekleyin.

> 💡 **İPUCU:** `EconomyManager` ve `UpgradeManager` için de aynı adımları tekrarlayın veya hepsini tek bir `GameManager` objesine ekleyin.

---

## 7. Test Etme

1. Unity'de **Play** butonuna basın.
2. Console penceresinde şu mesajları sırayla görmelisiniz:

```
FirebaseAuthManager: Firebase bagimliliklari kontrol ediliyor...
FirebaseAuthManager: Firebase basariyla hazirlandi!
FirebaseAuthManager: Misafir girisi yapiliyor...
FirebaseAuthManager: Misafir girisi basarili!
  -> Kullanici ID: abc123xyz...
  -> Anonim mi: True
```

3. Firebase Console'da **Authentication > Users** sekmesinde yeni bir anonim kullanıcı kaydı görünmelidir.

---

## 8. Sık Karşılaşılan Sorunlar

| Sorun | Çözüm |
|-------|-------|
| `DependencyStatus` hatası | Firebase SDK versiyonunun Unity versiyonuyla uyumlu olduğundan emin olun |
| `google-services.json bulunamadı` | Dosyanın `Assets/` kök dizininde olduğunu kontrol edin |
| Anonymous login çalışmıyor | Firebase Console'da Anonymous provider'ın aktif olduğunu doğrulayın |
| `FirebaseApp initialization failed` | İnternet bağlantınızı kontrol edin ve Firewall ayarlarınızı gözden geçirin |

---

## 9. Sonraki Adımlar

- [ ] Firestore entegrasyonu ile oyuncu verilerini buluta kaydetme
- [ ] Google Play / Apple Sign-In ile kalıcı hesap bağlama
- [ ] Çevrimdışı mod desteği ekleme

---

> 📅 Bu rehber `backend-veri-modelleri` branch'inde oluşturulmuştur.
