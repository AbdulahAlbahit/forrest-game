using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;
using ForrestGame.Managers;

/// <summary>
/// Envanter (Inventory) Paneli Ana Kontrolcüsü.
/// SADECE SADE VE TEMİZ ÜRÜN ÇANTASI OLARAK ÇALIŞIR.
/// Sekmelere veya ekstra karmaşık yapılara gerek kalmadan, oyuncunun deposunda SADECE STOKTA VAR OLAN (Stok > 0)
/// üretilmiş mobilyaları ikonları, stok miktarları ve piyasa satış değerleri ile listeler.
/// 
/// EKİP KURALLARI UYUMU:
/// - Sadece bu UI scripti üzerinden çalışır, backend Manager'larına dokunmaz.
/// - Veri okuma işlemleri CloudSaveManager.CurrentData.ShopInventory ve WorkshopManager.tumTarifler üzerinden yapılır.
/// </summary>
public class InventoryPanelController : MonoBehaviour
{
    public static InventoryPanelController Instance { get; private set; }

    [Header("--- Panel Referansları ---")]
    [Tooltip("Envanter paneli ana GameObject'i.")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Kapat (X) butonu.")]
    [SerializeField] private Button closeButton;

    [Header("--- Liste & İçerik Alanı ---")]
    [Tooltip("Stoktaki ürün kartlarının dizileceği ScrollRect Content transform'u.")]
    [SerializeField] private Transform productListContent;

    [Tooltip("Ürün kartı prefab'ı (ShopItemUI bileşeni içermelidir).")]
    [SerializeField] private GameObject shopItemPrefab;

    [Tooltip("Stokta hiç ürün olmadığında gösterilecek bilgilendirme metni (Örn: 'Envanterinizde henüz ürün yok').")]
    [SerializeField] private TextMeshProUGUI emptyInventoryText;

    [Header("--- Üst Bakiye Özet Metni ---")]
    [Tooltip("Toplam para bakiyesi göstergesi metni (Opsiyonel).")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    private List<ShopItemUI> spawnedProductItems = new List<ShopItemUI>();

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

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;

        ShopManager.OnStockChanged += HandleStockChanged;
        ShopManager.OnItemSold += HandleItemSold;
    }

    private void UnsubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;

        ShopManager.OnStockChanged -= HandleStockChanged;
        ShopManager.OnItemSold -= HandleItemSold;
    }

    private void HandleMoneyChanged(int newMoney)
    {
        UpdateGoldText(newMoney);
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
    //  Panel Yenileme & Yalnızca Stoktaki Ürünleri Doldurma
    // ═══════════════════════════════════════════

    public void RefreshPanel()
    {
        UpdateGoldText(EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : 0);
        PopulateStokedProducts();
    }

    private void UpdateGoldText(int goldAmount)
    {
        if (goldText != null)
            goldText.text = $"{goldAmount:N0} Gold";
    }

    private void PopulateStokedProducts()
    {
        ClearContainer(productListContent, spawnedProductItems);

        if (productListContent == null)
        {
            var found = transform.Find("ProductListContent") ?? transform.Find("Panel/ProductListContent") ?? transform.Find("Scroll View/Viewport/Content");
            if (found != null) productListContent = found;
        }

        if (emptyInventoryText == null)
        {
            var textObj = transform.Find("EmptyInventoryText") ?? transform.Find("Panel/EmptyInventoryText") ?? transform.Find("Scroll View/EmptyInventoryText");
            if (textObj != null) emptyInventoryText = textObj.GetComponent<TextMeshProUGUI>();
        }

        if (productListContent == null || shopItemPrefab == null)
        {
            Debug.LogWarning("[InventoryPanelController] 'productListContent' veya 'shopItemPrefab' Inspector'dan atanmamış!");
            return;
        }

        EnsureLayoutComponents(productListContent);

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
        RecipeConfig[] allRecipes = WorkshopManager.Instance != null ? WorkshopManager.Instance.tumTarifler : null;

        int totalStockedProductsCount = 0;

        if (allRecipes != null && allRecipes.Length > 0)
        {
            foreach (RecipeConfig recipe in allRecipes)
            {
                if (recipe == null || string.IsNullOrEmpty(recipe.ItemID)) continue;

                string itemId = recipe.ItemID;
                int stock = data != null && data.ShopInventory != null ? data.ShopInventory.Get(itemId, 0) : 0;

                // SADECE STOKTA VAR OLAN (Stok > 0) ÜRÜNLERİ LİSTELE
                if (stock <= 0) continue;

                totalStockedProductsCount++;

                int price = recipe.BaseSalePrice > 0 ? recipe.BaseSalePrice : 100;
                Sprite icon = recipe.ItemIcon;

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
                        false,
                        true,
                        (id) => OnProductCardClicked(id),
                        null
                    );

                    spawnedProductItems.Add(itemUI);
                }
            }
        }

        // Stokta hiç ürün var mı kontrol et
        bool hasAnyStockedProduct = totalStockedProductsCount > 0;

        if (emptyInventoryText != null)
        {
            emptyInventoryText.gameObject.SetActive(!hasAnyStockedProduct);
            emptyInventoryText.transform.SetAsLastSibling();
            if (!hasAnyStockedProduct)
            {
                emptyInventoryText.text = "Envanterinizde henüz üretilmiş ürün bulunmuyor.\nAtölyeden ürün üreterek çantanızı doldurabilirsiniz.";
            }
        }

        if (productListContent is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
        Canvas.ForceUpdateCanvases();
    }

    private void OnProductCardClicked(string itemId)
    {
        PlayButtonClickSFX();
    }

    private void EnsureLayoutComponents(Transform contentTransform)
    {
        if (contentTransform == null) return;

        LayoutGroup existingLayout = contentTransform.GetComponent<LayoutGroup>();
        if (existingLayout is VerticalLayoutGroup vLayout)
        {
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing = 6f;
        }
        else if (existingLayout == null)
        {
            VerticalLayoutGroup newLayout = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            if (newLayout != null)
            {
                newLayout.childControlWidth = true;
                newLayout.childControlHeight = true;
                newLayout.childForceExpandWidth = true;
                newLayout.childForceExpandHeight = false;
                newLayout.spacing = 6f;
            }
        }

        ContentSizeFitter fitter = contentTransform.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentTransform.gameObject.AddComponent<ContentSizeFitter>();
        }

        if (fitter != null)
        {
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }

    // ═══════════════════════════════════════════
    //  Yardımcı Temizlik & Ses
    // ═══════════════════════════════════════════

    private void ClearContainer<T>(Transform container, List<T> list) where T : Component
    {
        if (list != null)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != null && list[i].gameObject != null)
                {
                    list[i].gameObject.SetActive(false);
                    Destroy(list[i].gameObject);
                }
            }
            list.Clear();
        }
    }


    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }
}
