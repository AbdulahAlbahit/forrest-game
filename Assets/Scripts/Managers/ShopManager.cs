using System;
using UnityEngine;

namespace ForrestGame.Managers
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        // Event: ItemID ve Yeni Stok Miktari
        public static event Action<string, int> OnStockChanged;
        // Event: ItemID ve Satilan Miktar
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

        private void HandleDataLoaded() { }

        private void HandleTruckArrivalAtMarket()
        {
            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null) return;
            PlayerData data = CloudSaveManager.Instance.CurrentData;
            
            if (data.ReadyForTruck == null) return;
            
            var readyDict = data.ReadyForTruck.ToDictionary();
            bool anyLoaded = false;
            
            foreach (var kvp in readyDict)
            {
                string itemId = kvp.Key;
                int hazirMiktar = System.Convert.ToInt32(kvp.Value);
                
                if (hazirMiktar > 0)
                {
                    data.ReadyForTruck.Set(itemId, 0);
                    AddStock(itemId, hazirMiktar);
                    anyLoaded = true;
                }
            }
            
            if (anyLoaded)
            {
                Debug.Log("ShopManager: Kamyon markete ulasti, urunler stoga eklendi ve bekleyen siparisler kontrol edildi!");
            }
        }

        public void AddStock(string itemId, int amount)
        {
            if (amount <= 0) return;

            PlayerData data = CloudSaveManager.Instance.CurrentData;

            // 1. Check pending orders
            if (data.PendingCustomerOrders != null)
            {
                int pending = data.PendingCustomerOrders.Get(itemId, 0);
                if (pending > 0)
                {
                    int fulfilled = Mathf.Min(pending, amount);
                    
                    data.PendingCustomerOrders.Set(itemId, pending - fulfilled);
                    amount -= fulfilled;
                    
                    int unitPrice = 100;
                    if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null)
                    {
                        foreach (var r in WorkshopManager.Instance.tumTarifler)
                        {
                            if (r != null && r.ItemID == itemId) { unitPrice = r.BaseSalePrice; break; }
                        }
                    }
                    
                    EconomyManager.Instance.AddMoney(fulfilled * unitPrice);
                    NotificationManager.Instance?.SendNotification($"Bekleyen sipariş tamamlandı: {fulfilled}x {itemId}!");
                    Debug.Log($"ShopManager: Bekleyen siparis karsilandi. {fulfilled}x {itemId} satildi.");
                }
            }

            if (amount > 0)
            {
                if (data.ShopInventory == null) 
                {
                    data.ShopInventory = new SerializableDict();
                }

                int currentStock = data.ShopInventory.Get(itemId, 0);
                int newStock = currentStock + amount;

                data.ShopInventory.Set(itemId, newStock);
                OnStockChanged?.Invoke(itemId, newStock);
            }

            // Stok veya siparis degisince aninda buluta kaydet
            CloudSaveManager.Instance.SaveData(data);
        }

        public bool TryRemoveStock(string itemId, int amount)
        {
            if (amount <= 0) return false;

            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
            {
                Debug.LogWarning("ShopManager: CloudSaveManager veya PlayerData hazir degil.");
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
                // Stok dusunce aninda buluta kaydet
                CloudSaveManager.Instance.SaveData(data);
                Debug.Log($"ShopManager: {itemId} satis sonrasi stok {currentStock} -> {newStock} (buluta kaydedildi)");
                NotificationManager.Instance?.SendNotification($"{itemId} satıldı!");
                OnItemSold?.Invoke(itemId, amount);
                return true;
            }

            return false;
        }

        public void ProcessCustomerOrder(string itemId, int amount, int unitPrice)
        {
            if (TryRemoveStock(itemId, amount))
            {
                EconomyManager.Instance.AddMoney(amount * unitPrice);
                NotificationManager.Instance.SendNotification($"{amount} adet {itemId} satildi!");
            }
            else
            {
                if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
                {
                    Debug.LogWarning("ShopManager: CloudSaveManager veya PlayerData hazir degil.");
                    return;
                }

                PlayerData data = CloudSaveManager.Instance.CurrentData;
                
                if (data.PendingCustomerOrders == null) 
                {
                    data.PendingCustomerOrders = new SerializableDict();
                }

                int currentPending = data.PendingCustomerOrders.Get(itemId, 0);
                data.PendingCustomerOrders.Set(itemId, currentPending + amount);

                NotificationManager.Instance.SendNotification($"Yeni Siparis: {amount} adet {itemId} bekleniyor!");
                CloudSaveManager.Instance.SaveData(data);
            }
        }
    }
}
