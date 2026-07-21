using System;
using UnityEngine;

public class WorkshopManager : MonoBehaviour
{
    public static WorkshopManager Instance { get; private set; }

    private int mevcutKuyruk;
    private float uretimZamanlayicisi;

    // Cached degerler (her frame UpgradeManager'a sormamak icin)
    private float cachedSpeed;
    private int cachedCapacity;

    /// <summary>
    /// Atolye kuyrugu degistiginde tetiklenir. (mevcutKuyruk, maksKapasite)
    /// </summary>
    public event Action<int, int> OnQueueChanged;

    /// <summary>
    /// Bir odun islenip urune donustugunde tetiklenir.
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

    /// <summary>
    /// Buluttan veri yuklendiginde atolye degerlerini gunceller.
    /// </summary>
    private void HandleDataLoaded(PlayerData data)
    {
        RefreshCachedValues();
        Debug.Log($"WorkshopManager: Bulut verisi ile senkronize edildi. Hiz: {cachedSpeed:F2}/sn, Kapasite: {cachedCapacity}");
    }

    /// <summary>
    /// Herhangi bir esya yukseltildiginde, eger atolye ile ilgiliyse degerleri gunceller.
    /// </summary>
    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == "Workshop" || itemId == "WorkshopQueue")
        {
            RefreshCachedValues();
            OnWorkshopUpgraded?.Invoke(newLevel);
            Debug.Log($"WorkshopManager: Atolye yukseltildi! Hiz: {cachedSpeed:F2}/sn, Kapasite: {cachedCapacity}");
        }
    }

    /// <summary>
    /// UpgradeManager uzerinden atolye degerlerini yeniden hesaplar ve cache'ler.
    /// </summary>
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

    /// <summary>
    /// Atolye uretim hizini dondurur.
    /// </summary>
    public float GetProcessingSpeed()
    {
        if (UpgradeManager.Instance != null)
        {
            return UpgradeManager.Instance.GetItemValue("Workshop");
        }
        return cachedSpeed;
    }

    /// <summary>
    /// Atolye kuyruk kapasitesini dondurur.
    /// </summary>
    public int GetWorkshopCapacity()
    {
        if (UpgradeManager.Instance != null)
        {
            return Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue("WorkshopQueue"));
        }
        return cachedCapacity;
    }

    // ═══════════════════════════════════════════
    //  Oyun Mekanikleri (Degistirilmedi)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Atolyeye islenmesi icin odun ekler.
    /// Eger kapasite doluysa false doner ve eklemez.
    /// </summary>
    public bool AddWoodToQueue()
    {
        if (IsQueueFull)
        {
            Debug.LogWarning("WorkshopManager: Kuyruk dolu! Odun eklenemiyor.");
            return false;
        }

        mevcutKuyruk++;
        Debug.Log($"WorkshopManager: Kuyruga 1 odun eklendi. Durum: {mevcutKuyruk}/{GetWorkshopCapacity()}");
        OnQueueChanged?.Invoke(mevcutKuyruk, GetWorkshopCapacity());

        return true;
    }

    /// <summary>
    /// Kuyrukta odun varsa, atolye seviyesine bagli uretim hizina gore isler.
    /// </summary>
    private void ProcessQueue()
    {
        if (IsQueueEmpty)
        {
            if (uretimZamanlayicisi > 0)
                uretimZamanlayicisi = 0f;
            return;
        }

        float hiz = GetProcessingSpeed();
        
        // Hata onleyici Guard Clause: Hiz 0 veya negatif ise sonsuz donguyu engelle
        if (hiz <= 0f) 
        {
            return;
        }

        // Seviyeye gore hesaplanan hiz: saniyede X odun => 1 odun icin 1/X saniye gerek
        float saniyePerOdun = 1f / hiz;
        
        // Cok kucuk sayilar (sonsuz dongu riski) icin ekstra guvenlik onlemi
        if (saniyePerOdun <= 0.01f) 
        {
            saniyePerOdun = 0.01f;
        }

        uretimZamanlayicisi += Time.deltaTime;
        int islenenOdun = 0;

        // Unity'nin donmasini engellemek icin guvenli while dongusu
        while (uretimZamanlayicisi >= saniyePerOdun && mevcutKuyruk > 0)
        {
            mevcutKuyruk--;
            uretimZamanlayicisi -= saniyePerOdun;
            islenenOdun++;

            // StackOverflow veya Sonsuz Dongu onlemi: 1 frame'de 100'den fazla islem yapilmasin.
            if (islenenOdun >= 100) 
            {
                break;
            }
        }

        if (islenenOdun > 0)
        {
            Debug.Log($"WorkshopManager: {islenenOdun} odun islendi! Kalan: {mevcutKuyruk}/{GetWorkshopCapacity()}");
            OnWoodProcessed?.Invoke();
            OnQueueChanged?.Invoke(mevcutKuyruk, GetWorkshopCapacity());
        }
    }

    /// <summary>
    /// Isleme suresinin (0.0 ~ 1.0) yuzdesini dondurur.
    /// HUD'daki uretim bari icin kullanilabilir.
    /// </summary>
    public float GetProcessingProgress()
    {
        if (IsQueueEmpty) return 0f;
        float hiz = GetProcessingSpeed();
        if (hiz <= 0f) return 0f;
        float saniyePerOdun = 1f / hiz;
        return Mathf.Clamp01(uretimZamanlayicisi / saniyePerOdun);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
