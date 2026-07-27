using System;
using UnityEngine;
using ForrestGame.Utils;
using ForrestGame.Core;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    /
    /
    /
    public event Action<int> OnMoneyChanged;

    /
    /
    /
    /
    public int MevcutPara
    {
        get
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.Para;
            return 0;
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

    /
    /
    /
    private void HandleDataLoaded(PlayerData data)
    {
        OnMoneyChanged?.Invoke(data.Para);
        GameLogger.Log($"EconomyManager: Bulut verisi yuklendi. Para: {data.Para}");
    }

    /
    /
    /
    /
    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            GameLogger.LogWarning("EconomyManager: Eklenecek miktar pozitif olmalidir.");
            return;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("EconomyManager: CloudSaveManager veya CurrentData hazir degil, para eklenemiyor.");
            return;
        }

        int effectiveAmount = Mathf.RoundToInt(amount * GetSeasonGoldMultiplier());

        CloudSaveManager.Instance.CurrentData.Para += effectiveAmount;
        OnMoneyChanged?.Invoke(CloudSaveManager.Instance.CurrentData.Para);

        GameLogger.Log($"EconomyManager: {effectiveAmount} para eklendi. Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
    }

    private float GetSeasonGoldMultiplier()
    {
        if (SeasonManager.Instance == null || SeasonManager.Instance.ayarlar == null) return 1f;
        if (SeasonManager.Instance.aktifMevsim == MevsimTuru.Sonbahar) return SeasonManager.Instance.ayarlar.sonbaharAltinCarpani;
        return 1f;
    }

    /
    /
    /
    /
    public bool SpendMoney(int amount)
    {
        if (amount <= 0)
        {
            GameLogger.LogWarning("EconomyManager: Harcanacak miktar pozitif olmalidir.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("EconomyManager: CloudSaveManager veya CurrentData hazir degil, para harcanamiyor.");
            return false;
        }

        if (CloudSaveManager.Instance.CurrentData.Para < amount)
        {
            GameLogger.LogWarning($"EconomyManager: Yetersiz bakiye! Gereken: {amount}, Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
            NotificationManager.Instance?.SendNotification($"Yetersiz bakiye! Gereken: {amount}, Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
            return false;
        }

        CloudSaveManager.Instance.CurrentData.Para -= amount;
        OnMoneyChanged?.Invoke(CloudSaveManager.Instance.CurrentData.Para);
        
        GameLogger.Log($"EconomyManager: {amount} para harcandi. Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
        return true;
    }

    /
    /
    /
    private void OnApplicationQuit()
    {
        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            CloudSaveManager.Instance.SaveData(CloudSaveManager.Instance.CurrentData);
            GameLogger.Log("EconomyManager: Oyun kapaniyor, veriler (Para vb.) buluta kaydedildi.");
        }
    }
}
