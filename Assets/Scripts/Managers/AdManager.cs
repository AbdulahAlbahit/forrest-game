using UnityEngine;
using GoogleMobileAds.Api;
using ForrestGame.Utils;
using System;
using ForrestGame.Core;

namespace ForrestGame.Managers
{
    [DefaultExecutionOrder(-40)] // Initialize after CloudSaveManager and EconomyManager
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        // --- REKLAM KİMLİKLERİ ---
        // YAYINLAMADAN ÖNCE KENDİ KİMLİKLERİNİZLE DEĞİŞTİRİN
        
        // 1. Ödüllü Reklam Kimliği (Sizin Kimliğiniz: ca-app-pub-2250518655969980/3107518798)
#if UNITY_ANDROID
        private string _rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // Google Test Kimliği
#else
        private string _rewardedAdUnitId = "unused";
#endif

        // 2. Geçiş (Interstitial) Reklam Kimliği (AdMob'dan alıp buraya gireceksiniz)
#if UNITY_ANDROID
        private string _interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // Google Test Kimliği
#else
        private string _interstitialAdUnitId = "unused";
#endif

        // --- DEĞİŞKENLER ---
        private RewardedAd rewardedAd;
        private InterstitialAd interstitialAd;

        [Header("Ödüllü Reklam Ayarları")]
        public int rewardGold = 100;
        public int rewardWood = 50;

        [Header("Geçiş Reklamı (Otomatik) Ayarları")]
        [Tooltip("Geçiş reklamının kaç saniyede bir çıkacağı (Örn: 120 = 2 dakika)")]
        public float interstitialIntervalSeconds = 120f;
        private float timer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Google Mobile Ads SDK'yı başlat
            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                GameLogger.Log("AdManager: Mobile Ads SDK baslatildi.");
                
                // İki reklam türünü de arka planda yükleyip hazırla
                LoadRewardedAd();
                LoadInterstitialAd();
            });
        }

        private void Update()
        {
            // Zamanlayıcıyı çalıştır
            timer += Time.deltaTime;

            // Eğer 2 dakika dolduysa ve geçiş reklamı hazırsa göster
            if (timer >= interstitialIntervalSeconds)
            {
                ShowInterstitialAd();
            }
        }

        // ==========================================
        // 1. ÖDÜLLÜ REKLAM SİSTEMİ (REWARDED)
        // ==========================================

        public void LoadRewardedAd()
        {
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            var adRequest = new AdRequest();
            RewardedAd.Load(_rewardedAdUnitId, adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        GameLogger.LogError("AdManager: Odullu reklam yuklenemedi. Hata: " + error);
                        return;
                    }
                    rewardedAd = ad;
                    RegisterEventHandlers(rewardedAd);
                });
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                LoadRewardedAd(); // Oyuncu reklamı kapattığında yenisini yükle
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                LoadRewardedAd();
            };
        }

        public void ShowRewardedAd()
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show((Reward reward) =>
                {
                    // Oyuncu reklamı sonuna kadar izledi, ödülü ver
                    GiveRewardsToPlayer();
                });
            }
            else
            {
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.SendNotification("Reklam henüz hazır değil, lütfen biraz bekleyin.");
                LoadRewardedAd();
            }
        }

        private void GiveRewardsToPlayer()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.AddMoney(rewardGold);

            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
            {
                CloudSaveManager.Instance.CurrentData.PlayerWoodStock += rewardWood;
                CloudSaveManager.Instance.SaveData(CloudSaveManager.Instance.CurrentData);
            }

            if (NotificationManager.Instance != null)
                NotificationManager.Instance.SendNotification($"Reklam izlendi! +{rewardGold} Altın, +{rewardWood} Odun kazandınız!");
        }

        // ==========================================
        // 2. GEÇİŞ REKLAMI SİSTEMİ (INTERSTITIAL)
        // ==========================================

        public void LoadInterstitialAd()
        {
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            var adRequest = new AdRequest();
            InterstitialAd.Load(_interstitialAdUnitId, adRequest,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        GameLogger.LogError("AdManager: Gecis reklami yuklenemedi. Hata: " + error);
                        return;
                    }
                    interstitialAd = ad;
                    RegisterEventHandlers(interstitialAd);
                });
        }

        private void RegisterEventHandlers(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                // Reklam kapatıldığında sayacı sıfırla ve yeni reklam yükle
                timer = 0f;
                LoadInterstitialAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                // Hata olursa yine de sayacı sıfırla ki oyuncu peş peşe hatayla boğuşmasın
                timer = 0f;
                LoadInterstitialAd();
            };
        }

        public void ShowInterstitialAd()
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                interstitialAd.Show();
                // Not: Sayfayı kapattığında OnAdFullScreenContentClosed çalışacak ve timer sıfırlanacak.
                // Eğer burada sıfırlarsak reklam izlenirken de süre saymaya devam eder.
            }
            else
            {
                // Hazır değilse süreyi biraz geri al (örneğin 10 saniye sonra tekrar denesin)
                timer = interstitialIntervalSeconds - 10f;
                LoadInterstitialAd();
            }
        }
    }
}
