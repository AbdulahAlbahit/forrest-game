using System;
using UnityEngine;

public class TruckManager : MonoBehaviour
{
    public static TruckManager Instance { get; private set; }

    [Header("Kamyon Ayarlari")]
    [SerializeField] private TruckUpgradeConfig config;

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
    /// Kamyon seviyesine bagli olarak config uzerinden kamyon kapasitesini dondurur.
    /// </summary>
    public int GetTruckCapacity()
    {
        if (config == null)
        {
            Debug.LogError("TruckManager: TruckUpgradeConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return 10;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("TruckManager: CloudSaveManager veya CurrentData hazir degil, varsayilan kapasite donduruluyor.");
            return config.GetLevelData(1).TruckCapacity;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.KamyonSeviyesi;
        return config.GetLevelData(seviye).TruckCapacity;
    }

    /// <summary>
    /// Kamyonu bir seviye yukseltir. Maliyet config uzerinden alinir ve EconomyManager uzerinden kontrol edilir.
    /// Yeterli para varsa seviyeyi artirip buluta kaydeder.
    /// </summary>
    public bool UpgradeTruck()
    {
        if (config == null)
        {
            Debug.LogError("TruckManager: TruckUpgradeConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogError("TruckManager: CloudSaveManager veya CurrentData hazir degil. Yukseltme iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError("TruckManager: EconomyManager bulunamadi!");
            return false;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.KamyonSeviyesi;
        int cost = config.GetLevelData(seviye).UpgradeCost;

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
