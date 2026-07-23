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

    // Eskiden odunlar dogrudan uretiliyordu, simdi mevcutKuyruk "Ham Odun Bakiyesi" olarak calisiyor.
    private int mevcutKuyruk; 
    
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

    public int MevcutKuyruk => mevcutKuyruk;
    public bool IsQueueFull => mevcutKuyruk >= GetWorkshopCapacity();
    public bool IsQueueEmpty => mevcutKuyruk <= 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        mevcutKuyruk = 0;
        uretimZamanlayicisi = 0f;
        cachedSpeed = 0.5f;
        cachedCapacity = 10;
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;
        }
    }

    private void OnDisable()
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
        Debug.Log($"WorkshopManager: Bulut verisi ile senkronize edildi. Hiz: {cachedSpeed:F2}/sn, Kapasite: {cachedCapacity}");
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
        if (IsQueueFull)
        {
            Debug.LogWarning("WorkshopManager: Kapasite dolu! Odun eklenemiyor.");
            return false;
        }

        mevcutKuyruk++;
        OnQueueChanged?.Invoke(mevcutKuyruk, GetWorkshopCapacity());
        return true;
    }

    /// <summary>
    /// Test amaciyla veya kapasiteden bagimsiz dogrudan odun bakiyesi ekler.
    /// </summary>
    public void AddWood(int amount)
    {
        if (amount <= 0) return;
        mevcutKuyruk += amount;
        OnQueueChanged?.Invoke(mevcutKuyruk, GetWorkshopCapacity());
        Debug.Log($"WorkshopManager: Atolyeye {amount} odun dogrudan eklendi. Guncel bakiye: {mevcutKuyruk}");
    }

    /// <summary>
    /// Arayuz (UI) tarafindan cagrilarak belirtilen tariften uretim baslatir.
    /// </summary>
    public void StartProduction(RecipeConfig recipe, int amount)
    {
        if (recipe == null || amount <= 0) return;

        int requiredWood = recipe.RequiredWood * amount;

        // Eger atolyede yeterli ham odun (mevcutKuyruk) varsa
        if (mevcutKuyruk >= requiredWood)
        {
            mevcutKuyruk -= requiredWood;
            
            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null) return;
            
            PlayerData data = CloudSaveManager.Instance.CurrentData;
            if (data.WorkshopQueue == null) data.WorkshopQueue = new SerializableDict();

            // Uretim siparisini PlayerData uzerindeki WorkshopQueue'ya ekle
            int existingQueue = data.WorkshopQueue.Get(recipe.ItemID, 0);
            data.WorkshopQueue.Set(recipe.ItemID, existingQueue + amount);

            Debug.Log($"WorkshopManager: {amount} adet {recipe.ItemID} uretim siparisi alindi. Dusulen Odun: {requiredWood}");
            
            // Arayuz guncellemesi ve Bulut Kaydi
            OnQueueChanged?.Invoke(mevcutKuyruk, GetWorkshopCapacity());
            CloudSaveManager.Instance.SaveData(data);
        }
        else
        {
            Debug.LogWarning($"WorkshopManager: Yetersiz odun bakiyesi! Gereken: {requiredWood}, Mevcut: {mevcutKuyruk}");
        }
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

        // 2. Aktif uretilen esyanin tarifini bul
        RecipeConfig currentRecipe = null;
        if (tumTarifler != null)
        {
            foreach (var r in tumTarifler)
            {
                if (r.ItemID == aktifUretilenItem)
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
            
            // Bulut Kaydi
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
                if (r.ItemID == aktifUretilenItem) { currentRecipe = r; break; }
            }
        }
        
        if (currentRecipe == null) return 0f;

        float hizCarpani = GetProcessingSpeed();
        if (hizCarpani <= 0f) hizCarpani = 1f;
        float gercekUretimSuresi = currentRecipe.BaseProductionTime / hizCarpani;

        return Mathf.Clamp01(uretimZamanlayicisi / gercekUretimSuresi);
    }
}
