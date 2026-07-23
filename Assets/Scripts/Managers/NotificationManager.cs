using System;
using UnityEngine;

/// <summary>
/// Oyun ici bildirimler (Toast) icin merkezi haberlesme sistemi.
/// UI katmani OnNotificationReceived eventini dinleyerek mesajlari gosterebilir.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    /// <summary>
    /// Herhangi bir sistemden bildirim gonderildiginde tetiklenir.
    /// Parametre: gonderilen mesaj metni.
    /// </summary>
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

    /// <summary>
    /// Verilen mesaji OnNotificationReceived event'i araciligiyla yayinlar.
    /// UI katmanindaki dinleyiciler bu eventi yakalayarak toast/popup gosterebilir.
    /// </summary>
    public void SendNotification(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        Debug.Log($"[NotificationManager] {message}");
        OnNotificationReceived?.Invoke(message);
    }
}
