using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tum upgrade islemlerini merkezi olarak yoneten Singleton manager.
/// Inspector'dan atanan ItemUpgradeConfig listesi uzerinden herhangi bir
/// esyanin yukseltme islemini gerceklestirir.
///
/// Kullanim: UpgradeManager.Instance.UpgradeItem("Axe");
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade Config Listesi")]
    [Tooltip("Oyundaki tum upgrade edilebilir esyalarin config dosyalarini buraya surukleyin.")]
    [SerializeField] private List<ItemUpgradeConfig> configs = new List<ItemUpgradeConfig>();

    /// <summary>
    /// Herhangi bir esya yukseltildiginde tetiklenir.
    /// Parametreler: (ItemID, YeniSeviye)
    /// </summary>
    public event Action<string, int> OnItemUpgraded;

    // Config'leri hizli aramak icin runtime sozluk
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

    /// <summary>
    /// Inspector'dan atanan config listesini Dictionary'ye cevirir.
    /// Boylece her UpgradeItem cagirisinda listeyi donmek yerine O(1) erisim saglanir.
    /// </summary>
    private void BuildConfigLookup()
    {
        configLookup = new Dictionary<string, ItemUpgradeConfig>();

        foreach (var config in configs)
        {
            if (config == null)
            {
                Debug.LogWarning("[UpgradeManager] Config listesinde null eleman var, atlaniyor.");
                continue;
            }

            if (string.IsNullOrEmpty(config.ItemID))
            {
                Debug.LogWarning($"[UpgradeManager] Config '{config.name}' icin ItemID bos, atlaniyor.");
                continue;
            }

            if (configLookup.ContainsKey(config.ItemID))
            {
                Debug.LogWarning($"[UpgradeManager] Ayni ItemID ile birden fazla config var: '{config.ItemID}'. Sadece ilk eklenen kullanilacak.");
                continue;
            }

            configLookup[config.ItemID] = config;
        }

        Debug.Log($"[UpgradeManager] {configLookup.Count} adet upgrade config yuklendi.");
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Belirtilen esyayi bir seviye yukseltir.
    /// Fiyati formul ile hesaplar, EconomyManager uzerinden parayi duser,
    /// PlayerData'daki seviyeyi artirip CloudSave'e kaydeder.
    /// </summary>
    public bool UpgradeItem(string itemId)
    {
        // --- Guard Clause'lar ---
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError("[UpgradeManager] UpgradeItem: itemId bos olamaz!");
            return false;
        }

        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            Debug.LogError($"[UpgradeManager] '{itemId}' icin config bulunamadi! Inspector'dan eklenmis mi?");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogError("[UpgradeManager] CloudSaveManager veya CurrentData hazir degil. Yukseltme iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError("[UpgradeManager] EconomyManager bulunamadi!");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        int currentLevel = data.GetItemLevel(itemId);

        // --- Max Seviye Kontrolu ---
        if (config.IsMaxLevel(currentLevel))
        {
            Debug.LogWarning($"[UpgradeManager] '{itemId}' zaten maksimum seviyede ({currentLevel})!");
            return false;
        }

        // --- Fiyat Hesaplama ve Para Dusme ---
        int cost = config.GetUpgradeCost(currentLevel);

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.LogWarning($"[UpgradeManager] '{itemId}' yukseltme icin yetersiz para! Gereken: {cost}, Mevcut: {EconomyManager.Instance.MevcutPara}");
            return false;
        }

        // --- Seviye Artirma ---
        int newLevel = currentLevel + 1;
        data.SetItemLevel(itemId, newLevel);

        // --- Firebase'e Kaydet ---
        CloudSaveManager.Instance.SaveData(data);

        float newValue = config.GetStatValue(newLevel);
        Debug.Log($"[UpgradeManager] '{itemId}' yukseltildi! Seviye: {currentLevel} -> {newLevel}, Yeni Deger: {newValue:F1}, Harcanan: {cost}");

        // --- Event Tetikle ---
        OnItemUpgraded?.Invoke(itemId, newLevel);

        return true;
    }

    /// <summary>
    /// Belirtilen esyanin mevcut seviyesini PlayerData'dan okur.
    /// </summary>
    public int GetItemLevel(string itemId)
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
            return 1;

        return CloudSaveManager.Instance.CurrentData.GetItemLevel(itemId);
    }

    /// <summary>
    /// Belirtilen esyanin mevcut seviyesine gore stat degerini hesaplar.
    /// Ornek: Balta gucu, sepet kapasitesi, kamyon hizi vb.
    /// </summary>
    public float GetItemValue(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            Debug.LogWarning($"[UpgradeManager] GetItemValue: '{itemId}' icin config bulunamadi! Varsayilan 1 donduruluyor.");
            return 1f;
        }

        int level = GetItemLevel(itemId);
        return config.GetStatValue(level);
    }

    /// <summary>
    /// Belirtilen esyanin bir sonraki seviyeye gecis maliyetini hesaplar.
    /// UI'da fiyat gostermek icin kullanilir.
    /// </summary>
    public int GetUpgradeCost(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null)
        {
            Debug.LogWarning($"[UpgradeManager] GetUpgradeCost: '{itemId}' icin config bulunamadi!");
            return 0;
        }

        int level = GetItemLevel(itemId);
        return config.GetUpgradeCost(level);
    }

    /// <summary>
    /// Belirtilen esyanin maksimum seviyeye ulasip ulasmadigini kontrol eder.
    /// </summary>
    public bool IsMaxLevel(string itemId)
    {
        ItemUpgradeConfig config = FindConfig(itemId);
        if (config == null) return true; // Config yoksa upgrade yapilamaz

        int level = GetItemLevel(itemId);
        return config.IsMaxLevel(level);
    }

    // ═══════════════════════════════════════════
    //  Dahili Yardimci
    // ═══════════════════════════════════════════

    /// <summary>
    /// ItemID'ye gore config dosyasini bulur. Lookup tablosu kullanir (O(1)).
    /// </summary>
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
