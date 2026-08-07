using System;
using UnityEngine;
using ForrestGame.Core;
using System.Collections.Generic;
using ForrestGame.Utils;

[DefaultExecutionOrder(-50)]
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
    /// Sadece WorkshopRawWood artar. PlayerWoodStock (oyuncunun ayri "ambar" deposu,
    /// EfeWoodStorageBridge ile beslenir) bununla ilgisizdir ve burada degistirilmez —
    /// kamyona yuklenen odun zaten PlayerWoodStock'tan degil, karakterin tasidigi
    /// odundan (EfeOdunTasima) geliyor.
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

        data.WorkshopRawWood += amount;

        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        GameLogger.Log($"WorkshopManager: {amount} odun atolyeye transfer edildi. " +
                       $"WorkshopRawWood: {data.WorkshopRawWood}");
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

    /// <summary>
    /// Su an atolyede fiilen sayaci ilerleyen bir uretim var mi (kuyrukta urun olup
    /// odun yetersizligiyle beklemede olmak bu sayilmaz). Kamyonun atolyede
    /// beklemesi gerekip gerekmedigini disaridan sormak icin kullanilir.
    /// </summary>
    public bool IsActivelyProducing { get; private set; }

    /// <summary>
    /// WorkshopQueue icinde en az bir siparis (adedi 0'dan buyuk) var mi.
    /// Kamyonun atolyede beklemeye devam edip etmeyecegine karar vermek icin kullanilir.
    /// </summary>
    public bool HasPendingQueueOrders
    {
        get
        {
            PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
            if (data == null || data.WorkshopQueue == null) return false;

            for (int i = 0; i < data.WorkshopQueue.keys.Count; i++)
            {
                if (data.WorkshopQueue.values[i] > 0) return true;
            }

            return false;
        }
    }

    /// <summary>
    /// PlayerData.ReadyForTruck icindeki tum pozitif stoklari okuyup kaynagi sifirlar.
    /// Atolyeye dogmus kamyonun hazir urunleri "almasi" bu sekilde atomik olarak yapilir.
    /// </summary>
    public Dictionary<string, int> DrainReadyForTruck()
    {
        var drained = new Dictionary<string, int>();
        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;
        if (data == null || data.ReadyForTruck == null) return drained;

        for (int i = 0; i < data.ReadyForTruck.keys.Count; i++)
        {
            int amount = data.ReadyForTruck.values[i];
            if (amount > 0)
            {
                drained[data.ReadyForTruck.keys[i]] = amount;
                data.ReadyForTruck.values[i] = 0;
            }
        }

        return drained;
    }

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

        RecipeConfig currentRecipe = FindRecipe(aktifUretilenItem);

        if (currentRecipe == null)
        {
            GameLogger.LogWarning($"WorkshopManager: {aktifUretilenItem} icin RecipeConfig bulunamadi. Kuyruk temizleniyor.");
            data.WorkshopQueue.Set(aktifUretilenItem, 0);
            aktifUretilenItem = "";
            IsActivelyProducing = false;
            return;
        }

        if (uretimZamanlayicisi <= 0f && data.WorkshopRawWood < currentRecipe.RequiredWood)
        {
            // Bu item icin odun yetersiz. Onu kuyrukta oldugu yerde birakip, sirada
            // odunu su an yeten baska bir item var mi diye bak; oyle ki tek bir
            // odunsuz siparis butun kuyrugu kilitlemesin.
            string alternatifItem = FindNextAffordableItem(data, aktifUretilenItem);
            if (string.IsNullOrEmpty(alternatifItem))
            {
                IsActivelyProducing = false;
                return;
            }

            aktifUretilenItem = alternatifItem;
            currentRecipe = FindRecipe(aktifUretilenItem);
            if (currentRecipe == null)
            {
                IsActivelyProducing = false;
                return;
            }
        }

        if (uretimZamanlayicisi <= 0f)
        {
            data.WorkshopRawWood -= currentRecipe.RequiredWood;
            OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
            workshopVisuals?.PlayQueueCraftingStart();
        }

        IsActivelyProducing = true;

        float hizCarpani = GetProcessingSpeed();
        if (hizCarpani <= 0f) hizCarpani = 1f;

        float gercekUretimSuresi = currentRecipe.BaseProductionTime / hizCarpani;
        if (gercekUretimSuresi <= 0.1f) gercekUretimSuresi = 0.1f;

        uretimZamanlayicisi += Time.deltaTime;
        workshopVisuals?.PlayQueueCraftingProgress(uretimZamanlayicisi / gercekUretimSuresi);

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

    /// <summary>
    /// Bir urunun mevcut hiz seviyesinde uretilmesi icin gecen gercek sureyi (saniye) dondurur.
    /// GetProcessingSpeed() sadece bir carpan oldugu icin (seviye arttikca buyur, ornek: 1.0, 1.2, 1.4)
    /// dogrudan UI'da gosterilirse "yavaslıyor" gibi yanlis anlasilir. Bu metot gercek
    /// sn/urun degerini verir; seviye arttikca bu deger azalir (daha hizli uretim).
    /// </summary>
    public float GetEstimatedProductionTime()
    {
        RecipeConfig recipe = GetReferenceRecipe();
        if (recipe == null) return 0f;

        float hizCarpani = GetProcessingSpeed();
        if (hizCarpani <= 0f) hizCarpani = 1f;

        return recipe.BaseProductionTime / hizCarpani;
    }

    /// <summary>
    /// UI'da genel "uretim hizi" gostermek icin referans alinacak tarifi bulur:
    /// su an aktif uretilen tarif varsa onu, yoksa listedeki ilk tarifi kullanir.
    /// </summary>
    private RecipeConfig GetReferenceRecipe()
    {
        if (tumTarifler == null || tumTarifler.Length == 0) return null;

        if (!string.IsNullOrEmpty(aktifUretilenItem))
        {
            foreach (var r in tumTarifler)
            {
                if (r != null && r.ItemID == aktifUretilenItem) return r;
            }
        }

        return tumTarifler[0];
    }

    private RecipeConfig FindRecipe(string itemId)
    {
        if (tumTarifler == null || string.IsNullOrEmpty(itemId)) return null;

        foreach (var r in tumTarifler)
        {
            if (r != null && r.ItemID == itemId) return r;
        }

        return null;
    }

    /// <summary>
    /// Kuyrukta excludeItemId disinda, adedi 0'dan buyuk ve mevcut odunla
    /// karsilanabilecek ilk (FIFO) siparisi bulur. Odunu yetersiz bir siparisin
    /// kuyruktaki digerlerini bloke etmesini onlemek icin kullanilir.
    /// </summary>
    private string FindNextAffordableItem(PlayerData data, string excludeItemId)
    {
        for (int i = 0; i < data.WorkshopQueue.keys.Count; i++)
        {
            string itemId = data.WorkshopQueue.keys[i];
            if (itemId == excludeItemId || data.WorkshopQueue.Get(itemId, 0) <= 0)
            {
                continue;
            }

            RecipeConfig recipe = FindRecipe(itemId);
            if (recipe != null && data.WorkshopRawWood >= recipe.RequiredWood)
            {
                return itemId;
            }
        }

        return null;
    }
}