using System;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance { get; private set; }

    [Header("Bolge Ayarlari")]
    [SerializeField] private RegionConfig config;

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

    public bool IsRegionUnlocked(string regionName)
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("RegionManager: Veriler henuz yuklenmedi, bolge durumu kontrol edilemiyor.");
            return false;
        }

        List<string> acilanBolgeler = CloudSaveManager.Instance.CurrentData.UnlockedRegions;
        
        if (acilanBolgeler == null)
            return false;

        return acilanBolgeler.Contains(regionName);
    }


    public int GetRegionCost(int regionId)
    {
        if (config == null)
        {
            GameLogger.LogError("RegionManager: RegionConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return 0;
        }

        return config.GetRegionData(regionId).UnlockCost;
    }

  
    public bool UnlockRegion(int regionId)
    {
        if (config == null)
        {
            GameLogger.LogError("RegionManager: RegionConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogError("RegionManager: CloudSaveManager veya CurrentData hazir degil. Kilit acma iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            GameLogger.LogError("RegionManager: EconomyManager bulunamadi!");
            return false;
        }

        RegionData regionData = config.GetRegionData(regionId);

        if (string.IsNullOrEmpty(regionData.RegionName))
        {
            GameLogger.LogError($"RegionManager: RegionID {regionId} config dosyasinda bulunamadi!");
            return false;
        }

        if (IsRegionUnlocked(regionData.RegionName))
        {
            GameLogger.LogWarning($"RegionManager: {regionData.RegionName} zaten acik!");
            return false;
        }

        int cost = regionData.UnlockCost;

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            GameLogger.LogWarning($"RegionManager: {regionData.RegionName} kilidini acmak icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data.UnlockedRegions == null)
        {
            data.UnlockedRegions = new List<string>();
        }

        data.UnlockedRegions.Add(regionData.RegionName);
        data.Gold = EconomyManager.Instance.MevcutPara;

        CloudSaveManager.Instance.MarkDirty();

        GameLogger.Log($"RegionManager: {regionData.RegionName} basariyla {cost} karsiliginda acildi ve kaydedildi.");
        OnRegionUnlocked?.Invoke(regionData.RegionName);

        return true;
    }

    public bool UnlockRegion(string regionName)
    {
        if (config == null)
        {
            GameLogger.LogError("RegionManager: RegionConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogError("RegionManager: CloudSaveManager veya CurrentData hazir degil. Kilit acma iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            GameLogger.LogError("RegionManager: EconomyManager bulunamadi!");
            return false;
        }

        RegionData regionData = config.GetRegionDataByName(regionName);

        if (string.IsNullOrEmpty(regionData.RegionName))
        {
            GameLogger.LogError($"RegionManager: '{regionName}' config dosyasinda bulunamadi!");
            return false;
        }

        if (IsRegionUnlocked(regionName))
        {
            GameLogger.LogWarning($"RegionManager: {regionName} zaten acik!");
            return false;
        }

        int cost = regionData.UnlockCost;

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            GameLogger.LogWarning($"RegionManager: {regionName} kilidini acmak icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data.UnlockedRegions == null)
        {
            data.UnlockedRegions = new List<string>();
        }

        data.UnlockedRegions.Add(regionName);
        data.Gold = EconomyManager.Instance.MevcutPara;

        CloudSaveManager.Instance.MarkDirty();

        GameLogger.Log($"RegionManager: {regionName} basariyla {cost} karsiliginda acildi ve kaydedildi.");
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
