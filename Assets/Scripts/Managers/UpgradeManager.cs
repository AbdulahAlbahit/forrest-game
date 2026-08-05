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

    /// <summary>
    /// Ana upgrade islemi tamamlandiginda UI, ses ve analitik sistemlerine
    /// (itemId, yeniLevel, harcananPara, kalanPara) bilgisini verir.
    /// </summary>
    public event Action<string, int, int, int> OnUpgradeTransactionCompleted;

    private Dictionary<string, ItemUpgradeConfig> configLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // UpgradeManager farkli ekip prefablarinda yanlislikla tekrar bulunursa
            // o prefab kokundeki diger manager/UI componentlerini yok etme.
            // Yalnizca tekrar eden UpgradeManager componenti kaldirilir.
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildConfigLookup();
    }

    private void BuildConfigLookup()
    {
        configLookup = new Dictionary<string, ItemUpgradeConfig>(StringComparer.Ordinal);

        foreach (ItemUpgradeConfig config in configs)
        {
            if (config == null)
            {
                GameLogger.LogWarning("[UpgradeManager] Config listesinde null eleman var, atlaniyor.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(config.ItemID))
            {
                GameLogger.LogWarning($"[UpgradeManager] Config '{config.name}' icin ItemID bos, atlaniyor.");
                continue;
            }

            if (configLookup.ContainsKey(config.ItemID))
            {
                GameLogger.LogWarning($"[UpgradeManager] Ayni ItemID ile birden fazla config var: '{config.ItemID}'. Sadece ilk eklenen kullanilacak.");
                continue;
            }

            configLookup.Add(config.ItemID, config);
        }

        GameLogger.Log($"[UpgradeManager] {configLookup.Count} adet upgrade config yuklendi.");
    }

    /// <summary>
    /// Mevcut tek-item upgrade API'si. Eski UI ve test kodlari icin aynen korunur.
    /// </summary>
    public bool UpgradeItem(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            GameLogger.LogError($"[UpgradeManager] '{itemId}' icin config bulunamadi! Inspector'dan eklenmis mi?");
            return false;
        }

        int currentLevel = GetItemLevel(itemId);
        return UpgradeItemInternal(itemId, config.GetUpgradeCost(currentLevel), null);
    }

    /// <summary>
    /// Zone tabanli upgrade'lerde Inspector'dan gelen fiyati kullanir ve bagli
    /// item level'larini tek odeme/tek kayit ile ana level'a esler.
    /// </summary>
    public bool UpgradeItem(string itemId, int explicitCost, IReadOnlyList<string> linkedItemIds)
    {
        return UpgradeItemInternal(itemId, explicitCost, linkedItemIds);
    }

    private bool UpgradeItemInternal(string itemId, int cost, IReadOnlyList<string> linkedItemIds)
    {
        if (string.IsNullOrWhiteSpace(itemId))
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

        if (cost <= 0)
        {
            GameLogger.LogError($"[UpgradeManager] '{itemId}' icin gecersiz maliyet: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        int currentLevel = data.GetItemLevel(itemId);
        if (config.IsMaxLevel(currentLevel))
        {
            GameLogger.LogWarning($"[UpgradeManager] '{itemId}' zaten maksimum seviyede ({currentLevel})!");
            return false;
        }

        int newLevel = currentLevel + 1;
        List<string> validLinkedIds = ValidateLinkedItems(itemId, newLevel, linkedItemIds, data);
        if (validLinkedIds == null)
        {
            return false;
        }

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            GameLogger.LogWarning($"[UpgradeManager] '{itemId}' yukseltme icin yetersiz para! Gereken: {cost}, Mevcut: {EconomyManager.Instance.MevcutPara}");
            return false;
        }

        data.SetItemLevel(itemId, newLevel);
        foreach (string linkedId in validLinkedIds)
        {
            int linkedLevel = data.GetItemLevel(linkedId);
            data.SetItemLevel(linkedId, Mathf.Max(linkedLevel, newLevel));
        }

        CloudSaveManager.Instance.SaveData(data);

        float newValue = config.GetStatValue(newLevel);
        GameLogger.Log($"[UpgradeManager] '{itemId}' yukseltildi! Seviye: {currentLevel} -> {newLevel}, Yeni Deger: {newValue:F1}, Harcanan: {cost}");

        OnItemUpgraded?.Invoke(itemId, newLevel);
        foreach (string linkedId in validLinkedIds)
        {
            OnItemUpgraded?.Invoke(linkedId, data.GetItemLevel(linkedId));
        }

        OnUpgradeTransactionCompleted?.Invoke(itemId, newLevel, cost, EconomyManager.Instance.MevcutPara);

        // Analitik: Esya yukseltme takibi
        ForrestGame.Managers.AnalyticsManager.Instance?.LogItemUpgrade(itemId, newLevel, cost);

        return true;
    }

    private List<string> ValidateLinkedItems(
        string primaryItemId,
        int targetLevel,
        IReadOnlyList<string> linkedItemIds,
        PlayerData data)
    {
        List<string> validIds = new List<string>();
        if (linkedItemIds == null)
        {
            return validIds;
        }

        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal) { primaryItemId };
        foreach (string linkedId in linkedItemIds)
        {
            if (string.IsNullOrWhiteSpace(linkedId) || !uniqueIds.Add(linkedId))
            {
                continue;
            }

            ItemUpgradeConfig linkedConfig = FindConfig(linkedId);
            if (linkedConfig == null)
            {
                GameLogger.LogError($"[UpgradeManager] Bagli config bulunamadi: '{linkedId}'. Islem iptal edildi.");
                return null;
            }

            int linkedLevel = data.GetItemLevel(linkedId);
            if (linkedConfig.IsMaxLevel(linkedLevel) && linkedLevel < targetLevel)
            {
                GameLogger.LogWarning($"[UpgradeManager] Bagli item '{linkedId}' maksimum seviyede. Islem iptal edildi.");
                return null;
            }

            validIds.Add(linkedId);
        }

        return validIds;
    }

    public int GetItemLevel(string itemId)
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            return 1;
        }

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

        return config.GetStatValue(GetItemLevel(itemId));
    }

    public float GetItemValueAtLevel(string itemId, int level)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        return config != null ? config.GetStatValue(level) : 1f;
    }

    public int GetUpgradeCost(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            GameLogger.LogWarning($"[UpgradeManager] GetUpgradeCost: '{itemId}' icin config bulunamadi!");
            return 0;
        }

        return config.GetUpgradeCost(GetItemLevel(itemId));
    }

    public bool IsMaxLevel(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        return config == null || config.IsMaxLevel(GetItemLevel(itemId));
    }

    public bool TryGetConfig(string itemId, out ItemUpgradeConfig config)
    {
        config = FindConfig(itemId);
        return config != null;
    }

    /// <summary>
    /// Bir zone'a sonradan eklenen bagli item'lari, normal satin alma
    /// akisi disinda (para harcamadan) ana item'in mevcut seviyesine
    /// tek seferlik esitler. Zone zaten MAX seviyedeyse ve bir daha
    /// satin alma tetiklenemiyorsa kullanilir.
    /// </summary>
    public void ForceSyncLinkedItemLevel(string itemId, IReadOnlyList<string> linkedItemIds)
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null || linkedItemIds == null)
        {
            return;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        int targetLevel = data.GetItemLevel(itemId);

        foreach (string linkedId in linkedItemIds)
        {
            if (string.IsNullOrWhiteSpace(linkedId) || linkedId == itemId)
            {
                continue;
            }

            int linkedLevel = data.GetItemLevel(linkedId);
            if (linkedLevel >= targetLevel)
            {
                continue;
            }

            data.SetItemLevel(linkedId, targetLevel);
            OnItemUpgraded?.Invoke(linkedId, targetLevel);
            GameLogger.Log($"[UpgradeManager] '{linkedId}' zorla senkronlandi -> Seviye {targetLevel} (kaynak: '{itemId}')");
        }

        CloudSaveManager.Instance.SaveData(data);
    }

    private ItemUpgradeConfig FindConfig(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        if (configLookup == null)
        {
            BuildConfigLookup();
        }

        configLookup.TryGetValue(itemId, out ItemUpgradeConfig config);
        return config;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
