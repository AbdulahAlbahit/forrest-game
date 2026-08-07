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

        [Header("--- UI Konteyner ve Genel Ayarlar ---")]
        [Tooltip("Uçan yazıların içinde doğacağı Canvas / RectTransform (Boş bırakılırsa sahnede otomatik Canvas bulur).")]
        [SerializeField] private RectTransform textContainer;

        [Header("--- Sol Taraf Floating Text Ayarları (Altın, XP, Odun) ---")]
        [Tooltip("Altın, XP ve Odun yazılarının ekran sol-sağ konum oranı (0.18 = Ekranın %18 sol tarafı).")]
        [SerializeField] private float screenLeftRatio = 0.18f;

        [Tooltip("Altın, XP ve Odun yazılarının ekran yükseklik konumu oranı (0.55 = Ekranın %55 yüksekliği).")]
        [SerializeField] private float screenHeightRatio = 0.55f;

        [Tooltip("Ekranda uçma süresi (saniye).")]
        [SerializeField] private float duration = 0.85f;

        [Tooltip("Yukarı doğru kayma mesafesi (piksel).")]
        [SerializeField] private float floatDistance = 85f;

        [Tooltip("Arka arkaya fırlatılan yazılar arasındaki gecikme (saniye).")]
        [SerializeField] private float delayBetweenTexts = 0.22f;

        [Tooltip("Yazı tipi boyutu.")]
        [SerializeField] private float fontSize = 20f;

        [Header("--- Renk Paleti (Altın, XP, Odun) ---")]
        [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.15f, 1f);
        [SerializeField] private Color xpColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color woodColor = new Color(0.4f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color workshopCompleteColor = new Color(0.7f, 0.5f, 1f, 1f); // Mor-lila: Üretim tamamlandı

        [Header("--- Ekran Ortası Floating Text Ayarları (Ağaç Vuruş Hasarı) ---")]
        [Tooltip("Ağaç vuruşlarında ekran ortasında floating text çıksın mı?")]
        [SerializeField] private bool enableTreeDamageText = true;

        [Tooltip("1. Seviye / Min Hasar durumundaki metin rengi (Canlı Yeşil).")]
        [SerializeField] private Color minLevelDamageColor = new Color(0.2f, 0.95f, 0.3f, 1f);

        [Tooltip("Max Seviye / Max Hasar durumundaki metin rengi (Canlı Kırmızı).")]
        [SerializeField] private Color maxLevelDamageColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Tooltip("Ekran ortası konum oranı (0.50 X = Orta, 0.52 Y = Hafif Üst).")]
        [SerializeField] private Vector2 treeDamageScreenRatio = new Vector2(0.50f, 0.52f);

        [Tooltip("Ağaç vuruş metni formatı ({0} = hasar miktarı). Örn: '-{0} Hasar' veya '-{0}'")]
        [SerializeField] private string treeDamageFormat = "-{0} Hasar";

        [Tooltip("Ağaç vuruş metni font boyutu.")]
        [SerializeField] private float treeDamageFontSize = 20f;

        [Tooltip("Hızlı vuruşlarda üst üste binmeyi önlemek için rastgele offset aralığı (piksel).")]
        [SerializeField] private float randomOffsetRange = 25f;

        private Queue<(string message, Color color, Vector2? position)> textQueue = new Queue<(string, Color, Vector2?)>();
        private bool isProcessingQueue = false;

        private int lastObservedMoney = -1;
        private int lastObservedXP = -1;
        private int lastObservedWood = -1;

        private int lastHitFrame = -1;
        private float lastHitTime = -1f;

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
                    Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    Canvas targetCanvas = null;

                    foreach (var c in allCanvases)
                    {
                        if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                        {
                            targetCanvas = c;
                            break;
                        }
                    }

                    if (targetCanvas != null)
                    {
                        textContainer = targetCanvas.transform as RectTransform;
                    }
                    else
                    {
                        Debug.LogWarning("[UIFloatingTextManager] Screen Space Canvas bulunamadi! Yazi world space'te dogabilir.");
                    }
                }
            }
        }

        private void InitObservedValues()
        {
            if (EconomyManager.Instance != null) lastObservedMoney = EconomyManager.Instance.MevcutPara;
            else if (CloudSaveManager.Instance?.CurrentData != null) lastObservedMoney = CloudSaveManager.Instance.CurrentData.Gold;

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

            TreeManager.OnTreeHitAction -= HandleTreeHitWorldPos;
            TreeManager.OnTreeHitAction += HandleTreeHitWorldPos;

            TreeController.OnTreeHitAction -= HandleTreeHitSimple;
            TreeController.OnTreeHitAction += HandleTreeHitSimple;

            EfeOynanisEventleri.BaltaAgacaVurdu -= HandleBaltaAgacaVurdu;
            EfeOynanisEventleri.BaltaAgacaVurdu += HandleBaltaAgacaVurdu;

            WorkshopManager.OnProductionFinished -= HandleWorkshopProduction;
            WorkshopManager.OnProductionFinished += HandleWorkshopProduction;
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

            TreeManager.OnTreeHitAction -= HandleTreeHitWorldPos;
            TreeController.OnTreeHitAction -= HandleTreeHitSimple;
            EfeOynanisEventleri.BaltaAgacaVurdu -= HandleBaltaAgacaVurdu;
            WorkshopManager.OnProductionFinished -= HandleWorkshopProduction;
        }

        private void HandleDataLoaded(PlayerData data)
        {
            if (data == null) return;
            if (lastObservedMoney < 0) lastObservedMoney = data.Gold;
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
                SpawnText($"Satış Tamamlandı: +{result.EarnedAmount} Altın!", goldColor);
                UIScreenFlash.Instance?.Flash(new Color(1f, 0.85f, 0.2f, 1f), 0.25f, 0.18f);
                UIHapticFeedback.Medium();
            }
        }

        /// <summary>
        /// Atölye bir ürünü tamamladığında sol floating text alanında üretim bildirimi gösterir.
        /// </summary>
        private void HandleWorkshopProduction(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            // Tarif adını bul (ItemID'den okunabilir bir isim varsa onu kullan)
            string displayName = itemId;
            if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null)
            {
                foreach (var recipe in WorkshopManager.Instance.tumTarifler)
                {
                    if (recipe != null && recipe.ItemID == itemId)
                    {
                        // Description bos degilse ilk satiri al, yoksa ItemID
                        if (!string.IsNullOrWhiteSpace(recipe.Description))
                        {
                            string firstLine = recipe.Description.Split('\n')[0].Trim();
                            if (!string.IsNullOrEmpty(firstLine)) displayName = firstLine;
                        }
                        break;
                    }
                }
            }

            string amountSuffix = amount > 1 ? $" x{amount}" : "";
            SpawnText($"{displayName} Üretildi!{amountSuffix}", workshopCompleteColor);
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

            // Konumlandırma: Ekran oranlarını Anchor olarak kullanarak Canvas türünden bağımsız doğru yerleşim sağlar
            Vector2 ratioPos = new Vector2(screenLeftRatio, screenHeightRatio);
            if (screenPosition.HasValue)
            {
                // Eğer özel bir ekran pozisyonu geldiyse, onu ekrana oranlayarak Anchor'a çevir
                ratioPos = new Vector2(screenPosition.Value.x / Screen.width, screenPosition.Value.y / Screen.height);
            }

            rect.anchorMin = ratioPos;
            rect.anchorMax = ratioPos;
            rect.anchoredPosition = Vector2.zero; // Tam anchor noktasında başla
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

        // =========================================================================
        // AĞAÇ VURUŞ HASARI VE BALTA SEVİYE GRADYAN RENK SİSTEMİ
        // =========================================================================

        private void HandleTreeHitWorldPos(Vector3 worldHitPos)
        {
            TriggerTreeHitDamageText();
        }

        private void HandleTreeHitSimple()
        {
            TriggerTreeHitDamageText();
        }

        private void HandleBaltaAgacaVurdu(Transform source)
        {
            TriggerTreeHitDamageText();
        }

        /// <summary>
        /// Ağaca baltayla vurulduğunda çağrılır. 
        /// Ekranın ortasında balta seviyesine göre gradyan renkli (Yeşil -> Kırmızı) hasar floating text'i fırlatır.
        /// </summary>
        public void TriggerTreeHitDamageText()
        {
            if (!enableTreeDamageText) return;

            // Aynı karede veya çok kısa aralıklarla çifte tetiklenmeyi önleme
            if (Time.frameCount == lastHitFrame || (Time.time - lastHitTime < 0.05f))
            {
                return;
            }
            lastHitFrame = Time.frameCount;
            lastHitTime = Time.time;

            int damage = GetCurrentAxeDamage(out float levelProgress);

            // Balta Seviyesine/Hasarına Göre Gradyan Renk Hesaplama (0 = Min/Yeşil, 1 = Max/Kırmızı)
            Color damageColor = Color.Lerp(minLevelDamageColor, maxLevelDamageColor, levelProgress);
            string message = string.Format(treeDamageFormat, damage);

            // Ekranın Ortası + Dinamik Ufak Offset (Çakışmayı önlemek için)
            Vector2 randomOffset = new Vector2(UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange), UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange));

            CreateAndAnimateTreeDamageText(message, damageColor, treeDamageScreenRatio, randomOffset);
        }

        private int GetCurrentAxeDamage(out float levelProgress)
        {
            int damage = 1;
            levelProgress = 0f;

            // 1. EfeBaltaKontrol varsa aktif balta hasarını al
            EfeBaltaKontrol baltaKontrol = FindFirstObjectByType<EfeBaltaKontrol>();
            if (baltaKontrol != null)
            {
                damage = Mathf.Max(1, baltaKontrol.AktifAgacHasari);
            }

            // 2. UpgradeManager üzerinden Balta seviyesini ve gradyan oranını hesapla
            string axeItemId = "Axe";
            if (UpgradeManager.Instance != null)
            {
                int currentLevel = UpgradeManager.Instance.GetItemLevel(axeItemId);
                if (UpgradeManager.Instance.TryGetConfig(axeItemId, out ItemUpgradeConfig config) && config != null)
                {
                    int maxLvl = Mathf.Max(1, config.MaxLevel);
                    if (maxLvl > 1)
                    {
                        levelProgress = Mathf.Clamp01((float)(currentLevel - 1) / (maxLvl - 1));
                    }

                    float statVal = config.GetStatValue(currentLevel);
                    if (statVal > damage) damage = Mathf.RoundToInt(statVal);
                }
                else
                {
                    levelProgress = Mathf.Clamp01((float)(currentLevel - 1) / 9f); // Varsayılan 10 seviye kabulü
                }
            }

            return damage;
        }

        private void CreateAndAnimateTreeDamageText(string message, Color color, Vector2 screenRatio, Vector2 anchoredOffset)
        {
            EnsureContainer();

            GameObject textObj = new GameObject("TreeDamageText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            Transform parentTransform = textContainer != null ? textContainer : transform;
            textObj.transform.SetParent(parentTransform, false);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            CanvasGroup group = textObj.GetComponent<CanvasGroup>();

            // Anchor bazlı konumlandırma (Canvas Scaler'dan etkilenmez)
            rect.anchorMin = screenRatio;
            rect.anchorMax = screenRatio;
            rect.anchoredPosition = anchoredOffset;
            rect.sizeDelta = new Vector2(350f, 80f);

            tmp.text = message;
            tmp.fontSize = treeDamageFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;

            // Belirgin Dış Çizgi Stili
            tmp.outlineWidth = 0.35f;
            tmp.outlineColor = Color.black;

            StartCoroutine(AnimateTreeDamageTextRoutine(rect, tmp, group));
        }

        private IEnumerator AnimateTreeDamageTextRoutine(RectTransform rect, TextMeshProUGUI tmp, CanvasGroup group)
        {
            Vector3 startPos = rect.position;
            Vector3 targetPos = startPos + new Vector3(0f, floatDistance * 0.9f, 0f);

            float damageDuration = duration * 0.85f;
            float elapsed = 0f;

            while (elapsed < damageDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / damageDuration);

                // Punchy Pop Animasyonu (Hızlı Büyüyüp Yumuşakça Yerleşme)
                float scale;
                if (t < 0.2f)
                {
                    scale = Mathf.Lerp(0.6f, 1.35f, t / 0.2f);
                }
                else if (t < 0.4f)
                {
                    scale = Mathf.Lerp(1.35f, 1.0f, (t - 0.2f) / 0.2f);
                }
                else
                {
                    scale = 1.0f;
                }

                rect.localScale = new Vector3(scale, scale, 1f);
                rect.position = Vector3.Lerp(startPos, targetPos, t);

                // Saydamlaşma (Fade Out)
                if (group != null)
                {
                    group.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.45f) / 0.55f));
                }

                yield return null;
            }

            if (rect != null && rect.gameObject != null)
            {
                Destroy(rect.gameObject);
            }
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

        [ContextMenu("TEST: Ağaç Hasar Yazısı Fırlat (Seviye 1 - Yeşil)")]
        public void TestTreeDamageMinLevel()
        {
            Color damageColor = minLevelDamageColor;
            string message = string.Format(treeDamageFormat, 1);
            Vector2 randomOffset = new Vector2(UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange), UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange));
            CreateAndAnimateTreeDamageText(message, damageColor, treeDamageScreenRatio, randomOffset);
        }

        [ContextMenu("TEST: Ağaç Hasar Yazısı Fırlat (Max Seviye - Kırmızı)")]
        public void TestTreeDamageMaxLevel()
        {
            Color damageColor = maxLevelDamageColor;
            string message = string.Format(treeDamageFormat, 10);
            Vector2 randomOffset = new Vector2(UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange), UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange));
            CreateAndAnimateTreeDamageText(message, damageColor, treeDamageScreenRatio, randomOffset);
        }

        [ContextMenu("TEST: Satış Tamamlandı Floating Text")]
        public void TestSaleCompletedText()
        {
            SpawnText("Satış Tamamlandı: +1.250 Altın!", goldColor);
            UIScreenFlash.Instance?.Flash(new Color(1f, 0.85f, 0.2f, 1f), 0.28f, 0.22f);
        }
    }
}
