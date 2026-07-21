using UnityEngine;
using ForrestGame.Data;

namespace ForrestGame.Core
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance;

        [Header("Ayarlar")]
        public UpgradeSettings ayarlar; 
        
        [Header("Veriler")]
        public PlayerData playerData; 

        private void Awake()
        {
            if (Instance == null) Instance = this;
            
            if (playerData == null)
            {
                playerData = new PlayerData();
                playerData.Para = 5000; // Test için paramızı artırdım
            }
        }

        public void BaltaGelistir()
        {
            // Sınır Kontrolü
            if (playerData.BaltaSeviyesi >= ayarlar.baltaMaksimumSeviye)
            {
                Debug.LogWarning("[Upgrade] Balta MAKSİMUM seviyede!");
                return; // Kodu burada kes, aşağı inme
            }

            int maliyet = ayarlar.GetBaltaMaliyet(playerData.BaltaSeviyesi);
            SatinAl(maliyet, () => {
                playerData.BaltaSeviyesi++;
                Debug.Log($"[Upgrade] Balta Seviye {playerData.BaltaSeviyesi} oldu!");
            });
        }

        public void SepetGelistir()
        {
            if (playerData.SepetSeviyesi >= ayarlar.sepetMaksimumSeviye)
            {
                Debug.LogWarning("[Upgrade] Sepet MAKSİMUM seviyede!");
                return;
            }

            int maliyet = ayarlar.GetSepetMaliyet(playerData.SepetSeviyesi);
            SatinAl(maliyet, () => {
                playerData.SepetSeviyesi++;
                Debug.Log($"[Upgrade] Sepet Seviye {playerData.SepetSeviyesi} oldu!");
            });
        }

        public void KamyonGelistir()
        {
            if (playerData.KamyonSeviyesi >= ayarlar.kamyonMaksimumSeviye)
            {
                Debug.LogWarning("[Upgrade] Kamyon MAKSİMUM seviyede!");
                return;
            }

            int maliyet = ayarlar.GetKamyonMaliyet(playerData.KamyonSeviyesi);
            SatinAl(maliyet, () => {
                playerData.KamyonSeviyesi++;
                Debug.Log($"[Upgrade] Kamyon Seviye {playerData.KamyonSeviyesi} oldu!");
            });
        }

        public void AtolyeGelistir()
        {
            if (playerData.AtolyeSeviyesi >= ayarlar.atolyeMaksimumSeviye)
            {
                Debug.LogWarning("[Upgrade] Atölye MAKSİMUM seviyede!");
                return;
            }

            int maliyet = ayarlar.GetAtolyeMaliyet(playerData.AtolyeSeviyesi);
            SatinAl(maliyet, () => {
                playerData.AtolyeSeviyesi++;
                Debug.Log($"[Upgrade] Atölye Seviye {playerData.AtolyeSeviyesi} oldu!");
            });
        }

        // --- ORTAK SATIN ALMA FONKSİYONU ---
        // Kodu 4 kez tekrar yazmamak için ortak bir para düşme mekanizması kurduk.
        private void SatinAl(int maliyet, System.Action onBasarili)
        {
            if (playerData.Para >= maliyet)
            {
                playerData.Para -= maliyet;
                onBasarili?.Invoke(); // Seviye artırma işlemini tetikle
                
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UpgradeSesi"); 
                // TODO: Abdullah Firebase kaydını buraya ekleyecek.
            }
            else
            {
                Debug.LogWarning($"[Upgrade] YETERSİZ PARA! Gereken: {maliyet}, Sende Olan: {playerData.Para}");
            }
        }
    }
}
