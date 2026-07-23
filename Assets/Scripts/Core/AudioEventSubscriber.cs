using UnityEngine;
using ForrestGame.Core;

namespace ForrestGame.Audio
{
    /// <summary>
    /// Efe'nin oynanış sistemini (EfeOynanisEventleri) dinleyerek,
    /// hiçbir oyun koduna müdahale etmeden Yasir'in AudioManager'ını çalıştıran sistem.
    /// </summary>
    public class AudioEventSubscriber : MonoBehaviour
    {
        private void OnEnable()
        {
            // Olayları Dinlemeye Başla (Radyo Alıcısını Aç)
            EfeOynanisEventleri.BaltaSallandi += OnBaltaSallandi;
            EfeOynanisEventleri.BaltaAgacaVurdu += OnBaltaAgacaVurdu;
            
            // TODO: Efe kendi koduna "AgacYikildi" anonsunu eklediğinde aşağıdaki satırın başındaki // işaretlerini sil.
            // EfeOynanisEventleri.AgacYikildi += OnAgacYikildi;
            
            EfeOynanisEventleri.OdunToplandi += OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu += OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi += OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi += OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti += OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketBasladi += OnHareketBasladi;
            EfeOynanisEventleri.KosuBasladi += OnKosuBasladi;

            // --- LOOP SESLERİ İÇİN DİNLEYİCİLER (Hazır olduğunda başlarındaki // silinecek) ---
            // TODO: Mevsim ve oyun yöneticisi (SeasonManager vb.) eventleri yazdığında aktif et:
            // OyunEventleri.OyunBasladi += OnOyunBasladi;
            // MevsimEventleri.KisBasladi += OnKisBasladi;
            // MevsimEventleri.YazBasladi += OnYazBasladi;
            // MevsimEventleri.SonbaharBasladi += OnSonbaharBasladi;
            // MevsimEventleri.IlkbaharBasladi += OnIlkbaharBasladi;
            
            // TODO: Efe araba/kamyon motoru için event yazdığında aktif et:
            // EfeOynanisEventleri.KamyonCalisti += OnKamyonCalisti;
            // EfeOynanisEventleri.KamyonDurdu += OnKamyonDurdu;
        }

        private void OnDisable()
        {
            // Olayları Dinlemeyi Bırak (Hata çıkmasını önlemek için Alıcıyı Kapat)
            EfeOynanisEventleri.BaltaSallandi -= OnBaltaSallandi;
            EfeOynanisEventleri.BaltaAgacaVurdu -= OnBaltaAgacaVurdu;
            
            // TODO: Efe eventi eklediğinde burayı da aktif et.
            // EfeOynanisEventleri.AgacYikildi -= OnAgacYikildi;
            
            EfeOynanisEventleri.OdunToplandi -= OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu -= OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi -= OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi -= OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti -= OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketBasladi -= OnHareketBasladi;
            EfeOynanisEventleri.KosuBasladi -= OnKosuBasladi;

            // OyunEventleri.OyunBasladi -= OnOyunBasladi;
            // MevsimEventleri.KisBasladi -= OnKisBasladi;
            // MevsimEventleri.YazBasladi -= OnYazBasladi;
            // MevsimEventleri.SonbaharBasladi -= OnSonbaharBasladi;
            // MevsimEventleri.IlkbaharBasladi -= OnIlkbaharBasladi;
            
            // EfeOynanisEventleri.KamyonCalisti -= OnKamyonCalisti;
            // EfeOynanisEventleri.KamyonDurdu -= OnKamyonDurdu;
        }

        #region Ses Çalma Metotları

        private void OnBaltaSallandi(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("BaltaSallama");
        }

        private void OnBaltaAgacaVurdu(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("BaltaVurma");
        }

        private void OnAgacYikildi(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("AgacDusme");
        }

        private void OnOdunToplandi(Transform kaynak, int mevcut, int kapasite)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("OdunToplama");
        }

        private void OnSepetDoldu(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("SepetDoldu");
        }

        private void OnKamyonYuklemeBasladi(Transform kaynak)
        {
            // İsteğe bağlı: Kamyona yükleme başladığı an bir ses çıkabilir
            // if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("KamyonKapagiAcildi");
        }

        private void OnOdunKamyonaYuklendi(Transform kaynak, int mevcut, int kapasite)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("KamyonaOdunAtma");
        }

        private void OnKamyonYuklemeBitti(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("KamyonDoldu");
        }

        private void OnHareketBasladi(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("YurumeSesi");
        }

        private void OnKosuBasladi(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("KosmaSesi");
        }

        #endregion

        #region Loop (Döngü) Müzik ve Motor Sesleri
        
        // Sesin türü ne olursa olsun (SFX, Loop Müzik, Motor Sesi) buradan çağırabiliriz.
        // AudioManager zaten PlayMusic ve PlayVehicle fonksiyonlarında "loop=true" yapıyor.

        private void OnOyunBasladi()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("ArkaPlanMuzik");
        }

        private void OnKisBasladi()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("KisMuzik");
        }

        private void OnYazBasladi()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("YazMuzik");
        }

        private void OnSonbaharBasladi()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("SonbaharMuzik");
        }

        private void OnIlkbaharBasladi()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("IlkbaharMuzik");
        }

        private void OnKamyonCalisti(Transform kaynak)
        {
            // Motor sesini başlatır ve sonsuza kadar döngüye alır (loop)
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVehicle("ArabaMotoru");
        }

        private void OnKamyonDurdu(Transform kaynak)
        {
            // Motoru durdurur
            if (AudioManager.Instance != null) AudioManager.Instance.StopVehicle();
        }

        #endregion
    }
}
