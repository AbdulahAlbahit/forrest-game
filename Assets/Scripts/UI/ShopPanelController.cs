using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;
using ForrestGame.Managers;

/// <summary>
/// Mağaza (Shop) Paneli Ana Kontrolcüsü.
/// Sol tarafta mağaza stoğundaki ürünleri listeler, sağ tarafta seçili ürünün detayını
/// ve satış butonlarını (Tekli Sat / Tümünü Sat / Sipariş Karşıla) gösterir.
/// Alt kısımda bekleyen müşteri siparişlerini gösterir.
/// 
/// EKİP KURALLARI UYUMU:
/// - Sadece bu UI scripti üzerinden çalışır, backend Manager'larına dokunmaz.
/// - Satış işlemi için doğrudan ShopManager.Instance.TryRemoveStock() veya ProcessCustomerOrder() çağırır.
/// - Bakiye güncellemesi ve Firebase save işlemi ShopManager / EconomyManager içinde otomatik gerçekleşir.
/// </summary>
public class ShopPanelController : MonoBehaviour
{
    public static ShopPanelController Instance { get; private set; }

    [Header("--- Panel Referansları ---")]
    [Tooltip("Mağaza paneli ana GameObject'i.")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Kapat (X) butonu.")]
    [SerializeField] private Button closeButton;

    [Header("--- Sol Taraf: Ürün Listesi ---")]
    [Tooltip("Kaydırma alanı (ScrollRect).")]
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("Ürün kartlarının dizileceği Content transform'u.")]
    [SerializeField] private Transform productListContent;

    [Tooltip("Ürün kartı prefab'ı (ShopItemUI bileşeni içermelidir).")]
    [SerializeField] private GameObject shopItemPrefab;

    [Header("--- Sağ Taraf: Seçili Ürün Detayı ---")]
    [Tooltip("Seçili ürün adı.")]
    [SerializeField] private TextMeshProUGUI selectedNameText;

    [Tooltip("Seçili ürün açıklaması veya birim fiyat bilgisi.")]
    [SerializeField] private TextMeshProUGUI selectedPriceText;

    [Tooltip("Seçili ürün mevcut stok miktarı.")]
    [SerializeField] private TextMeshProUGUI selectedStockText;

    [Tooltip("Seçili ürün ikonu.")]
    [SerializeField] private Image selectedItemIcon;

    [Tooltip("1 Adet Satış Butonu.")]
    [SerializeField] private Button sellOneButton;

    [Tooltip("1 Adet Satış Buton Yazısı.")]
    [SerializeField] private TextMeshProUGUI sellOneButtonText;

    [Tooltip("Tüm Stoğu Sat Butonu.")]
    [SerializeField] private Button sellAllButton;

    [Tooltip("Tüm Stoğu Sat Buton Yazısı.")]
    [SerializeField] private TextMeshProUGUI sellAllButtonText;

    [Header("--- Alt Taraf: Müşteri Siparişleri & Bakiye ---")]
    [Tooltip("Bekleyen müşteri siparişleri özeti metni.")]
    [SerializeField] private TextMeshProUGUI pendingOrdersText;

    [Tooltip("Müşteri siparişini karşıla butonu.")]
    [SerializeField] private Button fulfillOrderButton;

    [Tooltip("Müşteri siparişi karşıla buton metni.")]
    [SerializeField] private TextMeshProUGUI fulfillOrderButtonText;

    [Tooltip("Mevcut para bakiyesi göstergesi metni (Örn: '1,250 Gold').")]
    [SerializeField] private TextMeshProUGUI totalGoldText;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();
    private string selectedItemId = "";

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

        if (sellOneButton != null)
            sellOneButton.onClick.AddListener(OnSellOneButtonClicked);

        if (sellAllButton != null)
            sellAllButton.onClick.AddListener(OnSellAllButtonClicked);

        if (fulfillOrderButton != null)
            fulfillOrderButton.onClick.AddListener(OnFulfillOrderButtonClicked);
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
        ShopManager.OnStockChanged += HandleStockChanged;
        ShopManager.OnItemSold += HandleItemSold;

        CustomerOrderManager.OnOrderCreated += HandleCustomerOrderCreated;
        CustomerOrderManager.OnOrderInstantlyFulfilled += HandleCustomerOrderFulfilled;

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
    }

    private void UnsubscribeEvents()
    {
        ShopManager.OnStockChanged -= HandleStockChanged;
        ShopManager.OnItemSold -= HandleItemSold;

        CustomerOrderManager.OnOrderCreated -= HandleCustomerOrderCreated;
        CustomerOrderManager.OnOrderInstantlyFulfilled -= HandleCustomerOrderFulfilled;

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleCustomerOrderCreated(CustomerOrderData orderData)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleCustomerOrderFulfilled(string itemId)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleStockChanged(string itemId, int newStock)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleItemSold(string itemId, int amount)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void HandleMoneyChanged(int newMoney)
    {
        UpdateGoldText(newMoney);
    }

    private void HandleDataLoaded(PlayerData data)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Aç / Kapat
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        PlayButtonClickSFX();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(true);

        GameObject targetObj = mainPanel != null ? mainPanel : gameObject;
        var anim = targetObj.GetComponent<ForrestGame.UI.UIPanelAnimator>();
        if (anim != null) anim.Show();

        RefreshPanel();
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();

        GameObject targetObj = mainPanel != null ? mainPanel : gameObject;
        var anim = targetObj.GetComponent<ForrestGame.UI.UIPanelAnimator>();

        if (anim != null)
        {
            anim.Hide(() =>
            {
                if (mainPanel != null)
                    mainPanel.SetActive(false);
                gameObject.SetActive(false);
            });
        }
        else
        {
            if (mainPanel != null)
                mainPanel.SetActive(false);

            gameObject.SetActive(false);
        }
    }

    public void TogglePanel()
    {
        bool isOpen = (mainPanel != null && mainPanel.activeInHierarchy) || gameObject.activeInHierarchy;
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Yenileme & Liste Doldurma
    // ═══════════════════════════════════════════

    public void RefreshPanel()
    {
        UpdateGoldText(EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0);
        PopulateProductList();
        UpdateSelectedProductDetails();
        UpdatePendingOrdersUI();
    }

    private void UpdateGoldText(int goldAmount)
    {
        if (totalGoldText != null)
            totalGoldText.text = $"{goldAmount:N0} Gold";
    }

    private void PopulateProductList()
    {
        ClearSpawnedItems();

        if (productListContent == null || shopItemPrefab == null) return;

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        // Mağazada gösterilecek tarifler listesi (WorkshopManager'dan alınır)
        RecipeConfig[] allRecipes = WorkshopManager.Instance != null ? WorkshopManager.Instance.tumTarifler : null;

        if (allRecipes == null || allRecipes.Length == 0)
        {
            return;
        }

        // İlk seçim yoksa ilk tarifi seç
        if (string.IsNullOrEmpty(selectedItemId) && allRecipes.Length > 0 && allRecipes[0] != null)
        {
            selectedItemId = allRecipes[0].ItemID;
        }

        foreach (RecipeConfig recipe in allRecipes)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.ItemID)) continue;

            string itemId = recipe.ItemID;
            int stock = data != null && data.ShopInventory != null ? data.ShopInventory.Get(itemId, 0) : 0;
            int price = recipe.BaseSalePrice > 0 ? recipe.BaseSalePrice : 100;
            Sprite icon = recipe.ItemIcon;
            bool isSelected = itemId == selectedItemId;

            GameObject obj = Instantiate(shopItemPrefab, productListContent, false);
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
                    isSelected,
                    (id) => OnSellClickedFromItem(id),
                    (id) => OnCardSelected(id)
                );

                spawnedItems.Add(itemUI);
            }
        }

        if (productListContent is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    private void OnCardSelected(string itemId)
    {
        PlayButtonClickSFX();
        selectedItemId = itemId;

        foreach (var item in spawnedItems)
        {
            if (item != null) item.SetSelected(false);
        }

        UpdateSelectedProductDetails();
    }

    private void OnSellClickedFromItem(string itemId)
    {
        selectedItemId = itemId;
        UpdateSelectedProductDetails();
        OnSellOneButtonClicked();
    }

    private void UpdateSelectedProductDetails()
    {
        if (string.IsNullOrEmpty(selectedItemId))
        {
            if (selectedNameText != null) selectedNameText.text = "Ürün Seçin";
            if (selectedPriceText != null) selectedPriceText.text = "";
            if (selectedStockText != null) selectedStockText.text = "";
            if (selectedItemIcon != null) selectedItemIcon.gameObject.SetActive(false);
            if (sellOneButton != null) sellOneButton.interactable = false;
            if (sellAllButton != null) sellAllButton.interactable = false;
            return;
        }

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
        int currentStock = data != null && data.ShopInventory != null ? data.ShopInventory.Get(selectedItemId, 0) : 0;

        RecipeConfig currentRecipe = GetRecipeConfig(selectedItemId);
        int price = currentRecipe != null && currentRecipe.BaseSalePrice > 0 ? currentRecipe.BaseSalePrice : 100;
        Sprite icon = currentRecipe != null ? currentRecipe.ItemIcon : null;

        if (selectedNameText != null)
            selectedNameText.text = selectedItemId;

        if (selectedPriceText != null)
            selectedPriceText.text = $"Birim Fiyat: {price:N0} Gold";

        if (selectedStockText != null)
            selectedStockText.text = $"Mağaza Stoğu: {currentStock} Adet";

        if (selectedItemIcon != null)
        {
            if (icon != null)
            {
                selectedItemIcon.sprite = icon;
                selectedItemIcon.gameObject.SetActive(true);
            }
            else
            {
                selectedItemIcon.gameObject.SetActive(false);
            }
        }

        bool hasStock = currentStock > 0;

        if (sellOneButton != null)
            sellOneButton.interactable = hasStock;

        if (sellOneButtonText != null)
            sellOneButtonText.text = hasStock ? $"1 Adet Sat (+{price} Gold)" : "Stok Yok";

        if (sellAllButton != null)
            sellAllButton.interactable = hasStock;

        if (sellAllButtonText != null)
        {
            int totalValue = currentStock * price;
            sellAllButtonText.text = hasStock ? $"Tümünü Sat ({currentStock} Adet = +{totalValue} Gold)" : "Stok Yok";
        }
    }

    private void UpdatePendingOrdersUI()
    {
        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        if (data == null || data.PendingCustomerOrders == null || data.PendingCustomerOrders.Count == 0)
        {
            if (pendingOrdersText != null) pendingOrdersText.text = "Bekleyen müşteri siparişi yok.";
            if (fulfillOrderButton != null) fulfillOrderButton.interactable = false;
            if (fulfillOrderButtonText != null) fulfillOrderButtonText.text = "Sipariş Yok";
            return;
        }

        int totalPendingCount = 0;
        string pendingSummary = "";
        bool canFulfillAny = false;
        Dictionary<string, int> pendingByItem = new Dictionary<string, int>();

        foreach (CustomerOrderData order in data.PendingCustomerOrders)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.ItemId)) continue;
            pendingByItem.TryGetValue(order.ItemId, out int currentPending);
            pendingByItem[order.ItemId] = currentPending + 1;
        }

        foreach (KeyValuePair<string, int> pendingEntry in pendingByItem)
        {
            string itemId = pendingEntry.Key;
            int pending = pendingEntry.Value;
            totalPendingCount += pending;
            int currentStock = data.ShopInventory != null ? data.ShopInventory.Get(itemId, 0) : 0;

            pendingSummary += $"- {itemId}: {pending} adet bekleniyor (Stok: {currentStock})\n";

            if (currentStock > 0)
                canFulfillAny = true;
        }

        if (totalPendingCount == 0)
        {
            if (pendingOrdersText != null) pendingOrdersText.text = "Bekleyen müşteri siparişi yok.";
            if (fulfillOrderButton != null) fulfillOrderButton.interactable = false;
            if (fulfillOrderButtonText != null) fulfillOrderButtonText.text = "Sipariş Yok";
            return;
        }

        if (pendingOrdersText != null)
            pendingOrdersText.text = pendingSummary.TrimEnd();

        if (fulfillOrderButton != null)
            fulfillOrderButton.interactable = canFulfillAny;

        if (fulfillOrderButtonText != null)
            fulfillOrderButtonText.text = canFulfillAny ? "Bekleyen Siparişleri Karşıla" : "Stok Yetersiz";
    }

    // ═══════════════════════════════════════════
    //  Satış Aksiyonları
    // ═══════════════════════════════════════════

    private void OnSellOneButtonClicked()
    {
        PlayButtonClickSFX();

        if (string.IsNullOrEmpty(selectedItemId)) return;

        if (ShopManager.Instance != null)
        {
            RecipeConfig recipe = GetRecipeConfig(selectedItemId);
            int unitPrice = recipe != null && recipe.BaseSalePrice > 0 ? recipe.BaseSalePrice : 100;

            bool success = ShopManager.Instance.TryRemoveStock(selectedItemId, 1);
            if (success)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddMoney(unitPrice);
                }

                NotificationUIController.Instance?.ShowNotification($"1x {selectedItemId} satıldı! (+{unitPrice} Gold)");
            }
            else
            {
                NotificationUIController.Instance?.ShowNotification("Stok Yetersiz!");
            }
        }
        else
        {
            Debug.LogWarning("[ShopPanel] ShopManager sahnede bulunamadı!");
        }

        RefreshPanel();
    }

    private void OnSellAllButtonClicked()
    {
        PlayButtonClickSFX();

        if (string.IsNullOrEmpty(selectedItemId)) return;

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
        int currentStock = data != null && data.ShopInventory != null ? data.ShopInventory.Get(selectedItemId, 0) : 0;

        if (currentStock <= 0) return;

        if (ShopManager.Instance != null)
        {
            RecipeConfig recipe = GetRecipeConfig(selectedItemId);
            int unitPrice = recipe != null && recipe.BaseSalePrice > 0 ? recipe.BaseSalePrice : 100;

            bool success = ShopManager.Instance.TryRemoveStock(selectedItemId, currentStock);
            if (success)
            {
                int totalEarnings = currentStock * unitPrice;
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddMoney(totalEarnings);
                }

                NotificationUIController.Instance?.ShowNotification($"{currentStock}x {selectedItemId} satıldı! (+{totalEarnings} Gold)");
            }
            else
            {
                NotificationUIController.Instance?.ShowNotification("Satış gerçekleştirilemedi!");
            }
        }

        RefreshPanel();
    }

    private void OnFulfillOrderButtonClicked()
    {
        PlayButtonClickSFX();

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
        if (data == null) return;

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.CheckAndFulfillPendingOrders(data, true);
        }

        RefreshPanel();
    }

    // ═══════════════════════════════════════════
    //  Yardımcı Metotlar
    // ═══════════════════════════════════════════

    private RecipeConfig GetRecipeConfig(string itemId)
    {
        if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null)
        {
            foreach (var r in WorkshopManager.Instance.tumTarifler)
            {
                if (r != null && r.ItemID == itemId) return r;
            }
        }
        return null;
    }

    private void ClearSpawnedItems()
    {
        if (productListContent != null)
        {
            for (int i = productListContent.childCount - 1; i >= 0; i--)
            {
                Transform child = productListContent.GetChild(i);
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
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }
}
