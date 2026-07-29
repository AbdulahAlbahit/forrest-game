using System;
using UnityEngine;
using ForrestGame.Core;
using System.Collections.Generic;
using ForrestGame.Utils;

public class WorkshopManager : MonoBehaviour
{
    public static WorkshopManager Instance { get; private set; }

    [Header("Atolye Ayarlari")]
    [Tooltip("Uretilebilecek tum tarifleri buraya surukleyin.")]
    public RecipeConfig[] tumTarifler;

    [Header("Gorsel Uretim (Isci/Kutu Animasyonlari)")]
    [Tooltip("Kuyruktan bagimsiz olarak arka planda calisan uretimin isci/halka/kutu gorsellerini oynatir.")]
    [SerializeField] private WorkshopProductionManager workshopVisuals;

    private float uretimZamanlayicisi;
    private string aktifUretilenItem = "";
    private float cachedSpeed;
    private int cachedCapacity;

    /// <summary>
    /// Kamyon veya baska sistemler urunun bittigini bu event ile anlar (ItemID, Adet)
    /// </summary>
    public static event Action<string, int> OnProductionFinished;

    /// <summary>
    /// Atolye ham odun kuyrugu degistiginde tetiklenir. (mevcutOdun, maksKapasite)
    /// </summary>
    public event Action<int, int> OnQueueChanged;

    /// <summary>
    /// Eski sistem (gorsel/ses efektleri) icin korunan event.
    /// </summary>
    public event Action OnWoodProcessed;

    /// <summary>
    /// Atolye seviyesi yukseltildiginde tetiklenir. Yeni seviyeyi parametre olarak verir.
    /// </summary>
    public event Action<int> OnWorkshopUpgraded;

    public int MevcutKuyruk 
    {
        get 
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.WorkshopRawWood;
            return 0;
        }
    }
    public bool IsQueueFull => MevcutKuyruk >= GetWorkshopCapacity();
    public bool IsQueueEmpty => MevcutKuyruk <= 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        uretimZamanlayicisi = 0f;
        cachedSpeed = 0.5f;
        cachedCapacity = 10;

        if (workshopVisuals == null)
        {
            workshopVisuals = FindFirstObjectByType<WorkshopProductionManager>();
        }
    }

    private void Start()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
            if (CloudSaveManager.Instance.CurrentData != null)
            {
                HandleDataLoaded(CloudSaveManager.Instance.CurrentData);
            }
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;
            RefreshCachedValues();
        }
    }

    private void OnDestroy()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;
        }
    }

    private void HandleDataLoaded(PlayerData data)
    {
        RefreshCachedValues();
        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        GameLogger.Log($"WorkshopManager: Bulut verisi ile senkronize edildi. Hiz: {cachedSpeed:F2}/sn, Kapasite: {cachedCapacity}, Odun: {MevcutKuyruk}");
    }

    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == "Workshop" || itemId == "WorkshopQueue")
        {
            RefreshCachedValues();
            OnWorkshopUpgraded?.Invoke(newLevel);
            GameLogger.Log($"WorkshopManager: Atolye yukseltildi! Hiz: {cachedSpeed:F2}/sn, Kapasite: {cachedCapacity}");
        }
    }

    private void RefreshCachedValues()
    {
        if (UpgradeManager.Instance != null)
        {
            cachedSpeed = UpgradeManager.Instance.GetItemValue("Workshop");
            cachedCapacity = Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue("WorkshopQueue"));
        }
    }

    private void Update()
    {
        ProcessQueue();
    }

    public float GetProcessingSpeed()
    {
        float baseSpeed = cachedSpeed > 0 ? cachedSpeed : 1f;
        return baseSpeed * GetSeasonProductionMultiplier();
    }

    private float GetSeasonProductionMultiplier()
    {
        if (SeasonManager.Instance == null || SeasonManager.Instance.ayarlar == null) return 1f;
        if (SeasonManager.Instance.aktifMevsim == MevsimTuru.Yaz) return SeasonManager.Instance.ayarlar.summerProductionSpeed;
        return 1f;
    }

    public int GetWorkshopCapacity()
    {
        return cachedCapacity > 0 ? cachedCapacity : 10;
    }

    // ═══════════════════════════════════════════
    //  YENİ SİSTEM: TARİF VE ÜRETİM KUYRUĞU
    // ═══════════════════════════════════════════

    /// <summary>
    /// Atolyeye (Ham Madde Stoklarına) islenmesi icin odun ekler.
    /// </summary>
    public bool AddWoodToQueue()
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("WorkshopManager: Bulut verisi hazir degil, odun eklenemiyor.");
            return false;
        }

        if (IsQueueFull)
        {
            GameLogger.LogWarning("WorkshopManager: Kapasite dolu! Odun eklenemiyor.");
            return false;
        }

        CloudSaveManager.Instance.CurrentData.WorkshopRawWood++;
        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        return true;
    }

    /// <summary>
    /// Kamyon teslimati veya test amaciyla atolye ham odun stoguna ekler.
    /// WorkshopRawWood artar, PlayerWoodStock ayni miktar azalir (transfer tamamlandi).
    /// </summary>
    public void AddWood(int amount)
    {
        if (amount <= 0) return;
        
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("WorkshopManager: Bulut verisi hazir degil, odun eklenemiyor.");
            return;
        }

        var data = CloudSaveManager.Instance.CurrentData;

        // Atolye stogu artar
        data.WorkshopRawWood += amount;

        // Oyuncunun deposu azalir (kamyon odunlari buradan aldi ve atolyeye birakti)
        data.PlayerWoodStock = Mathf.Max(0, data.PlayerWoodStock - amount);

        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        GameLogger.Log($"WorkshopManager: {amount} odun atolyeye transfer edildi. " +
                       $"WorkshopRawWood: {data.WorkshopRawWood}, PlayerWoodStock: {data.PlayerWoodStock}");
    }

    /// <summary>
    /// Arayuz (UI) tarafindan cagrilarak belirtilen tariften uretim baslatir.
    /// Basarili olursa true, hata olursa false dondurur.
    /// </summary>
    public bool TryStartProduction(RecipeConfig recipe, int amount)
    {
        if (recipe == null || amount <= 0) return false;

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("WorkshopManager: Bulut verisi henuz hazir degil. Uretim baslatilamiyor.");
            NotificationManager.Instance?.SendNotification("Kayıt verisi hazır değil!");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        if (data.WorkshopQueue == null) data.WorkshopQueue = new SerializableDict();

        int existingQueue = data.WorkshopQueue.Get(recipe.ItemID, 0);
        data.WorkshopQueue.Set(recipe.ItemID, existingQueue + amount);

        GameLogger.Log($"WorkshopManager: {amount} adet {recipe.ItemID} uretim sirasina alindi.");
        NotificationManager.Instance?.SendNotification($"{amount} adet üretime eklendi!");

        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        
        return true;
    }

    private float debugLogTimer = 0f;

    /// <summary>
    /// Su an atolyede fiilen sayaci ilerleyen bir uretim var mi (kuyrukta urun olup
    /// odun yetersizligiyle beklemede olmak bu sayilmaz). Kamyonun atolyede
    /// beklemesi gerekip gerekmedigini disaridan sormak icin kullanilir.
    /// </summary>
    public bool IsActivelyProducing { get; private set; }

    /// <summary>
    /// WorkshopQueue icindeki siparisleri tek tek tarife gore uretir.
    /// </summary>
    private void ProcessQueue()
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            IsActivelyProducing = false;
            return;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data.WorkshopQueue == null || data.WorkshopQueue.keys.Count == 0)
        {
            uretimZamanlayicisi = 0f;
            aktifUretilenItem = "";
            IsActivelyProducing = false;
            return;
        }

        if (string.IsNullOrEmpty(aktifUretilenItem) || data.WorkshopQueue.Get(aktifUretilenItem, 0) <= 0)
        {
            aktifUretilenItem = "";
            for (int i = 0; i < data.WorkshopQueue.keys.Count; i++)
            {
                string itemId = data.WorkshopQueue.keys[i];
                if (data.WorkshopQueue.Get(itemId, 0) > 0)
                {
                    aktifUretilenItem = itemId;
                    break;
                }
            }

            if (string.IsNullOrEmpty(aktifUretilenItem))
            {
                IsActivelyProducing = false;
                return;
            }
        }

        RecipeConfig currentRecipe = null;
        if (tumTarifler != null)
        {
            foreach (var r in tumTarifler)
            {
                if (r != null && r.ItemID == aktifUretilenItem)
                {
                    currentRecipe = r;
                    break;
                }
            }
        }

        if (currentRecipe == null)
        {
            GameLogger.LogWarning($"WorkshopManager: {aktifUretilenItem} icin RecipeConfig bulunamadi. Kuyruk temizleniyor.");
            data.WorkshopQueue.Set(aktifUretilenItem, 0);
            aktifUretilenItem = "";
            IsActivelyProducing = false;
            return;
        }

        if (uretimZamanlayicisi <= 0f)
        {
            if (data.WorkshopRawWood >= currentRecipe.RequiredWood)
            {
                data.WorkshopRawWood -= currentRecipe.RequiredWood;
                OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
                workshopVisuals?.PlayQueueCraftingStart();
            }
            else
            {
                // Kuyrukta urun var ama odun yetersiz: aktif uretim sayilmaz, kamyon beklemesin.
                IsActivelyProducing = false;
                return;
            }
        }

        IsActivelyProducing = true;

        float hizCarpani = GetProcessingSpeed();
        if (hizCarpani <= 0f) hizCarpani = 1f;

        float gercekUretimSuresi = currentRecipe.BaseProductionTime / hizCarpani;
        if (gercekUretimSuresi <= 0.1f) gercekUretimSuresi = 0.1f;

        uretimZamanlayicisi += Time.deltaTime;
        workshopVisuals?.PlayQueueCraftingProgress(uretimZamanlayicisi / gercekUretimSuresi);

        debugLogTimer += Time.deltaTime;
        if (debugLogTimer >= 2f)
        {
            debugLogTimer = 0f;
            GameLogger.Log($"[Workshop Timer] {aktifUretilenItem}: {uretimZamanlayicisi:F2} / {gercekUretimSuresi:F2} sn (Hiz:{hizCarpani:F2})");
        }

        if (uretimZamanlayicisi >= gercekUretimSuresi)
        {
            uretimZamanlayicisi -= gercekUretimSuresi;

            int currentAmount = data.WorkshopQueue.Get(aktifUretilenItem, 0);
            int newAmount = Mathf.Max(0, currentAmount - 1);
            data.WorkshopQueue.Set(aktifUretilenItem, newAmount);
            
            if (data.ReadyForTruck == null) data.ReadyForTruck = new SerializableDict();
            int readyAmount = data.ReadyForTruck.Get(aktifUretilenItem, 0);
            data.ReadyForTruck.Set(aktifUretilenItem, readyAmount + 1);

            GameLogger.Log($"WorkshopManager: 1x {aktifUretilenItem} uretimi bitti! (Kamyon Bekliyor: {readyAmount + 1}, Kuyruk Kaldi: {newAmount})");
            NotificationManager.Instance?.SendNotification($"{aktifUretilenItem} üretimi tamamlandı!");
            workshopVisuals?.PlayQueueCraftingComplete();

            OnWoodProcessed?.Invoke();
            OnProductionFinished?.Invoke(aktifUretilenItem, 1);
            
            uretimZamanlayicisi = 0f;

            if (newAmount <= 0)
            {
                aktifUretilenItem = "";
            }
        }
    }

    /// <summary>
    /// Guncel uretilen esyanin yuzdelik tamamlanma durumunu dondurur. (Arayuz bari icin)
    /// </summary>
    public float GetProcessingProgress()
    {
        if (string.IsNullOrEmpty(aktifUretilenItem)) return 0f;

        RecipeConfig currentRecipe = null;
        if (tumTarifler != null)
        {
            foreach (var r in tumTarifler)
            {
                if (r != null && r.ItemID == aktifUretilenItem) { currentRecipe = r; break; }
            }
        }
        
        if (currentRecipe == null) return 0f;

        float hizCarpani = GetProcessingSpeed();
        if (hizCarpani <= 0f) hizCarpani = 1f;
        float gercekUretimSuresi = currentRecipe.BaseProductionTime / hizCarpani;

        return Mathf.Clamp01(uretimZamanlayicisi / gercekUretimSuresi);
    }
}
