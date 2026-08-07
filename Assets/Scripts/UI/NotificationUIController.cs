using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum NotificationType { Default, Success, Warning, Error }

/// <summary>
/// Oyun genelinde tost/pop-up bildirimlerini yöneten arayüz script'i.
/// Ekranda ufak bir görsel (Image) ve yazı (TextMeshProUGUI) ile kısa süreli bildirim gösterir (Örn: "Yetersiz Odun!").
/// Yumuşak yukarı kayma (slide-up) ve saydamlaşma (fade in/out) animasyonu içerir.
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

    [Tooltip("Giriş/Çıkış animasyonu süresi (saniye).")]
    [SerializeField] private float animDuration = 0.22f;

    private Coroutine currentNoticeCoroutine;
    private Image panelImage;
    private CanvasGroup canvasGroup;
    private RectTransform targetRectTransform;
    private Vector2 originalAnchoredPosition;
    private Color originalPanelColor = Color.clear;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        panelImage = GetComponent<Image>();
        if (panelImage == null && notificationPanel != null)
            panelImage = notificationPanel.GetComponent<Image>();
        if (panelImage != null)
            originalPanelColor = panelImage.color;

        GameObject targetObj = notificationPanel != null ? notificationPanel : gameObject;
        canvasGroup = targetObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = targetObj.AddComponent<CanvasGroup>();
        }

        targetRectTransform = targetObj.GetComponent<RectTransform>();
        if (targetRectTransform != null)
        {
            originalAnchoredPosition = targetRectTransform.anchoredPosition;
        }

        // Başlangıçta sadece görselleri gizle
        SetVisibilityImmediate(false);
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
        if (string.IsNullOrEmpty(message)) return;

        // XP kazanım bildirimlerini tost pop-up'ta gösterme (Sol taraftaki floating text kullanılıyor)
        if (message.Contains("XP eklendi") || message.Contains("XP kazanildi")) return;

        ShowNotification(message);
    }

    private System.Collections.Generic.Queue<(string message, float duration, NotificationType type)> notificationQueue = new System.Collections.Generic.Queue<(string, float, NotificationType)>();
    private bool isProcessingQueue = false;

    /// <summary>
    /// Ekranda kısa süreli bildirim pop-up'ı gösterir (Kuyruğa ekler).
    /// </summary>
    public void ShowNotification(string message, float duration = -1f)
    {
        ShowNotification(message, NotificationType.Default, duration);
    }

    /// <summary>
    /// Ekranda renklendirilmiş tipte bildirim pop-up'ı gösterir.
    /// </summary>
    public void ShowNotification(string message, NotificationType type, float duration = -1f)
    {
        if (string.IsNullOrEmpty(message)) return;

        // XP ile ilgili hiçbir bildirimi notification panelinde gösterme (Sol taraftaki floating text kullanılıyor)
        if (message.IndexOf("XP", System.StringComparison.OrdinalIgnoreCase) >= 0) return;
        if (message.StartsWith("BİLGİ:", System.StringComparison.OrdinalIgnoreCase)) return;

        if (duration <= 0) duration = defaultDuration;

        // Mükerrer bildirim engelleme
        if (notificationQueue.Count > 0)
        {
            var array = notificationQueue.ToArray();
            if (array[array.Length - 1].message == message) return;
        }

        if (isProcessingQueue && notificationText != null && notificationText.text == message)
        {
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Maksimum 5 bildirim birikebilsin
        if (notificationQueue.Count < 5)
        {
            notificationQueue.Enqueue((message, duration, type));
        }

        if (!isProcessingQueue)
        {
            currentNoticeCoroutine = StartCoroutine(ProcessQueueRoutine());
        }
    }

    private IEnumerator ProcessQueueRoutine()
    {
        isProcessingQueue = true;

        while (notificationQueue.Count > 0)
        {
            var item = notificationQueue.Dequeue();
            yield return StartCoroutine(NotificationRoutine(item.message, item.duration, item.type));
        }

        isProcessingQueue = false;
        currentNoticeCoroutine = null;
    }

    private IEnumerator NotificationRoutine(string message, float duration, NotificationType type)
    {
        if (notificationText != null)
            notificationText.text = message;

        // Renk Tipine Göre Arka Plan Ayarı
        ApplyNotificationTypeColor(type);

        // Görünür yap ve animasyonu başlat
        SetVisibilityImmediate(true);

        // --- Fade In & Slide Up ---
        float elapsed = 0f;
        Vector2 startPos = originalAnchoredPosition - new Vector2(0f, 30f);
        Vector2 targetPos = originalAnchoredPosition;

        if (canvasGroup != null) canvasGroup.alpha = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            // Smooth step
            t = t * t * (3f - 2f * t);

            if (canvasGroup != null) canvasGroup.alpha = t;
            if (targetRectTransform != null) targetRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (targetRectTransform != null) targetRectTransform.anchoredPosition = targetPos;

        // --- Ekranda Kalma ---
        yield return new WaitForSecondsRealtime(duration);

        // --- Fade Out & Slide Up ---
        elapsed = 0f;
        Vector2 exitPos = originalAnchoredPosition + new Vector2(0f, 20f);

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            t = t * t * (3f - 2f * t);

            if (canvasGroup != null) canvasGroup.alpha = 1f - t;
            if (targetRectTransform != null) targetRectTransform.anchoredPosition = Vector2.Lerp(targetPos, exitPos, t);
            yield return null;
        }

        SetVisibilityImmediate(false);
        if (targetRectTransform != null) targetRectTransform.anchoredPosition = originalAnchoredPosition;
    }

    private void ApplyNotificationTypeColor(NotificationType type)
    {
        if (panelImage == null) return;

        switch (type)
        {
            case NotificationType.Success:
                panelImage.color = new Color(0.12f, 0.55f, 0.25f, 0.92f); // Yeşil
                break;
            case NotificationType.Warning:
                panelImage.color = new Color(0.85f, 0.58f, 0.10f, 0.92f); // Kehribar/Sarı
                break;
            case NotificationType.Error:
                panelImage.color = new Color(0.75f, 0.15f, 0.15f, 0.92f); // Kırmızı
                break;
            default:
                panelImage.color = originalPanelColor != Color.clear ? originalPanelColor : new Color(0.12f, 0.15f, 0.22f, 0.92f);
                break;
        }
    }

    private void SetVisibilityImmediate(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
        }

        if (notificationPanel != null && notificationPanel != gameObject)
        {
            notificationPanel.SetActive(visible);
            return;
        }

        if (panelImage != null)
            panelImage.enabled = visible;

        if (notificationText != null)
            notificationText.enabled = visible;
    }

    [ContextMenu("TEST: Yeşil Başarı Bildirimi")]
    public void TestSuccessNotification()
    {
        ShowNotification("Üretim Başarıyla Tamamlandı!", NotificationType.Success);
    }

    [ContextMenu("TEST: Sarı Uyarı Bildirimi")]
    public void TestWarningNotification()
    {
        ShowNotification("Sepetiniz Dolmak Üzere!", NotificationType.Warning);
    }

    [ContextMenu("TEST: Kırmızı Hata Bildirimi")]
    public void TestErrorNotification()
    {
        ShowNotification("Yetersiz Altın!", NotificationType.Error);
    }
}

