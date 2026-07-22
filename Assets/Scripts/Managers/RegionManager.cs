using System;
using System.Collections.Generic;
using UnityEngine;

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance { get; private set; }

    [Header("Bolge Ayarlari")]
    [SerializeField] private RegionConfig config;

    /// <summary>
    /// Yeni bir bolge basariyla acildiginda tetiklenir. Bolge adini dondurur.
    /// </summary>
    public event Action<string> OnRegionUnlocked;

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
    /// Belirtilen bolgenin kilidinin acik olup olmadigini kontrol eder.
    /// </summary>
    public bool IsRegionUnlocked(string regionName)
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("RegionManager: Veriler henuz yuklenmedi, bolge durumu kontrol edilemiyor.");
            return false;
        }

        List<string> acilanBolgeler = CloudSaveManager.Instance.CurrentData.AcilanBolgeler;
        
        if (acilanBolgeler == null)
            return false;

        return acilanBolgeler.Contains(regionName);
    }

    /// <summary>
    /// Verilen regionId'ye ait bolgenin kilit acma maliyetini config dosyasindan okuyarak dondurur.
    /// Ileride butonlara fiyat yazdirmak icin kullanilacak.
    /// </summary>
    public int GetRegionCost(int regionId)
    {
        if (config == null)
        {
            Debug.LogError("RegionManager: RegionConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return 0;
        }

        return config.GetRegionData(regionId).UnlockCost;
    }

    /// <summary>
    /// RegionID ile bolge kilidini acar.
    /// Fiyati config dosyasindan okur, EconomyManager ile parayi duser ve buluta kaydeder.
    /// </summary>
    public bool UnlockRegion(int regionId)
    {
        if (config == null)
        {
            Debug.LogError("RegionManager: RegionConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogError("RegionManager: CloudSaveManager veya CurrentData hazir degil. Kilit acma iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError("RegionManager: EconomyManager bulunamadi!");
            return false;
        }

        RegionData regionData = config.GetRegionData(regionId);

        if (string.IsNullOrEmpty(regionData.RegionName))
        {
            Debug.LogError($"RegionManager: RegionID {regionId} config dosyasinda bulunamadi!");
            return false;
        }

        if (IsRegionUnlocked(regionData.RegionName))
        {
            Debug.LogWarning($"RegionManager: {regionData.RegionName} zaten acik!");
            return false;
        }

        int cost = regionData.UnlockCost;

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.LogWarning($"RegionManager: {regionData.RegionName} kilidini acmak icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data.AcilanBolgeler == null)
        {
            data.AcilanBolgeler = new List<string>();
        }

        data.AcilanBolgeler.Add(regionData.RegionName);
        data.Para = EconomyManager.Instance.MevcutPara;

        CloudSaveManager.Instance.SaveData(data);

        Debug.Log($"RegionManager: {regionData.RegionName} basariyla {cost} karsiliginda acildi ve kaydedildi.");
        OnRegionUnlocked?.Invoke(regionData.RegionName);

        return true;
    }

    /// <summary>
    /// Eski uyumluluk icin string tabanli bolge acma metodu.
    /// Bolge adini config icinde arayip maliyetini oradan okur.
    /// </summary>
    public bool UnlockRegion(string regionName)
    {
        if (config == null)
        {
            Debug.LogError("RegionManager: RegionConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogError("RegionManager: CloudSaveManager veya CurrentData hazir degil. Kilit acma iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError("RegionManager: EconomyManager bulunamadi!");
            return false;
        }

        RegionData regionData = config.GetRegionDataByName(regionName);

        if (string.IsNullOrEmpty(regionData.RegionName))
        {
            Debug.LogError($"RegionManager: '{regionName}' config dosyasinda bulunamadi!");
            return false;
        }

        if (IsRegionUnlocked(regionName))
        {
            Debug.LogWarning($"RegionManager: {regionName} zaten acik!");
            return false;
        }

        int cost = regionData.UnlockCost;

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.LogWarning($"RegionManager: {regionName} kilidini acmak icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data.AcilanBolgeler == null)
        {
            data.AcilanBolgeler = new List<string>();
        }

        data.AcilanBolgeler.Add(regionName);
        data.Para = EconomyManager.Instance.MevcutPara;

        CloudSaveManager.Instance.SaveData(data);

        Debug.Log($"RegionManager: {regionName} basariyla {cost} karsiliginda acildi ve kaydedildi.");
        OnRegionUnlocked?.Invoke(regionName);

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
