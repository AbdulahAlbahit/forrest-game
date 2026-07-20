using System;
using UnityEngine;

public class AxeManager : MonoBehaviour
{
    public static AxeManager Instance { get; private set; }

    [Header("Balta Ayarlari")]
    [SerializeField] private AxeUpgradeConfig config;

    /// <summary>
    /// Balta seviyesi yukseltildiginde tetiklenir. Yeni seviyeyi parametre olarak verir.
    /// </summary>
    public event Action<int> OnAxeUpgraded;

    /// <summary>
    /// Mevcut balta seviyesini dogrudan bulut verisinden dondurur.
    /// </summary>
    public int BaltaSeviyesi
    {
        get
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.BaltaSeviyesi;
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
    /// Balta seviyesine bagli olarak config uzerinden balta gucunu dondurur.
    /// </summary>
    public float GetAxePower()
    {
        int seviye = CloudSaveManager.Instance.CurrentData.BaltaSeviyesi;
        return config.GetLevelData(seviye).AxePower;
    }

    /// <summary>
    /// Baltayi bir seviye yukseltir. Maliyet config uzerinden alinir ve EconomyManager uzerinden kontrol edilir.
    /// Yeterli para varsa seviyeyi artirip buluta kaydeder.
    /// </summary>
    public bool UpgradeAxe()
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("AxeManager: EconomyManager bulunamadi!");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("AxeManager: CloudSaveManager veya CurrentData hazir degil.");
            return false;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.BaltaSeviyesi;
        int cost = config.GetLevelData(seviye).UpgradeCost;

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.LogWarning($"AxeManager: Balta yukseltmek icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        data.BaltaSeviyesi++;

        CloudSaveManager.Instance.SaveData(data);

        Debug.Log($"AxeManager: Balta yukseltildi! Yeni Seviye: {data.BaltaSeviyesi}, Yeni Guc: {GetAxePower()}");
        OnAxeUpgraded?.Invoke(data.BaltaSeviyesi);

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
