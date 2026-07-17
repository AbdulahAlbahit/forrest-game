using System;
using UnityEngine;

public class TruckManager : MonoBehaviour
{
    public static TruckManager Instance { get; private set; }

    [Header("Kamyon Ayarlari")]
    [Tooltip("Seviye 1'deki temel kamyon kapasitesi (tek seferde tasiyabilecegi max odun).")]
    [SerializeField] private int temelKapasite = 10;

    [Tooltip("Her kamyon seviyesinde eklenen ekstra kapasite miktari.")]
    [SerializeField] private int kapasiteCarpani = 5;

    /// <summary>
    /// Kamyon seviyesi yukseltildiginde tetiklenir. Yeni seviyeyi parametre olarak verir.
    /// </summary>
    public event Action<int> OnTruckUpgraded;

    /// <summary>
    /// Mevcut kamyon seviyesini dogrudan bulut verisinden dondurur.
    /// </summary>
    public int KamyonSeviyesi
    {
        get
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.KamyonSeviyesi;
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

    /// <summary>
    /// Kamyon seviyesine bagli olarak tek seferde tasinabilecek maksimum odun miktarini hesaplar.
    /// Formul: temelKapasite + (KamyonSeviyesi * kapasiteCarpani)
    /// Ornek (temel=10, carpan=5):
    ///   Seviye 1: 15 odun
    ///   Seviye 2: 20 odun
    ///   Seviye 3: 25 odun
    ///   Seviye 4: 30 odun
    /// </summary>
    public int GetTruckCapacity()
    {
        return temelKapasite + (KamyonSeviyesi * kapasiteCarpani);
    }

    /// <summary>
    /// Kamyonu bir seviye yukseltir. Maliyet EconomyManager uzerinden kontrol edilir.
    /// Yeterli para varsa seviyeyi artirip buluta kaydeder.
    /// </summary>
    public bool UpgradeTruck(int cost)
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("TruckManager: EconomyManager bulunamadi!");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("TruckManager: CloudSaveManager veya CurrentData hazir degil.");
            return false;
        }

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.LogWarning($"TruckManager: Kamyon yukseltmek icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        data.KamyonSeviyesi++;

        CloudSaveManager.Instance.SaveData(data);

        Debug.Log($"TruckManager: Kamyon yukseltildi! Yeni Seviye: {data.KamyonSeviyesi}, Yeni Kapasite: {GetTruckCapacity()}");
        OnTruckUpgraded?.Invoke(data.KamyonSeviyesi);

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
