using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Managers;
using ForrestGame.Core;

/// <summary>
/// Balta kademesi için Inspector'dan ayarlanabilir veri yapısı.
/// </summary>
[System.Serializable]
public class AxeTierData
{
    [Tooltip("Kademe adı (Örn: Paslı Balta, Çelik Balta vb.)")]
    public string tierName = "Balta Seviyesi";

    [Tooltip("Kartta gösterilecek balta ikonu.")]
    public Sprite icon;

    [Tooltip("AxeVisualManager için 3D görsel model ID'si (Örn: lowpoly_axe, axe_tier_2 vb.)")]
    public string visualAxeId = "";
}

/// <summary>
/// Evdeki Balta Masası (AxeTable) etkileşiminde açılan panel kontrolcüsü.
/// Projedeki ItemUpgradeConfig (Axe.asset) ve UpgradeManager ile %100 entegre çalışır.
/// </summary>
public class AxeTablePanelController : MonoBehaviour
{
    public static AxeTablePanelController Instance { get; private set; }

    [Header("--- Panel Referansları ---")]
    [Tooltip("Balta Masası ana panel objesi.")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Kapat (X) butonu.")]
    [SerializeField] private Button closeButton;

    [Header("--- Upgrade Config Referansı ---")]
    [Tooltip("Axe.asset (ItemUpgradeConfig) referansı. Boşsa UpgradeManager'dan otomatik çekilir.")]
    [SerializeField] private ItemUpgradeConfig axeUpgradeConfig;

    [Header("--- Balta Kademeleri (Inspector Ayarları) ---")]
    [Tooltip("Inspector'dan özel kademe isimleri, ikonları ve 3D model ID'leri eklemek için liste.")]
    [SerializeField] private List<AxeTierData> axeTiers = new List<AxeTierData>();

    [Header("--- Balta Liste İçeriği ---")]
    [Tooltip("Kaydırma alanı (ScrollRect). Kaydırma konumunun korunmasını sağlar.")]
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("Balta kademe kartlarının dizileceği Content transform'u.")]
    [SerializeField] private Transform axeListContent;

    [Tooltip("Balta kademe kartı prefab'ı (AxeItemUI bileşeni içermelidir).")]
    [SerializeField] private GameObject axeItemPrefab;

    [Header("--- Üst Statü Özet Metinleri ---")]
    [Tooltip("Mevcut kuşanılan baltanın gücü.")]
    [SerializeField] private TextMeshProUGUI currentDamageText;

    [Tooltip("Sonraki seviyedeki balta gücü.")]
    [SerializeField] private TextMeshProUGUI nextDamageText;

    [Tooltip("Oyuncu seviye göstergesi.")]
    [SerializeField] private TextMeshProUGUI playerLevelText;

    [Tooltip("Mevcut sepet kapasitesi göstergesi.")]
    [SerializeField] private TextMeshProUGUI basketCapacityText;

    [Header("--- Ayarlar ---")]
    [Tooltip("Gösterilecek toplam balta kademe sayısı (Varsayılan: 5).")]
    [SerializeField] private int displayTierCount = 5;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    private const string AXE_ITEM_ID = "Axe";
    private List<AxeItemUI> spawnedItems = new List<AxeItemUI>();

    // Dahili kuşanılan seviye takibi
    private int currentlyEquippedTier = 1;

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
        // Inspector'da axeUpgradeConfig boşsa UpgradeManager'dan otomatik çek
        if (axeUpgradeConfig == null && UpgradeManager.Instance != null)
        {
            // UpgradeManager'ın configs listesinden "Axe" ID'li config'i bul
            var configField = typeof(UpgradeManager).GetField("configs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                var configs = configField.GetValue(UpgradeManager.Instance) as List<ItemUpgradeConfig>;
                if (configs != null)
                {
                    axeUpgradeConfig = configs.Find(c => c != null && c.ItemID == AXE_ITEM_ID);
                    if (axeUpgradeConfig != null)
                        Debug.Log("[AxeTablePanel] axeUpgradeConfig, UpgradeManager'dan otomatik çekildi.");
                }
            }
        }

        SyncEquippedTierFromManager();

        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;

        AxeVisualManager.OnAxeChanged += HandleAxeChanged;

        SyncEquippedTierFromManager();

        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void OnDisable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;

        AxeVisualManager.OnAxeChanged -= HandleAxeChanged;
    }

    private void HandleMoneyChanged(int newMoney)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == AXE_ITEM_ID && mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleAxeChanged(string axeId)
    {
        SyncEquippedTierFromManager();
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleDataLoaded(PlayerData data)
    {
        SyncEquippedTierFromManager();
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Aç / Kapat
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        PlayButtonClickSFX();

        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SyncEquippedTierFromManager();
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
        bool isActive = mainPanel != null ? mainPanel.activeSelf : gameObject.activeSelf;
        if (isActive)
            ClosePanel();
        else
            OpenPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Yenileme & Liste Doldurma
    // ═══════════════════════════════════════════

    public void RefreshPanel()
    {
        UpdateHeaderStats();
        PopulateAxeList();
    }

    private void UpdateHeaderStats()
    {
        int playerLevel = XPManager.Instance != null ? XPManager.Instance.MevcutSeviye : 1;
        int basketCapacity = BasketManager.Instance != null ? BasketManager.Instance.MaksKapasite : 5;

        int maxUnlockedAxeLevel = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel(AXE_ITEM_ID) : 1;

        float currentPower = GetStatValueForLevel(currentlyEquippedTier);
        float nextPower = GetStatValueForLevel(maxUnlockedAxeLevel + 1);

        if (playerLevelText != null)
            playerLevelText.text = $"Seviye: {playerLevel}";

        if (basketCapacityText != null)
            basketCapacityText.text = $"Sepet Kapasitesi: {basketCapacity}";

        if (currentDamageText != null)
            currentDamageText.text = $"Kuşanılan Balta Gücü: {currentPower:F0} (Sev. {currentlyEquippedTier})";

        if (nextDamageText != null)
            nextDamageText.text = $"Sonraki Yükseltme Gücü: {nextPower:F0}";
    }

    private void PopulateAxeList()
    {
        float savedScrollPos = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1f;

        ClearSpawnedItems();

        if (axeListContent == null || axeItemPrefab == null) return;

        int currentAxeLevel = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel(AXE_ITEM_ID) : 1;
        int playerGold = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0;

        for (int tier = 1; tier <= displayTierCount; tier++)
        {
            GameObject obj = Instantiate(axeItemPrefab, axeListContent, false);
            obj.SetActive(true);

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }

            AxeItemUI itemUI = obj.GetComponent<AxeItemUI>();
            if (itemUI != null)
            {
                float statValue = GetStatValueForLevel(tier);
                int costToUnlock = GetUpgradeCostForLevel(tier - 1);

                bool isUnlocked = (tier <= currentAxeLevel);
                bool isNextUpgrade = (tier == currentAxeLevel + 1);
                bool isEquipped = (tier == currentlyEquippedTier);

                Sprite icon = GetTierIcon(tier);
                string tierName = GetTierName(tier);
                int capturedTier = tier;

                itemUI.Setup(
                    tier,
                    tierName,
                    icon,
                    statValue,
                    costToUnlock,
                    isUnlocked,
                    isEquipped,
                    isNextUpgrade,
                    playerGold,
                    () => OnAxeTierClicked(capturedTier, isUnlocked, isNextUpgrade)
                );

                spawnedItems.Add(itemUI);
            }
        }

        if (axeListContent is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        if (scrollRect != null)
        {
            StartCoroutine(RestoreScrollPositionRoutine(savedScrollPos));
        }
    }

    private IEnumerator RestoreScrollPositionRoutine(float position)
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = position;
        }
    }

    private void OnAxeTierClicked(int tier, bool isUnlocked, bool isNextUpgrade)
    {
        PlayButtonClickSFX();

        string visualAxeId = GetVisualAxeId(tier);

        if (isUnlocked)
        {
            // Kazanılmış seviyedeki balta görselini giy
            currentlyEquippedTier = tier;

            if (AxeVisualManager.Instance != null)
            {
                AxeVisualManager.Instance.EquipAxe(visualAxeId);
            }

            string nameStr = GetTierName(tier);
            NotificationUIController.Instance?.ShowNotification($"{nameStr} kuşanıldı!");
        }
        else if (isNextUpgrade)
        {
            // UpgradeManager üzerinden seviye yükselt (altın harcanır, cloud save yapılır)
            if (UpgradeManager.Instance != null)
            {
                bool success = UpgradeManager.Instance.UpgradeItem(AXE_ITEM_ID);
                if (success)
                {
                    currentlyEquippedTier = tier;

                    if (AxeVisualManager.Instance != null)
                    {
                        AxeVisualManager.Instance.EquipAxe(visualAxeId);
                    }

                    string nameStr = GetTierName(tier);
                    NotificationUIController.Instance?.ShowNotification($"{nameStr} seviyesine yükseltildi!");
                }
                else
                {
                    NotificationUIController.Instance?.ShowNotification("Yetersiz Altın!");
                }
            }
        }

        RefreshPanel();
    }

    private void SyncEquippedTierFromManager()
    {
        if (AxeVisualManager.Instance == null) return;

        string id = AxeVisualManager.Instance.currentAxeId;
        if (string.IsNullOrEmpty(id))
        {
            currentlyEquippedTier = 1;
            return;
        }

        // Önce Inspector'daki axeTiers listesinden eşleşen visualAxeId ara
        if (axeTiers != null)
        {
            for (int i = 0; i < axeTiers.Count; i++)
            {
                if (axeTiers[i] != null && !string.IsNullOrEmpty(axeTiers[i].visualAxeId) &&
                    axeTiers[i].visualAxeId.Equals(id, System.StringComparison.OrdinalIgnoreCase))
                {
                    currentlyEquippedTier = i + 1;
                    return;
                }
            }
        }

        // Fallback varsayılan kontrol
        if (id == "lowpoly_axe" || id == "axe_tier_1")
        {
            currentlyEquippedTier = 1;
            return;
        }

        if (id.StartsWith("axe_tier_"))
        {
            if (int.TryParse(id.Replace("axe_tier_", ""), out int t))
            {
                currentlyEquippedTier = t;
            }
        }
    }

    private float GetStatValueForLevel(int level)
    {
        if (axeUpgradeConfig != null)
            return axeUpgradeConfig.GetStatValue(level);

        if (UpgradeManager.Instance != null)
            return UpgradeManager.Instance.GetItemValue(AXE_ITEM_ID);

        return 10f + (level - 1) * 5f;
    }

    private int GetUpgradeCostForLevel(int level)
    {
        if (axeUpgradeConfig != null)
            return axeUpgradeConfig.GetUpgradeCost(level);

        if (UpgradeManager.Instance != null)
            return UpgradeManager.Instance.GetUpgradeCost(AXE_ITEM_ID);

        return Mathf.RoundToInt(50f * Mathf.Pow(1.5f, level - 1));
    }

    private string GetTierName(int tier)
    {
        // 1. Önce Inspector'daki axeTiers listesine bak
        if (axeTiers != null && tier - 1 < axeTiers.Count && axeTiers[tier - 1] != null &&
            !string.IsNullOrEmpty(axeTiers[tier - 1].tierName))
        {
            return axeTiers[tier - 1].tierName;
        }

        // 2. Varsayılan dinamik isim (fallback)
        return $"Seviye {tier} Balta";
    }

    private Sprite GetTierIcon(int tier)
    {
        // Inspector'daki axeTiers listesinden ikonu al
        if (axeTiers != null && tier - 1 < axeTiers.Count && axeTiers[tier - 1] != null)
        {
            return axeTiers[tier - 1].icon;
        }

        return null;
    }

    private string GetVisualAxeId(int tier)
    {
        // 1. Önce Inspector'daki axeTiers listesine bak
        if (axeTiers != null && tier - 1 < axeTiers.Count && axeTiers[tier - 1] != null &&
            !string.IsNullOrEmpty(axeTiers[tier - 1].visualAxeId))
        {
            return axeTiers[tier - 1].visualAxeId;
        }

        // 2. Varsayılan ID kuralı (fallback)
        return (tier == 1) ? "lowpoly_axe" : $"axe_tier_{tier}";
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

    // ═══════════════════════════════════════════
    //  Ses
    // ═══════════════════════════════════════════

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }
}
