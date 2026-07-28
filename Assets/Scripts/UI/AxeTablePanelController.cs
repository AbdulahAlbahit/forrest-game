using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Managers;

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

    [Tooltip("Balta kademeleri için ikon görselleri (Seviye 1, 2, 3, 4, 5...).")]
    [SerializeField] private List<Sprite> axeTierIcons = new List<Sprite>();

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

    // ═══════════════════════════════════════════
    //  Panel Aç / Kapat
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SyncEquippedTierFromManager();
        RefreshPanel();
    }

    public void ClosePanel()
    {
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

                Sprite icon = (axeTierIcons != null && tier - 1 < axeTierIcons.Count) ? axeTierIcons[tier - 1] : null;
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
        if (isUnlocked)
        {
            // Kazanılmış seviyedeki balta görselini giy
            currentlyEquippedTier = tier;
            string visualAxeId = (tier == 1) ? "lowpoly_axe" : $"axe_tier_{tier}";

            if (AxeVisualManager.Instance != null)
            {
                AxeVisualManager.Instance.EquipAxe(visualAxeId);
            }

            NotificationUIController.Instance?.ShowNotification($"Seviye {tier} balta kuşanıldı!");
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
                    string visualAxeId = (tier == 1) ? "lowpoly_axe" : $"axe_tier_{tier}";

                    if (AxeVisualManager.Instance != null)
                    {
                        AxeVisualManager.Instance.EquipAxe(visualAxeId);
                    }

                    NotificationUIController.Instance?.ShowNotification($"Balta Seviye {tier}'e yükseltildi!");
                }
            }
        }

        RefreshPanel();
    }

    private void SyncEquippedTierFromManager()
    {
        if (AxeVisualManager.Instance == null) return;

        string id = AxeVisualManager.Instance.currentAxeId;
        if (string.IsNullOrEmpty(id) || id == "lowpoly_axe" || id == "axe_tier_1")
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
        switch (tier)
        {
            case 1: return "Başlangıç Baltası";
            case 2: return "Paslı Balta";
            case 3: return "Çelik Balta";
            case 4: return "Altın Balta";
            case 5: return "Elmas Balta";
            default: return $"Seviye {tier} Balta";
        }
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
}
