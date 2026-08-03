using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;
using ForrestGame.Managers;

namespace ForrestGame.UI
{
    /// <summary>
    /// Oyunda Para, XP veya Odun kazanıldığında ekranda sırayla yukarı doğru süzülen
    /// renkli ve animasyonlu sayılar (Floating Text / Pop-up Numbers) gösteren UI kontrolcüsü.
    /// 
    /// DÜZELTMELER:
    /// 1. Başlangıçta toplam bakiyenin uçmasını önlemek için lastObserved değerleri mevcut bakiyeyle ilklendirilir.
    /// 2. BasketManager ve EfeOynanisEventleri ortak dinlenerek Odun kazanımları (+1 Odun) sorunsuz çalıştırılır.
    /// </summary>
    public class UIFloatingTextManager : MonoBehaviour
    {
        public static UIFloatingTextManager Instance { get; private set; }

        [Header("--- UI Konteyner ve Ayarlar ---")]
        [Tooltip("Uçan yazıların içinde doğacağı Canvas / RectTransform (Boş bırakılırsa sahnede otomatik Canvas bulur).")]
        [SerializeField] private RectTransform textContainer;

        [Tooltip("Ekran sol-sağ konum oranı (0.22 = Ekranın %22 sol tarafı).")]
        [SerializeField] private float screenLeftRatio = 0.22f;

        [Tooltip("Ekran yükseklik konumu oranı (0.55 = Ekranın %55 yüksekliği).")]
        [SerializeField] private float screenHeightRatio = 0.55f;

        [Tooltip("Ekranda uçma süresi (saniye).")]
        [SerializeField] private float duration = 0.85f;

        [Tooltip("Yukarı doğru kayma mesafesi (piksel).")]
        [SerializeField] private float floatDistance = 85f;

        [Tooltip("Arka arkaya fırlatılan yazılar arasındaki gecikme (saniye).")]
        [SerializeField] private float delayBetweenTexts = 0.22f;

        [Tooltip("Yazı tipi boyutu.")]
        [SerializeField] private float fontSize = 32f;

        [Header("--- Renk Paleti ---")]
        [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.15f, 1f);
        [SerializeField] private Color xpColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color woodColor = new Color(0.4f, 0.9f, 0.3f, 1f);

        private Queue<(string message, Color color, Vector2? position)> textQueue = new Queue<(string, Color, Vector2?)>();
        private bool isProcessingQueue = false;

        private int lastObservedMoney = -1;
        private int lastObservedXP = -1;
        private int lastObservedWood = -1;

        private Coroutine bindRoutine;
        private EconomyManager boundEconomy;
        private XPManager boundXP;
        private BasketManager boundBasket;
        private CloudSaveManager boundCloudSave;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureContainer();
        }

        private void OnEnable()
        {
            EnsureContainer();
            InitObservedValues();
            SubscribeEvents();

            if (bindRoutine != null) StopCoroutine(bindRoutine);
            bindRoutine = StartCoroutine(BindManagersRoutine());
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }
        }

        private void EnsureContainer()
        {
            if (textContainer == null)
            {
                textContainer = GetComponent<RectTransform>();

                if (textContainer == null || GetComponentInParent<Canvas>() == null)
                {
                    Canvas targetCanvas = FindFirstObjectByType<Canvas>();
                    if (targetCanvas != null)
                    {
                        textContainer = targetCanvas.transform as RectTransform;
                    }
                }
            }
        }

        private void InitObservedValues()
        {
            if (EconomyManager.Instance != null) lastObservedMoney = EconomyManager.Instance.MevcutPara;
            else if (CloudSaveManager.Instance?.CurrentData != null) lastObservedMoney = CloudSaveManager.Instance.CurrentData.Para;

            if (XPManager.Instance != null) lastObservedXP = XPManager.Instance.MevcutXP;
            else if (CloudSaveManager.Instance?.CurrentData != null) lastObservedXP = CloudSaveManager.Instance.CurrentData.XP;

            if (BasketManager.Instance != null) lastObservedWood = BasketManager.Instance.MevcutDoluluk;
            else if (CloudSaveManager.Instance?.CurrentData != null) lastObservedWood = CloudSaveManager.Instance.CurrentData.PlayerWoodStock;
        }

        private IEnumerator BindManagersRoutine()
        {
            while (isActiveAndEnabled)
            {
                SubscribeEvents();
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private void SubscribeEvents()
        {
            if (EconomyManager.Instance != null && boundEconomy != EconomyManager.Instance)
            {
                if (boundEconomy != null) boundEconomy.OnMoneyChanged -= HandleMoneyChanged;
                boundEconomy = EconomyManager.Instance;
                if (boundEconomy != null)
                {
                    lastObservedMoney = boundEconomy.MevcutPara;
                    boundEconomy.OnMoneyChanged += HandleMoneyChanged;
                }
            }

            if (XPManager.Instance != null && boundXP != XPManager.Instance)
            {
                if (boundXP != null) boundXP.OnXPChanged -= HandleXPChanged;
                boundXP = XPManager.Instance;
                if (boundXP != null)
                {
                    lastObservedXP = boundXP.MevcutXP;
                    boundXP.OnXPChanged += HandleXPChanged;
                }
            }

            if (BasketManager.Instance != null && boundBasket != BasketManager.Instance)
            {
                if (boundBasket != null) boundBasket.OnBasketChanged -= HandleBasketChanged;
                boundBasket = BasketManager.Instance;
                if (boundBasket != null)
                {
                    lastObservedWood = boundBasket.MevcutDoluluk;
                    boundBasket.OnBasketChanged += HandleBasketChanged;
                }
            }

            if (CloudSaveManager.Instance != null && boundCloudSave != CloudSaveManager.Instance)
            {
                if (boundCloudSave != null) boundCloudSave.OnDataLoaded -= HandleDataLoaded;
                boundCloudSave = CloudSaveManager.Instance;
                if (boundCloudSave != null) boundCloudSave.OnDataLoaded += HandleDataLoaded;
            }

            EfeMarketSalesController.OnSaleCompleted -= HandleMarketSaleCompleted;
            EfeMarketSalesController.OnSaleCompleted += HandleMarketSaleCompleted;

            EfeOynanisEventleri.OdunToplandi -= HandleEfeWoodCollected;
            EfeOynanisEventleri.OdunToplandi += HandleEfeWoodCollected;
        }

        private void UnsubscribeEvents()
        {
            if (boundEconomy != null)
            {
                boundEconomy.OnMoneyChanged -= HandleMoneyChanged;
                boundEconomy = null;
            }

            if (boundXP != null)
            {
                boundXP.OnXPChanged -= HandleXPChanged;
                boundXP = null;
            }

            if (boundBasket != null)
            {
                boundBasket.OnBasketChanged -= HandleBasketChanged;
                boundBasket = null;
            }

            if (boundCloudSave != null)
            {
                boundCloudSave.OnDataLoaded -= HandleDataLoaded;
                boundCloudSave = null;
            }

            EfeMarketSalesController.OnSaleCompleted -= HandleMarketSaleCompleted;
            EfeOynanisEventleri.OdunToplandi -= HandleEfeWoodCollected;
        }

        private void HandleDataLoaded(PlayerData data)
        {
            if (data == null) return;
            if (lastObservedMoney < 0) lastObservedMoney = data.Para;
            if (lastObservedXP < 0) lastObservedXP = data.XP;
            if (lastObservedWood < 0) lastObservedWood = data.PlayerWoodStock;
        }

        private void HandleMoneyChanged(int newMoney)
        {
            if (lastObservedMoney >= 0 && newMoney > lastObservedMoney)
            {
                int diff = newMoney - lastObservedMoney;
                SpawnText($"+{diff} Altın", goldColor);
            }
            lastObservedMoney = newMoney;
        }

        private void HandleXPChanged(int newXP)
        {
            if (lastObservedXP >= 0 && newXP > lastObservedXP)
            {
                int diff = newXP - lastObservedXP;
                SpawnText($"+{diff} XP", xpColor);
            }
            lastObservedXP = newXP;
        }

        private void HandleBasketChanged(int currentWood, int maxCapacity)
        {
            ProcessWoodChange(currentWood);
        }

        private void HandleEfeWoodCollected(Transform source, int currentWood, int maxCapacity)
        {
            ProcessWoodChange(currentWood);
        }

        private void ProcessWoodChange(int currentWood)
        {
            if (lastObservedWood >= 0 && currentWood > lastObservedWood)
            {
                int diff = currentWood - lastObservedWood;
                SpawnText($"+{diff} Odun", woodColor);
            }
            else if (lastObservedWood < 0 && currentWood > 0)
            {
                SpawnText($"+{currentWood} Odun", woodColor);
            }
            lastObservedWood = currentWood;
        }

        private void HandleMarketSaleCompleted(EfeMarketSalesController.SaleResult result)
        {
            if (result.EarnedAmount > 0)
            {
                SpawnText($"+{result.EarnedAmount} Altın!", goldColor);
            }
        }

        /// <summary>
        /// Ekranda yukarı doğru süzülen renkli metin fırlatır (Kuyruğa ekler).
        /// </summary>
        public void SpawnText(string textMessage, Color color, Vector2? screenPosition = null)
        {
            if (string.IsNullOrEmpty(textMessage)) return;

            textQueue.Enqueue((textMessage, color, screenPosition));

            if (!isProcessingQueue)
            {
                StartCoroutine(ProcessQueueRoutine());
            }
        }

        private IEnumerator ProcessQueueRoutine()
        {
            isProcessingQueue = true;

            while (textQueue.Count > 0)
            {
                var item = textQueue.Dequeue();
                CreateAndAnimateText(item.message, item.color, item.position);

                // Üst üste binmeyi önlemek için kısa bir süre bekle
                yield return new WaitForSecondsRealtime(delayBetweenTexts);
            }

            isProcessingQueue = false;
        }

        private void CreateAndAnimateText(string textMessage, Color color, Vector2? screenPosition)
        {
            EnsureContainer();

            GameObject textObj = new GameObject("FloatingText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            Transform parentTransform = textContainer != null ? textContainer : transform;
            textObj.transform.SetParent(parentTransform, false);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            CanvasGroup group = textObj.GetComponent<CanvasGroup>();

            // Konumlandırma (Sol kenara yakın: Screen.width * screenLeftRatio)
            Vector2 defaultPos = new Vector2(Screen.width * screenLeftRatio, Screen.height * screenHeightRatio);
            Vector2 spawnPos = screenPosition ?? defaultPos;
            rect.position = spawnPos;
            rect.sizeDelta = new Vector2(400f, 70f);

            tmp.text = textMessage;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = color;
            tmp.raycastTarget = false;

            // Dış Çizgi Stili
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;

            StartCoroutine(AnimateFloatingTextRoutine(rect, tmp, group));
        }

        private IEnumerator AnimateFloatingTextRoutine(RectTransform rect, TextMeshProUGUI tmp, CanvasGroup group)
        {
            Vector3 startPos = rect.position;
            Vector3 targetPos = startPos + new Vector3(0f, floatDistance, 0f);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Hafif büyüme (Pulse) ve yukarı süzülme
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
                rect.localScale = new Vector3(scale, scale, 1f);
                rect.position = Vector3.Lerp(startPos, targetPos, t);

                // Son %60'lık kısımda saydamlaş (Fade Out)
                if (group != null)
                {
                    group.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.4f) / 0.6f));
                }

                yield return null;
            }

            Destroy(rect.gameObject);
        }

        [ContextMenu("TEST: Görev Ödülü Fırlat (+50 Altın +50 XP)")]
        public void TestQuestReward()
        {
            SpawnText("+50 Altın", goldColor);
            SpawnText("+50 XP", xpColor);
        }

        [ContextMenu("TEST: Altın Yazısı Fırlat")]
        public void TestGoldText()
        {
            SpawnText("+250 Altın!", goldColor);
        }

        [ContextMenu("TEST: XP Yazısı Fırlat")]
        public void TestXPText()
        {
            SpawnText("+50 XP", xpColor);
        }

        [ContextMenu("TEST: Odun Yazısı Fırlat")]
        public void TestWoodText()
        {
            SpawnText("+1 Odun", woodColor);
        }
    }
}
