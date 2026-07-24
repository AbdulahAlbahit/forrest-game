using UnityEngine;
using Firebase;
using Firebase.Analytics;

namespace ForrestGame.Managers
{
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private bool firebaseInitialized = false;

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
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    firebaseInitialized = true;
                    Debug.Log("AnalyticsManager: Firebase Analytics basariyla baslatildi.");
                }
                else
                {
                    Debug.LogError($"AnalyticsManager: Firebase Analytics baslatilamadi: {dependencyStatus}");
                }
            });
        }

        public void LogCustomEvent(string eventName, string parameterName, string parameterValue)
        {
            if (!firebaseInitialized) return;

            FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, parameterValue));
            Debug.Log($"AnalyticsManager: Event gonderildi -> {eventName} | {parameterName}: {parameterValue}");
        }

        public void LogCustomEvent(string eventName, string parameterName, int parameterValue)
        {
            if (!firebaseInitialized) return;

            FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, parameterValue));
            Debug.Log($"AnalyticsManager: Event gonderildi -> {eventName} | {parameterName}: {parameterValue}");
        }

        public void LogCustomEvent(string eventName, string parameterName, float parameterValue)
        {
            if (!firebaseInitialized) return;

            FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, parameterValue));
            Debug.Log($"AnalyticsManager: Event gonderildi -> {eventName} | {parameterName}: {parameterValue}");
        }
    }
}
