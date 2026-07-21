using UnityEngine;
using UnityEngine.InputSystem; 

namespace ForrestGame.Core
{
    /// <summary>
    /// YASİR'İN TEST ALANI
    /// Klavyedeki tuşlara basarak AudioManager'ı test etmeni sağlar.
    /// </summary>
    public class AudioTester : MonoBehaviour
    {
        void Update()
        {
            if (Keyboard.current == null) return;

            // --- MÜZİKLER ---

            // 'M' tuşuna basınca Müzik Çalar
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlayMusic("yağmur"); 
                Debug.Log("yağmur sesi çalışıyor.");
            }

            // 'S' tuşuna basınca Müziği Durdurur
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                AudioManager.Instance.StopMusic();
            }


            // --- KISA EFEKTLER (SFX) ---
            
            // 1. SES: 'A' tuşuna basınca
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("Balta vuruşu"); 
            }
            
            // 2. SES: 'C' tuşuna basınca
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("Ağaç yıkılma"); 
            }

            // 3. SES: 'Space' (Boşluk) tuşuna basınca
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("KamyonKornasi"); // İsmi kafama göre yazdım, kendi isminle değiştir.
            }

            // 4. SES: 'W' tuşuna basınca (Örnek olarak ekledim)
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("TestereSesi"); 
            }

            /* 
             * YENİ SES EKLEMEK İSTERSEN NASIL YAPACAKSIN?
             * Yukarıdaki "if" bloklarından birini kopyala, hemen altına yapıştır.
             * "wKey" yazan yeri "eKey", "rKey", "enterKey" gibi başka bir tuş yap.
             * Tırnak içindeki ismi kendi eklediğin yeni sesin ismiyle değiştir. Hepsi bu!
             */
        }
    }
}
