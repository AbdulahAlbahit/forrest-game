using System;
using UnityEngine;
using ForrestGame.Utils;





public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }





    public static event Action<string> OnNotificationReceived;

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





    public void SendNotification(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        GameLogger.Log($"[NotificationManager] {message}");
        OnNotificationReceived?.Invoke(message);
        NotificationUIController.Instance?.ShowNotification(message);
    }
}
