using UnityEngine;
using GoogleMobileAds.Api;
using ForrestGame.Utils;
using System;
using System.Collections.Generic;
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

        // Ana thread'de çalıştırılacak aksiyonlar için kuyruk
        // AdMob callback'leri background thread'den gelebildiği için tüm
        // Unity API çağrılarını buraya koyarak main thread'e taşıyoruz.
        private readonly Queue<Action> _mainThreadActions = new Queue<Action>();
        private readonly object _lockObj = new object();

        // Ödüllü reklam ödülü için bekleyen aksiyon
        private bool _pendingReward = false;

        [Header("Ödüllü Reklam Ayarları")]
        public int rewardGold = 100;
        public int rewardWood = 50;

        [Header("Geçiş Reklamı (Otomatik) Ayarları")]
        [Tooltip("Geçiş reklamının kaç saniyede bir çıkacağı (Örn: 600 = 10 dakika)")]
        public float interstitialIntervalSeconds = 600f;
        private float timer = 0f;

        // Reklam gösterilirken timer'ı durdur
        private bool isAdShowing = false;

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
                // Bu callback de background thread'den gelebilir, main thread'e aktar
                EnqueueMainThread(() =>
                {
                    GameLogger.Log("AdManager: Mobile Ads SDK baslatildi.");
                    LoadRewardedAd();
                    LoadInterstitialAd();
                });
            });
        }

        private void Update()
        {
            // --- Main thread kuyruğunu işle ---
            // Bu sayede tüm AdMob callback'leri güvenli şekilde burada çalışır
            lock (_lockObj)
            {
                while (_mainThreadActions.Count > 0)
                {
                    _mainThreadActions.Dequeue()?.Invoke();
                }
            }

            // Ödül bekleniyorsa ver
            if (_pendingReward)
            {
                _pendingReward = false;
                GiveRewardsToPlayer();
            }

            // Reklam gösterilirken sayacı durdur
            if (isAdShowing) return;

            // Zamanlayıcıyı çalıştır
            timer += Time.deltaTime;

            // Eğer süre dolduysa ve geçiş reklamı hazırsa göster
            if (timer >= interstitialIntervalSeconds)
            {
                ShowInterstitialAd();
            }
        }

        // Bir aksiyonu main thread kuyruğuna ekler (thread-safe)
        private void EnqueueMainThread(Action action)
        {
            lock (_lockObj)
            {
                _mainThreadActions.Enqueue(action);
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
                    // Yükleme callback'i de background thread'den gelebilir
                    EnqueueMainThread(() =>
                    {
                        rewardedAd = ad;
                        RegisterRewardedEventHandlers(rewardedAd);
                    });
                });
        }

        private void RegisterRewardedEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                EnqueueMainThread(() =>
                {
                    isAdShowing = true;
                    GameLogger.Log("AdManager: Odullu reklam acildi.");
                });
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                EnqueueMainThread(() =>
                {
                    isAdShowing = false;
                    timer = 0f; // Reklam kapanınca interstitial sayacını sıfırla
                    GameLogger.Log("AdManager: Odullu reklam kapatildi, yeni reklam yukleniyor.");
                    LoadRewardedAd();
                });
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                EnqueueMainThread(() =>
                {
                    isAdShowing = false;
                    GameLogger.LogError("AdManager: Odullu reklam gosterilemedi. Hata: " + error);
                    LoadRewardedAd();
                });
            };
        }

        public void ShowRewardedAd()
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show((Reward reward) =>
                {
                    // Reward callback de background thread'den gelebilir
                    // Unity API'larına doğrudan erişmek yerine flag kullan
                    _pendingReward = true;
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
                    EnqueueMainThread(() =>
                    {
                        interstitialAd = ad;
                        RegisterInterstitialEventHandlers(interstitialAd);
                    });
                });
        }

        private void RegisterInterstitialEventHandlers(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                EnqueueMainThread(() =>
                {
                    isAdShowing = true;
                });
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                EnqueueMainThread(() =>
                {
                    isAdShowing = false;
                    timer = 0f;
                    LoadInterstitialAd();
                });
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                EnqueueMainThread(() =>
                {
                    isAdShowing = false;
                    timer = interstitialIntervalSeconds - 10f;
                    LoadInterstitialAd();
                });
            };
        }

        public void ShowInterstitialAd()
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                interstitialAd.Show();
            }
            else
            {
                // Hazır değilse 10 saniye sonra tekrar denesin
                timer = interstitialIntervalSeconds - 10f;
                LoadInterstitialAd();
            }
        }
    }
}
