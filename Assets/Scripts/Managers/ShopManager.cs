using System;
using UnityEngine;
using ForrestGame.Utils;

namespace ForrestGame.Managers
{
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

            bool anyLoaded = false;

            foreach (var kvp in truck.LoadedProducts)
            {
                if (kvp.Value <= 0) continue;
                AddStock(kvp.Key, kvp.Value);
                anyLoaded = true;
            }

            if (anyLoaded)
            {
                GameLogger.Log("ShopManager: Kamyon markete ulasti, urunler stoga eklendi ve bekleyen siparisler kontrol edildi!");
            }
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
            
            for (int i = 0; i < data.PendingCustomerOrders.keys.Count; i++)
            {
                string itemId = data.PendingCustomerOrders.keys[i];
                int pending = data.PendingCustomerOrders.values[i];
                
                if (pending > 0)
                {
                    int currentStock = data.ShopInventory.Get(itemId, 0);
                    if (currentStock > 0)
                    {
                        int fulfilled = Mathf.Min(pending, currentStock);
                        
                        data.PendingCustomerOrders.Set(itemId, pending - fulfilled);
                        data.ShopInventory.Set(itemId, currentStock - fulfilled);
                        
                        int unitPrice = 100;
                        if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null)
                        {
                            foreach (var r in WorkshopManager.Instance.tumTarifler)
                            {
                                if (r != null && r.ItemID == itemId) { unitPrice = r.BaseSalePrice; break; }
                            }
                        }
                        
                        EconomyManager.Instance?.AddMoney(fulfilled * unitPrice);
                        OnItemSold?.Invoke(itemId, fulfilled);
                        OnStockChanged?.Invoke(itemId, currentStock - fulfilled);
                        AnalyticsManager.Instance?.LogCustomEvent("item_sold", "item_id", itemId);
                        NotificationManager.Instance?.SendNotification($"Bekleyen siparis karsilandi: {fulfilled}x {itemId}!");
                        GameLogger.Log($"ShopManager: Bekleyen siparis karsilandi. {fulfilled}x {itemId} satildi.");
                        anyFulfilled = true;
                    }
                }
            }

            if (anyFulfilled && autoSave)
            {
                CloudSaveManager.Instance.SaveData(data);
            }
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

        public void ProcessCustomerOrder(string itemId, int amount, int unitPrice)
        {
            if (TryRemoveStock(itemId, amount))
            {
                EconomyManager.Instance?.AddMoney(amount * unitPrice);
                NotificationManager.Instance?.SendNotification($"{amount} adet {itemId} satildi!");
            }
            else
            {
                if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
                {
                    GameLogger.LogWarning("ShopManager: CloudSaveManager veya PlayerData hazir degil.");
                    return;
                }

                PlayerData data = CloudSaveManager.Instance.CurrentData;
                
                if (data.PendingCustomerOrders == null) 
                {
                    data.PendingCustomerOrders = new SerializableDict();
                }

                int currentPending = data.PendingCustomerOrders.Get(itemId, 0);
                data.PendingCustomerOrders.Set(itemId, currentPending + amount);

                NotificationManager.Instance?.SendNotification($"Yeni Siparis: {amount} adet {itemId} bekleniyor!");
                
                CheckAndFulfillPendingOrders(data, false);
                
                CloudSaveManager.Instance.SaveData(data);
            }
        }
    }
}
