using System.Collections;
using UnityEngine;

namespace ForrestGame.UI
{
    /// <summary>
    /// Paneller (Popup / Menu / Dialog) açılırken ve kapanırken yumuşak pop-up (scale)
    /// ve fade-in/fade-out animasyonu sağlayan ortak UI bileşeni.
    /// Herhangi bir panel GameObject'ine eklenebilir.
    /// Time.unscaledDeltaTime kullandığı için oyun duraklatıldığında (Pause) da sorunsuz çalışır.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIPanelAnimator : MonoBehaviour
    {
        [Header("--- Animasyon Süreleri ---")]
        [Tooltip("Açılış animasyonu süresi (saniye).")]
        [SerializeField] private float openDuration = 0.25f;

        [Tooltip("Kapanış animasyonu süresi (saniye).")]
        [SerializeField] private float closeDuration = 0.18f;

        [Header("--- Ölçek Ayarları ---")]
        [Tooltip("Açılışa başlama ölçeği.")]
        [SerializeField] private Vector3 startScale = new Vector3(0.85f, 0.85f, 0.85f);

        [Tooltip("Açılışta yaylanma (overshoot) miktarı.")]
        [SerializeField] private float overshoot = 1.05f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Coroutine activeAnimationCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// Paneli yumuşak animasyonla (Scale + Fade In) açar.
        /// </summary>
        public void Show()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            if (activeAnimationCoroutine != null)
            {
                StopCoroutine(activeAnimationCoroutine);
            }

            gameObject.SetActive(true);
            activeAnimationCoroutine = StartCoroutine(OpenRoutine());
        }

        /// <summary>
        /// Paneli yumuşak animasyonla (Scale + Fade Out) kapatır ve ardından GameObject'i deaktif eder.
        /// </summary>
        /// <param name="onComplete">Kapanış tamamlandığında çağrılacak opsiyonel geri çağırım.</param>
        public void Hide(System.Action onComplete = null)
        {
            if (!gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                return;
            }

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            if (activeAnimationCoroutine != null)
            {
                StopCoroutine(activeAnimationCoroutine);
            }

            activeAnimationCoroutine = StartCoroutine(CloseRoutine(onComplete));
        }

        private IEnumerator OpenRoutine()
        {
            canvasGroup.alpha = 0f;
            rectTransform.localScale = startScale;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);

                // Alpha fade in
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);

                // EaseOutBack benzeri yaylanan ölçek hesabı
                float scaleT;
                if (t < 0.7f)
                {
                    scaleT = Mathf.Lerp(startScale.x, overshoot, t / 0.7f);
                }
                else
                {
                    scaleT = Mathf.Lerp(overshoot, 1f, (t - 0.7f) / 0.3f);
                }

                rectTransform.localScale = new Vector3(scaleT, scaleT, scaleT);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            rectTransform.localScale = Vector3.one;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            activeAnimationCoroutine = null;
        }

        private IEnumerator CloseRoutine(System.Action onComplete)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            Vector3 initialCloseScale = rectTransform.localScale;
            float initialAlpha = canvasGroup.alpha;

            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeDuration);

                // Alpha fade out
                canvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, t);

                // Scale down slightly (1.0 -> 0.90)
                float scaleVal = Mathf.Lerp(initialCloseScale.x, 0.90f, t);
                rectTransform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);

                yield return null;
            }

            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.one;
            activeAnimationCoroutine = null;

            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
