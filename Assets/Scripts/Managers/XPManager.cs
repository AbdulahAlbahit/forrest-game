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

    private int mevcutXP;
    private int mevcutSeviye;

    /// <summary>
    /// XP miktari degistiginde tetiklenir. Mevcut XP degerini parametre olarak verir.
    /// </summary>
    public event Action<int> OnXPChanged;

    /// <summary>
    /// Oyuncu seviye atladiginda tetiklenir. Yeni seviyeyi parametre olarak verir.
    /// </summary>
    public event Action<int> OnLevelUp;

    /// <summary>
    /// Mevcut XP miktarini dondurur.
    /// </summary>
    public int MevcutXP => mevcutXP;

    /// <summary>
    /// Mevcut oyuncu seviyesini dondurur.
    /// </summary>
    public int MevcutSeviye => mevcutSeviye;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        mevcutXP = 0;
        mevcutSeviye = 1;
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
    /// Buluttan veri yuklendiginde XP ve seviye degerlerini senkronize eder.
    /// </summary>
    private void HandleDataLoaded(PlayerData data)
    {
        mevcutXP = data.XP;
        mevcutSeviye = data.OyuncuSeviyesi;

        OnXPChanged?.Invoke(mevcutXP);

        Debug.Log($"XPManager: Bulut verisi ile senkronize edildi. XP: {mevcutXP}, Seviye: {mevcutSeviye}");
    }

    /// <summary>
    /// Belirtilen miktarda XP ekler. Yeterli XP'ye ulasilirsa seviye atlatir.
    /// Ornek kullanim: XPManager.Instance.AddXP(25);
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("XPManager: Eklenecek XP miktari pozitif olmalidir.");
            return;
        }

        mevcutXP += amount;
        Debug.Log($"XPManager: +{amount} XP kazanildi. Toplam XP: {mevcutXP}");

        // Seviye atlama kontrolu (birden fazla seviye atlanabilir)
        while (mevcutXP >= GetRequiredXPForLevel(mevcutSeviye))
        {
            mevcutXP -= GetRequiredXPForLevel(mevcutSeviye);
            mevcutSeviye++;

            Debug.Log($"XPManager: LEVEL UP! Yeni seviye: {mevcutSeviye}");
            OnLevelUp?.Invoke(mevcutSeviye);
        }

        OnXPChanged?.Invoke(mevcutXP);

        // PlayerData'yi guncelle ve buluta kaydet
        SyncAndSave();
    }

    /// <summary>
    /// Belirtilen seviye icin gereken toplam XP miktarini hesaplar.
    /// Formul: temelGerekenXP * (seviyeCarpani ^ (seviye - 1))
    /// Ornek (temel=100, carpan=1.4):
    ///   Seviye 1 -> 2: 100 XP
    ///   Seviye 2 -> 3: 140 XP
    ///   Seviye 3 -> 4: 196 XP
    ///   Seviye 4 -> 5: 274 XP
    /// </summary>
    public int GetRequiredXPForLevel(int level)
    {
        float requiredXP = temelGerekenXP * Mathf.Pow(seviyeCarpani, level - 1);
        return Mathf.RoundToInt(requiredXP);
    }

    /// <summary>
    /// Mevcut seviye icin XP ilerleme yuzdesini dondurur (0.0 ~ 1.0).
    /// HUD'daki XP bari icin kullanilabilir.
    /// </summary>
    public float GetXPProgress()
    {
        int gerekenXP = GetRequiredXPForLevel(mevcutSeviye);
        if (gerekenXP <= 0) return 1f;
        return Mathf.Clamp01((float)mevcutXP / gerekenXP);
    }

    /// <summary>
    /// Mevcut XP ve Seviye degerlerini PlayerData'ya yazar ve buluta kaydeder.
    /// </summary>
    private void SyncAndSave()
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("XPManager: CloudSaveManager veya CurrentData bulunamadi, kayit yapilamadi.");
            return;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        data.XP = mevcutXP;
        data.OyuncuSeviyesi = mevcutSeviye;

        CloudSaveManager.Instance.SaveData(data);
        Debug.Log($"XPManager: Veriler buluta kaydedildi. XP: {mevcutXP}, Seviye: {mevcutSeviye}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
