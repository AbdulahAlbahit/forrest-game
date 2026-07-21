# 🎵 Forrest Game - Ses Sistemi & Unity Temelleri Rehberi

Bu doküman, projeye seslerin nasıl ekleneceğini ve Unity'deki temel kavramları (Sahne, Script, Prefab, Çakışma) özetlemek amacıyla oluşturulmuştur.

---

## 🎭 1. Unity Temel Kavramları
* **Sahne (Scene):** Oyunun geçtiği ortamın ta kendisidir (Tiyatro sahnesi veya film seti gibi). İçinde 3D modeller, kameralar ve ışıklar bulunur.
* **Script:** Oyundaki cansız objelere mantık, hareket ve kurallar ekleyen kod dosyalarıdır. Objelere takılan bir "beyin" veya oyuncu senaryosu gibi düşünülebilir.

## ⚔️ 2. Kod Çakışması (Merge Conflict) Nedir?
İki ekip üyesinin aynı anda aynı script dosyasının (örn: `PlayerController.cs`) aynı satırlarını değiştirmesi durumunda ortaya çıkar. Bunu önlemek için "Modüler" çalışmalı ve başkalarının kod dosyalarını değiştirmek yerine, kendi sistemlerimizi bağımsız bir **Prefab** olarak hazırlamalıyız.

---

## 🎧 3. Ses Dosyalarını Projeye Eklemek
1. **Kısa Efektler (SFX):** Balta vuruşu, odun kırılma, para sesi gibi anlık sesleri `Assets/Audio/SFX/` klasörüne sürükleyin. (Önerilen Format: `.wav`)
2. **Uzun Müzikler (Music):** Arka plan, mevsim müzikleri gibi döngüsel sesleri `Assets/Audio/Music/` klasörüne sürükleyin. (Önerilen Format: `.mp3` veya `.ogg`)

---

## 📦 4. AudioManager Prefab'ı Nasıl Kurulur? (Adım Adım)
Kod çakışmasını önlemek için ses sistemimizi bir "Hazır Paket" (Prefab) haline getiriyoruz.

1. **Boş Obje Oluşturma:** Hierarchy penceresine (soldaki liste) sağ tıklayıp `Create Empty` deyin. Adını `AudioManager` yapın.
2. **Bileşenleri Ekleme:** Obje seçiliyken sağdaki Inspector panelinden `Add Component` butonuna tıklayıp **2 adet `Audio Source`** ve **1 adet `AudioManager`** scripti ekleyin.
3. **Kablolama:** Eklediğiniz iki `Audio Source` bileşenini farenizle sürükleyerek, scriptin içindeki `Music Source` ve `Sfx Source` kutularına bırakın.
4. **Sesleri Tanıtma:** Scriptin içindeki `Sfx Sounds` ve `Music Sounds` listelerinin yanındaki oka tıklayın. `+` butonuna (veya Size kısmına) basarak yeni eleman ekleyin. Elemana bir isim (Örn: `AxeHit`) verin ve klasördeki ses dosyanızı `Clip` yazan yere sürükleyin.
5. **Prefab Yapma (Paketleme):** Hierarchy'deki `AudioManager` objesini farenizle tutup aşağıdaki `Assets/Prefabs/` klasörünün içine sürükleyin. Mavi bir küp ikonuna dönüştüğünde işlem tamamdır!

---

## 🔧 5. Sonradan Yeni Ses Eklemek (Prefab Mode)
Projeye sonradan yeni bir müzik veya efekt eklemek isterseniz:
1. `Assets/Prefabs/` klasöründeki mavi `AudioManager` ikonuna **çift tıklayın**. (Ekran grileşecek ve "Prefab Modu"na gireceksiniz).
2. Tıpkı 4. adımda yaptığınız gibi listedeki `+` butonuna basıp yeni sesinizi ekleyin.
3. Sol üst köşedeki sol ok (`<`) işaretine tıklayarak çıkın. Yaptığınız değişiklik otomatik kaydedilir ve tüm ekip arkadaşlarınıza (GitHub üzerinden) hazır olarak gider!

*Efe'nin yapması gereken tek şey, senin hazırladığın bu mavi Prefab küpünü kendi sahnesine sürükleyip bırakmaktır!*
