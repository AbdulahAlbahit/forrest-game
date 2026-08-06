using System.Collections;
using UnityEngine;
using ForrestGame.Core;
using ForrestGame.Managers;

namespace ForrestGame.UI
{
    /// <summary>
    /// Oyuncu belirli bir süre (örneğin 25 saniye) boyunca işlem yapmadığında (rölanti / idle kaldığında)
    /// oyun durumunu analiz ederek ekranda nazik bir bağlamsal yönlendirme ipucu gösterir.
    /// 
    /// Tamamen otomatik çalışır. Herhangi bir girdi alındığında ipucu anında kapanır.
    /// </summary>
    public class UIContextualHintUI : MonoBehaviour
    {
        public static UIContextualHintUI Instance { get; private set; }

        [Header("--- Ayarlar ---")]
        [Tooltip("Boşta kalma süresi sınırı (saniye).")]
        [SerializeField] private float idleThresholdSeconds = 25f;

        [Tooltip("İpucu sisteminin çalışıp çalışmayacağını kontrol eder.")]
        [SerializeField] private bool enableContextualHint = false;

        private float lastInputTime;
        private bool isHintShowing = false;
        private string activeHintMessage = "";
        private float ignoreInputUntilTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            lastInputTime = Time.unscaledTime;
        }

        private void Update()
        {
            // Kullanıcı isteği üzerine rölanti ipucu sistemi devreden çıkarıldı
            if (!enableContextualHint) return;

            CheckPlayerInput();
            EvaluateIdleState();
        }

        private void CheckPlayerInput()
        {
            if (Time.unscaledTime < ignoreInputUntilTime) return;

            bool hasInput = false;

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed)
                    hasInput = true;
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.isPressed)
                    hasInput = true;
                if (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.01f)
                    hasInput = true;
            }
            catch { }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.touchCount > 0 || Input.GetMouseButtonDown(0) || Input.anyKeyDown)
                    hasInput = true;
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f)
                    hasInput = true;
            }
            catch { }
#endif

            // Fallback genel koruma
            if (!hasInput)
            {
                try
                {
                    if (Input.touchCount > 0 || Input.GetMouseButton(0))
                        hasInput = true;
                }
                catch { }
            }

            if (hasInput)
            {
                lastInputTime = Time.unscaledTime;
                if (isHintShowing)
                {
                    HideHint();
                }
            }
        }

        private void EvaluateIdleState()
        {
            if (isHintShowing) return;

            if (Time.unscaledTime - lastInputTime >= idleThresholdSeconds)
            {
                string hint = DetermineBestHint();
                if (!string.IsNullOrEmpty(hint))
                {
                    ShowHint(hint);
                }
            }
        }

        private string DetermineBestHint()
        {
            // 1. Sepet Dolu mu?
            bool isBasketFull = BasketManager.Instance != null && BasketManager.Instance.IsFull;
            if (!isBasketFull)
            {
                var efeOdun = FindFirstObjectByType<EfeOdunTasima>();
                if (efeOdun != null) isBasketFull = efeOdun.SepetDolu;
            }

            if (isBasketFull)
            {
                return "Sepetiniz Dolu! Odunları kamyona aktarabilirsiniz.";
            }

            // 2. Yeterli Para Var ve Balta Yükseltilebilir mi?
            int currentMoney = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0;
            if (currentMoney >= 150)
            {
                return "Yeterli Altınınız Var! Balta masasında baltanızı yükseltebilirsiniz.";
            }

            // 3. Atölye Boş mu ve Oyuncuda Odun Var mı?
            int woodInHand = BasketManager.Instance != null ? BasketManager.Instance.MevcutDoluluk : 0;
            if (WorkshopManager.Instance != null && woodInHand > 0 && WorkshopManager.Instance.MevcutKuyruk <= 0)
            {
                return "Atölyeniz Boş! Atölye panelini açıp mobilya üretimi başlatabilirsiniz.";
            }

            return "Ağaç keserek daha fazla odun toplayabilirsiniz!";
        }

        private void ShowHint(string message)
        {
            isHintShowing = true;
            activeHintMessage = message;

            // Bildirim tostunda da belirgin biçimde göster (Sarı/Kehribar uyarı olarak)
            NotificationUIController.Instance?.ShowNotification(message, NotificationType.Warning, 4.5f);

            // Alt etkileşim ipucu ekranında da göster
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.ShowPrompt(message);
            }
        }

        private void HideHint()
        {
            isHintShowing = false;
            activeHintMessage = "";

            if (InteractionPromptUI.Instance != null && InteractionPromptUI.Instance.IsVisible)
            {
                InteractionPromptUI.Instance.HidePrompt();
            }
        }

        [ContextMenu("TEST: İpucu Balonunu Şimdi Göster")]
        public void TestShowHint()
        {
            ignoreInputUntilTime = Time.unscaledTime + 4.0f; // Test sırasındaki tıklamadan dolayı hemen kapanmasın
            string hint = DetermineBestHint();
            ShowHint(hint);
        }

        [ContextMenu("TEST: İpucunu Gizle")]
        public void TestHideHint()
        {
            ignoreInputUntilTime = 0f;
            HideHint();
        }
    }
}
