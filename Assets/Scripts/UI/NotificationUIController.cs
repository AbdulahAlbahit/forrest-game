using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyun genelinde tost/pop-up bildirimlerini yöneten arayüz script'i.
/// Ekranda ufak bir görsel (Image) ve yazı (TextMeshProUGUI) ile kısa süreli bildirim gösterir (Örn: "Yetersiz Odun!").
///
/// KULLANIM:
/// 1. CanvasUI altına küçük bir Bildirim Paneli (Image + Text) ekleyin.
/// 2. Bu scripti o objeye ekleyin ve NotificationUIController.Instance.ShowNotification("Mesajiniz") şeklinde çağırın.
/// </summary>
public class NotificationUIController : MonoBehaviour
{
    public static NotificationUIController Instance { get; private set; }

    [Header("--- UI Elemanları ---")]
    [Tooltip("Bildirim arka plan paneli/görseli (Image).")]
    [SerializeField] private GameObject notificationPanel;

    [Tooltip("Bildirim mesajı metni.")]
    [SerializeField] private TextMeshProUGUI notificationText;

    [Header("--- Ayarlar ---")]
    [Tooltip("Varsayılan ekranda kalma süresi (saniye).")]
    [SerializeField] private float defaultDuration = 2f;

    private Coroutine currentNoticeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Başlangıçta paneli gizle
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    private void OnEnable()
    {
        NotificationManager.OnNotificationReceived += HandleNotificationReceived;
    }

    private void OnDisable()
    {
        NotificationManager.OnNotificationReceived -= HandleNotificationReceived;
    }

    private void HandleNotificationReceived(string message)
    {
        ShowNotification(message);
    }

    /// <summary>
    /// Ekranda kısa süreli bildirim pop-up'ı gösterir.
    /// </summary>
    /// <param name="message">Gösterilecek bildirim mesajı.</param>
    /// <param name="duration">Ekranda kalma süresi (Opsiyonel).</param>
    public void ShowNotification(string message, float duration = -1f)
    {
        if (duration <= 0) duration = defaultDuration;

        // Önce paneli ve script objesini aktif et ki Coroutine başlayabilsin
        gameObject.SetActive(true);
        if (notificationPanel != null)
            notificationPanel.SetActive(true);

        if (currentNoticeCoroutine != null)
            StopCoroutine(currentNoticeCoroutine);

        currentNoticeCoroutine = StartCoroutine(NotificationRoutine(message, duration));
    }

    private IEnumerator NotificationRoutine(string message, float duration)
    {
        if (notificationText != null)
            notificationText.text = message;

        if (notificationPanel != null)
            notificationPanel.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        currentNoticeCoroutine = null;
    }
}
