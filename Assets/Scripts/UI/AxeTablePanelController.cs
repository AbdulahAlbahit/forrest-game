using System.Collections;
using System.Collections.Generic;
using ForrestGame.Core;
using ForrestGame.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[System.Serializable]
public class AxeTierData
{
    [Tooltip("Bu kartın bağlı olduğu bağımsız ItemUpgradeConfig ItemID değeri.")]
    public string itemId = "";

    [Tooltip("Kartta gösterilecek balta adı.")]
    public string tierName = "Balta";

    [Tooltip("Kartta gösterilecek balta ikonu.")]
    public Sprite icon;

    [Tooltip("AxeVisualManager tarafından kullanılan 3D balta model ID'si.")]
    public string visualAxeId = "";

    [Min(0)]
    [Tooltip("Baltanın ilk kilidini açma fiyatı. İlk balta her zaman ücretsiz ve açıktır.")]
    public int purchasePrice;
}

/// <summary>
/// Beş bağımsız baltayı kendi ItemID, sahiplik, seviye, fiyat ve güç değerleriyle yönetir.
/// Kilitli kart satın alınır; açık kart kuşanılır. Yükseltme yalnızca upgrade zone'da yapılır.
/// </summary>
public class AxeTablePanelController : MonoBehaviour
{
    public static AxeTablePanelController Instance { get; private set; }

    [Header("--- Panel Referansları ---")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button closeButton;

    [Header("--- Eski Tek Balta Referansı (Geriye Uyumluluk) ---")]
    [Tooltip("Eski sahnelerin Axe kartı için fallback config'i. Yeni kartlar itemId kullanır.")]
    [SerializeField] private ItemUpgradeConfig axeUpgradeConfig;

    [Header("--- Bağımsız Baltalar ---")]
    [SerializeField] private List<AxeTierData> axeTiers = new List<AxeTierData>();

    [Header("--- Balta Liste İçeriği ---")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform axeListContent;
    [SerializeField] private GameObject axeItemPrefab;

    [Header("--- Üst Statü Özet Metinleri ---")]
    [SerializeField] private TextMeshProUGUI currentDamageText;
    [SerializeField] private TextMeshProUGUI nextDamageText;
    [FormerlySerializedAs("playerLevelText")]
    [SerializeField] private TextMeshProUGUI selectedAxeNameText;
    [FormerlySerializedAs("basketCapacityText")]
    [SerializeField] private TextMeshProUGUI upgradePriceText;

    [Header("--- Ayarlar ---")]
    [Min(1)]
    [SerializeField] private int displayTierCount = 5;

    [Header("--- Ses Ayarları ---")]
    [SerializeField] private string buttonSfxName = "";

    public const string EquippedAxeSaveKey = "EquippedAxe";
    private readonly List<AxeItemUI> spawnedItems = new List<AxeItemUI>();

    // Bu değer artık balta seviyesi değil, axeTiers listesindeki 1 tabanlı kart indeksidir.
    private int currentlyEquippedTier = 1;
    private bool upgradeEventSubscribed;
    private bool dataEventSubscribed;
    private bool axePurchaseInProgress;

    // ─── Onay Modalı ───
    private GameObject confirmModalRoot;
    private int confirmPendingTier;
    private bool confirmModalActive;
    private RectTransform confirmButtonRect;
    private Image confirmButtonImage;
    private Coroutine shakeCoroutine;

    public int CurrentlyEquippedTier => currentlyEquippedTier;
    public string CurrentlyEquippedItemId => GetItemId(currentlyEquippedTier);
    public string CurrentlyEquippedDisplayName => GetTierName(currentlyEquippedTier);
    public int CurrentlyEquippedLevel => GetItemLevel(CurrentlyEquippedItemId);
    public float CurrentlyEquippedPower => GetItemValue(CurrentlyEquippedItemId);

    public static event System.Action OnAxeEquipped;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void Start()
    {
        SubscribeEvents();
        SyncEquippedFromAvailableData();

        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        SubscribeEvents();
        AxeVisualManager.OnAxeChanged += HandleAxeChanged;
        SyncEquippedFromAvailableData();

        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void OnDisable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;

        if (upgradeEventSubscribed && UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;

        if (dataEventSubscribed && CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;

        AxeVisualManager.OnAxeChanged -= HandleAxeChanged;
        upgradeEventSubscribed = false;
        dataEventSubscribed = false;
    }

    private void SubscribeEvents()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
            EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        }

        if (!upgradeEventSubscribed && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;
            upgradeEventSubscribed = true;
        }

        if (!dataEventSubscribed && CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
            dataEventSubscribed = true;
        }
    }

    private void HandleMoneyChanged(int newMoney)
    {
        if (!axePurchaseInProgress && IsPanelOpen())
            RefreshPanel();
    }

    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (IsAxeItemId(itemId))
        {
            OnAxeEquipped?.Invoke();
            if (IsPanelOpen())
                RefreshPanel();
        }
    }

    private void HandleAxeChanged(string axeId)
    {
        SyncEquippedTierFromVisual(axeId);
        OnAxeEquipped?.Invoke();

        if (IsPanelOpen())
            RefreshPanel();
    }

    private void HandleDataLoaded(PlayerData data)
    {
        if (data != null)
            SyncEquippedAxeFromData(data);
        else
            SyncEquippedTierFromManager();

        if (IsPanelOpen())
            RefreshPanel();
    }

    private void SyncEquippedFromAvailableData()
    {
        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
            SyncEquippedAxeFromData(CloudSaveManager.Instance.CurrentData);
        else
            SyncEquippedTierFromManager();
    }

    private void SyncEquippedAxeFromData(PlayerData data)
    {
        if (data == null) return;

        int savedIndex = data.ItemLevels != null && data.ItemLevels.ContainsKey(EquippedAxeSaveKey)
            ? data.GetItemLevel(EquippedAxeSaveKey)
            : 1;

        currentlyEquippedTier = Mathf.Clamp(savedIndex, 1, GetDisplayedAxeCount());
        if (!IsTierOwned(currentlyEquippedTier, data))
        {
            currentlyEquippedTier = 1;
            data.SetItemLevel(EquippedAxeSaveKey, currentlyEquippedTier);
            CloudSaveManager.Instance?.SaveData(data);
        }

        if (!EquipVisual(currentlyEquippedTier))
            OnAxeEquipped?.Invoke();
    }

    public void OpenPanel()
    {
        PlayButtonClickSFX();

        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SubscribeEvents();
        SyncEquippedFromAvailableData();
        RefreshPanel();
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();

        if (mainPanel != null)
            mainPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void TogglePanel()
    {
        if (IsPanelOpen())
            ClosePanel();
        else
            OpenPanel();
    }

    public void RefreshPanel()
    {
        UpdateHeaderStats();
        PopulateAxeList();
    }

    private void UpdateHeaderStats()
    {
        string itemId = CurrentlyEquippedItemId;
        int axeLevel = GetItemLevel(itemId);
        float currentPower = GetItemValue(itemId);
        bool isMaxLevel = UpgradeManager.Instance != null && UpgradeManager.Instance.IsMaxLevel(itemId);
        float nextPower = isMaxLevel ? currentPower : GetItemValueAtLevel(itemId, axeLevel + 1);
        int upgradeCost = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetUpgradeCost(itemId)
            : 0;

        if (selectedAxeNameText != null)
            selectedAxeNameText.text = GetTierName(currentlyEquippedTier);

        if (upgradePriceText != null)
            upgradePriceText.text = isMaxLevel
                ? "Yükseltme: Maksimum Seviye"
                : $"Yükseltme Fiyatı: {upgradeCost} Para";

        if (currentDamageText != null)
            currentDamageText.text = $"Seviye: {axeLevel} | Güç: {currentPower:F0}";

        if (nextDamageText != null)
            nextDamageText.text = isMaxLevel
                ? "Bu balta maksimum seviyede"
                : $"Sonraki Seviye Gücü: {nextPower:F0}";
    }

    private void PopulateAxeList()
    {
        float savedScrollPosition = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1f;
        ClearSpawnedItems();

        if (axeListContent == null || axeItemPrefab == null) return;

        int count = GetDisplayedAxeCount();

        for (int tier = 1; tier <= count; tier++)
        {
            string itemId = GetItemId(tier);
            int currentLevel = GetItemLevel(itemId);
            float statValue = GetItemValue(itemId);
            bool isOwned = IsTierOwned(tier);
            bool isEquipped = tier == currentlyEquippedTier;

            GameObject itemObject = Instantiate(axeItemPrefab, axeListContent, false);
            itemObject.SetActive(true);

            if (itemObject.TryGetComponent(out RectTransform rect))
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }

            AxeItemUI itemUI = itemObject.GetComponent<AxeItemUI>();
            if (itemUI == null) continue;

            int capturedTier = tier;
            itemUI.SetupIndependent(
                tier,
                GetTierName(tier),
                GetTierIcon(tier),
                statValue,
                currentLevel,
                isOwned,
                isEquipped,
                GetPurchasePrice(tier),
                () => HandleAxeAction(capturedTier));

            spawnedItems.Add(itemUI);
        }

        if (axeListContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        if (scrollRect != null)
            StartCoroutine(RestoreScrollPositionRoutine(savedScrollPosition));
    }

    private IEnumerator RestoreScrollPositionRoutine(float position)
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = position;
    }

    private void EquipAxe(int tier)
    {
        if (!IsTierOwned(tier))
        {
            NotificationUIController.Instance?.ShowNotification(
                $"{GetTierName(tier)} önce satın alınmalı!");
            RefreshPanel();
            return;
        }

        PlayButtonClickSFX();
        currentlyEquippedTier = Mathf.Clamp(tier, 1, GetDisplayedAxeCount());

        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            CloudSaveManager.Instance.CurrentData.SetItemLevel(
                EquippedAxeSaveKey,
                currentlyEquippedTier);
            CloudSaveManager.Instance.SaveData(CloudSaveManager.Instance.CurrentData);
        }

        bool visualChanged = EquipVisual(currentlyEquippedTier);
        NotificationUIController.Instance?.ShowNotification(
            $"{GetTierName(currentlyEquippedTier)} kuşanıldı!");
        if (!visualChanged)
            OnAxeEquipped?.Invoke();
        RefreshPanel();
    }

    private void HandleAxeAction(int tier)
    {
        ShowConfirmModal(tier);
    }

    // ═════════════════════════════════════════════════════════════════
    //  ONAY MODALI
    // ═════════════════════════════════════════════════════════════════

    private void ShowConfirmModal(int tier)
    {
        if (confirmModalActive) return;

        // Canvas bul
        Canvas canvas = mainPanel != null
            ? mainPanel.GetComponentInParent<Canvas>()
            : GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            // Canvas yoksa doğrudan işlemi yap
            ExecuteAxeAction(tier);
            return;
        }

        confirmPendingTier = tier;
        confirmModalActive = true;

        // ─── Overlay (yarı şeffaf siyah) ───
        confirmModalRoot = new GameObject("AxeConfirmModal", typeof(RectTransform), typeof(Image));
        confirmModalRoot.transform.SetParent(canvas.transform, false);
        confirmModalRoot.transform.SetAsLastSibling();

        RectTransform overlayRect = confirmModalRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;
        confirmModalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        confirmModalRoot.GetComponent<Image>().raycastTarget = true;

        // ─── Kart paneli (merkez) ───
        GameObject card = new GameObject("ModalCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        card.transform.SetParent(confirmModalRoot.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(500f, 310f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.10f, 0.12f, 0.17f, 1f);
        CanvasGroup cardGroup = card.GetComponent<CanvasGroup>();

        BuildModalContent(card.transform, tier);

        // Açılış animasyonu
        StartCoroutine(ModalOpenRoutine(cardRect, cardGroup));
    }

    private void BuildModalContent(Transform parent, int tier)
    {
        bool isOwned = IsTierOwned(tier);
        string tierName = GetTierName(tier);
        string equippedName = GetTierName(currentlyEquippedTier);
        float currentPower = GetItemValue(CurrentlyEquippedItemId);
        float newPower = GetItemValue(GetItemId(tier));
        int price = GetPurchasePrice(tier);
        int playerMoney = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0;
        bool canAfford = isOwned || playerMoney >= price;

        // ─── Başlık ───
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(parent, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 52f);
        titleRect.anchoredPosition = new Vector2(0f, -14f);
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.text = isOwned ? $"{tierName} Kuşan" : $"{tierName} Satın Al";
        titleTMP.fontSize = 22f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.9f, 0.4f, 1f);

        // ─── Karşılaştırma metni ───
        GameObject infoObj = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoObj.transform.SetParent(parent, false);
        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0f, 0.35f);
        infoRect.anchorMax = new Vector2(1f, 0.85f);
        infoRect.sizeDelta = new Vector2(-30f, 0f);
        infoRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI infoTMP = infoObj.GetComponent<TextMeshProUGUI>();

        string powerChange = "";
        if (!Mathf.Approximately(currentPower, 0f) && newPower > currentPower)
            powerChange = $" <color=#78FF78>(+{(newPower - currentPower):F0})</color>";
        else if (newPower < currentPower)
            powerChange = $" <color=#FF7878>({(newPower - currentPower):F0})</color>";

        string priceInfo = isOwned
            ? ""
            : $"\n\n<size=17>Fiyat: <color=#FFD740>{price:N0} Para</color>" +
              $"  |  Bakıye: <color={(canAfford ? "#78FF78" : "#FF7878")}>{playerMoney:N0} Para</color></size>";

        infoTMP.text = $"<size=15>Mevcut: <color=#AAAAAA>{equippedName}</color>  Güç: {currentPower:F0}</size>\n" +
                       $"<size=17>Hedef:  <color=#FFFFFF>{tierName}</color>  Güç: <color=#FFD740>{newPower:F0}</color>{powerChange}</size>" +
                       priceInfo;
        infoTMP.fontSize = 16f;
        infoTMP.alignment = TextAlignmentOptions.Center;
        infoTMP.richText = true;
        infoTMP.color = Color.white;

        // ─── İptal Butonu ───
        GameObject cancelObj = new GameObject("CancelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        cancelObj.transform.SetParent(parent, false);
        RectTransform cancelRect = cancelObj.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.05f, 0f);
        cancelRect.anchorMax = new Vector2(0.46f, 0f);
        cancelRect.sizeDelta = new Vector2(0f, 52f);
        cancelRect.anchoredPosition = new Vector2(0f, 22f);
        cancelObj.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.28f, 1f);
        Button cancelBtn = cancelObj.GetComponent<Button>();
        cancelBtn.onClick.AddListener(HideConfirmModal);
        AddButtonText(cancelObj.transform, "İptal", 17f, Color.white);

        // ─── Onayla Butonu ───
        GameObject confirmObj = new GameObject("ConfirmBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        confirmObj.transform.SetParent(parent, false);
        confirmButtonRect = confirmObj.GetComponent<RectTransform>();
        confirmButtonRect.anchorMin = new Vector2(0.54f, 0f);
        confirmButtonRect.anchorMax = new Vector2(0.95f, 0f);
        confirmButtonRect.sizeDelta = new Vector2(0f, 52f);
        confirmButtonRect.anchoredPosition = new Vector2(0f, 22f);
        confirmButtonImage = confirmObj.GetComponent<Image>();
        confirmButtonImage.color = canAfford
            ? new Color(0.12f, 0.65f, 0.30f, 1f)  // Yeşil
            : new Color(0.55f, 0.12f, 0.12f, 1f); // Kırmızı
        Button confirmBtn = confirmObj.GetComponent<Button>();
        int capturedTier = tier;
        confirmBtn.onClick.AddListener(() => OnConfirmClicked(capturedTier));
        AddButtonText(confirmObj.transform, isOwned ? "Kuşan" : "Satın Al", 17f, Color.white);
    }

    private void AddButtonText(Transform parent, string text, float fontSize, Color color)
    {
        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(parent, false);
        RectTransform r = txtObj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero;
        r.anchoredPosition = Vector2.zero;
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
    }

    private void OnConfirmClicked(int tier)
    {
        bool isOwned = IsTierOwned(tier);
        if (!isOwned)
        {
            int price = GetPurchasePrice(tier);
            int playerMoney = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0;
            if (playerMoney < price)
            {
                // Yetersiz para → shake + bildirim (modal kapanmıyor)
                TriggerConfirmButtonShake();
                NotificationUIController.Instance?.ShowNotification(
                    $"Yetersiz Para! {price - playerMoney:N0} daha gerekiyor.");
                return;
            }
        }

        HideConfirmModal();
        ExecuteAxeAction(tier);
    }

    private void TriggerConfirmButtonShake()
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        if (confirmButtonRect != null)
            shakeCoroutine = StartCoroutine(ConfirmButtonShakeRoutine());
    }

    private IEnumerator ConfirmButtonShakeRoutine()
    {
        Vector3 originalPos = confirmButtonRect.anchoredPosition3D;
        Color originalColor = confirmButtonImage != null ? confirmButtonImage.color : Color.red;
        Color flashRed = new Color(0.85f, 0.1f, 0.1f, 1f);

        if (confirmButtonImage != null) confirmButtonImage.color = flashRed;

        float elapsed = 0f;
        float totalDur = 0.40f;
        float amount = 11f;

        while (elapsed < totalDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / totalDur;
            float offset = Mathf.Sin(t * Mathf.PI * 7f) * amount * (1f - t);
            confirmButtonRect.anchoredPosition3D = originalPos + new Vector3(offset, 0f, 0f);
            yield return null;
        }

        confirmButtonRect.anchoredPosition3D = originalPos;
        if (confirmButtonImage != null) confirmButtonImage.color = originalColor;
        shakeCoroutine = null;
    }

    private void HideConfirmModal()
    {
        confirmModalActive = false;
        confirmButtonRect = null;
        confirmButtonImage = null;
        if (shakeCoroutine != null) { StopCoroutine(shakeCoroutine); shakeCoroutine = null; }

        if (confirmModalRoot != null)
        {
            StartCoroutine(ModalCloseAndDestroyRoutine(confirmModalRoot));
            confirmModalRoot = null;
        }
    }

    private void ExecuteAxeAction(int tier)
    {
        if (IsTierOwned(tier))
            EquipAxe(tier);
        else
            PurchaseAxe(tier);
    }

    private IEnumerator ModalOpenRoutine(RectTransform cardRect, CanvasGroup cardGroup)
    {
        cardGroup.alpha = 0f;
        cardRect.localScale = new Vector3(0.75f, 0.75f, 0.75f);

        float elapsed = 0f;
        float dur = 0.22f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float smooth = t * (2f - t); // EaseOutQuad
            cardGroup.alpha = smooth;
            cardRect.localScale = Vector3.LerpUnclamped(new Vector3(0.75f, 0.75f, 1f), Vector3.one, smooth);
            yield return null;
        }
        cardGroup.alpha = 1f;
        cardRect.localScale = Vector3.one;
    }

    private IEnumerator ModalCloseAndDestroyRoutine(GameObject modalRoot)
    {
        CanvasGroup group = modalRoot.GetComponentInChildren<CanvasGroup>();
        RectTransform rt = group != null ? group.GetComponent<RectTransform>() : null;

        float elapsed = 0f;
        float dur = 0.16f;
        float startAlpha = group != null ? group.alpha : 1f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            if (group != null) group.alpha = Mathf.Lerp(startAlpha, 0f, t);
            if (rt != null) rt.localScale = Vector3.LerpUnclamped(Vector3.one, new Vector3(0.85f, 0.85f, 1f), t);
            yield return null;
        }

        if (modalRoot != null)
            Destroy(modalRoot);
    }

    private void PurchaseAxe(int tier)
    {
        PlayButtonClickSFX();

        if (tier <= 1 || IsTierOwned(tier))
        {
            EquipAxe(tier);
            return;
        }

        CloudSaveManager cloudSaveManager = CloudSaveManager.Instance;
        PlayerData data = cloudSaveManager != null ? cloudSaveManager.CurrentData : null;
        if (data == null || EconomyManager.Instance == null)
        {
            NotificationUIController.Instance?.ShowNotification(
                "Oyuncu verileri henüz hazır değil. Lütfen tekrar deneyin.");
            return;
        }

        int purchasePrice = GetPurchasePrice(tier);
        if (purchasePrice <= 0)
        {
            NotificationUIController.Instance?.ShowNotification(
                $"{GetTierName(tier)} için geçerli satın alma fiyatı bulunamadı.");
            return;
        }

        axePurchaseInProgress = true;
        bool paymentSucceeded = EconomyManager.Instance.SpendMoney(purchasePrice);
        if (!paymentSucceeded)
        {
            axePurchaseInProgress = false;
            RefreshPanel();
            return;
        }

        string itemId = GetItemId(tier);
        data.SetItemLevel(AxeVisualManager.GetOwnershipSaveKey(itemId), 1);
        data.SetItemLevel(itemId, 1);
        cloudSaveManager.SaveData(data);
        axePurchaseInProgress = false;

        NotificationUIController.Instance?.ShowNotification(
            $"{GetTierName(tier)} açıldı! Artık kuşanabilirsiniz.");
        RefreshPanel();
    }

    private bool EquipVisual(int tier)
    {
        if (AxeVisualManager.Instance == null)
            return false;

        string visualId = GetVisualAxeId(tier);
        bool willChange = !string.Equals(
            AxeVisualManager.Instance.currentAxeId,
            visualId,
            System.StringComparison.OrdinalIgnoreCase);
        AxeVisualManager.Instance.EquipAxe(visualId);
        return willChange;
    }

    private void SyncEquippedTierFromManager()
    {
        if (AxeVisualManager.Instance != null)
            SyncEquippedTierFromVisual(AxeVisualManager.Instance.currentAxeId);
    }

    private void SyncEquippedTierFromVisual(string visualId)
    {
        if (string.IsNullOrWhiteSpace(visualId)) return;

        int count = GetDisplayedAxeCount();
        for (int tier = 1; tier <= count; tier++)
        {
            if (GetVisualAxeId(tier).Equals(
                    visualId,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                currentlyEquippedTier = tier;
                return;
            }
        }
    }

    private int GetDisplayedAxeCount()
    {
        int configuredCount = axeTiers != null ? axeTiers.Count : 0;
        return Mathf.Max(1, Mathf.Min(displayTierCount, Mathf.Max(1, configuredCount)));
    }

    private AxeTierData GetTierData(int tier)
    {
        int index = tier - 1;
        return axeTiers != null && index >= 0 && index < axeTiers.Count
            ? axeTiers[index]
            : null;
    }

    private string GetItemId(int tier)
    {
        AxeTierData entry = GetTierData(tier);
        if (entry != null && !string.IsNullOrWhiteSpace(entry.itemId))
            return entry.itemId.Trim();

        return tier <= 1 ? "Axe" : $"Axe{tier}";
    }

    private string GetTierName(int tier)
    {
        AxeTierData entry = GetTierData(tier);
        if (entry != null && !string.IsNullOrWhiteSpace(entry.tierName))
            return entry.tierName;

        return $"Balta {tier}";
    }

    private Sprite GetTierIcon(int tier)
    {
        return GetTierData(tier)?.icon;
    }

    private string GetVisualAxeId(int tier)
    {
        AxeTierData entry = GetTierData(tier);
        if (entry != null && !string.IsNullOrWhiteSpace(entry.visualAxeId))
            return entry.visualAxeId.Trim();

        return tier == 1 ? "lowpoly_axe" : $"axe_tier_{tier}";
    }

    private int GetPurchasePrice(int tier)
    {
        if (tier <= 1) return 0;

        AxeTierData entry = GetTierData(tier);
        return entry != null ? Mathf.Max(0, entry.purchasePrice) : 0;
    }

    private bool IsTierOwned(int tier, PlayerData data = null)
    {
        if (tier <= 1) return true;

        data ??= CloudSaveManager.Instance != null
            ? CloudSaveManager.Instance.CurrentData
            : null;
        return AxeVisualManager.IsAxeOwned(data, tier, GetItemId(tier));
    }

    private int GetItemLevel(string itemId)
    {
        return UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetItemLevel(itemId)
            : 1;
    }

    private float GetItemValue(string itemId)
    {
        if (UpgradeManager.Instance != null)
            return UpgradeManager.Instance.GetItemValue(itemId);

        if (itemId == "Axe" && axeUpgradeConfig != null)
            return axeUpgradeConfig.GetStatValue(1);

        return 1f;
    }

    private float GetItemValueAtLevel(string itemId, int level)
    {
        if (UpgradeManager.Instance != null)
            return UpgradeManager.Instance.GetItemValueAtLevel(itemId, level);

        if (itemId == "Axe" && axeUpgradeConfig != null)
            return axeUpgradeConfig.GetStatValue(level);

        return GetItemValue(itemId);
    }

    private bool IsAxeItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return false;

        int count = GetDisplayedAxeCount();
        for (int tier = 1; tier <= count; tier++)
        {
            if (GetItemId(tier).Equals(
                    itemId,
                    System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool IsPanelOpen()
    {
        return mainPanel != null ? mainPanel.activeSelf : gameObject.activeSelf;
    }

    private void ClearSpawnedItems()
    {
        if (axeListContent != null)
        {
            for (int i = axeListContent.childCount - 1; i >= 0; i--)
            {
                Transform child = axeListContent.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        spawnedItems.Clear();
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonSfxName);
    }
}
