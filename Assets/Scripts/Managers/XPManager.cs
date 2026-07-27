using System;
using UnityEngine;
using ForrestGame.Utils;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    [Header("Seviye Ayarlari")]
    [Tooltip("Seviye 1'den 2'ye gecmek icin gereken temel XP miktari.")]
    [SerializeField] private int temelGerekenXP = 100;

    [Tooltip("Her seviyede gereken XP bu carpan ile artar. Ornek: 1.4 ise her seviye %40 daha fazla XP gerektirir.")]
    [SerializeField] private float seviyeCarpani = 1.4f;

    /
    /
    /
    public event Action<int> OnXPChanged;

    /
    /
    /
    public event Action<int> OnLevelUp;

    /
    /
    /
    public int MevcutXP 
    {
        get 
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.XP;
            return 0;
        }
    }

    /
    /
    /
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

    /
    /
    /
    private void HandleDataLoaded(PlayerData data)
    {
        OnXPChanged?.Invoke(data.XP);
        GameLogger.Log($"XPManager: Bulut verisi ile senkronize edildi. XP: {data.XP}, Seviye: {data.OyuncuSeviyesi}");
    }

    /
    /
    /
    /
    public void AddXP(int amount)
    {
        if (amount <= 0)
        {
            GameLogger.LogWarning("XPManager: Eklenecek XP miktari pozitif olmalidir.");
            return;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("XPManager: CloudSaveManager veya CurrentData hazir degil. XP eklenemiyor.");
            return;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        data.XP += amount;
        GameLogger.Log($"XPManager: +{amount} XP kazanildi. Toplam XP: {data.XP}");


        while (data.XP >= GetRequiredXPForLevel(data.OyuncuSeviyesi))
        {
            data.XP -= GetRequiredXPForLevel(data.OyuncuSeviyesi);
            data.OyuncuSeviyesi++;

            GameLogger.Log($"XPManager: LEVEL UP! Yeni seviye: {data.OyuncuSeviyesi}");
            OnLevelUp?.Invoke(data.OyuncuSeviyesi);
        }

        OnXPChanged?.Invoke(data.XP);


        CloudSaveManager.Instance.SaveData(data);
    }

    /
    /
    /
    /
    public int GetRequiredXPForLevel(int level)
    {
        float requiredXP = temelGerekenXP * Mathf.Pow(seviyeCarpani, level - 1);
        return Mathf.RoundToInt(requiredXP);
    }

    /
    /
    /
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
