using System;
using System.Collections.Generic;
using UnityEngine;

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance { get; private set; }

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
    /// Yeni bir bolgenin kilidini acar.
    /// Eger yeterli para varsa, parayi duser, bolgeyi listeye ekler ve buluta kaydeder.
    /// </summary>
    public bool UnlockRegion(string regionName, int cost)
    {
        if (IsRegionUnlocked(regionName))
        {
            Debug.LogWarning($"RegionManager: {regionName} zaten acik!");
            return false;
        }

        // Para harcama islemi
        if (EconomyManager.Instance != null && EconomyManager.Instance.SpendMoney(cost))
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
            {
                PlayerData data = CloudSaveManager.Instance.CurrentData;
                
                // Liste yoksa olustur
                if (data.AcilanBolgeler == null) 
                {
                    data.AcilanBolgeler = new List<string>();
                }

                // Bolgeyi ekle
                data.AcilanBolgeler.Add(regionName);
                
                // Guncel parayi da senkronize et ki buluta dogru miktar gitsin
                data.Para = EconomyManager.Instance.MevcutPara;
                
                // Buluta kaydet
                CloudSaveManager.Instance.SaveData(data);
                
                Debug.Log($"RegionManager: {regionName} basariyla {cost} karsiliginda acildi ve kaydedildi.");
                OnRegionUnlocked?.Invoke(regionName);
                
                return true;
            }
            else
            {
                Debug.LogError("RegionManager: CloudSaveManager hazir degil, bolge kaydedilemedi!");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"RegionManager: {regionName} kilidini acmak icin yeterli para yok! Gereken: {cost}");
            return false;
        }
    }
}
