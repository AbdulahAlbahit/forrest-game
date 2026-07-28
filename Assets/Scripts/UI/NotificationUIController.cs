using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyun genelinde tost/pop-up bildirimlerini yöneten arayüz script'i.
/// Ekranda ufak bir görsel (Image) ve yazı (TextMeshProUGUI) ile kısa süreli bildirim gösterir (Örn: "Yetersiz Odun!").
/// </summary>
public class NotificationUIController : MonoBehaviour
{
    public static NotificationUIController Instance { get; private set; }

    [Header("--- UI Elemanları ---")]
    [Tooltip("Bildirim arka plan paneli/görseli (Image). Ayrı bir çocuk obje ise onu gizler; aynı objeyse Image/Text bileşenlerini kapatır.")]
    [SerializeField] private GameObject notificationPanel;

    [Tooltip("Bildirim mesajı metni.")]
    [SerializeField] private TextMeshProUGUI notificationText;

    [Header("--- Ayarlar ---")]
    [Tooltip("Varsayılan ekranda kalma süresi (saniye).")]
    [SerializeField] private float defaultDuration = 2f;

    private Coroutine currentNoticeCoroutine;
    private Image panelImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        panelImage = GetComponent<Image>();

        // Başlangıçta sadece görselleri gizle (Script objesi uyanık kalsın ki Coroutine çalışabilsin)
        SetVisibility(false);
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
    public void ShowNotification(string message, float duration = -1f)
    {
        if (duration <= 0) duration = defaultDuration;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (currentNoticeCoroutine != null)
            StopCoroutine(currentNoticeCoroutine);

        currentNoticeCoroutine = StartCoroutine(NotificationRoutine(message, duration));
    }

    private IEnumerator NotificationRoutine(string message, float duration)
    {
        if (notificationText != null)
            notificationText.text = message;

        SetVisibility(true);

        yield return new WaitForSeconds(duration);

        SetVisibility(false);

        currentNoticeCoroutine = null;
    }

    private void SetVisibility(bool visible)
    {
        // 1. Eğer notificationPanel ayrı bir çocuk obje ise onu aktif/pasif yap
        if (notificationPanel != null && notificationPanel != gameObject)
        {
            notificationPanel.SetActive(visible);
            return;
        }

        // 2. Eğer script doğrudan NotificationPanel üzerindeyse, GameObject'i kapatma (yoksa Coroutine durur)!
        // Bunun yerine Image ve Text bileşenlerini kapat/aç.
        if (panelImage != null)
            panelImage.enabled = visible;

        if (notificationText != null)
            notificationText.enabled = visible;
    }
}
