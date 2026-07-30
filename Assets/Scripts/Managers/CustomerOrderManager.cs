using System;
using System.Collections;
using UnityEngine;
using ForrestGame.Managers;
using ForrestGame.Utils;

/// <summary>
/// Belirli araliklarla atolyede uretilebilen urunlerden rastgele musteri siparisi olusturur.
/// Siparis edilen urun magaza stogunda varsa aninda satar, yoksa ShopManager'in bekleyen
/// siparis kuyruguna ekler (kamyon markete ulastiginda ShopManager karsilar).
/// </summary>
public class CustomerOrderManager : MonoBehaviour
{
    public static CustomerOrderManager Instance { get; private set; }

    [Header("Siparis Araligi (saniye)")]
    [SerializeField] private float minIntervalSeconds = 10f;
    [SerializeField] private float maxIntervalSeconds = 30f;

    public static event Action<CustomerOrderData> OnOrderCreated;
    public static event Action<string> OnOrderInstantlyFulfilled;

    private Coroutine _spawnRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
            if (CloudSaveManager.Instance.CurrentData != null)
            {
                HandleDataLoaded(CloudSaveManager.Instance.CurrentData);
            }
        }
    }

    private void OnDestroy()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
        }
    }

    private void HandleDataLoaded(PlayerData data)
    {
        if (data == null) return;
        if (_spawnRoutine == null)
        {
            _spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = UnityEngine.Random.Range(minIntervalSeconds, maxIntervalSeconds);
            yield return new WaitForSeconds(wait);

            SpawnRandomOrder();
        }
    }

    public void SpawnRandomOrder()
    {
        RecipeConfig[] tarifler = WorkshopManager.Instance != null ? WorkshopManager.Instance.tumTarifler : null;
        if (tarifler == null || tarifler.Length == 0)
        {
            GameLogger.LogWarning("CustomerOrderManager: WorkshopManager.tumTarifler bos, siparis olusturulamadi.");
            return;
        }

        int startIndex = UnityEngine.Random.Range(0, tarifler.Length);
        for (int offset = 0; offset < tarifler.Length; offset++)
        {
            RecipeConfig recipe = tarifler[(startIndex + offset) % tarifler.Length];
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.ItemID)) continue;

            CreateOrder(recipe);
            return;
        }

        GameLogger.LogWarning("CustomerOrderManager: Gecerli bir RecipeConfig bulunamadi, siparis olusturulamadi.");
    }

    public void CreateOrder(RecipeConfig recipe)
    {
        if (recipe == null || string.IsNullOrWhiteSpace(recipe.ItemID)) return;
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            GameLogger.LogWarning("CustomerOrderManager: Bulut verisi hazir degil, siparis olusturulamadi.");
            return;
        }

        string itemId = recipe.ItemID;

        if (ShopManager.Instance != null && ShopManager.Instance.TryRemoveStock(itemId, 1))
        {
            EconomyManager.Instance?.AddMoney(recipe.BaseSalePrice);
            NotificationManager.Instance?.SendNotification($"Musteri geldi, stoktaki {itemId} aninda satildi!");
            GameLogger.Log($"CustomerOrderManager: {itemId} stokta hazirdi, musteriye aninda satildi (+{recipe.BaseSalePrice}).");
            OnOrderInstantlyFulfilled?.Invoke(itemId);
            return;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        if (data.PendingCustomerOrders == null) data.PendingCustomerOrders = new System.Collections.Generic.List<CustomerOrderData>();

        CustomerOrderData order = new CustomerOrderData
        {
            OrderId = data.NextCustomerOrderId++,
            ItemId = itemId
        };
        data.PendingCustomerOrders.Add(order);

        NotificationManager.Instance?.SendNotification($"Yeni siparis: {itemId} bekleniyor!");
        GameLogger.Log($"CustomerOrderManager: Yeni siparis olusturuldu (#{order.OrderId}) - {itemId}.");
        OnOrderCreated?.Invoke(order);

        CloudSaveManager.Instance.SaveData(data);
    }
}
