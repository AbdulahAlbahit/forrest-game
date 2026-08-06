using System;
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

    [Tooltip("Atölyenin mevcut ham odun stoğunu gösteren metin (panel içindeki mevcut 'WorkshopWoodText' objesine bağlanmalı). Örn: '10 / 25'")]
    [SerializeField] private TextMeshProUGUI workshopWoodStockText;

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
    /// <summary>
    /// Kuyruğa eklemeyi onaylamak için ikinci tıklama beklenen tarif. İlk tıklama
    /// sadece seçer (sağ paneli doldurur), aynı tarife ikinci art arda tıklama kuyruğa gönderir.
    /// </summary>
    private RecipeConfig recipeArmedForQueue;
    private List<RecipeItemUI> spawnedRecipeItems = new List<RecipeItemUI>();
    private List<QueueItemUI> spawnedQueueItems = new List<QueueItemUI>();
    private bool externalQueueDisplayActive;

    /// <summary>
    /// Harici bir oynanis sistemi panele baglandiginda Uret butonu dogrudan
    /// WorkshopManager'i calistirmak yerine secilen tarifi bu olayla bildirir.
    /// Dinleyici yoksa panelin mevcut uretim davranisi aynen devam eder.
    /// </summary>
    public event Action<RecipeConfig> ProductionRequested;
    public event Action<RecipeConfig> RecipeChosen;
    public event Action CloseRequested;

    public RecipeConfig SelectedRecipe => selectedRecipe;
    public bool HasExternalProductionHandler => ProductionRequested != null;

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

        if (WorkshopManager.Instance != null)
        {
            UpdateWorkshopWoodStockText(WorkshopManager.Instance.MevcutKuyruk, WorkshopManager.Instance.GetWorkshopCapacity());
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
        UpdateWorkshopWoodStockText(currentWood, capacity);
    }

    private void UpdateWorkshopWoodStockText(int current, int capacity)
    {
        if (workshopWoodStockText != null)
            workshopWoodStockText.text = $"{current} / {capacity}";
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
        recipeArmedForQueue = null;

        foreach (var item in spawnedRecipeItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedRecipeItems.Clear();

        if (WorkshopManager.Instance == null || WorkshopManager.Instance.tumTarifler == null || WorkshopManager.Instance.tumTarifler.Length == 0)
        {
            Debug.LogWarning("[WorkshopPanel] WorkshopManager veya tarifler bulunamadı!");
            return;
        }

        for (int i = 0; i < WorkshopManager.Instance.tumTarifler.Length; i++)
        {
            RecipeConfig recipe = WorkshopManager.Instance.tumTarifler[i];
            if (recipe == null) continue;

            GameObject obj = Instantiate(recipeItemPrefab, recipeListContent);
            RecipeItemUI itemUI = obj.GetComponent<RecipeItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(recipe, OnRecipeClicked);
                spawnedRecipeItems.Add(itemUI);
            }
        }

        if (WorkshopManager.Instance.tumTarifler.Length > 0 && WorkshopManager.Instance.tumTarifler[0] != null)
        {
            OnRecipeSelected(WorkshopManager.Instance.tumTarifler[0]);
        }
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

        // Mobilya ikonunu göster (Mert'in eklediği Sprite)
        if (selectedItemIcon != null && recipe.ItemIcon != null)
            selectedItemIcon.sprite = recipe.ItemIcon;

        if (produceButton != null)
            produceButton.interactable = true;
    }

    private void OnRecipeClicked(RecipeConfig recipe)
    {
        bool isConfirmingClick = recipe != null && recipeArmedForQueue == recipe;

        OnRecipeSelected(recipe);

        if (!isConfirmingClick)
        {
            recipeArmedForQueue = recipe;
            return;
        }

        recipeArmedForQueue = null;

        if (RecipeChosen == null) return;
        foreach (Action<RecipeConfig> handler in RecipeChosen.GetInvocationList())
        {
            try
            {
                handler(recipe);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }

    // ═══════════════════════════════════════════
    //  Sağ Taraf: Üretim Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnProduceButtonClicked()
    {
        PlayButtonClickSFX();

        if (selectedRecipe == null) return;

        // Harici üretim dinleyicisi varsa (EfeWorkshopPanelBridge vb.) ona bildir
        if (ProductionRequested != null)
        {
            foreach (Action<RecipeConfig> handler in ProductionRequested.GetInvocationList())
            {
                try
                {
                    handler(selectedRecipe);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            return;
        }

        // WorkshopManager üzerinden üretim başlat
        if (WorkshopManager.Instance != null && CloudSaveManager.Instance?.CurrentData != null)
        {
            bool success = WorkshopManager.Instance.TryStartProduction(selectedRecipe, 1);
            if (success)
            {
                Debug.Log($"[WorkshopPanel] {selectedRecipe.ItemID} üretimi sıraya eklendi!");
                RefreshQueueUI();
            }
            else
            {
                NotificationUIController.Instance?.ShowNotification("Yetersiz Odun!");
            }
        }
        else
        {
            Debug.LogWarning("[WorkshopPanel] WorkshopManager veya CloudSave verisi bulunamadı!");
            NotificationUIController.Instance?.ShowNotification("Sistem hazır değil!");
        }
    }

    // ═══════════════════════════════════════════
    //  Alt Taraf: Üretim Sırası (Queue) UI Güncelleme
    // ═══════════════════════════════════════════

    public void RefreshQueueUI()
    {
        if (externalQueueDisplayActive) return;

        foreach (var item in spawnedQueueItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedQueueItems.Clear();

        SerializableDict workshopQueue = CloudSaveManager.Instance?.CurrentData?.WorkshopQueue;
        if (workshopQueue == null) return;

        bool isFirst = true;

        // Siraya girdigi (FIFO) sirayla gostermek icin Dictionary yerine
        // dogrudan sirali keys/values listeleri kullanilir.
        for (int i = 0; i < workshopQueue.keys.Count; i++)
        {
            string itemId = workshopQueue.keys[i];
            int count = workshopQueue.values[i];

            if (count <= 0) continue;

            GameObject obj = Instantiate(queueItemPrefab, queueListContent);
            QueueItemUI queueUI = obj.GetComponent<QueueItemUI>();

            if (queueUI != null)
            {
                queueUI.Setup(itemId, count, isFirst, (id) => OnQueueItemClicked(id));
                spawnedQueueItems.Add(queueUI);
                isFirst = false;
            }
        }
    }

    private void OnQueueItemClicked(string itemId)
    {
        PlayButtonClickSFX();

        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
        if (data == null || data.WorkshopQueue == null) return;

        int currentCount = data.WorkshopQueue.Get(itemId, 0);
        if (currentCount > 0)
        {
            int newCount = currentCount - 1;
            data.WorkshopQueue.Set(itemId, newCount);

            NotificationUIController.Instance?.ShowNotification($"1 adet {itemId} sıradan çıkarıldı!");

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData(data);
            }

            RefreshQueueUI();
        }
    }


    private void UpdateActiveQueueProgress()
    {
        if (externalQueueDisplayActive) return;

        if (WorkshopManager.Instance != null && spawnedQueueItems.Count > 0 && spawnedQueueItems[0] != null)
        {
            float progress = WorkshopManager.Instance.GetProcessingProgress();
            spawnedQueueItems[0].UpdateProgress(progress);
        }
    }

    // ═══════════════════════════════════════════
    //  Panel Açma / Kapatma & Ses
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        PlayButtonClickSFX();
        gameObject.SetActive(true);
        var anim = GetComponent<ForrestGame.UI.UIPanelAnimator>();
        if (anim != null) anim.Show();
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();

        if (CloseRequested != null)
        {
            foreach (Action handler in CloseRequested.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
            return;
        }

        var anim = GetComponent<ForrestGame.UI.UIPanelAnimator>();
        if (anim != null)
        {
            anim.Hide(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void CloseImmediately()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Efe'nin secimli uretim kuyrugunu Bulut'un mevcut queue alaninda gosterir.
    /// Kaynak WorkshopManager kuyrugunu veya kalici veriyi degistirmez.
    /// </summary>
    public void ShowExternalQueue(IReadOnlyList<RecipeConfig> queuedRecipes, int remainingWood)
    {
        externalQueueDisplayActive = true;

        if (produceButton != null)
        {
            produceButton.gameObject.SetActive(false);
        }

        foreach (QueueItemUI item in spawnedQueueItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedQueueItems.Clear();

        if (queuedRecipes != null)
        {
            for (int i = 0; i < queuedRecipes.Count; i++)
            {
                RecipeConfig recipe = queuedRecipes[i];
                if (recipe == null || queueItemPrefab == null || queueListContent == null) continue;

                GameObject obj = Instantiate(queueItemPrefab, queueListContent);
                QueueItemUI queueUI = obj.GetComponent<QueueItemUI>();
                if (queueUI != null)
                {
                    queueUI.Setup(recipe.ItemID, 1, i == 0);
                    spawnedQueueItems.Add(queueUI);
                }
            }
        }

        if (productionTimeText != null)
        {
            productionTimeText.text = $"Kalan Odun: {Mathf.Max(0, remainingWood)} | Otomatik Üretim";
        }
    }

    public void ClearExternalQueueDisplay()
    {
        externalQueueDisplayActive = false;

        if (produceButton != null)
        {
            produceButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Atolye secimi zorunluyken panelin kapatilip kamyonun beklemede kalmasini
    /// engellemek icin harici baglanti tarafindan kullanilir.
    /// </summary>
    public void SetCloseButtonInteractable(bool interactable)
    {
        if (closeButton != null)
        {
            closeButton.interactable = interactable;
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
