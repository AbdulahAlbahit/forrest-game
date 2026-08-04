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
        if (IsTierOwned(tier))
            EquipAxe(tier);
        else
            PurchaseAxe(tier);
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
