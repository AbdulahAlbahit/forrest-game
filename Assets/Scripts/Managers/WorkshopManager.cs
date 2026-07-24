using System;
using UnityEngine;
using ForrestGame.Core;
using System.Collections.Generic;

public class WorkshopManager : MonoBehaviour
{
    public static WorkshopManager Instance { get; private set; }

    [Header("Atolye Ayarlari")]
    [Tooltip("Uretilebilecek tum tarifleri buraya surukleyin.")]
    public RecipeConfig[] tumTarifler;

    // Odunlar dogrudan PlayerData uzerinden okunur ve guncellenir.
    
    // Uretim surecleri
    private float uretimZamanlayicisi;
    private string aktifUretilenItem = "";

    // Cached degerler (her frame UpgradeManager'a sormamak icin)
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
        Debug.Log($"WorkshopManager: Bulut verisi ile senkronize edildi. Hiz: {cachedSpeed:F2}/sn, Kapasite: {cachedCapacity}, Odun: {MevcutKuyruk}");
    }

    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == "Workshop" || itemId == "WorkshopQueue")
        {
            RefreshCachedValues();
            OnWorkshopUpgraded?.Invoke(newLevel);
            Debug.Log($"WorkshopManager: Atolye yukseltildi! Hiz: {cachedSpeed:F2}/sn, Kapasite: {cachedCapacity}");
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
        float baseSpeed = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemValue("Workshop") : cachedSpeed;
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
        if (UpgradeManager.Instance != null)
        {
            return Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue("WorkshopQueue"));
        }
        return cachedCapacity;
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
            Debug.LogWarning("WorkshopManager: Bulut verisi hazir degil, odun eklenemiyor.");
            return false;
        }

        if (IsQueueFull)
        {
            Debug.LogWarning("WorkshopManager: Kapasite dolu! Odun eklenemiyor.");
            return false;
        }

        CloudSaveManager.Instance.CurrentData.WorkshopRawWood++;
        CloudSaveManager.Instance.SaveData(CloudSaveManager.Instance.CurrentData);
        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        return true;
    }

    /// <summary>
    /// Test amaciyla veya kapasiteden bagimsiz dogrudan odun bakiyesi ekler.
    /// </summary>
    public void AddWood(int amount)
    {
        if (amount <= 0) return;
        
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("WorkshopManager: Bulut verisi hazir degil, odun eklenemiyor.");
            return;
        }

        CloudSaveManager.Instance.CurrentData.WorkshopRawWood += amount;
        CloudSaveManager.Instance.SaveData(CloudSaveManager.Instance.CurrentData);
        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        Debug.Log($"WorkshopManager: Atolyeye {amount} odun dogrudan eklendi. Guncel bakiye: {MevcutKuyruk}");
    }

    /// <summary>
    /// Arayuz (UI) tarafindan cagrilarak belirtilen tariften uretim baslatir.
    /// Basarili olursa true, hata olursa false dondurur.
    /// </summary>
    public bool TryStartProduction(RecipeConfig recipe, int amount)
    {
        if (recipe == null || amount <= 0) return false;

        // 1. Kayit verisi kontrolu
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("WorkshopManager: Bulut verisi henuz hazir degil. Uretim baslatilamiyor.");
            NotificationManager.Instance?.SendNotification("Kayıt verisi hazır değil!");
            return false;
        }

        int requiredWood = recipe.RequiredWood * amount;

        // 2. Odun bakiyesi kontrolu
        if (MevcutKuyruk < requiredWood)
        {
            Debug.LogWarning($"WorkshopManager: Yetersiz odun bakiyesi! Gereken: {requiredWood}, Mevcut: {MevcutKuyruk}");
            NotificationManager.Instance?.SendNotification("Yetersiz odun!");
            return false;
        }

        // 3. Kontroller gecti, islemi onayla ve odunu dus
        PlayerData data = CloudSaveManager.Instance.CurrentData;
        data.WorkshopRawWood -= requiredWood; // Firebase verisini de guncelle

        if (data.WorkshopQueue == null) data.WorkshopQueue = new SerializableDict();

        // Uretim siparisini PlayerData uzerindeki WorkshopQueue'ya ekle
        int existingQueue = data.WorkshopQueue.Get(recipe.ItemID, 0);
        data.WorkshopQueue.Set(recipe.ItemID, existingQueue + amount);

        // Arayuz guncellemesi ve aninda bulut kaydi
        OnQueueChanged?.Invoke(MevcutKuyruk, GetWorkshopCapacity());
        CloudSaveManager.Instance.SaveData(data);
        
        return true;
    }

    private float debugLogTimer = 0f;

    /// <summary>
    /// WorkshopQueue icindeki siparisleri tek tek tarife gore uretir.
    /// </summary>
    private void ProcessQueue()
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null) return;

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        
        if (data.WorkshopQueue == null || data.WorkshopQueue.ToDictionary().Keys.Count == 0)
        {
            uretimZamanlayicisi = 0f;
            aktifUretilenItem = "";
            return;
        }

        // 1. Eger su an uretilen bir sey yoksa kuyruktan bul
        if (string.IsNullOrEmpty(aktifUretilenItem) || data.WorkshopQueue.Get(aktifUretilenItem, 0) <= 0)
        {
            aktifUretilenItem = "";
            foreach (string itemId in data.WorkshopQueue.ToDictionary().Keys)
            {
                if (data.WorkshopQueue.Get(itemId, 0) > 0)
                {
                    aktifUretilenItem = itemId;
                    break;
                }
            }

            if (string.IsNullOrEmpty(aktifUretilenItem)) return; // Uretilecek hicbir sey kalmamis
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

        // Tarif bulunamazsa hatali veriyi temizle
        if (currentRecipe == null)
        {
            Debug.LogWarning($"WorkshopManager: {aktifUretilenItem} icin RecipeConfig bulunamadi. Kuyruk temizleniyor.");
            data.WorkshopQueue.Set(aktifUretilenItem, 0);
            aktifUretilenItem = "";
            return;
        }

        // 3. Uretim suresi hesaplamasi
        float hizCarpani = GetProcessingSpeed();
        if (hizCarpani <= 0f) hizCarpani = 1f;

        float gercekUretimSuresi = currentRecipe.BaseProductionTime / hizCarpani;
        if (gercekUretimSuresi <= 0.1f) gercekUretimSuresi = 0.1f;

        // 4. Zamanlayiciyi calistir
        uretimZamanlayicisi += Time.deltaTime;

        // Her 2 saniyede bir timer durumunu logla
        debugLogTimer += Time.deltaTime;
        if (debugLogTimer >= 2f)
        {
            debugLogTimer = 0f;
            Debug.Log($"[Workshop Timer] {aktifUretilenItem}: {uretimZamanlayicisi:F2} / {gercekUretimSuresi:F2} sn (Hiz:{hizCarpani:F2})");
        }

        if (uretimZamanlayicisi >= gercekUretimSuresi)
        {
            uretimZamanlayicisi -= gercekUretimSuresi;

            int currentAmount = data.WorkshopQueue.Get(aktifUretilenItem, 0);
            int newAmount = Mathf.Max(0, currentAmount - 1);
            data.WorkshopQueue.Set(aktifUretilenItem, newAmount);
            
            // Fabrika Bandı: Üretilen malı Kamyon Bekleyenler'e (ReadyForTruck) ekle
            if (data.ReadyForTruck == null) data.ReadyForTruck = new SerializableDict();
            int readyAmount = data.ReadyForTruck.Get(aktifUretilenItem, 0);
            data.ReadyForTruck.Set(aktifUretilenItem, readyAmount + 1);

            Debug.Log($"WorkshopManager: 1x {aktifUretilenItem} uretimi bitti! (Kamyon Bekliyor: {readyAmount + 1}, Kuyruk Kaldi: {newAmount})");
            NotificationManager.Instance?.SendNotification($"{aktifUretilenItem} üretimi tamamlandı!");

            OnWoodProcessed?.Invoke();
            OnProductionFinished?.Invoke(aktifUretilenItem, 1);
            
            // Aninda buluta kaydet
            CloudSaveManager.Instance.SaveData(data);

            // Zamanlayici sifirla
            uretimZamanlayicisi = 0f;

            // Bu urunden siparish bittiyse bir sonraki urune gec
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
