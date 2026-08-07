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
                AudioManager.Instance.PlayMusic("ArkaPlan"); 
                Debug.Log("arkaplan sesi çalışıyor.");
            }

            // 'S' tuşuna basınca Müziği Durdurur
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                AudioManager.Instance.StopMusic();
                Debug.Log("ilkbahar sesi Durdu.");
            }


            // --- KISA EFEKTLER (SFX) ---
            
            // 1. SES: 'A' tuşuna basınca
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("BaltaVurma");
                Debug.Log("Balta sesi çalışıyor.");
            }
            
            // 2. SES: 'C' tuşuna basınca
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("OdunToplama");
                Debug.Log("Odun sesi çalışıyor.");
            }

            // 3. SES: 'Space' (Boşluk) tuşuna basınca
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("Button0"); // İsmi kafama göre yazdım, kendi isminle değiştir.
                Debug.Log("Button0 sesi çalışıyor.");
            }

            // 4. SES: 'W' tuşuna basınca (Örnek olarak ekledim)
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                AudioManager.Instance.PlaySFX("Button1");
                Debug.Log("Button1 sesi çalışıyor.");
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
