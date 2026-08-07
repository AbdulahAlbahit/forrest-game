using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;
using ForrestGame.Managers;

namespace ForrestGame.UI
{
    /// <summary>
    /// Oyuncu seviye atladığında ekranda dramatik, temiz bir Seviye Atlama kutlaması
    /// gösteren UI kontrolcüsü.
    /// 
    /// XPManager.OnLevelUp eventini otomatik dinler ve runtime'da pop-up oluşturur.
    /// Prefab veya manuel sahne nesnesi gerektirmez.
    /// </summary>
    public class LevelUpPopupUI : MonoBehaviour
    {
        public static LevelUpPopupUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("[LevelUpPopupUI]", typeof(LevelUpPopupUI));
                DontDestroyOnLoad(go);
            }
        }

        [SerializeField] private Sprite popupBackgroundSprite;

        private Sprite FindGenelPanelSprite()
        {
            if (popupBackgroundSprite != null) return popupBackgroundSprite;

            // Sahnedeki Image bileşenlerinden "GenelPanelBack" sprite'ını ara ve al
            Image[] allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (Image img in allImages)
            {
                if (img != null && img.sprite != null && img.sprite.name.Contains("GenelPanelBack"))
                {
                    popupBackgroundSprite = img.sprite;
                    return popupBackgroundSprite;
                }
            }

            // Fallback: Herhangi bir panel sprite'ı
            foreach (Image img in allImages)
            {
                if (img != null && img.sprite != null && (img.sprite.name.ToLower().Contains("panel") || img.sprite.name.ToLower().Contains("back")))
                {
                    popupBackgroundSprite = img.sprite;
                    return popupBackgroundSprite;
                }
            }

            return null;
        }

        private Coroutine activePopupRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (XPManager.Instance != null)
            {
                XPManager.Instance.OnLevelUp -= HandleLevelUp;
                XPManager.Instance.OnLevelUp += HandleLevelUp;
            }
        }

        private void UnsubscribeEvents()
        {
            if (XPManager.Instance != null)
            {
                XPManager.Instance.OnLevelUp -= HandleLevelUp;
            }
        }

        private void Update()
        {
            // XPManager sahnede geç oluştuysa otomatik bağlan
            if (XPManager.Instance != null)
            {
                SubscribeEvents();
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            ShowLevelUpPopup(newLevel);
        }

        /// <summary>
        /// Seviye atlama pop-up'ını başlatır.
        /// </summary>
        public void ShowLevelUpPopup(int newLevel)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas canvas = null;

            foreach (var c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    canvas = c;
                    break;
                }
            }

            if (canvas == null)
            {
                // Create a temporary ScreenSpaceOverlay canvas for the popup
                GameObject canvasObj = new GameObject("LevelUpCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                UnityEngine.UI.CanvasScaler scaler = canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // Optional: Destroy it after the popup is done
                Destroy(canvasObj, 5f);
            }

            // Görsel & Dokunsal efektler
            UIScreenFlash.Instance?.Flash(new Color(0.2f, 0.75f, 1f, 1f), 0.35f, 0.28f);
            UIHapticFeedback.Heavy();

            AudioManager audioMgr = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
            if (audioMgr != null)
            {
                try
                {
                    audioMgr.PlaySFX("LevelUp");
                }
                catch { }
            }

            if (activePopupRoutine != null)
                StopCoroutine(activePopupRoutine);

            activePopupRoutine = StartCoroutine(LevelUpRoutine(canvas, newLevel));
        }

        private IEnumerator LevelUpRoutine(Canvas canvas, int newLevel)
        {
            // ─── Modal Root ───
            GameObject modalRoot = new GameObject("LevelUpPopup", typeof(RectTransform), typeof(CanvasGroup));
            modalRoot.transform.SetParent(canvas.transform, false);
            modalRoot.transform.SetAsLastSibling();

            RectTransform rootRect = modalRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.55f);
            rootRect.anchorMax = new Vector2(0.5f, 0.55f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(650f, 260f);
            rootRect.anchoredPosition = Vector2.zero;

            CanvasGroup group = modalRoot.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            // ─── Arka Plan Kartı ───
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(modalRoot.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            
            Sprite genelPanelSprite = FindGenelPanelSprite();
            if (genelPanelSprite != null)
            {
                bgImg.sprite = genelPanelSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);
            }

            // ─── Üst Banner Metni ───
            GameObject headerObj = new GameObject("HeaderLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(modalRoot.transform, false);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.08f, 0.58f);
            headerRect.anchorMax = new Vector2(0.92f, 0.95f);
            headerRect.sizeDelta = Vector2.zero;
            headerRect.anchoredPosition = new Vector2(0f, -8f);
            TextMeshProUGUI headerTMP = headerObj.GetComponent<TextMeshProUGUI>();
            headerTMP.text = "TEBRİKLER!";
            headerTMP.fontSize = 20f;
            headerTMP.fontStyle = FontStyles.Bold;
            headerTMP.alignment = TextAlignmentOptions.Center;
            headerTMP.color = new Color(1f, 0.85f, 0.2f, 1f);

            // ─── Seviye Metni ───
            GameObject levelObj = new GameObject("LevelLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            levelObj.transform.SetParent(modalRoot.transform, false);
            RectTransform levelRect = levelObj.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.08f, 0.15f);
            levelRect.anchorMax = new Vector2(0.92f, 0.60f);
            levelRect.sizeDelta = Vector2.zero;
            levelRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI levelTMP = levelObj.GetComponent<TextMeshProUGUI>();
            levelTMP.text = $"SEVİYE {newLevel}";
            levelTMP.fontSize = 20f;
            levelTMP.fontStyle = FontStyles.Bold;
            levelTMP.alignment = TextAlignmentOptions.Center;
            levelTMP.color = Color.white;

            // ─── Phase 1: Scale In & Fade In (0.28s) ───
            float elapsed = 0f;
            float animDur = 0.28f;
            rootRect.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            while (elapsed < animDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDur);
                float smooth = t * (2f - t); // EaseOutQuad

                group.alpha = smooth;
                rootRect.localScale = Vector3.LerpUnclamped(new Vector3(0.6f, 0.6f, 1f), Vector3.one * 1.08f, smooth);
                yield return null;
            }

            // Punchy settle (1.08 -> 1.0)
            elapsed = 0f;
            animDur = 0.14f;
            while (elapsed < animDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDur);
                rootRect.localScale = Vector3.LerpUnclamped(Vector3.one * 1.08f, Vector3.one, t);
                yield return null;
            }
            rootRect.localScale = Vector3.one;

            // ─── Phase 2: Ekranda Bekleme (2.0s) ───
            yield return new WaitForSecondsRealtime(2.0f);

            // ─── Phase 3: Fade Out & Slide Up (0.25s) ───
            elapsed = 0f;
            animDur = 0.25f;
            Vector3 startPos = rootRect.anchoredPosition3D;
            Vector3 targetPos = startPos + new Vector3(0f, 40f, 0f);

            while (elapsed < animDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDur);
                group.alpha = 1f - t;
                rootRect.anchoredPosition3D = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            Destroy(modalRoot);
            activePopupRoutine = null;
        }

        [ContextMenu("TEST: Seviye 5 Atlama")]
        public void TestLevelUp()
        {
            ShowLevelUpPopup(5);
        }
    }
}
