using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForrestGame.UI
{
    /// <summary>
    /// Ekranın tamamında yumuşak, göz yormayan kısa renk flaşı tetikleyen UI bileşeni.
    /// Tamamen kodla (runtime) çalışır, sahneye manuel Obje veya Canvas eklenmesi GEREKMEZ.
    /// 
    /// KULLANIM:
    /// UIScreenFlash.Instance?.Flash(Color.yellow); // Örn: Seviye Atlama veya Satış
    /// </summary>
    public class UIScreenFlash : MonoBehaviour
    {
        public static UIScreenFlash Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("[UIScreenFlash]", typeof(UIScreenFlash));
                DontDestroyOnLoad(go);
            }
        }

        private Image flashImage;
        private CanvasGroup canvasGroup;
        private Coroutine activeFlashRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureFlashOverlay();
        }

        private void EnsureFlashOverlay()
        {
            if (flashImage != null) return;

            Canvas targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null) return;

            GameObject flashObj = new GameObject("UIScreenFlashOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            flashObj.transform.SetParent(targetCanvas.transform, false);
            flashObj.transform.SetAsLastSibling();

            RectTransform rect = flashObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            flashImage = flashObj.GetComponent<Image>();
            flashImage.color = Color.white;
            flashImage.raycastTarget = false; // Tıklamaları engellemesin

            canvasGroup = flashObj.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        /// <summary>
        /// Ekranda göz yormayan yumuşak bir renk flaşı patlatır.
        /// </summary>
        /// <param name="flashColor">Flaş rengi</param>
        /// <param name="duration">Süre (Varsayılan 0.28s)</param>
        /// <param name="maxAlpha">Maksimum opaklık (Göz yormamak için varsayılan 0.22)</param>
        public void Flash(Color flashColor, float duration = 0.28f, float maxAlpha = 0.22f)
        {
            EnsureFlashOverlay();
            if (flashImage == null || canvasGroup == null) return;

            if (activeFlashRoutine != null)
                StopCoroutine(activeFlashRoutine);

            flashImage.color = flashColor;
            activeFlashRoutine = StartCoroutine(FlashRoutine(duration, maxAlpha));
        }

        private IEnumerator FlashRoutine(float duration, float maxAlpha)
        {
            float halfDuration = duration * 0.4f;
            float fadeOutDuration = duration * 0.6f;

            // Fade in
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                canvasGroup.alpha = Mathf.Lerp(0f, maxAlpha, t);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = Mathf.Lerp(maxAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            activeFlashRoutine = null;
        }

        [ContextMenu("TEST: Mavi Flaş (Level Up)")]
        public void TestBlueFlash()
        {
            Flash(new Color(0.2f, 0.75f, 1f, 1f), 0.35f, 0.28f);
        }

        [ContextMenu("TEST: Sarı Flaş (Satış)")]
        public void TestYellowFlash()
        {
            Flash(new Color(1f, 0.85f, 0.2f, 1f), 0.28f, 0.22f);
        }
    }
}
