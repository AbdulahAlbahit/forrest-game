using System;
using UnityEngine;

public class WorkshopManager : MonoBehaviour
{
    public static WorkshopManager Instance { get; private set; }

    [Header("Atolye Ayarlari")]
    [Tooltip("Seviye 1'deki temel uretim hizi (saniyede islenen odun sayisi).")]
    [SerializeField] private float temelUretimHizi = 0.5f;

    [Tooltip("Her atolye seviyesinde eklenen uretim hizi artisi.")]
    [SerializeField] private float uretimHiziCarpani = 0.25f;

    [Tooltip("Seviye 1'deki temel atolye kapasitesi (max birikebilecek urun sayisi).")]
    [SerializeField] private int temelKapasite = 10;

    [Tooltip("Her atolye seviyesinde eklenen ekstra kapasite.")]
    [SerializeField] private int kapasiteCarpani = 5;

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

    /// <summary>
    /// Atolye seviyesine bagli olarak saniyede kac odun islendigini hesaplar.
    /// Formul: temelUretimHizi + (AtolyeSeviyesi * uretimHiziCarpani)
    /// Ornek (temel=0.5, carpan=0.25):
    ///   Seviye 1: 0.75 odun/sn
    ///   Seviye 2: 1.00 odun/sn
    ///   Seviye 3: 1.25 odun/sn
    /// </summary>
    public float GetProcessingSpeed()
    {
        return temelUretimHizi + (AtolyeSeviyesi * uretimHiziCarpani);
    }

    /// <summary>
    /// Atolye seviyesine bagli olarak max birikebilecek urun kapasitesini hesaplar.
    /// Formul: temelKapasite + (AtolyeSeviyesi * kapasiteCarpani)
    /// Ornek (temel=10, carpan=5):
    ///   Seviye 1: 15 urun
    ///   Seviye 2: 20 urun
    ///   Seviye 3: 25 urun
    /// </summary>
    public int GetWorkshopCapacity()
    {
        return temelKapasite + (AtolyeSeviyesi * kapasiteCarpani);
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

        // Seviyeye gore hesaplanan hiz: saniyede X odun => 1 odun icin 1/X saniye gerek
        float saniyePerOdun = 1f / GetProcessingSpeed();
        uretimZamanlayicisi += Time.deltaTime;

        if (uretimZamanlayicisi >= saniyePerOdun)
        {
            mevcutKuyruk--;
            uretimZamanlayicisi -= saniyePerOdun;

            Debug.Log($"WorkshopManager: 1 odun islendi! Kalan: {mevcutKuyruk}/{GetWorkshopCapacity()}");

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

    /// <summary>
    /// Atolyeyi bir seviye yukseltir. Maliyet EconomyManager uzerinden kontrol edilir.
    /// Yeterli para varsa seviyeyi artirip buluta kaydeder.
    /// </summary>
    public bool UpgradeWorkshop(int cost)
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("WorkshopManager: EconomyManager bulunamadi!");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("WorkshopManager: CloudSaveManager veya CurrentData hazir degil.");
            return false;
        }

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
