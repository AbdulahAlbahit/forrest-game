using UnityEngine;

namespace ForrestGame.Data
{
    [CreateAssetMenu(fileName = "UpgradeSettings", menuName = "Forrest Game/Upgrade Settings")]
    public class UpgradeSettings : ScriptableObject
    {
        [Header("Balta (Axe) Geliştirmeleri")]
        public int baltaMaksimumSeviye = 50; // Sınır Koyduk!
        public float baltaTabanHasar = 10f;
        public float baltaHasarCarpani = 1.2f;
        public int baltaTabanMaliyet = 100;
        public float baltaMaliyetCarpani = 1.5f;

        [Header("Sepet (Basket) Geliştirmeleri")]
        public int sepetMaksimumSeviye = 50; // Sınır Koyduk!
        public int sepetTabanKapasite = 10;
        public int sepetKapasiteArtisi = 5; 
        public int sepetTabanMaliyet = 50;
        public float sepetMaliyetCarpani = 1.3f;

        [Header("Kamyon (Truck) Geliştirmeleri")]
        public int kamyonMaksimumSeviye = 20;
        [Tooltip("Kamyonun kasasının alacağı maksimum odun sayısı")]
        public int kamyonTabanKapasite = 50;
        public int kamyonKapasiteArtisi = 25; 
        public int kamyonTabanMaliyet = 500;
        public float kamyonMaliyetCarpani = 1.8f;

        [Header("Atölye (Workshop) Geliştirmeleri")]
        public int atolyeMaksimumSeviye = 10;
        [Tooltip("Atölyede aynı anda kaç işçi/mobilya yuvası çalışacak?")]
        public int atolyeTabanUretimKapasitesi = 1; 
        public int atolyeKapasiteArtisi = 1; // Her seviyede +1 mobilya yuvası
        public int atolyeTabanMaliyet = 1000;
        public float atolyeMaliyetCarpani = 2.0f;

        // --- HESAPLAMA FONKSİYONLARI ---

        public int GetBaltaMaliyet(int seviye) => Mathf.RoundToInt(baltaTabanMaliyet * Mathf.Pow(baltaMaliyetCarpani, seviye - 1));
        public float GetBaltaHasar(int seviye) => baltaTabanHasar * Mathf.Pow(baltaHasarCarpani, seviye - 1);

        public int GetSepetMaliyet(int seviye) => Mathf.RoundToInt(sepetTabanMaliyet * Mathf.Pow(sepetMaliyetCarpani, seviye - 1));
        public int GetSepetKapasite(int seviye) => sepetTabanKapasite + (sepetKapasiteArtisi * (seviye - 1));

        public int GetKamyonMaliyet(int seviye) => Mathf.RoundToInt(kamyonTabanMaliyet * Mathf.Pow(kamyonMaliyetCarpani, seviye - 1));
        public int GetKamyonKapasite(int seviye) => kamyonTabanKapasite + (kamyonKapasiteArtisi * (seviye - 1));

        public int GetAtolyeMaliyet(int seviye) => Mathf.RoundToInt(atolyeTabanMaliyet * Mathf.Pow(atolyeMaliyetCarpani, seviye - 1));
        public int GetAtolyeKapasite(int seviye) => atolyeTabanUretimKapasitesi + (atolyeKapasiteArtisi * (seviye - 1));
    }
}
