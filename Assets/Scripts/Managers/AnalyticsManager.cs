using UnityEngine;
using ForrestGame.Utils;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;

namespace ForrestGame.Managers
{
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private bool firebaseInitialized = false;
        private float sessionStartTime;

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
                }
                else
                {
                    GameLogger.LogError($"AnalyticsManager: Firebase Analytics baslatilamadi: {dependencyStatus}");
                }
            });
        }

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

        private void OnApplicationPause(bool isPaused)
        {
            if (!firebaseInitialized) return;

            if (isPaused)
            {
                float sessionMinutes = (Time.realtimeSinceStartup - sessionStartTime) / 60f;
                LogCustomEvent("game_session_end", "session_minutes", sessionMinutes);
            }
            else
            {
                sessionStartTime = Time.realtimeSinceStartup;
            }
        }

        private void OnApplicationQuit()
        {
            if (!firebaseInitialized) return;

            float sessionMinutes = (Time.realtimeSinceStartup - sessionStartTime) / 60f;
            LogCustomEvent("game_session_end", "session_minutes", sessionMinutes);
        }
    }
}
