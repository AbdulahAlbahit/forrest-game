using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;

/// <summary>
/// Atölye (Zanaat/Üretim) Paneli Ana Kontrolcüsü.
/// Sol tarafta mobilya tariflerini listeler, sağ tarafta seçili mobilyanın detaylarını ve 'Üret' butonunu gösterir.
/// Alt tarafta ise üretim sırasını (Queue) ve aktif üretilen ürünün canlı % doluluk barını yönetir.
/// Panel kapansa dahi üretim arka planda kesintisiz devam eder.
/// </summary>
public class WorkshopPanelController : MonoBehaviour
{
    public static WorkshopPanelController Instance { get; private set; }

    [Header("--- Sol Taraf: Mobilya Liste Ayarları ---")]
    [Tooltip("Sol taraftaki ScrollRect içindeki Content objesi (Kutuların dizileceği alan).")]
    [SerializeField] private Transform recipeListContent;

    [Tooltip("Sol liste için üretilecek kutucuk prefab'i (RecipeItemUI component'i içermelidir).")]
    [SerializeField] private GameObject recipeItemPrefab;

    [Header("--- Sağ Taraf: Seçili Mobilya Detayı ---")]
    [Tooltip("Seçili mobilyanın adı.")]
    [SerializeField] private TextMeshProUGUI selectedNameText;

    [Tooltip("Seçili mobilya için gereken odun miktarı. Örn: 'Gerekli Odun: 5'")]
    [SerializeField] private TextMeshProUGUI requiredWoodText;

    [Tooltip("Seçili mobilyanın üretim süresi. Örn: 'Süre: 5 sn'")]
    [SerializeField] private TextMeshProUGUI productionTimeText;

    [Tooltip("Seçili mobilya ikonu.")]
    [SerializeField] private Image selectedItemIcon;

    [Tooltip("Üretim siparişi verme butonu.")]
    [SerializeField] private Button produceButton;

    [Header("--- Alt Taraf: Üretim Sırası (Queue) ---")]
    [Tooltip("Alt taraftaki Horizontal ScrollRect içindeki Content objesi.")]
    [SerializeField] private Transform queueListContent;

    [Tooltip("Üretim sırasındaki her kutucuk için prefab (QueueItemUI component'i içermelidir).")]
    [SerializeField] private GameObject queueItemPrefab;

    [Header("--- Diğer UI ---")]
    [Tooltip("Paneli kapatma (X) butonu.")]
    [SerializeField] private Button closeButton;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    // Dahili Durum Değişkenleri
    private RecipeConfig selectedRecipe;
    private List<RecipeItemUI> spawnedRecipeItems = new List<RecipeItemUI>();
    private List<QueueItemUI> spawnedQueueItems = new List<QueueItemUI>();

    // SÜREKLİ ARKA PLAN ZAMANLAYICISI (Panel Kapalı Olsa Bile Çalışır)
    public class OfflineOrder
    {
        public string ItemID;
        public float Elapsed;
        public float Duration;
    }
    public static List<OfflineOrder> GlobalOfflineOrders = new List<OfflineOrder>();
    public static int OfflineWoodBalance = 25;

    private static List<RecipeConfig> testRecipes = new List<RecipeConfig>();

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (produceButton != null)
            produceButton.onClick.AddListener(OnProduceButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        SubscribeEvents();
        PopulateRecipeList();
        RefreshQueueUI();

        // Paneli açtığında arka planda ilerleyen sürece göre UI'ı güncelle
        if (MainHUDController.Instance != null && (WorkshopManager.Instance == null || CloudSaveManager.Instance?.CurrentData == null))
        {
            MainHUDController.Instance.SetWoodTextDirect(OfflineWoodBalance, 25);
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        // Panel açıkken arayüzdeki % barını güncelle
        UpdateActiveQueueProgress();
    }

    // ARKA PLANDA SÜREKLİ AKAN ZAMANLAYICI (Global Hizmet)
    public static void TickBackgroundProduction(float deltaTime)
    {
        if (GlobalOfflineOrders.Count == 0) return;

        var current = GlobalOfflineOrders[0];
        current.Elapsed += deltaTime;

        if (current.Elapsed >= current.Duration)
        {
            Debug.Log($"[Atölye Arka Plan] '{current.ItemID}' ÜRETİMİ TAMAMLANDI! 🎉");
            GlobalOfflineOrders.RemoveAt(0);

            // Eğer panel o an ekranda açıksa Queue listesini yenile ve biten kutuyu kaldır
            if (Instance != null && Instance.gameObject.activeInHierarchy)
            {
                Instance.RefreshQueueUI();
            }
        }
    }

    // ═══════════════════════════════════════════
    //  Event Abonelikleri
    // ═══════════════════════════════════════════

    private void SubscribeEvents()
    {
        if (WorkshopManager.Instance != null)
        {
            WorkshopManager.Instance.OnQueueChanged += HandleQueueChanged;
        }

        WorkshopManager.OnProductionFinished += HandleProductionFinishedGlobal;

        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void UnsubscribeEvents()
    {
        if (WorkshopManager.Instance != null)
        {
            WorkshopManager.Instance.OnQueueChanged -= HandleQueueChanged;
        }

        WorkshopManager.OnProductionFinished -= HandleProductionFinishedGlobal;

        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
        }
    }

    private void HandleProductionFinishedGlobal(string itemID, int count)
    {
        RefreshQueueUI();
    }

    private void HandleQueueChanged(int currentWood, int capacity)
    {
        RefreshQueueUI();
    }

    private void HandleDataLoaded(PlayerData data)
    {
        RefreshQueueUI();
    }

    // ═══════════════════════════════════════════
    //  Sol Taraf: Mobilya Listesi Oluşturma
    // ═══════════════════════════════════════════

    private void PopulateRecipeList()
    {
        foreach (var item in spawnedRecipeItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedRecipeItems.Clear();

        List<RecipeConfig> listToUse = new List<RecipeConfig>();

        if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null && WorkshopManager.Instance.tumTarifler.Length > 0)
        {
            listToUse.AddRange(WorkshopManager.Instance.tumTarifler);
        }

        if (listToUse.Count == 0)
        {
            listToUse = GetTestRecipes();
        }

        for (int i = 0; i < listToUse.Count; i++)
        {
            RecipeConfig recipe = listToUse[i];
            if (recipe == null) continue;

            GameObject obj = Instantiate(recipeItemPrefab, recipeListContent);
            RecipeItemUI itemUI = obj.GetComponent<RecipeItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(recipe, OnRecipeSelected);
                spawnedRecipeItems.Add(itemUI);
            }
        }

        if (listToUse.Count > 0 && listToUse[0] != null)
        {
            OnRecipeSelected(listToUse[0]);
        }
    }

    private List<RecipeConfig> GetTestRecipes()
    {
        if (testRecipes.Count == 0)
        {
            string[] testNames = { "Masa", "Sandalye", "Ahşap Dolap" };
            foreach (var name in testNames)
            {
                RecipeConfig config = ScriptableObject.CreateInstance<RecipeConfig>();
                config.ItemID = name;
                config.RequiredWood = 5;
                config.BaseProductionTime = 5f;
                testRecipes.Add(config);
            }
        }
        return testRecipes;
    }

    private void OnRecipeSelected(RecipeConfig recipe)
    {
        PlayButtonClickSFX();
        selectedRecipe = recipe;

        if (recipe == null) return;

        foreach (var item in spawnedRecipeItems)
        {
            if (item != null) item.SetSelected(false);
        }

        if (selectedNameText != null)
            selectedNameText.text = recipe.ItemID;

        if (requiredWoodText != null)
            requiredWoodText.text = $"Gerekli Odun: {recipe.RequiredWood}";

        if (productionTimeText != null)
            productionTimeText.text = $"Süre: {recipe.BaseProductionTime} sn";

        if (produceButton != null)
            produceButton.interactable = true;
    }

    // ═══════════════════════════════════════════
    //  Sağ Taraf: Üretim Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnProduceButtonClicked()
    {
        PlayButtonClickSFX();

        if (selectedRecipe == null) return;

        int cost = selectedRecipe.RequiredWood > 0 ? selectedRecipe.RequiredWood : 5;

        // 1. GERÇEK SİSTEM AKTİFSE
        if (WorkshopManager.Instance != null && CloudSaveManager.Instance?.CurrentData != null)
        {
            bool success = WorkshopManager.Instance.TryStartProduction(selectedRecipe, 1);
            if (success)
            {
                Debug.Log($"[WorkshopPanel] {selectedRecipe.ItemID} üretimi sıraya eklendi!");
                RefreshQueueUI();
                return;
            }
            else
            {
                NotificationUIController.Instance?.ShowNotification("⚠️ Yetersiz Odun!");
            }
        }

        // 2. ÇEVRİMDİŞİ TEST SİMÜLASYONU
        if (OfflineWoodBalance < cost)
        {
            Debug.LogWarning($"[Workshop Test] YETERSIZ ODUN! Gereken: {cost}, Mevcut Test Odunu: {OfflineWoodBalance}.");
            NotificationManager.Instance?.SendNotification("Yetersiz Odun!");
            NotificationUIController.Instance?.ShowNotification("⚠️ Yetersiz Odun!");
            return;
        }

        OfflineWoodBalance -= cost;
        Debug.Log($"[Workshop Test] -{cost} Odun Harcandı! Kalan Test Odunu: {OfflineWoodBalance}");

        if (MainHUDController.Instance != null)
        {
            MainHUDController.Instance.SetWoodTextDirect(OfflineWoodBalance, 25);
        }

        AddOfflineTestOrder(selectedRecipe);
    }

    private void AddOfflineTestOrder(RecipeConfig recipe)
    {
        OfflineOrder item = new OfflineOrder
        {
            ItemID = recipe.ItemID,
            Elapsed = 0f,
            Duration = recipe.BaseProductionTime > 0 ? recipe.BaseProductionTime : 5f
        };

        GlobalOfflineOrders.Add(item);
        RefreshQueueUI();
    }

    // ═══════════════════════════════════════════
    //  Alt Taraf: Üretim Sırası (Queue) UI Güncelleme
    // ═══════════════════════════════════════════

    public void RefreshQueueUI()
    {
        foreach (var item in spawnedQueueItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedQueueItems.Clear();

        bool isFirst = true;

        // 1. GERÇEK SİSTEM SİPARİŞLERİ
        if (CloudSaveManager.Instance?.CurrentData?.WorkshopQueue != null)
        {
            var queueDict = CloudSaveManager.Instance.CurrentData.WorkshopQueue.ToDictionary();

            foreach (var kvp in queueDict)
            {
                string itemId = kvp.Key;
                int count = System.Convert.ToInt32(kvp.Value);

                if (count <= 0) continue;

                GameObject obj = Instantiate(queueItemPrefab, queueListContent);
                QueueItemUI queueUI = obj.GetComponent<QueueItemUI>();

                if (queueUI != null)
                {
                    queueUI.Setup(itemId, count, isFirst);
                    spawnedQueueItems.Add(queueUI);
                    isFirst = false;
                }
            }
        }

        // 2. ÇEVRİMDİŞİ / TEST SİPARİŞLERİ
        for (int i = 0; i < GlobalOfflineOrders.Count; i++)
        {
            GameObject obj = Instantiate(queueItemPrefab, queueListContent);
            QueueItemUI queueUI = obj.GetComponent<QueueItemUI>();

            if (queueUI != null)
            {
                bool active = isFirst && (i == 0);
                queueUI.Setup(GlobalOfflineOrders[i].ItemID, 1, active);
                spawnedQueueItems.Add(queueUI);
                if (isFirst) isFirst = false;
            }
        }
    }

    private void UpdateActiveQueueProgress()
    {
        if (WorkshopManager.Instance != null && spawnedQueueItems.Count > 0)
        {
            float progress = WorkshopManager.Instance.GetProcessingProgress();
            if (spawnedQueueItems[0] != null)
            {
                spawnedQueueItems[0].UpdateProgress(progress);
            }
            return;
        }

        if (GlobalOfflineOrders.Count > 0 && spawnedQueueItems.Count > 0 && spawnedQueueItems[0] != null)
        {
            float progress = GlobalOfflineOrders[0].Elapsed / GlobalOfflineOrders[0].Duration;
            spawnedQueueItems[0].UpdateProgress(progress);

            // Ürün tamamlandıysa listeyi yeniden çiz
            if (GlobalOfflineOrders[0].Elapsed >= GlobalOfflineOrders[0].Duration)
            {
                RefreshQueueUI();
            }
        }
    }

    // ═══════════════════════════════════════════
    //  Panel Açma / Kapatma & Ses
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        PlayButtonClickSFX();
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();
        gameObject.SetActive(false);
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }

    [ContextMenu("TEST: +20 Ham Odun Ekle")]
    public void TestAddWoodToWorkshop()
    {
        if (WorkshopManager.Instance != null && CloudSaveManager.Instance?.CurrentData != null)
        {
            WorkshopManager.Instance.AddWood(20);
        }
        else
        {
            OfflineWoodBalance += 20;
            if (MainHUDController.Instance != null)
                MainHUDController.Instance.SetWoodTextDirect(OfflineWoodBalance, 25);
            Debug.Log($"[Workshop Test] +20 Odun Eklendi. Güncel Test Odunu: {OfflineWoodBalance}");
        }
    }
}
