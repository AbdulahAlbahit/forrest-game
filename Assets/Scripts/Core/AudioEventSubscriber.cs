using UnityEngine;
using ForrestGame.Core;
using System.Collections;

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
            
            TreeManager.OnTreeFelledAction += OnAgacYikildi;
            
            EfeOynanisEventleri.OdunToplandi += OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu += OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi += OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi += OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti += OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketDurumuDegisti += OnHareketDurumuDegisti;

            // --- LOOP SESLERİ İÇİN DİNLEYİCİLER (Hazır olduğunda başlarındaki // silinecek) ---
            // TODO: Mevsim ve oyun yöneticisi (SeasonManager vb.) eventleri yazdığında aktif et:
            // OyunEventleri.OyunBasladi += OnOyunBasladi;
            // MevsimEventleri.KisBasladi += OnKisBasladi;
            // MevsimEventleri.YazBasladi += OnYazBasladi;
            // MevsimEventleri.SonbaharBasladi += OnSonbaharBasladi;
            // MevsimEventleri.IlkbaharBasladi += OnIlkbaharBasladi;
            
            TruckRouteController.OnTruckDepartedHome += OnKamyonCalisti;
            TruckRouteController.OnTruckDeparture += OnKamyonCalisti;
            TruckRouteController.OnTruckDepartedMarket += OnKamyonCalisti;

            TruckRouteController.OnTruckArrival += OnKamyonDurdu;
            TruckRouteController.OnMarketSale += OnKamyonDurdu;
            TruckRouteController.OnTruckArrivedHome += OnKamyonDurdu;
        }

        private void OnDisable()
        {
            // Olayları Dinlemeyi Bırak (Hata çıkmasını önlemek için Alıcıyı Kapat)
            EfeOynanisEventleri.BaltaSallandi -= OnBaltaSallandi;
            EfeOynanisEventleri.BaltaAgacaVurdu -= OnBaltaAgacaVurdu;
            
            TreeManager.OnTreeFelledAction -= OnAgacYikildi;
            
            EfeOynanisEventleri.OdunToplandi -= OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu -= OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi -= OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi -= OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti -= OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketDurumuDegisti -= OnHareketDurumuDegisti;

            // OyunEventleri.OyunBasladi -= OnOyunBasladi;
            // MevsimEventleri.KisBasladi -= OnKisBasladi;
            // MevsimEventleri.YazBasladi -= OnYazBasladi;
            // MevsimEventleri.SonbaharBasladi -= OnSonbaharBasladi;
            // MevsimEventleri.IlkbaharBasladi -= OnIlkbaharBasladi;
            
            TruckRouteController.OnTruckDepartedHome -= OnKamyonCalisti;
            TruckRouteController.OnTruckDeparture -= OnKamyonCalisti;
            TruckRouteController.OnTruckDepartedMarket -= OnKamyonCalisti;

            TruckRouteController.OnTruckArrival -= OnKamyonDurdu;
            TruckRouteController.OnMarketSale -= OnKamyonDurdu;
            TruckRouteController.OnTruckArrivedHome -= OnKamyonDurdu;
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

        private void OnAgacYikildi(Vector3 agacPozisyonu)
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

        private Coroutine hareketSesCoroutine;

        private void OnHareketDurumuDegisti(Transform kaynak, EfeHareketDurumu durum)
        {
            if (AudioManager.Instance == null) return;

            // Mevcut hareket sesini durdur (yeni hıza geçiş yaparken veya tamamen dururken)
            if (hareketSesCoroutine != null)
            {
                StopCoroutine(hareketSesCoroutine);
                hareketSesCoroutine = null;
            }

            if (durum == EfeHareketDurumu.Yuruyor)
            {
                hareketSesCoroutine = StartCoroutine(HareketSesiCal("YurumeSesi"));
            }
            else if (durum == EfeHareketDurumu.Kosuyor)
            {
                hareketSesCoroutine = StartCoroutine(HareketSesiCal("KosmaSesi"));
            }
        }

        private IEnumerator HareketSesiCal(string sesAdi)
        {
            Sound s = System.Array.Find(AudioManager.Instance.sfxSounds, x => x.name == sesAdi);
            
            if (s == null || s.clip == null)
            {
                Debug.LogWarning($"[AudioEventSubscriber] {sesAdi} bulunamadı veya AudioClip atanmamış!");
                yield break;
            }

            // Mükemmel döngü: Dosyanın kendi saniye uzunluğunu alıyoruz
            float beklemeSuresi = s.clip.length;

            while (true)
            {
                AudioManager.Instance.PlaySFX(sesAdi);
                yield return new WaitForSeconds(beklemeSuresi);
            }
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

        private void OnKamyonCalisti()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVehicle("ArabaMotoru");
        }

        private void OnKamyonDurdu()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.StopVehicle();
        }

        #endregion
    }
}
