using System;
using UnityEngine;
using ForrestGame.Core;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    /// <summary>
    /// Para miktari degistiginde tetiklenir. Yeni para miktarini parametre olarak verir.
    /// </summary>
    public event Action<int> OnMoneyChanged;

    /// <summary>
    /// Mevcut para miktarini dogrudan bulut verisinden dondurur.
    /// Eger veriler henuz yuklenmediyse 0 dondurur.
    /// </summary>
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

    /// <summary>
    /// Buluttan veri ilk yüklendiginde arayuzu senkronize eder.
    /// </summary>
    private void HandleDataLoaded(PlayerData data)
    {
        OnMoneyChanged?.Invoke(data.Para);
        Debug.Log($"EconomyManager: Bulut verisi yuklendi. Para: {data.Para}");
    }

    /// <summary>
    /// Belirtilen miktarda para ekler.
    /// Islem dogrudan PlayerData uzerinde yapilir ancak buluta kayit hemen atilmaz (Performans amaciyla).
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("EconomyManager: Eklenecek miktar pozitif olmalidir.");
            return;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("EconomyManager: CloudSaveManager veya CurrentData hazir degil, para eklenemiyor.");
            return;
        }

        int effectiveAmount = Mathf.RoundToInt(amount * GetSeasonGoldMultiplier());

        CloudSaveManager.Instance.CurrentData.Para += effectiveAmount;
        OnMoneyChanged?.Invoke(CloudSaveManager.Instance.CurrentData.Para);

        Debug.Log($"EconomyManager: {effectiveAmount} para eklendi. Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
    }

    private float GetSeasonGoldMultiplier()
    {
        if (SeasonManager.Instance == null || SeasonManager.Instance.ayarlar == null) return 1f;
        if (SeasonManager.Instance.aktifMevsim == MevsimTuru.Sonbahar) return SeasonManager.Instance.ayarlar.sonbaharAltinCarpani;
        return 1f;
    }

    /// <summary>
    /// Belirtilen miktarda para harcar. Yeterli para varsa true, yoksa false dondurur.
    /// Islem dogrudan PlayerData uzerinde yapilir ancak buluta kayit hemen atilmaz.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("EconomyManager: Harcanacak miktar pozitif olmalidir.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("EconomyManager: CloudSaveManager veya CurrentData hazir degil, para harcanamiyor.");
            return false;
        }

        if (CloudSaveManager.Instance.CurrentData.Para < amount)
        {
            Debug.LogWarning($"EconomyManager: Yetersiz bakiye! Gereken: {amount}, Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
            NotificationManager.Instance?.SendNotification($"Yetersiz bakiye! Gereken: {amount}, Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
            return false;
        }

        CloudSaveManager.Instance.CurrentData.Para -= amount;
        OnMoneyChanged?.Invoke(CloudSaveManager.Instance.CurrentData.Para);
        
        Debug.Log($"EconomyManager: {amount} para harcandi. Mevcut: {CloudSaveManager.Instance.CurrentData.Para}");
        return true;
    }

    /// <summary>
    /// Oyun kapanirken guncel parayi (ve diger verileri) buluta kaydeder.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            CloudSaveManager.Instance.SaveData(CloudSaveManager.Instance.CurrentData);
            Debug.Log("EconomyManager: Oyun kapaniyor, veriler (Para vb.) buluta kaydedildi.");
        }
    }
}
