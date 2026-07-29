using UnityEngine;
using ForrestGame.Utils;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using System;
using UnityEngine.InputSystem;

namespace ForrestGame.Managers
{
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private bool firebaseInitialized = false;
        private float sessionStartTime;
        private DateTime sessionStartDateTime;

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
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    firebaseInitialized = true;
                    GameLogger.Log("AnalyticsManager: Firebase Analytics basariyla baslatildi.");

                    string todayString = System.DateTime.Now.ToString("yyyy-MM-dd");
                    string lastLoginDate = PlayerPrefs.GetString("LastLoginDate", "");
                    int dailySessionCount = PlayerPrefs.GetInt("DailySessionCount", 0);

                    if (lastLoginDate != todayString)
                    {
                        dailySessionCount = 1;
                        PlayerPrefs.SetString("LastLoginDate", todayString);
                    }
                    else
                    {
                        dailySessionCount++;
                    }

                    PlayerPrefs.SetInt("DailySessionCount", dailySessionCount);
                    PlayerPrefs.Save();

                    LogCustomEvent("game_session_start", "daily_session_count", dailySessionCount);
                    sessionStartTime = Time.realtimeSinceStartup;
                    sessionStartDateTime = DateTime.Now;
                }
                else
                {
                    GameLogger.LogError($"AnalyticsManager: Firebase Analytics baslatilamadi: {dependencyStatus}");
                }
            });
        }

        // ── Gecici Test Tuslari (sadece Editor'de aktif) ──
#if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                GameLogger.Log("AnalyticsManager: [TEST] X tusuna basildi -> LogUserLogin cagriliyor.");
                LogUserLogin();
            }

            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                GameLogger.Log("AnalyticsManager: [TEST] Y tusuna basildi -> LogAdImpression cagriliyor.");
                LogAdImpression();
            }

            if (Keyboard.current.zKey.wasPressedThisFrame)
            {
                GameLogger.Log("AnalyticsManager: [TEST] Z tusuna basildi -> LogSessionEnd cagriliyor.");
                LogSessionEnd();
            }
        }
#endif

        public void LogCustomEvent(string eventName, string parameterName, string parameterValue)
        {
            if (!firebaseInitialized) return;

            FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, parameterValue));
            GameLogger.Log($"AnalyticsManager: Event gonderildi -> {eventName} | {parameterName}: {parameterValue}");
        }

        public void LogCustomEvent(string eventName, string parameterName, int parameterValue)
        {
            if (!firebaseInitialized) return;

            FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, parameterValue));
            GameLogger.Log($"AnalyticsManager: Event gonderildi -> {eventName} | {parameterName}: {parameterValue}");
        }

        public void LogCustomEvent(string eventName, string parameterName, float parameterValue)
        {
            if (!firebaseInitialized) return;

            FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, parameterValue));
            GameLogger.Log($"AnalyticsManager: Event gonderildi -> {eventName} | {parameterName}: {parameterValue}");
        }

        /// <summary>
        /// Firebase standart login eventini tetikler.
        /// </summary>
        public void LogUserLogin(string loginMethod = "guest")
        {
            if (!firebaseInitialized)
            {
                GameLogger.LogWarning("AnalyticsManager: Firebase henuz hazir degil, login eventi gonderilemedi.");
                return;
            }

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLogin,
                new Parameter(FirebaseAnalytics.ParameterMethod, loginMethod));
            GameLogger.Log($"AnalyticsManager: Login eventi gonderildi -> method: {loginMethod}");
        }

        /// <summary>
        /// Firebase standart reklam gosterim eventini tetikler.
        /// </summary>
        public void LogAdImpression(string adNetwork = "dummy_network")
        {
            if (!firebaseInitialized)
            {
                GameLogger.LogWarning("AnalyticsManager: Firebase henuz hazir degil, ad impression eventi gonderilemedi.");
                return;
            }

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAdImpression,
                new Parameter("ad_network", adNetwork));
            GameLogger.Log($"AnalyticsManager: Ad impression eventi gonderildi -> network: {adNetwork}");
        }

        /// <summary>
        /// Oturum bitis eventini gonderir, sure dakika cinsinden hesaplanir.
        /// </summary>
        private void LogSessionEnd()
        {
            if (!firebaseInitialized) return;

            double sessionLengthMinutes = (DateTime.Now - sessionStartDateTime).TotalMinutes;

            FirebaseAnalytics.LogEvent("game_session_end",
                new Parameter("session_length_minutes", sessionLengthMinutes));
            GameLogger.Log($"AnalyticsManager: game_session_end gonderildi -> session_length_minutes: {sessionLengthMinutes:F2}");
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!firebaseInitialized) return;

            if (isPaused)
            {
                LogSessionEnd();
            }
            else
            {
                sessionStartTime = Time.realtimeSinceStartup;
                sessionStartDateTime = DateTime.Now;
            }
        }

        private void OnApplicationQuit()
        {
            LogSessionEnd();
        }
    }
}
