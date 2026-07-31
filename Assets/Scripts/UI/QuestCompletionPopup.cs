using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ForrestGame.UI
{
    /// <summary>
    /// Görev tamamlandığında ekranın ortasında fırlayarak beliren,
    /// ✅ ikonu, görev adı ve kazanılan ödülleri gösteren kutlama pop-up kontrolcüsü.
    /// NotificationManager event'ini dinleyerek otomatik tetiklenebilir veya doğrudan ShowPopup metoduyla çağrılabilir.
    /// </summary>
    public class QuestCompletionPopup : MonoBehaviour
    {
        public static QuestCompletionPopup Instance { get; private set; }

        [Header("--- UI Referansları ---")]
        [Tooltip("Pop-up ana konteyner GameObject'i.")]
        [SerializeField] private GameObject popupContainer;

        [Tooltip("Üst başlık metni (Örn: 'GÖREV TAMAMLANDI!').")]
        [SerializeField] private TextMeshProUGUI headerTitleText;

        [Tooltip("Tamamlanan görevin adı metni (Örn: 'Ağaç Katliamı').")]
        [SerializeField] private TextMeshProUGUI questNameText;

        [Tooltip("Kazanılan ödül detay metni (Örn: '+150 Altın, +50 XP').")]
        [SerializeField] private TextMeshProUGUI rewardDetailText;

        [Tooltip("Kutlama ikonu / Rozet görseli (İsteğe bağlı).")]
        [SerializeField] private Image badgeIconImage;

        [Header("--- Ayarlar & Animasyon ---")]
        [Tooltip("Ekranda kalma süresi (saniye).")]
        [SerializeField] private float displayDuration = 2.6f;

        [Tooltip("Açılış/Kapanış animasyonu süresi (saniye).")]
        [SerializeField] private float animDuration = 0.25f;

        [Tooltip("Kutlama anında çalacak SFX ismi (Opsiyonel).")]
        [SerializeField] private string celebrationSfxName = "";

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Coroutine activePopupCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GameObject targetObj = popupContainer != null ? popupContainer : gameObject;
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
            NotificationManager.OnNotificationReceived += HandleNotificationReceived;
        }

        private void OnDisable()
        {
            NotificationManager.OnNotificationReceived -= HandleNotificationReceived;
        }

        private void HandleNotificationReceived(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Bildirim "Gorev Tamamlandi" veya "Görev Tamamlandı" içeriyorsa pop-up'ı başlat
            if (message.StartsWith("Gorev Tamamlandi") || message.StartsWith("Görev Tamamlandı"))
            {
                ParseAndShowPopup(message);
            }
        }

        /// <summary>
        /// Bildirim metninden görev adı ve ödülleri ayıklayıp pop-up gösterir.
        /// Örnek format: "Gorev Tamamlandi: Ağaç Kesme! +100 Altin, +50 XP"
        /// </summary>
        private void ParseAndShowPopup(string rawMessage)
        {
            string questName = "Görev Tamamlandı";
            string rewards = "";

            int colonIndex = rawMessage.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < rawMessage.Length - 1)
            {
                string rest = rawMessage.Substring(colonIndex + 1).Trim();
                int exclamIndex = rest.IndexOf('!');
                if (exclamIndex >= 0)
                {
                    questName = rest.Substring(0, exclamIndex).Trim();
                    rewards = rest.Substring(exclamIndex + 1).Trim();
                }
                else
                {
                    questName = rest;
                }
            }

            ShowPopup(questName, rewards);
        }

        /// <summary>
        /// Belirtilen görev adı ve ödül metni ile kutlama pop-up'ını başlatır.
        /// </summary>
        public void ShowPopup(string questName, string rewardText = "")
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (popupContainer != null && !popupContainer.activeSelf)
                popupContainer.SetActive(true);

            if (activePopupCoroutine != null)
                StopCoroutine(activePopupCoroutine);

            activePopupCoroutine = StartCoroutine(PopupRoutine(questName, rewardText));
        }

        private IEnumerator PopupRoutine(string questName, string rewardText)
        {
            if (headerTitleText != null)
                headerTitleText.text = "GÖREV TAMAMLANDI!";

            if (questNameText != null)
                questNameText.text = questName;

            if (rewardDetailText != null)
            {
                rewardDetailText.text = string.IsNullOrEmpty(rewardText) ? "Ödül Kazanıldı!" : rewardText;
                rewardDetailText.gameObject.SetActive(true);
            }

            if (!string.IsNullOrEmpty(celebrationSfxName) && ForrestGame.Core.AudioManager.Instance != null)
            {
                ForrestGame.Core.AudioManager.Instance.PlaySFX(celebrationSfxName);
            }

            // --- Açılış Animasyonu (Scale 0.7 -> 1.08 -> 1.0 & Fade-In) ---
            float elapsed = 0f;
            Vector3 startScale = new Vector3(0.7f, 0.7f, 0.7f);
            Vector3 overshootScale = new Vector3(1.08f, 1.08f, 1.08f);

            canvasGroup.alpha = 0f;
            if (rectTransform != null) rectTransform.localScale = startScale;

            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);

                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);

                float scaleT;
                if (t < 0.7f)
                {
                    scaleT = Mathf.Lerp(0.7f, 1.08f, t / 0.7f);
                }
                else
                {
                    scaleT = Mathf.Lerp(1.08f, 1.0f, (t - 0.7f) / 0.3f);
                }

                if (rectTransform != null) rectTransform.localScale = new Vector3(scaleT, scaleT, scaleT);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            if (rectTransform != null) rectTransform.localScale = Vector3.one;

            // --- Bekleme ---
            yield return new WaitForSecondsRealtime(displayDuration);

            // --- Kapanış Animasyonu (Scale 1.0 -> 0.9 & Fade-Out) ---
            elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);

                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                float scaleVal = Mathf.Lerp(1.0f, 0.90f, t);
                if (rectTransform != null) rectTransform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
                yield return null;
            }

            SetVisibilityImmediate(false);
            activePopupCoroutine = null;
        }

        private void SetVisibilityImmediate(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (popupContainer != null && popupContainer != gameObject)
            {
                popupContainer.SetActive(visible);
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }

        // ═══════════════════════════════════════════
        //  Inspector Test Metodu (ContextMenu)
        // ═══════════════════════════════════════════

        [ContextMenu("TEST: Görev Tamamlandı Pop-up Göster")]
        public void TestShowQuestPopup()
        {
            ShowPopup("Süper Oduncu (Test Görevi)", "+250 Altın, +100 XP");
        }
    }
}
