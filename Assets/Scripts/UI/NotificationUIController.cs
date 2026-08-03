using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        panelImage = GetComponent<Image>();

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

        currentNoticeCoroutine = null;
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
}

