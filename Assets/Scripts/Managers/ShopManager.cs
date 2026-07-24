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

        public void AddStock(string itemId, int amount)
        {
            if (amount <= 0) return;

            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
            {
                Debug.LogWarning("ShopManager: CloudSaveManager veya PlayerData hazir degil.");
                return;
            }

            PlayerData data = CloudSaveManager.Instance.CurrentData;
            
            if (data.ShopInventory == null) 
            {
                data.ShopInventory = new SerializableDict();
            }

            int currentStock = data.ShopInventory.Get(itemId, 0);
            int newStock = currentStock + amount;

            data.ShopInventory.Set(itemId, newStock);
            OnStockChanged?.Invoke(itemId, newStock);
            // Stok degisince aninda buluta kaydet
            CloudSaveManager.Instance.SaveData(data);
            Debug.Log($"ShopManager: {itemId} stoku {currentStock} -> {newStock} (buluta kaydedildi)");
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
