using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;
using ForrestGame.Managers;

/// <summary>
/// Envanter (Inventory) Paneli Ana Kontrolcüsü.
/// Üç sekmeden oluşur:
/// 1. KAYNAKLAR (Altın, Sepet Odunu, Ambar Odunu, Atölye Ham Odunu)
/// 2. ÜRÜNLER (Mağaza stoğundaki üretilmiş mobilyalar)
/// 3. EKİPMAN & UPGRADE (Balta, Sepet, Kamyon, Atölye Hızı ve Atölye Kuyruğu Yükseltmeleri)
/// 
/// EKİP KURALLARI UYUMU:
/// - Sadece bu UI scripti üzerinden çalışır, backend Manager'larına dokunmaz.
/// - Upgrade işlemi için doğrudan UpgradeManager.Instance.UpgradeItem() çağırır.
/// - Veri kaydetme işlemi UpgradeManager / EconomyManager içinde otomatik gerçekleşir.
/// </summary>
public class InventoryPanelController : MonoBehaviour
{
    public static InventoryPanelController Instance { get; private set; }

    public enum InventoryTab
    {
        Resources,
        Products,
        Upgrades
    }

    [Header("--- Panel Referansları ---")]
    [Tooltip("Envanter paneli ana GameObject'i.")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Kapat (X) butonu.")]
    [SerializeField] private Button closeButton;

    [Header("--- Sekme Butonları ---")]
    [Tooltip("Kaynaklar sekmesi açma butonu.")]
    [SerializeField] private Button resourcesTabButton;

    [Tooltip("Ürünler sekmesi açma butonu.")]
    [SerializeField] private Button productsTabButton;

    [Tooltip("Ekipman & Upgrade sekmesi açma butonu.")]
    [SerializeField] private Button upgradesTabButton;

    [Header("--- Sekme İçerik Alanları ---")]
    [Tooltip("Kaynaklar sekmesi içerik paneli (StatRowUI prefab'ları eklenir).")]
    [SerializeField] private Transform resourcesContent;

    [Tooltip("Ürünler sekmesi içerik paneli (ShopItemUI prefab'ları eklenir).")]
    [SerializeField] private Transform productsContent;

    [Tooltip("Ekipman sekmesi içerik paneli (UpgradeItemUI prefab'ları eklenir).")]
    [SerializeField] private Transform upgradesContent;

    [Header("--- Prefab Referansları ---")]
    [Tooltip("Stat / Kaynak satırı prefab'ı (StatRowUI bileşeni içermelidir).")]
    [SerializeField] private GameObject statRowPrefab;

    [Tooltip("Ürün kartı prefab'ı (ShopItemUI bileşeni içermelidir).")]
    [SerializeField] private GameObject shopItemPrefab;

    [Tooltip("Upgrade kartı prefab'ı (UpgradeItemUI bileşeni içermelidir).")]
    [SerializeField] private GameObject upgradeItemPrefab;

    [Header("--- Üst Bakiye Özet Metni ---")]
    [Tooltip("Toplam para bakiyesi göstergesi metni.")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    private InventoryTab currentTab = InventoryTab.Resources;
    private List<StatRowUI> spawnedResourceRows = new List<StatRowUI>();
    private List<ShopItemUI> spawnedProductItems = new List<ShopItemUI>();
    private List<UpgradeItemUI> spawnedUpgradeItems = new List<UpgradeItemUI>();

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

        if (resourcesTabButton != null)
            resourcesTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Resources));

        if (productsTabButton != null)
            productsTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Products));

        if (upgradesTabButton != null)
            upgradesTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Upgrades));
    }

    private void Start()
    {
        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        SubscribeEvents();

        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged += HandleBasketChanged;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged += HandleQueueChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
    }

    private void UnsubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged -= HandleBasketChanged;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged -= HandleQueueChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleMoneyChanged(int newMoney)
    {
        UpdateGoldText(newMoney);
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleBasketChanged(int cur, int max)
    {
        if (mainPanel != null && mainPanel.activeSelf && currentTab == InventoryTab.Resources)
            RefreshPanel();
    }

    private void HandleQueueChanged(int cur, int max)
    {
        if (mainPanel != null && mainPanel.activeSelf && currentTab == InventoryTab.Resources)
            RefreshPanel();
    }

    private void HandleDataLoaded(PlayerData data)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Aç / Kapat & Sekme Değiştirme
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        PlayButtonClickSFX();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(true);

        SwitchTab(InventoryTab.Resources);
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();

        if (mainPanel != null)
            mainPanel.SetActive(false);

        gameObject.SetActive(false);
    }

    public void TogglePanel()
    {
        bool isOpen = (mainPanel != null && mainPanel.activeInHierarchy) || gameObject.activeInHierarchy;
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void SwitchTab(InventoryTab tab)
    {
        PlayButtonClickSFX();
        currentTab = tab;

        if (resourcesContent != null) resourcesContent.gameObject.SetActive(tab == InventoryTab.Resources);
        if (productsContent != null) productsContent.gameObject.SetActive(tab == InventoryTab.Products);
        if (upgradesContent != null) upgradesContent.gameObject.SetActive(tab == InventoryTab.Upgrades);

        RefreshPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Yenileme & Liste Doldurma
    // ═══════════════════════════════════════════

    public void RefreshPanel()
    {
        UpdateGoldText(EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0);

        switch (currentTab)
        {
            case InventoryTab.Resources:
                PopulateResourcesTab();
                break;
            case InventoryTab.Products:
                PopulateProductsTab();
                break;
            case InventoryTab.Upgrades:
                PopulateUpgradesTab();
                break;
        }
    }

    private void UpdateGoldText(int goldAmount)
    {
        if (goldText != null)
            goldText.text = $"{goldAmount:N0} Gold";
    }

    // ── 1. KAYNAKLAR SEKMESİ ──
    private void PopulateResourcesTab()
    {
        ClearContainer(resourcesContent, spawnedResourceRows);

        if (resourcesContent == null || statRowPrefab == null) return;

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        // Başlık
        AddResourceHeader("HAM MADDE & BAKİYE DUMUMU", new Color(0.4f, 0.7f, 1f, 1f));

        // 1. Altın
        int gold = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : (data != null ? data.Para : 0);
        AddResourceRow("💰 Altın Bakiyesi", $"{gold:N0} Gold", new Color(1f, 0.85f, 0.2f, 1f));

        // 2. Sepet Odunu
        int basketCur = BasketManager.Instance != null ? BasketManager.Instance.MevcutDoluluk : 0;
        int basketMax = BasketManager.Instance != null ? BasketManager.Instance.MaksKapasite : 5;
        AddResourceRow("🪵 Sepetteki Odun", $"{basketCur} / {basketMax}", new Color(0.95f, 0.95f, 0.95f, 1f));

        // 3. Ambar Odunu
        int playerWoodStock = data != null ? data.PlayerWoodStock : 0;
        AddResourceRow("🏠 Ev Ambarı Odunu", $"{playerWoodStock} Odun", new Color(0.4f, 0.9f, 0.4f, 1f));

        // 4. Atölye Odunu
        int workshopRawWood = data != null ? data.WorkshopRawWood : 0;
        int workshopCap = WorkshopManager.Instance != null ? WorkshopManager.Instance.GetWorkshopCapacity() : 10;
        AddResourceRow("🏭 Atölye Ham Odunu", $"{workshopRawWood} / {workshopCap}", new Color(0.9f, 0.5f, 0.9f, 1f));

        if (resourcesContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void AddResourceHeader(string title, Color color)
    {
        GameObject obj = Instantiate(statRowPrefab, resourcesContent, false);
        obj.SetActive(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
        }

        StatRowUI row = obj.GetComponent<StatRowUI>();
        if (row != null)
        {
            row.SetupAsHeader(title, color);
            spawnedResourceRows.Add(row);
        }
    }

    private void AddResourceRow(string label, string value, Color valColor)
    {
        GameObject obj = Instantiate(statRowPrefab, resourcesContent, false);
        obj.SetActive(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
        }

        StatRowUI row = obj.GetComponent<StatRowUI>();
        if (row != null)
        {
            row.Setup(label, value, valColor);
            spawnedResourceRows.Add(row);
        }
    }

    // ── 2. ÜRÜNLER SEKMESİ ──
    private void PopulateProductsTab()
    {
        ClearContainer(productsContent, spawnedProductItems);

        if (productsContent == null || shopItemPrefab == null) return;

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
        RecipeConfig[] allRecipes = WorkshopManager.Instance != null ? WorkshopManager.Instance.tumTarifler : null;

        if (allRecipes == null || allRecipes.Length == 0) return;

        foreach (RecipeConfig recipe in allRecipes)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.ItemID)) continue;

            string itemId = recipe.ItemID;
            int stock = data != null && data.ShopInventory != null ? data.ShopInventory.Get(itemId, 0) : 0;
            int price = recipe.BaseSalePrice > 0 ? recipe.BaseSalePrice : 100;
            Sprite icon = recipe.ItemIcon;

            GameObject obj = Instantiate(shopItemPrefab, productsContent, false);
            obj.SetActive(true);

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }

            ShopItemUI itemUI = obj.GetComponent<ShopItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(
                    itemId,
                    icon,
                    stock,
                    price,
                    false,
                    (id) => OnSellFromProductsTab(id),
                    null
                );

                spawnedProductItems.Add(itemUI);
            }
        }

        if (productsContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void OnSellFromProductsTab(string itemId)
    {
        PlayButtonClickSFX();

        if (ShopManager.Instance != null)
            ShopManager.Instance.TryRemoveStock(itemId, 1);

        RefreshPanel();
    }

    // ── 3. EKİPMAN & UPGRADE SEKMESİ ──
    private void PopulateUpgradesTab()
    {
        ClearContainer(upgradesContent, spawnedUpgradeItems);

        if (upgradesContent == null || upgradeItemPrefab == null) return;

        int currentGold = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0;

        // Upgrade edilebilir 5 ana eşya kategorisi
        var upgradeItems = new (string id, string name)[]
        {
            ("Axe", "🪓 Balta Hasarı / Gücü"),
            ("Basket", "🧺 Sepet Taşıma Kapasitesi"),
            ("Truck", "🚛 Kamyon Yük Kapasitesi"),
            ("Workshop", "⚙️ Atölye Üretim Hızı"),
            ("WorkshopQueue", "📦 Atölye Depo Kapasitesi")
        };

        foreach (var (itemId, displayName) in upgradeItems)
        {
            int level = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel(itemId) : 1;
            float statValue = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemValue(itemId) : 10f;
            int cost = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetUpgradeCost(itemId) : 50;
            bool isMax = UpgradeManager.Instance != null && UpgradeManager.Instance.IsMaxLevel(itemId);
            Sprite icon = GetUpgradeIcon(itemId);

            GameObject obj = Instantiate(upgradeItemPrefab, upgradesContent, false);
            obj.SetActive(true);

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }

            UpgradeItemUI itemUI = obj.GetComponent<UpgradeItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(
                    itemId,
                    displayName,
                    icon,
                    level,
                    statValue,
                    cost,
                    isMax,
                    currentGold,
                    (id) => OnUpgradeButtonClicked(id)
                );

                spawnedUpgradeItems.Add(itemUI);
            }
        }

        if (upgradesContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void OnUpgradeButtonClicked(string itemId)
    {
        PlayButtonClickSFX();

        if (UpgradeManager.Instance != null)
        {
            bool success = UpgradeManager.Instance.UpgradeItem(itemId);
            if (success)
            {
                NotificationUIController.Instance?.ShowNotification($"{itemId} yükseltildi!");
            }
            else
            {
                NotificationUIController.Instance?.ShowNotification("Yükseltme başarısız (Yetersiz Gold)!");
            }
        }

        RefreshPanel();
    }

    private Sprite GetUpgradeIcon(string itemId)
    {
        // Varsa ilgili tarif iconunu fallback göster
        if (itemId == "Axe")
        {
            if (AxeTablePanelController.Instance != null)
                return null;
        }
        return null;
    }

    // ═══════════════════════════════════════════
    //  Yardımcı Temizlik & Ses
    // ═══════════════════════════════════════════

    private void ClearContainer<T>(Transform container, List<T> list) where T : Component
    {
        if (container != null)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
        list.Clear();
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }
}
