using UnityEngine;

public class SystemTester : MonoBehaviour
{
    [ContextMenu("Test 1: 1000 Altin Ekle")]
    public void TestAddMoney()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(1000);
            Debug.Log($"[Test 1] 1000 Altin Eklendi. Guncel Para: {EconomyManager.Instance.MevcutPara}");
        }
        else
        {
            Debug.LogError("[Test 1] EconomyManager bulunamadi!");
        }
    }

    [ContextMenu("Test 2: 500 XP Ekle (Level Testi)")]
    public void TestAddXP()
    {
        if (XPManager.Instance != null && CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            XPManager.Instance.AddXP(500);
            int guncelSeviye = CloudSaveManager.Instance.CurrentData.OyuncuSeviyesi;
            Debug.Log($"[Test 2] 500 XP Eklendi. PlayerData'dan okunan guncel seviye: {guncelSeviye}");
        }
        else
        {
            Debug.LogError("[Test 2] XPManager veya CloudSaveManager (CurrentData) bulunamadi!");
        }
    }

    [ContextMenu("Test 3: Sepeti Gelistir (Upgrade)")]
    public void TestUpgradeBasket()
    {
        if (BasketManager.Instance != null && EconomyManager.Instance != null)
        {
            bool basarili = BasketManager.Instance.UpgradeBasket();
            if (basarili)
            {
                Debug.Log($"[Test Sepet] Sepet yukseltildi! Kalan Para: {EconomyManager.Instance.MevcutPara}, Yeni Kapasite: {BasketManager.Instance.GetBasketCapacity()}");
            }
            else
            {
                Debug.LogWarning($"[Test Sepet] Sepet yukseltme basarisiz. (Para yetersiz veya sistem hazir degil - Mevcut: {EconomyManager.Instance.MevcutPara})");
            }
        }
        else
        {
            Debug.LogError("[Test Sepet] BasketManager veya EconomyManager bulunamadi!");
        }
    }

    [ContextMenu("Test: Bolge 1'i Ac (Config)")]
    public void TestUnlockRegion()
    {
        TestUnlockRegionById(1);
    }

    [ContextMenu("Test: Bolge 2'yi Ac (Config)")]
    public void TestUnlockRegion2()
    {
        TestUnlockRegionById(2);
    }

    [ContextMenu("Test: Bolge 3'u Ac (Config)")]
    public void TestUnlockRegion3()
    {
        TestUnlockRegionById(3);
    }

    private void TestUnlockRegionById(int regionId)
    {
        if (RegionManager.Instance == null)
        {
            Debug.LogError($"[Test Bolge] RegionManager bulunamadi!");
            return;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError($"[Test Bolge] EconomyManager bulunamadi!");
            return;
        }

        int maliyet = RegionManager.Instance.GetRegionCost(regionId);
        Debug.Log($"Bolge {regionId} acilis testi baslatildi. Config'den okunan maliyet: {maliyet}");

        bool basarili = RegionManager.Instance.UnlockRegion(regionId);
        if (basarili)
        {
            Debug.Log($"[Test Bolge] Bolge {regionId} kilidi acildi! Kalan Para: {EconomyManager.Instance.MevcutPara}");
        }
        else
        {
            Debug.LogWarning($"[Test Bolge] Bolge {regionId} acilamadi. (Onceden acildi veya para yetersiz - Mevcut: {EconomyManager.Instance.MevcutPara})");
        }
    }

    [ContextMenu("Test: Bolgelerin Durumunu Kontrol Et")]
    public void TestCheckRegionStatus()
    {
        if (RegionManager.Instance != null)
        {
            bool bolge1 = RegionManager.Instance.IsRegionUnlocked("Bolge 1");
            bool bolge2 = RegionManager.Instance.IsRegionUnlocked("Bolge_2");
            bool bolge3 = RegionManager.Instance.IsRegionUnlocked("Bolge_3");

            Debug.Log($"[Test Durum] Bolge 1 Acik Mi: {bolge1}");
            Debug.Log($"[Test Durum] Bolge 2 Acik Mi: {bolge2}");
            Debug.Log($"[Test Durum] Bolge 3 Acik Mi: {bolge3}");
        }
        else
        {
            Debug.LogError("[Test Durum] RegionManager bulunamadi!");
        }
    }

    [ContextMenu("Test: Baltayi Gelistir (Maliyet 100)")]
    public void TestUpgradeAxe()
    {
        if (AxeManager.Instance != null && EconomyManager.Instance != null)
        {
            bool basarili = AxeManager.Instance.UpgradeAxe();
            if (basarili)
            {
                Debug.Log($"[Test Balta] Balta yukseltildi! Kalan Para: {EconomyManager.Instance.MevcutPara}, Yeni Guc: {AxeManager.Instance.GetAxePower()}");
            }
            else
            {
                Debug.LogWarning($"[Test Balta] Balta yukseltme basarisiz. (Para yetersiz veya sistem hazir degil - Mevcut: {EconomyManager.Instance.MevcutPara})");
            }
        }
        else
        {
            Debug.LogError("[Test Balta] AxeManager veya EconomyManager bulunamadi!");
        }
    }

    [ContextMenu("Test: Kamyonu Gelistir (Maliyet 150)")]
    public void TestUpgradeTruck()
    {
        if (TruckManager.Instance != null && EconomyManager.Instance != null)
        {
            bool basarili = TruckManager.Instance.UpgradeTruck();
            if (basarili)
            {
                Debug.Log($"[Test Kamyon] Kamyon yukseltildi! Kalan Para: {EconomyManager.Instance.MevcutPara}, Yeni Kapasite: {TruckManager.Instance.GetTruckCapacity()}");
            }
            else
            {
                Debug.LogWarning($"[Test Kamyon] Kamyon yukseltme basarisiz. (Para yetersiz veya sistem hazir degil - Mevcut: {EconomyManager.Instance.MevcutPara})");
            }
        }
        else
        {
            Debug.LogError("[Test Kamyon] TruckManager veya EconomyManager bulunamadi!");
        }
    }

    [ContextMenu("Test: Atolyeyi Gelistir")]
    public void TestUpgradeWorkshop()
    {
        if (WorkshopManager.Instance != null && EconomyManager.Instance != null)
        {
            bool basarili = WorkshopManager.Instance.UpgradeWorkshop();
            if (basarili)
            {
                Debug.Log($"[Test Atolye] Atolye yukseltildi!\n" +
                          $"  Kalan Para    : {EconomyManager.Instance.MevcutPara}\n" +
                          $"  Yeni Uretim Hizi : {WorkshopManager.Instance.GetProcessingSpeed():F2} odun/sn\n" +
                          $"  Yeni Kapasite    : {WorkshopManager.Instance.GetWorkshopCapacity()} urun");
            }
            else
            {
                Debug.LogWarning($"[Test Atolye] Atolye yukseltme basarisiz. " +
                                 $"(Para yetersiz veya sistem hazir degil - Mevcut: {EconomyManager.Instance.MevcutPara})");
            }
        }
        else
        {
            Debug.LogError("[Test Atolye] WorkshopManager veya EconomyManager bulunamadi!");
        }
    }
}
