using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ForrestGame.UI
{
    /// <summary>
    /// Oyuncu bir nesneye (Ağaç, Kamyon, Ambar vb.) yaklaştığında ekranda dinamik
    /// etkileşim uyarısı ("Kesmek İçin Dokun", "Odunları Aktar" vb.) gösteren UI kontrolcüsü.
    /// 
    /// KULLANIM:
    /// 1. Canvas altına alt-orta konumlandırılmış bir panel ve metin ekleyin.
    /// 2. Bu scripti objeye bağlayıp Inspector'dan promptText ve panel referansını atayın.
    /// 3. InteractionPromptUI.Instance?.ShowPrompt("Kesmek İçin Dokun"); şeklinde çağrılabilir.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        public static InteractionPromptUI Instance { get; private set; }

        [Header("--- UI Referansları ---")]
        [Tooltip("İpucu arka plan paneli/objesi.")]
        [SerializeField] private GameObject promptContainer;

        [Tooltip("İpucu mesajı metni.")]
        [SerializeField] private TextMeshProUGUI promptText;

        [Tooltip("İpucu ikonu (Opsiyonel).")]
        [SerializeField] private Image promptIcon;

        [Header("--- Ayarlar & Animasyon ---")]
        [Tooltip("Açılış ve kapanış yumuşatma süresi (saniye).")]
        [SerializeField] private float fadeDuration = 0.18f;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Coroutine activeCoroutine;
        private bool isVisible = false;

        public bool IsVisible => isVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GameObject targetObj = promptContainer != null ? promptContainer : gameObject;
            canvasGroup = targetObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = targetObj.AddComponent<CanvasGroup>();
            }

            rectTransform = targetObj.GetComponent<RectTransform>();

            SetVisibilityImmediate(false);
        }

        private void OnEnable()
        {
            EnsureCanvasGroup();
        }

        private void EnsureCanvasGroup()
        {
            GameObject targetObj = (promptContainer != null && promptContainer != gameObject) ? promptContainer : gameObject;
            if (canvasGroup == null)
            {
                canvasGroup = targetObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = targetObj.AddComponent<CanvasGroup>();
                }
            }

            if (rectTransform == null)
            {
                rectTransform = targetObj.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Ekranda etkileşim ipucunu gösterir.
        /// </summary>
        /// <param name="message">Gösterilecek ipucu metni (Örn: "Kesmek İçin Dokun")</param>
        /// <param name="icon">Opsiyonel ikon sprite'ı</param>
        public void ShowPrompt(string message, Sprite icon = null)
        {
            EnsureCanvasGroup();

            if (promptText != null)
                promptText.text = message;

            if (promptIcon != null)
            {
                promptIcon.sprite = icon;
                promptIcon.gameObject.SetActive(icon != null);
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (promptContainer != null && !promptContainer.activeSelf)
                promptContainer.SetActive(true);

            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            activeCoroutine = StartCoroutine(FadeRoutine(true));
        }

        /// <summary>
        /// Etkileşim ipucunu yumuşak şekilde gizler.
        /// </summary>
        public void HidePrompt()
        {
            if (!isVisible) return;

            EnsureCanvasGroup();

            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            activeCoroutine = StartCoroutine(FadeRoutine(false));
        }

        private IEnumerator FadeRoutine(bool show)
        {
            isVisible = show;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
            float targetAlpha = show ? 1f : 0f;

            Vector3 startScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
            Vector3 targetScale = show ? Vector3.one : new Vector3(0.85f, 0.85f, 0.85f);

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                t = t * t * (3f - 2f * t); // SmoothStep

                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                }
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = targetAlpha;
            if (rectTransform != null) rectTransform.localScale = targetScale;

            if (!show)
            {
                SetVisibilityImmediate(false);
            }

            activeCoroutine = null;
        }

        private void SetVisibilityImmediate(bool visible)
        {
            EnsureCanvasGroup();
            isVisible = visible;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (promptContainer != null && promptContainer != gameObject)
            {
                promptContainer.SetActive(visible);
            }
        }

        [ContextMenu("TEST: İpucu Göster")]
        public void TestShowPrompt()
        {
            ShowPrompt("Ağaç Kes!");
        }

        [ContextMenu("TEST: İpucu Gizle")]
        public void TestHidePrompt()
        {
            HidePrompt();
        }
    }
}
