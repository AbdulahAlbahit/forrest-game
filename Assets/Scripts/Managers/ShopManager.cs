using System;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;

namespace ForrestGame.Managers
{
    [DefaultExecutionOrder(-50)]
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public static event Action<string, int> OnStockChanged;
        public static event Action<string, int> OnItemSold;

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
            }
            TruckRouteController.OnMarketSale += HandleTruckArrivalAtMarket;
        }

        private void OnDestroy()
        {
            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
            }
            TruckRouteController.OnMarketSale -= HandleTruckArrivalAtMarket;
        }

        private void HandleDataLoaded(PlayerData data)
        {
            if (data == null) return;
            CheckAndFulfillPendingOrders(data, true);
        }

        private void HandleTruckArrivalAtMarket(TruckRouteController truck)
        {
            if (truck == null) return;
            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null) return;

            PlayerData data = CloudSaveManager.Instance.CurrentData;
            bool anyLoaded = false;

            foreach (var kvp in truck.LoadedProducts)
            {
                if (kvp.Value <= 0) continue;

                if (HasPendingOrderFor(data, kvp.Key))
                {
                    AddStock(kvp.Key, kvp.Value);
                    anyLoaded = true;
                }
                else
                {
                    NotificationManager.Instance?.SendNotification($"{kvp.Key} icin siparis yoktu, kamyonla kayboldu!");
                    GameLogger.Log($"ShopManager: {kvp.Value}x {kvp.Key} icin bekleyen siparis yok, kamyonla kayboldu.");
                }
            }

            if (anyLoaded)
            {
                GameLogger.Log("ShopManager: Kamyon markete ulasti, siparis edilen urunler stoga eklendi ve bekleyen siparisler kontrol edildi!");
            }
        }

        private bool HasPendingOrderFor(PlayerData data, string itemId)
        {
            if (data == null || data.PendingCustomerOrders == null) return false;

            foreach (CustomerOrderData order in data.PendingCustomerOrders)
            {
                if (order != null && order.ItemId == itemId) return true;
            }

            return false;
        }

        public void AddStock(string itemId, int amount)
        {
            if (amount <= 0) return;
            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null) return;

            PlayerData data = CloudSaveManager.Instance.CurrentData;

            if (data.ShopInventory == null) 
            {
                data.ShopInventory = new SerializableDict();
            }

            int currentStock = data.ShopInventory.Get(itemId, 0);
            int newStock = currentStock + amount;
            data.ShopInventory.Set(itemId, newStock);
            OnStockChanged?.Invoke(itemId, newStock);

            CheckAndFulfillPendingOrders(data, false);

            CloudSaveManager.Instance.SaveData(data);
        }

        public void CheckAndFulfillPendingOrders(PlayerData data, bool autoSave = true)
        {
            if (data == null || data.PendingCustomerOrders == null || data.ShopInventory == null) return;

            bool anyFulfilled = false;

            int i = 0;
            while (i < data.PendingCustomerOrders.Count)
            {
                CustomerOrderData order = data.PendingCustomerOrders[i];
                if (order == null)
                {
                    data.PendingCustomerOrders.RemoveAt(i);
                    continue;
                }

                int currentStock = data.ShopInventory.Get(order.ItemId, 0);
                if (currentStock <= 0)
                {
                    i++;
                    continue;
                }

                data.PendingCustomerOrders.RemoveAt(i);
                data.ShopInventory.Set(order.ItemId, currentStock - 1);

                int unitPrice = ResolveUnitPrice(order.ItemId);

                EconomyManager.Instance?.AddMoney(unitPrice);
                OnItemSold?.Invoke(order.ItemId, 1);
                OnStockChanged?.Invoke(order.ItemId, currentStock - 1);
                AnalyticsManager.Instance?.LogCustomEvent("item_sold", "item_id", order.ItemId);
                NotificationManager.Instance?.SendNotification($"Bekleyen siparis karsilandi: 1x {order.ItemId}!");
                GameLogger.Log($"ShopManager: Bekleyen siparis (#{order.OrderId}) karsilandi. 1x {order.ItemId} satildi.");
                anyFulfilled = true;
            }

            if (anyFulfilled && autoSave)
            {
                CloudSaveManager.Instance.SaveData(data);
            }
        }

        private int ResolveUnitPrice(string itemId)
        {
            int unitPrice = 100;
            if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null)
            {
                foreach (var r in WorkshopManager.Instance.tumTarifler)
                {
                    if (r != null && r.ItemID == itemId) { unitPrice = r.BaseSalePrice; break; }
                }
            }
            return unitPrice;
        }

        public bool TryRemoveStock(string itemId, int amount)
        {
            if (amount <= 0) return false;

            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
            {
                GameLogger.LogWarning("ShopManager: CloudSaveManager veya PlayerData hazir degil.");
                return false;
            }

            PlayerData data = CloudSaveManager.Instance.CurrentData;
            
            if (data.ShopInventory == null) 
            {
                data.ShopInventory = new SerializableDict();
            }

            int currentStock = data.ShopInventory.Get(itemId, 0);

            if (currentStock >= amount)
            {
                int newStock = currentStock - amount;
                data.ShopInventory.Set(itemId, newStock);
                OnStockChanged?.Invoke(itemId, newStock);
                
                NotificationManager.Instance?.SendNotification($"{itemId} satıldı!");
                
                OnItemSold?.Invoke(itemId, amount);
                
                AnalyticsManager.Instance?.LogCustomEvent("item_sold", "item_id", itemId);

                CloudSaveManager.Instance.SaveData(data);
                GameLogger.Log($"ShopManager: {itemId} satis sonrasi stok {currentStock} -> {newStock} (buluta kaydedildi)");
                
                return true;
            }

            return false;
        }
    }
}
