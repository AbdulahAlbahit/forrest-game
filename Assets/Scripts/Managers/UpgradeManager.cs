using System;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;








public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade Config Listesi")]
    [Tooltip("Oyundaki tum upgrade edilebilir esyalarin config dosyalarini buraya surukleyin.")]
    [SerializeField] private List<ItemUpgradeConfig> configs = new List<ItemUpgradeConfig>();





    public event Action<string, int> OnItemUpgraded;


    private Dictionary<string, ItemUpgradeConfig> configLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildConfigLookup();
    }





    private void BuildConfigLookup()
    {
        configLookup = new Dictionary<string, ItemUpgradeConfig>();

        foreach (var config in configs)
        {
            if (config == null)
            {
                GameLogger.LogWarning("[UpgradeManager] Config listesinde null eleman var, atlaniyor.");
                continue;
            }

            if (string.IsNullOrEmpty(config.ItemID))
            {
                GameLogger.LogWarning($"[UpgradeManager] Config '{config.name}' icin ItemID bos, atlaniyor.");
                continue;
            }

            if (configLookup.ContainsKey(config.ItemID))
            {
                GameLogger.LogWarning($"[UpgradeManager] Ayni ItemID ile birden fazla config var: '{config.ItemID}'. Sadece ilk eklenen kullanilacak.");
                continue;
            }

            configLookup[config.ItemID] = config;
        }

        GameLogger.Log($"[UpgradeManager] {configLookup.Count} adet upgrade config yuklendi.");
    }










    public bool UpgradeItem(string itemId)
    {

        if (string.IsNullOrEmpty(itemId))
        {
            GameLogger.LogError("[UpgradeManager] UpgradeItem: itemId bos olamaz!");
            return false;
        }

        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            GameLogger.LogError($"[UpgradeManager] '{itemId}' icin config bulunamadi! Inspector'dan eklenmis mi?");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogError("[UpgradeManager] CloudSaveManager veya CurrentData hazir degil. Yukseltme iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            GameLogger.LogError("[UpgradeManager] EconomyManager bulunamadi!");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        int currentLevel = data.GetItemLevel(itemId);


        if (config.IsMaxLevel(currentLevel))
        {
            GameLogger.LogWarning($"[UpgradeManager] '{itemId}' zaten maksimum seviyede ({currentLevel})!");
            return false;
        }


        int cost = config.GetUpgradeCost(currentLevel);

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            GameLogger.LogWarning($"[UpgradeManager] '{itemId}' yukseltme icin yetersiz para! Gereken: {cost}, Mevcut: {EconomyManager.Instance.MevcutPara}");
            return false;
        }


        int newLevel = currentLevel + 1;
        data.SetItemLevel(itemId, newLevel);


        CloudSaveManager.Instance.SaveData(data);

        float newValue = config.GetStatValue(newLevel);
        GameLogger.Log($"[UpgradeManager] '{itemId}' yukseltildi! Seviye: {currentLevel} -> {newLevel}, Yeni Deger: {newValue:F1}, Harcanan: {cost}");


        OnItemUpgraded?.Invoke(itemId, newLevel);

        return true;
    }




    public int GetItemLevel(string itemId)
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
            return 1;

        return CloudSaveManager.Instance.CurrentData.GetItemLevel(itemId);
    }





    public float GetItemValue(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            GameLogger.LogWarning($"[UpgradeManager] GetItemValue: '{itemId}' icin config bulunamadi! Varsayilan 1 donduruluyor.");
            return 1f;
        }

        int level = GetItemLevel(itemId);
        return config.GetStatValue(level);
    }





    public int GetUpgradeCost(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            GameLogger.LogWarning($"[UpgradeManager] GetUpgradeCost: '{itemId}' icin config bulunamadi!");
            return 0;
        }

        int level = GetItemLevel(itemId);
        return config.GetUpgradeCost(level);
    }




    public bool IsMaxLevel(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null) return true;

        int level = GetItemLevel(itemId);
        return config.IsMaxLevel(level);
    }








    private ItemUpgradeConfig FindConfig(string itemId)
    {
        if (configLookup == null)
        {
            BuildConfigLookup();
        }

        if (configLookup.TryGetValue(itemId, out ItemUpgradeConfig config))
        {
            return config;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
