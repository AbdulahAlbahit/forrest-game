using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ForrestGame.Utils;

[Serializable]
public class UpgradeLevelDescription
{
    [Min(1)] public int level = 1;
    [TextArea(1, 3)] public string description;
}

public class UpgradeZoneController : MonoBehaviour
{
    [Header("Upgrade Info")]
    public string upgradeTitle = "Table Upgrade";

    [Header("Managed Upgrade Connection")]
    [SerializeField] private string itemId = "Axe";
    [Tooltip("Yalnizca balta zone'unda acilir; sabit itemId yerine kusanilan baltayi yukseltir.")]
    [SerializeField] private bool useEquippedAxeItem;
    [Tooltip("Tek odemeyle ana item ile ayni seviyeye getirilecek ek item kimlikleri. Workshop icin WorkshopQueue eklenebilir.")]
    [SerializeField] private List<string> linkedItemIds = new List<string>();
    [Tooltip("Aciksa fiyat ItemUpgradeConfig'ten, kapaliysa asagidaki zone formulunden okunur.")]
    [SerializeField] private bool useConfigPrice;
    [Min(1)] [SerializeField] private int startingPrice = 100;
    [Min(0)] [SerializeField] private int priceIncreasePerLevel = 50;
    [Tooltip("0 ise ItemUpgradeConfig MaxLevel kullanilir.")]
    [Min(0)] [SerializeField] private int maxLevelOverride;
    [Min(0.1f)] [SerializeField] private float purchaseHoldDuration = 1f;

    [Header("Managed Upgrade World Text")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Aciksa yazida seviye vb. yerine sadece ucret (sade sayi) yazar.")]
    [SerializeField] private bool showOnlyPrice = true;
    [SerializeField] private string currencyLabel = "Para";
    [SerializeField] private List<UpgradeLevelDescription> levelDescriptions = new List<UpgradeLevelDescription>();

    [Header("VFX Settings")]
    [SerializeField] private GameObject upgradeVfxPrefab;
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("Visual Feedback")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color completedColor = new Color(0.1f, 0.9f, 0.2f, 1f);
    public Image fillImage;

    [Header("Status")]
    public float currentPaid;
    public bool isCompleted;

    [Header("Unity Events")]
    public UnityEvent OnUpgradeComplete;

    public static event Action OnUpgradeCompletedAction;
    public static event Action<string, int> OnManagedUpgradeCompleted;

    private MeshRenderer _padRenderer;
    private Material _padMaterialInstance;
    private float _managedHoldProgress;
    private float _nextDisplayRefresh;
    private int _lastProcessedFrame = -1;

    public string ItemId => ResolveItemId();
    public int CurrentLevel => UpgradeManager.Instance != null
        ? UpgradeManager.Instance.GetItemLevel(ItemId)
        : 1;
    public int CurrentPrice => GetCurrentPrice(CurrentLevel);

    private void Start()
    {
        _padRenderer = GetComponent<MeshRenderer>();
        if (_padRenderer != null)
        {
            _padMaterialInstance = _padRenderer.material;
            UpdatePadColor(emptyColor);
        }

        ResetProgressVisual();
        RefreshManagedDisplay();
    }

    private void Update()
    {
        if (useEquippedAxeItem)
        {
            // Maksimum seviyedeki baltadan baska bir baltaya gecilince zone tekrar calissin.
            isCompleted = IsManagedMaxLevel(CurrentLevel);
        }

        if (Time.unscaledTime < _nextDisplayRefresh)
        {
            return;
        }

        _nextDisplayRefresh = Time.unscaledTime + 0.25f;
        RefreshManagedDisplay();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted || !IsPlayer(other)) return;
        RefreshManagedDisplay();
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCompleted || !IsPlayer(other) || _lastProcessedFrame == Time.frameCount)
        {
            return;
        }

        _lastProcessedFrame = Time.frameCount;
        ProcessManagedUpgradeHold();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) || isCompleted)
        {
            return;
        }

        _managedHoldProgress = 0f;
        currentPaid = 0f;
        ResetProgressVisual();
    }

    private void ProcessManagedUpgradeHold()
    {
        if (!ManagedDependenciesReady())
        {
            return;
        }

        int level = CurrentLevel;
        if (IsManagedMaxLevel(level))
        {
            isCompleted = true;
            SetProgressVisual(1f);
            RefreshManagedDisplay();
            return;
        }

        int price = GetCurrentPrice(level);
        _managedHoldProgress += Time.deltaTime / Mathf.Max(0.1f, purchaseHoldDuration);
        float progress = Mathf.Clamp01(_managedHoldProgress);
        currentPaid = price * progress;
        SetProgressVisual(progress);

        if (_managedHoldProgress >= 1f)
        {
            TryPurchaseUpgrade();
        }
    }

    /// <summary>
    /// UI butonu da ayni guvenli satin alma hattini kullanabilsin diye acik API.
    /// </summary>
    public bool TryPurchaseUpgrade()
    {
        _managedHoldProgress = 0f;
        currentPaid = 0f;

        if (!ManagedDependenciesReady())
        {
            ResetProgressVisual();
            return false;
        }

        int oldLevel = CurrentLevel;
        if (IsManagedMaxLevel(oldLevel))
        {
            isCompleted = true;
            SetProgressVisual(1f);
            RefreshManagedDisplay();
            return false;
        }

        int price = GetCurrentPrice(oldLevel);
        string resolvedItemId = ItemId;
        bool success = UpgradeManager.Instance.UpgradeItem(resolvedItemId, price, linkedItemIds);
        if (!success)
        {
            ResetProgressVisual();
            RefreshManagedDisplay();
            return false;
        }

        int newLevel = UpgradeManager.Instance.GetItemLevel(resolvedItemId);
        GameLogger.Log($"[UpgradeZone] {upgradeTitle}: {oldLevel} -> {newLevel}, fiyat: {price}", this);
        PlayUpgradeVfx();
        OnUpgradeComplete?.Invoke();
        OnUpgradeCompletedAction?.Invoke();
        OnManagedUpgradeCompleted?.Invoke(resolvedItemId, newLevel);

        isCompleted = IsManagedMaxLevel(newLevel);
        SetProgressVisual(isCompleted ? 1f : 0f);
        RefreshManagedDisplay();
        return true;
    }

    private bool ManagedDependenciesReady()
    {
        return UpgradeManager.Instance != null
            && EconomyManager.Instance != null
            && CloudSaveManager.Instance != null
            && CloudSaveManager.Instance.CurrentData != null;
    }

    private int GetCurrentPrice(int currentLevel)
    {
        if (useConfigPrice && UpgradeManager.Instance != null)
        {
            int configPrice = UpgradeManager.Instance.GetUpgradeCost(ItemId);
            if (configPrice > 0) return configPrice;
        }

        return Mathf.Max(1, startingPrice + Mathf.Max(0, currentLevel - 1) * priceIncreasePerLevel);
    }

    private bool IsManagedMaxLevel(int currentLevel)
    {
        if (maxLevelOverride > 0)
        {
            return currentLevel >= maxLevelOverride;
        }

        return UpgradeManager.Instance != null && UpgradeManager.Instance.IsMaxLevel(ItemId);
    }

    private string ResolveItemId()
    {
        if (useEquippedAxeItem)
        {
            if (AxeTablePanelController.Instance != null &&
                !string.IsNullOrWhiteSpace(AxeTablePanelController.Instance.CurrentlyEquippedItemId))
            {
                return AxeTablePanelController.Instance.CurrentlyEquippedItemId;
            }
            else if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
            {
                // Fallback: Read directly from save data if panel is not initialized
                PlayerData data = CloudSaveManager.Instance.CurrentData;
                int equippedTier = data.ItemLevels != null && data.ItemLevels.ContainsKey("EquippedAxe")
                    ? data.GetItemLevel("EquippedAxe")
                    : 1;
                return equippedTier <= 1 ? itemId : $"Axe{equippedTier}";
            }
        }

        return itemId;
    }

    public void RefreshManagedDisplay()
    {
        if (statusText == null)
        {
            return;
        }

        int level = CurrentLevel;
        if (IsManagedMaxLevel(level))
        {
            statusText.text = showOnlyPrice ? "MAX" : BuildDisplayText(level, GetLevelDescription(level), "MAKSIMUM SEVIYE");
            return;
        }

        if (showOnlyPrice)
        {
            statusText.text = GetCurrentPrice(level).ToString();
            return;
        }

        string currentDescription = GetLevelDescription(level);
        string nextDescription = GetLevelDescription(level + 1);
        string changeLine = string.IsNullOrWhiteSpace(nextDescription)
            ? currentDescription
            : string.IsNullOrWhiteSpace(currentDescription)
                ? nextDescription
                : $"{currentDescription} -> {nextDescription}";
        string priceLine = $"Fiyat: {GetCurrentPrice(level)} {currencyLabel}";
        statusText.text = BuildDisplayText(level, changeLine, priceLine);
    }

    private string BuildDisplayText(int level, string middleLine, string lastLine)
    {
        string result = $"{upgradeTitle}\nSeviye {level}";
        if (!string.IsNullOrWhiteSpace(middleLine)) result += $"\n{middleLine}";
        if (!string.IsNullOrWhiteSpace(lastLine)) result += $"\n{lastLine}";
        return result;
    }

    private string GetLevelDescription(int level)
    {
        foreach (UpgradeLevelDescription entry in levelDescriptions)
        {
            if (entry != null && entry.level == level)
            {
                return entry.description;
            }
        }

        return string.Empty;
    }

    private static bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        string lowerName = other.name.ToLowerInvariant();
        return other.CompareTag("Player")
            || lowerName.Contains("player")
            || lowerName.Contains("mert")
            || lowerName.Contains("fake")
            || other.GetComponentInParent<EfeBaltaKontrol>() != null;
    }

    private void SetProgressVisual(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
            fillImage.color = completedColor;
        }
        else
        {
            UpdatePadColor(Color.Lerp(emptyColor, completedColor, progress));
        }
    }

    private void ResetProgressVisual()
    {
        SetProgressVisual(0f);
    }

    private void UpdatePadColor(Color color)
    {
        if (_padMaterialInstance == null) return;
        _padMaterialInstance.color = color;
        if (_padMaterialInstance.HasProperty("_BaseColor"))
        {
            _padMaterialInstance.SetColor("_BaseColor", color);
        }
    }

    private void PlayUpgradeVfx()
    {
        if (upgradeVfxPrefab != null)
        {
            Vector3 spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            GameObject vfxInstance = Instantiate(upgradeVfxPrefab, spawnPos, Quaternion.identity);
            Destroy(vfxInstance, 5f);
        }
    }

    private void OnValidate()
    {
        startingPrice = Mathf.Max(1, startingPrice);
        priceIncreasePerLevel = Mathf.Max(0, priceIncreasePerLevel);
        maxLevelOverride = Mathf.Max(0, maxLevelOverride);
        purchaseHoldDuration = Mathf.Max(0.1f, purchaseHoldDuration);
    }
}
