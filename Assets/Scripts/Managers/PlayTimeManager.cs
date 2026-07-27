using UnityEngine;

public class PlayTimeManager : MonoBehaviour
{
    public static PlayTimeManager Instance { get; private set; }

    private float sessionPlayTime = 0f;

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

    private void OnEnable()
    {
        // OnDataSaved (otomatik kayit) eventine baglanmiyoruz (Firebase'i spamlamamak icin)
    }

    private void OnDisable()
    {
        // Event kaldirildi
    }

    private void Update()
    {
        sessionPlayTime += Time.deltaTime;

        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            CloudSaveManager.Instance.CurrentData.OyunSuresi += Time.deltaTime;
        }
    }

    private void OnApplicationQuit()
    {
        LogSessionPlayTime();
    }

    private void LogSessionPlayTime()
    {
        if (ForrestGame.Managers.AnalyticsManager.Instance != null && sessionPlayTime > 0)
        {
            int sessionMinutes = Mathf.FloorToInt(sessionPlayTime / 60f);
            if (sessionMinutes > 0)
            {
                ForrestGame.Managers.AnalyticsManager.Instance.LogCustomEvent("session_playtime", "minutes", sessionMinutes);
            }
        }
    }
}
