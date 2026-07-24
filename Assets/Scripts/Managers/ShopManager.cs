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

        private void HandleDataLoaded(PlayerData data)
        {
            if (data == null) return;
            CheckAndFulfillPendingOrders(data, true);
        }

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
            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null) return;

            PlayerData data = CloudSaveManager.Instance.CurrentData;

            if (data.ShopInventory == null) 
            {
                data.ShopInventory = new SerializableDict();
            }

            // 1. Once stogu envantere ekle
            int currentStock = data.ShopInventory.Get(itemId, 0);
            int newStock = currentStock + amount;
            data.ShopInventory.Set(itemId, newStock);
            OnStockChanged?.Invoke(itemId, newStock);

            // 2. Bekleyen siparisleri mevcut stokla karsila
            CheckAndFulfillPendingOrders(data, false);

            // 3. Degisiklikleri kaydet
            CloudSaveManager.Instance.SaveData(data);
        }

        public void CheckAndFulfillPendingOrders(PlayerData data, bool autoSave = true)
        {
            if (data == null || data.PendingCustomerOrders == null || data.ShopInventory == null) return;
            
            bool anyFulfilled = false;
            var pendingDict = data.PendingCustomerOrders.ToDictionary();
            
            foreach (var kvp in pendingDict)
            {
                string itemId = kvp.Key;
                int pending = System.Convert.ToInt32(kvp.Value);
                
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
                        Debug.Log($"ShopManager: Bekleyen siparis karsilandi. {fulfilled}x {itemId} satildi.");
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
                
                NotificationManager.Instance?.SendNotification($"{itemId} satıldı!");
                
                // Eventi tetikle (QuestManager vb. dinleyebilir)
                OnItemSold?.Invoke(itemId, amount);
                
                AnalyticsManager.Instance?.LogCustomEvent("item_sold", "item_id", itemId);

                // Satis ve event sonrasi aninda buluta kaydet
                CloudSaveManager.Instance.SaveData(data);
                Debug.Log($"ShopManager: {itemId} satis sonrasi stok {currentStock} -> {newStock} (buluta kaydedildi)");
                
                return true;
            }

            return false;
        }

        public void ProcessCustomerOrder(string itemId, int amount, int unitPrice)
        {
            if (TryRemoveStock(itemId, amount))
            {
                EconomyManager.Instance?.AddMoney(amount * unitPrice);
                // OnItemSold?.Invoke kaldirildi cunku TryRemoveStock icinde zaten cagiriliyor.
                NotificationManager.Instance?.SendNotification($"{amount} adet {itemId} satildi!");
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

                NotificationManager.Instance?.SendNotification($"Yeni Siparis: {amount} adet {itemId} bekleniyor!");
                
                // Siparisin karsilanabilen kismini hemen sat (stokta az da olsa varsa degerlendir)
                CheckAndFulfillPendingOrders(data, false);
                
                CloudSaveManager.Instance.SaveData(data);
            }
        }
    }
}
