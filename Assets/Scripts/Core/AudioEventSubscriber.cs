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
            EfeOynanisEventleri.OdunToplandi += OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu += OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi += OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi += OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti += OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketBasladi += OnHareketBasladi;
            EfeOynanisEventleri.KosuBasladi += OnKosuBasladi;
        }

        private void OnDisable()
        {
            // Olayları Dinlemeyi Bırak (Hata çıkmasını önlemek için Alıcıyı Kapat)
            EfeOynanisEventleri.BaltaSallandi -= OnBaltaSallandi;
            EfeOynanisEventleri.BaltaAgacaVurdu -= OnBaltaAgacaVurdu;
            EfeOynanisEventleri.OdunToplandi -= OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu -= OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi -= OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi -= OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti -= OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketBasladi -= OnHareketBasladi;
            EfeOynanisEventleri.KosuBasladi -= OnKosuBasladi;
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
            // Yürüme ayak sesini sürekli olarak çalmak için Ambiyans/Araç kaynağı gibi loop bir kaynak kullanılabilir.
            // Şimdilik kısa bir ses ise PlaySFX ile de çalınabilir.
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("YurumeSesi");
        }

        private void OnKosuBasladi(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("KosmaSesi");
        }

        #endregion
    }
}
