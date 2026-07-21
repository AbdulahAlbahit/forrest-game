using System;
using UnityEngine;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    [Header("Seviye Ayarlari")]
    [Tooltip("Seviye 1'den 2'ye gecmek icin gereken temel XP miktari.")]
    [SerializeField] private int temelGerekenXP = 100;

    [Tooltip("Her seviyede gereken XP bu carpan ile artar. Ornek: 1.4 ise her seviye %40 daha fazla XP gerektirir.")]
    [SerializeField] private float seviyeCarpani = 1.4f;

    /// <summary>
    /// XP miktari degistiginde tetiklenir. Mevcut XP degerini parametre olarak verir.
    /// </summary>
    public event Action<int> OnXPChanged;

    /// <summary>
    /// Oyuncu seviye atladiginda tetiklenir. Yeni seviyeyi parametre olarak verir.
    /// </summary>
    public event Action<int> OnLevelUp;

    /// <summary>
    /// Mevcut XP miktarini dogrudan bulut verisinden dondurur.
    /// </summary>
    public int MevcutXP 
    {
        get 
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.XP;
            return 0;
        }
    }

    /// <summary>
    /// Mevcut oyuncu seviyesini dogrudan bulut verisinden dondurur.
    /// </summary>
    public int MevcutSeviye 
    {
        get 
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.OyuncuSeviyesi;
            return 1;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void OnDisable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
        }
    }

    /// <summary>
    /// Buluttan veri yuklendiginde arayuzu guncellemek icin tetiklenir.
    /// </summary>
    private void HandleDataLoaded(PlayerData data)
    {
        OnXPChanged?.Invoke(data.XP);
        Debug.Log($"XPManager: Bulut verisi ile senkronize edildi. XP: {data.XP}, Seviye: {data.OyuncuSeviyesi}");
    }

    /// <summary>
    /// Belirtilen miktarda XP ekler. Yeterli XP'ye ulasilirsa seviye atlatir.
    /// Tumu dogrudan CurrentData uzerinden hesaplanir.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("XPManager: Eklenecek XP miktari pozitif olmalidir.");
            return;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("XPManager: CloudSaveManager veya CurrentData hazir degil. XP eklenemiyor.");
            return;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        data.XP += amount;
        Debug.Log($"XPManager: +{amount} XP kazanildi. Toplam XP: {data.XP}");

        // Seviye atlama kontrolu (birden fazla seviye atlanabilir)
        while (data.XP >= GetRequiredXPForLevel(data.OyuncuSeviyesi))
        {
            data.XP -= GetRequiredXPForLevel(data.OyuncuSeviyesi);
            data.OyuncuSeviyesi++;

            Debug.Log($"XPManager: LEVEL UP! Yeni seviye: {data.OyuncuSeviyesi}");
            OnLevelUp?.Invoke(data.OyuncuSeviyesi);
        }

        OnXPChanged?.Invoke(data.XP);

        // Degisiklikleri buluta kaydet
        CloudSaveManager.Instance.SaveData(data);
    }

    /// <summary>
    /// Belirtilen seviye icin gereken toplam XP miktarini hesaplar.
    /// Formul: temelGerekenXP * (seviyeCarpani ^ (seviye - 1))
    /// </summary>
    public int GetRequiredXPForLevel(int level)
    {
        float requiredXP = temelGerekenXP * Mathf.Pow(seviyeCarpani, level - 1);
        return Mathf.RoundToInt(requiredXP);
    }

    /// <summary>
    /// Mevcut seviye icin XP ilerleme yuzdesini dondurur (0.0 ~ 1.0).
    /// </summary>
    public float GetXPProgress()
    {
        int gerekenXP = GetRequiredXPForLevel(MevcutSeviye);
        if (gerekenXP <= 0) return 1f;
        return Mathf.Clamp01((float)MevcutXP / gerekenXP);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
