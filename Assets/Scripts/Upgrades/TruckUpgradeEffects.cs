using System;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;

[Serializable]
public class TruckCapacityUpgradeLevel
{
    [Min(1)] public int level = 1;
    [Min(1)] public int capacity = 30;
}

[Serializable]
public class TruckVisualUpgradeTier
{
    [Tooltip("Bu modelin aktif oldugu ilk kamyon leveli.")]
    [Min(1)] public int minimumLevel = 1;
    public GameObject modelPrefab;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;
}

/// <summary>
/// Mevcut Truck levelini TruckRouteController kapasitesine ve gorsel modeline baglar.
/// TruckRouteController, rota, yuk ve market state'i aynen korunur.
/// </summary>
[DisallowMultipleComponent]
public class TruckUpgradeEffects : MonoBehaviour
{
    [Header("Hazir Sistem Baglantilari")]
    [SerializeField] private TruckRouteController truckRouteController;
    [SerializeField] private string truckUpgradeItemId = "Truck";

    [Header("Kapasite - Level Baglantisi")]
    [SerializeField] private List<TruckCapacityUpgradeLevel> capacityLevels = new List<TruckCapacityUpgradeLevel>
    {
        new TruckCapacityUpgradeLevel { level = 1, capacity = 30 },
        new TruckCapacityUpgradeLevel { level = 2, capacity = 35 },
        new TruckCapacityUpgradeLevel { level = 3, capacity = 40 },
        new TruckCapacityUpgradeLevel { level = 4, capacity = 45 },
        new TruckCapacityUpgradeLevel { level = 5, capacity = 50 },
        new TruckCapacityUpgradeLevel { level = 6, capacity = 55 },
        new TruckCapacityUpgradeLevel { level = 7, capacity = 60 },
        new TruckCapacityUpgradeLevel { level = 8, capacity = 65 },
        new TruckCapacityUpgradeLevel { level = 9, capacity = 70 },
        new TruckCapacityUpgradeLevel { level = 10, capacity = 75 }
    };

    [Header("Gorsel Model Baglantisi")]
    [Tooltip("Yalnizca gorsel modellerin bulunacagi cocuk obje. Truck root verilmemeli.")]
    [SerializeField] private Transform modelContainer;
    [Tooltip("Sahnede baslangicta gorunen mevcut gorsel. Ilk degisimde guvenle kaldirilir.")]
    [SerializeField] private GameObject currentVisual;
    [Tooltip("Prefab baglantisini bozmadan ilk model degisiminde kapatilacak mevcut kamyon gorsel parcalari.")]
    [SerializeField] private List<GameObject> initialVisualObjects = new List<GameObject>();
    [SerializeField] private List<TruckVisualUpgradeTier> visualTiers = new List<TruckVisualUpgradeTier>
    {
        new TruckVisualUpgradeTier { minimumLevel = 1, localScale = Vector3.one },
        new TruckVisualUpgradeTier { minimumLevel = 3, localScale = Vector3.one },
        new TruckVisualUpgradeTier { minimumLevel = 5, localScale = Vector3.one },
        new TruckVisualUpgradeTier { minimumLevel = 7, localScale = Vector3.one },
        new TruckVisualUpgradeTier { minimumLevel = 9, localScale = Vector3.one }
    };
    [Tooltip("Kamyon yuku veya urun manifestosu varken model degisimini eve donene kadar erteler.")]
    [SerializeField] private bool deferVisualSwapWhileLoaded = true;

    public event Action<int, GameObject> OnTruckVisualChanged;

    private int _activeVisualTierIndex = -1;
    private int _pendingVisualLevel = -1;
    private bool _initialVisualsHidden;
    private bool _upgradeEventSubscribed;
    private bool _dataEventSubscribed;

    public int CurrentTruckLevel => UpgradeManager.Instance != null
        ? UpgradeManager.Instance.GetItemLevel(truckUpgradeItemId)
        : 1;

    private void Reset()
    {
        truckRouteController = GetComponent<TruckRouteController>();
    }

    private void Awake()
    {
        if (truckRouteController == null)
        {
            truckRouteController = GetComponent<TruckRouteController>();
        }
    }

    private void OnEnable()
    {
        SubscribeToExistingManagers();
    }

    private void Start()
    {
        SubscribeToExistingManagers();
        ApplyCurrentUpgradeLevel();
    }

    private void Update()
    {
        if (_pendingVisualLevel > 0 && CanSwapVisualNow())
        {
            int pendingLevel = _pendingVisualLevel;
            _pendingVisualLevel = -1;
            ApplyVisualForLevel(pendingLevel);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromManagers();
    }

    private void SubscribeToExistingManagers()
    {
        if (!_upgradeEventSubscribed && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;
            _upgradeEventSubscribed = true;
        }

        if (!_dataEventSubscribed && CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
            _dataEventSubscribed = true;
        }
    }

    private void UnsubscribeFromManagers()
    {
        if (_upgradeEventSubscribed && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;
        }

        if (_dataEventSubscribed && CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
        }

        _upgradeEventSubscribed = false;
        _dataEventSubscribed = false;
    }

    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == truckUpgradeItemId)
        {
            ApplyUpgradeLevel(newLevel);
        }
    }

    private void HandleDataLoaded(PlayerData data)
    {
        ApplyCurrentUpgradeLevel();
    }

    public void ApplyCurrentUpgradeLevel()
    {
        ApplyUpgradeLevel(CurrentTruckLevel);
    }

    public void ApplyUpgradeLevel(int level)
    {
        level = Mathf.Max(1, level);
        ApplyCapacityForLevel(level);

        if (deferVisualSwapWhileLoaded && !CanSwapVisualNow())
        {
            _pendingVisualLevel = level;
            return;
        }

        ApplyVisualForLevel(level);
    }

    private void ApplyCapacityForLevel(int level)
    {
        if (truckRouteController == null) return;
        TruckCapacityUpgradeLevel setting = FindCapacitySetting(level);
        if (setting == null) return;

        // Yuk varken kapasite mevcut yukun altina dusurulmez.
        truckRouteController.requiredWoodToDepart = Mathf.Max(
            setting.capacity,
            truckRouteController.currentLoadedWood);
    }

    private bool CanSwapVisualNow()
    {
        if (truckRouteController == null) return false;
        return truckRouteController.isAtHomePosition
            && truckRouteController.currentLoadedWood <= 0
            && truckRouteController.TotalLoadedProducts <= 0;
    }

    private void ApplyVisualForLevel(int level)
    {
        if (modelContainer == null || visualTiers == null || visualTiers.Count == 0)
        {
            return;
        }

        int tierIndex = FindVisualTierIndex(level);
        if (tierIndex < 0 || tierIndex == _activeVisualTierIndex)
        {
            return;
        }

        TruckVisualUpgradeTier tier = visualTiers[tierIndex];
        if (tier == null || tier.modelPrefab == null)
        {
            GameLogger.LogWarning($"[TruckUpgradeEffects] Level {level} icin model prefab atanmamis. Mevcut model korunuyor.", this);
            return;
        }

        if (tier.modelPrefab.GetComponentInChildren<TruckRouteController>(true) != null)
        {
            GameLogger.LogError("[TruckUpgradeEffects] Model prefab TruckRouteController iceriyor. Yalnizca gorsel prefab kullanilmali; mevcut model korunuyor.", this);
            return;
        }

        GameObject newVisual = Instantiate(tier.modelPrefab, modelContainer, false);
        newVisual.name = $"TruckVisual_Level_{level}_{tier.modelPrefab.name}";
        newVisual.transform.localPosition = tier.localPosition;
        newVisual.transform.localRotation = Quaternion.Euler(tier.localEulerAngles);
        newVisual.transform.localScale = tier.localScale;

        if (!TryBindNewVisual(newVisual))
        {
            Destroy(newVisual);
            GameLogger.LogError("[TruckUpgradeEffects] Yeni modelde teker baglantisi bulunamadi. Mevcut model korunuyor.", this);
            return;
        }

        GameObject oldVisual = currentVisual;
        currentVisual = newVisual;
        _activeVisualTierIndex = tierIndex;

        HideInitialVisualObjects();

        if (oldVisual != null && oldVisual != newVisual)
        {
            Destroy(oldVisual);
        }

        OnTruckVisualChanged?.Invoke(level, newVisual);
        GameLogger.Log($"[TruckUpgradeEffects] Truck level {level}, model: {tier.modelPrefab.name}", this);
    }

    private bool TryBindNewVisual(GameObject visual)
    {
        if (truckRouteController == null || visual == null) return false;

        TruckVisualModelBindings bindings = visual.GetComponentInChildren<TruckVisualModelBindings>(true);
        List<Transform> newWheels = new List<Transform>();
        if (bindings != null)
        {
            foreach (Transform wheel in bindings.Wheels)
            {
                if (wheel != null) newWheels.Add(wheel);
            }
        }

        if (newWheels.Count == 0)
        {
            foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
            {
                if (child != visual.transform && child.name.ToLowerInvariant().Contains("wheel"))
                {
                    newWheels.Add(child);
                }
            }
        }

        if (newWheels.Count == 0)
        {
            return false;
        }

        truckRouteController.wheels.Clear();
        truckRouteController.wheels.AddRange(newWheels);
        if (bindings != null && bindings.ExhaustVFX != null)
        {
            truckRouteController.exhaustVFX = bindings.ExhaustVFX;
        }

        return true;
    }

    private void HideInitialVisualObjects()
    {
        if (_initialVisualsHidden) return;

        foreach (GameObject visualObject in initialVisualObjects)
        {
            if (visualObject != null && visualObject != currentVisual)
            {
                visualObject.SetActive(false);
            }
        }

        _initialVisualsHidden = true;
    }

    private TruckCapacityUpgradeLevel FindCapacitySetting(int level)
    {
        TruckCapacityUpgradeLevel result = null;
        foreach (TruckCapacityUpgradeLevel setting in capacityLevels)
        {
            if (setting != null && setting.level <= level && (result == null || setting.level > result.level))
            {
                result = setting;
            }
        }

        return result;
    }

    private int FindVisualTierIndex(int level)
    {
        int result = -1;
        int bestMinimumLevel = int.MinValue;
        for (int i = 0; i < visualTiers.Count; i++)
        {
            TruckVisualUpgradeTier tier = visualTiers[i];
            if (tier != null && tier.minimumLevel <= level && tier.minimumLevel > bestMinimumLevel)
            {
                result = i;
                bestMinimumLevel = tier.minimumLevel;
            }
        }

        return result;
    }

    private void OnValidate()
    {
        foreach (TruckCapacityUpgradeLevel setting in capacityLevels)
        {
            if (setting == null) continue;
            setting.level = Mathf.Max(1, setting.level);
            setting.capacity = Mathf.Max(1, setting.capacity);
        }

        foreach (TruckVisualUpgradeTier tier in visualTiers)
        {
            if (tier == null) continue;
            tier.minimumLevel = Mathf.Max(1, tier.minimumLevel);
            if (tier.localScale == Vector3.zero) tier.localScale = Vector3.one;
        }
    }
}
