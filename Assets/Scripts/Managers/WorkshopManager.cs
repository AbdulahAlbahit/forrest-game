using System;
using UnityEngine;

public class WorkshopManager : MonoBehaviour
{
    public static WorkshopManager Instance { get; private set; }

    [Header("Atolye Ayarlari")]
    [SerializeField] private WorkshopUpgradeConfig config;

    private int mevcutKuyruk;
    private float uretimZamanlayicisi;

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

    /// <summary>
    /// Mevcut atolye seviyesini dogrudan bulut verisinden dondurur.
    /// </summary>
    public int AtolyeSeviyesi
    {
        get
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.AtolyeSeviyesi;
            return 1;
        }
    }

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
    }

    private void Update()
    {
        ProcessQueue();
    }

    public float GetProcessingSpeed()
    {
        if (config == null)
        {
            Debug.LogError("WorkshopManager: WorkshopUpgradeConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return 0.5f;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("WorkshopManager: CloudSaveManager veya CurrentData hazir degil, varsayilan hiz donduruluyor.");
            return config.GetLevelData(1).CraftSpeed;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.AtolyeSeviyesi;
        return config.GetLevelData(seviye).CraftSpeed;
    }

    public int GetWorkshopCapacity()
    {
        if (config == null)
        {
            Debug.LogError("WorkshopManager: WorkshopUpgradeConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return 10;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("WorkshopManager: CloudSaveManager veya CurrentData hazir degil, varsayilan kapasite donduruluyor.");
            return config.GetLevelData(1).QueueCapacity;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.AtolyeSeviyesi;
        return config.GetLevelData(seviye).QueueCapacity;
    }

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
        float saniyePerOdun = 1f / GetProcessingSpeed();
        return Mathf.Clamp01(uretimZamanlayicisi / saniyePerOdun);
    }

    public bool UpgradeWorkshop()
    {
        if (config == null)
        {
            Debug.LogError("WorkshopManager: WorkshopUpgradeConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogError("WorkshopManager: CloudSaveManager veya CurrentData hazir degil. Yukseltme iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError("WorkshopManager: EconomyManager bulunamadi!");
            return false;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.AtolyeSeviyesi;
        int cost = config.GetLevelData(seviye).UpgradeCost;

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.LogWarning($"WorkshopManager: Atolye yukseltmek icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        data.AtolyeSeviyesi++;

        CloudSaveManager.Instance.SaveData(data);

        Debug.Log($"WorkshopManager: Atolye yukseltildi! Yeni Seviye: {data.AtolyeSeviyesi}, Yeni Hiz: {GetProcessingSpeed()}/sn, Yeni Kapasite: {GetWorkshopCapacity()}");
        OnWorkshopUpgraded?.Invoke(data.AtolyeSeviyesi);

        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
