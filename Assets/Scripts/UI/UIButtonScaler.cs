using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForrestGame.UI
{
    /// <summary>
    /// Butonlara ve tıklanabilir UI elemanlarına basıldığında yumuşak basılma (scale down)
    /// ve bırakıldığında geri yaylanma (scale up) efekti veren yardımcı bileşen.
    /// Herhangi bir GameObject veya Button üzerine eklenebilir.
    /// </summary>
    public class UIButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("--- Ölçek Ayarları ---")]
        [Tooltip("Basıldığında ulaşılacak hedef ölçek oranı.")]
        [SerializeField] private float pressedScale = 0.92f;

        [Tooltip("Basılma animasyonu süresi (saniye).")]
        [SerializeField] private float pressDuration = 0.08f;

        [Tooltip("Serbest bırakma / yaylanma süresi (saniye).")]
        [SerializeField] private float releaseDuration = 0.12f;

        [Tooltip("Sadece buton aktif/etkileşimli durumdayken mi çalışsın?")]
        [SerializeField] private bool checkButtonInteractable = true;

        private Vector3 initialScale;
        private Coroutine activeScaleCoroutine;
        private Button targetButton;

        private void Awake()
        {
            initialScale = transform.localScale;
            targetButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            transform.localScale = initialScale;
        }

        private void OnDisable()
        {
            if (activeScaleCoroutine != null)
            {
                StopCoroutine(activeScaleCoroutine);
                activeScaleCoroutine = null;
            }
            transform.localScale = initialScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            StartScaleAnimation(initialScale * pressedScale, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            StartScaleAnimation(initialScale, releaseDuration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                TriggerRedShake();
                return;
            }

            UIHapticFeedback.Light();

            // Click olayında dokunmanın tescil edildiğini hissettirmek için mikro bounce efekti
            StartScaleAnimation(initialScale * 1.05f, 0.05f, () =>
            {
                StartScaleAnimation(initialScale, 0.08f);
            });
        }

        /// <summary>
        /// Yetersiz para veya pasif durum tıklamalarında kırmızılı yatay sarsıntı patlatır.
        /// </summary>
        public void TriggerRedShake()
        {
            if (!gameObject.activeInHierarchy) return;
            UIHapticFeedback.Medium();

            if (activeScaleCoroutine != null) StopCoroutine(activeScaleCoroutine);
            activeScaleCoroutine = StartCoroutine(RedShakeRoutine());
        }

        private IEnumerator RedShakeRoutine()
        {
            Vector3 originalPos = transform.localPosition;
            Image img = GetComponent<Image>();
            Color originalColor = img != null ? img.color : Color.white;
            Color shakeColor = new Color(0.9f, 0.2f, 0.2f, 1f);

            if (img != null) img.color = shakeColor;

            float elapsed = 0f;
            float totalDuration = 0.35f;
            float shakeAmount = 8f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / totalDuration;
                float offset = Mathf.Sin(t * Mathf.PI * 6f) * shakeAmount * (1f - t);
                transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }

            transform.localPosition = originalPos;
            if (img != null) img.color = originalColor;
            activeScaleCoroutine = null;
        }

        private bool IsInteractable()
        {
            if (!checkButtonInteractable) return true;
            if (targetButton != null) return targetButton.interactable && targetButton.enabled;
            return true;
        }

        private void StartScaleAnimation(Vector3 targetScale, float duration, System.Action onComplete = null)
        {
            if (activeScaleCoroutine != null)
            {
                StopCoroutine(activeScaleCoroutine);
            }

            if (gameObject.activeInHierarchy)
            {
                activeScaleCoroutine = StartCoroutine(ScaleRoutine(targetScale, duration, onComplete));
            }
            else
            {
                transform.localScale = targetScale;
                onComplete?.Invoke();
            }
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale, float duration, System.Action onComplete = null)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Yumuşak SmoothStep eğrisi
                t = t * t * (3f - 2f * t);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
            activeScaleCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
