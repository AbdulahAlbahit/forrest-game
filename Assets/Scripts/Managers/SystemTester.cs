using UnityEngine;
using ForrestGame.Utils;
using ForrestGame.Managers;

public class SystemTester : MonoBehaviour
{
    [Header("Bolge Test Ayarlari")]
    [Tooltip("Acilacak bolgenin RegionID'si (Inspector'dan girin).")]
    public int testRegionId = 1;
    [Tooltip("RegionConfig asset'ini buraya suruklerin - bolge adini dinamik okumak icin.")]
    public RegionConfig testRegionConfig;

    [Header("Upgrade Test Ayarlari")]
    [Tooltip("Test edilecek esyanin ItemID'si (Axe, Basket, Truck, Workshop, WorkshopQueue vb.)")]
    public string testItemId = "Axe";

    [Header("Uretim ve Dukkan Test Ayarlari")]
    public RecipeConfig testRecipe;
    public int testWoodAmount = 50;
    public int testDeliveryAmount = 5;





    [ContextMenu("Test 1: 1000 Altin Ekle")]
    public void TestAddMoney()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(1000);
            GameLogger.Log($"[Test 1] 1000 Altin Eklendi. Guncel Para: {EconomyManager.Instance.MevcutPara}");
        }
        else
        {
            GameLogger.LogError("[Test 1] EconomyManager bulunamadi!");
        }
    }





    [ContextMenu("Test 2: 500 XP Ekle (Level Testi)")]
    public void TestAddXP()
    {
        if (XPManager.Instance != null && CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            XPManager.Instance.AddXP(500);
            int guncelSeviye = CloudSaveManager.Instance.CurrentData.OyuncuSeviyesi;
            GameLogger.Log($"[Test 2] 500 XP Eklendi. PlayerData'dan okunan guncel seviye: {guncelSeviye}");
        }
        else
        {
            GameLogger.LogError("[Test 2] XPManager veya CloudSaveManager (CurrentData) bulunamadi!");
        }
    }





    [ContextMenu("Test: Secili Esyayi Gelistir")]
    public void TestUpgradeItem()
    {
        if (UpgradeManager.Instance == null)
        {
            GameLogger.LogError($"[Test Upgrade] UpgradeManager bulunamadi!");
            return;
        }

        if (EconomyManager.Instance == null)
        {
            GameLogger.LogError($"[Test Upgrade] EconomyManager bulunamadi!");
            return;
        }

        int eskiSeviye = UpgradeManager.Instance.GetItemLevel(testItemId);
        int maliyet = UpgradeManager.Instance.GetUpgradeCost(testItemId);
        GameLogger.Log($"[Test Upgrade] '{testItemId}' yukseltme deneniyor. Mevcut Seviye: {eskiSeviye}, Maliyet: {maliyet}, Para: {EconomyManager.Instance.MevcutPara}");

        bool basarili = UpgradeManager.Instance.UpgradeItem(testItemId);
        if (basarili)
        {
            int yeniSeviye = UpgradeManager.Instance.GetItemLevel(testItemId);
            float yeniDeger = UpgradeManager.Instance.GetItemValue(testItemId);
            GameLogger.Log($"[Test Upgrade] '{testItemId}' yukseltildi! Seviye: {eskiSeviye} -> {yeniSeviye}, Deger: {yeniDeger:F1}, Kalan Para: {EconomyManager.Instance.MevcutPara}");
        }
        else
        {
            GameLogger.LogWarning($"[Test Upgrade] '{testItemId}' yukseltme basarisiz. (Max seviye, yetersiz para veya config eksik)");
        }
    }

    [ContextMenu("Test: Secili Esyanin Bilgilerini Goster")]
    public void TestShowItemInfo()
    {
        if (UpgradeManager.Instance == null)
        {
            GameLogger.LogError("[Test Info] UpgradeManager bulunamadi!");
            return;
        }

        int seviye = UpgradeManager.Instance.GetItemLevel(testItemId);
        float deger = UpgradeManager.Instance.GetItemValue(testItemId);
        int sonrakiMaliyet = UpgradeManager.Instance.GetUpgradeCost(testItemId);
        bool maxMi = UpgradeManager.Instance.IsMaxLevel(testItemId);

        GameLogger.Log($"[Test Info] === {testItemId} ===\n" +
                  $"  Seviye      : {seviye}\n" +
                  $"  Guncel Deger: {deger:F1}\n" +
                  $"  Sonraki Maliyet: {sonrakiMaliyet}\n" +
                  $"  Max Seviye Mi: {maxMi}");
    }





    [ContextMenu("Test: Secili Bolgeyi Ac (Config)")]
    public void TestUnlockSelectedRegion()
    {
        if (CloudSaveManager.Instance?.CurrentData == null)
        {
            GameLogger.LogError("[Test Bolge] CloudSaveManager veya CurrentData hazir degil!");
            return;
        }
        if (RegionManager.Instance == null)
        {
            GameLogger.LogError("[Test Bolge] RegionManager bulunamadi!");
            return;
        }


        string regionName = null;
        if (testRegionConfig != null)
        {
            RegionData regionData = testRegionConfig.GetRegionData(testRegionId);
            regionName = regionData.RegionName;
        }

        if (string.IsNullOrEmpty(regionName))
        {
            GameLogger.LogWarning($"[Test Bolge] RegionConfig'den ID={testRegionId} bolgesi okunamadi. RegionManager.UnlockRegion(int) ile devam ediliyor.");
        }
        else
        {
            GameLogger.Log($"[Test Bolge] Acilacak bolge: '{regionName}' (ID={testRegionId})");
        }


        bool basarili = RegionManager.Instance.UnlockRegion(testRegionId);
        if (basarili)
        {
            string ad = regionName ?? $"ID:{testRegionId}";
            GameLogger.Log($"[Test Bolge] '{ad}' kilidi acildi ve aninda Firestore'a kaydedildi!");
        }
        else
        {
            GameLogger.LogWarning($"[Test Bolge] Bolge ID={testRegionId} acilamadi. (Zaten acik, para yetersiz veya config eksik)");
        }
    }

    [ContextMenu("Test: Bolgelerin Durumunu Kontrol Et")]
    public void TestCheckRegionStatus()
    {
        if (RegionManager.Instance == null) { GameLogger.LogError("[Test Durum] RegionManager bulunamadi!"); return; }
        if (CloudSaveManager.Instance?.CurrentData == null) { GameLogger.LogError("[Test Durum] CurrentData null!"); return; }

        var acilanBolgeler = CloudSaveManager.Instance.CurrentData.AcilanBolgeler;

        if (acilanBolgeler == null || acilanBolgeler.Count == 0)
        {
            GameLogger.Log("[Test Durum] Hic acilmis bolge yok.");
            return;
        }

        GameLogger.Log($"[Test Durum] Acik Bolge Sayisi: {acilanBolgeler.Count}");
        foreach (string bolge in acilanBolgeler)
        {
            GameLogger.Log($"[Test Durum]   ✓ {bolge}");
        }


        if (testRegionConfig != null)
        {
            RegionData rd = testRegionConfig.GetRegionData(testRegionId);
            if (!string.IsNullOrEmpty(rd.RegionName))
            {
                bool acikMi = RegionManager.Instance.IsRegionUnlocked(rd.RegionName);
                GameLogger.Log($"[Test Durum] Secili bolge '{rd.RegionName}' acik mi: {acikMi}");
            }
        }
    }






    [ContextMenu("TEST 0: Sistem Durumunu Kontrol Et")]
    public void TestDiagnostic()
    {
        if (CloudSaveManager.Instance == null) { GameLogger.LogError("[DIAGNOZ] CloudSaveManager YOK!"); return; }
        if (CloudSaveManager.Instance.CurrentData == null) { GameLogger.LogError("[DIAGNOZ] CurrentData NULL - Firebase login oldu mu?"); return; }
        if (WorkshopManager.Instance == null) { GameLogger.LogError("[DIAGNOZ] WorkshopManager YOK!"); return; }
        if (ShopManager.Instance == null) { GameLogger.LogError("[DIAGNOZ] ShopManager YOK!"); return; }

        PlayerData d = CloudSaveManager.Instance.CurrentData;

        GameLogger.Log($"[DIAGNOZ] === GENEL DURUM RAPORU ===");
        GameLogger.Log($"  Atolye Ham Odun Bakiyesi : {WorkshopManager.Instance.MevcutKuyruk}");

        LogDict("WorkshopQueue (Uretim Sirasi)", d.WorkshopQueue);
        LogDict("ReadyForTruck (Kamyon Bekliyor)", d.ReadyForTruck);
        LogDict("ShopInventory (Dukkan Stok)", d.ShopInventory);

        if (testRecipe != null)
        {
            string itemId = testRecipe.ItemID;
            bool tarifVar = false;
            if (WorkshopManager.Instance.tumTarifler != null)
            {
                foreach (var r in WorkshopManager.Instance.tumTarifler)
                {
                    if (r != null && r.ItemID == itemId) { tarifVar = true; break; }
                }
            }
            if (!tarifVar)
                GameLogger.LogError($"[DIAGNOZ] !!! KRITIK: WorkshopManager Inspector'unda 'Tum Tarifler' dizisine '{itemId}' RecipeConfig'ini SURUKLE! Yoksa uretim sessizce iptal ediliyor.");
        }
        else
        {
            GameLogger.LogWarning("[DIAGNOZ] testRecipe atanmamis, test icin atayin.");
        }
    }

    private void LogDict(string title, SerializableDict dict)
    {
        if (dict == null || dict.keys.Count == 0)
        {
            GameLogger.Log($"  {title} : BOS");
            return;
        }
        string s = $"  {title} : ";
        var map = dict.ToDictionary();
        foreach (var kvp in map)
        {
            if (System.Convert.ToInt32(kvp.Value) > 0)
                s += $"[{kvp.Key}: {kvp.Value}] ";
        }
        GameLogger.Log(s);
    }


    [ContextMenu("Test 1: Odun Ekle")]
    public void TestAddWoodToWorkshop()
    {
        if (WorkshopManager.Instance == null) { GameLogger.LogError("[Test 1] WorkshopManager bulunamadi!"); return; }
        WorkshopManager.Instance.AddWood(testWoodAmount);
        GameLogger.Log($"[Test 1] +{testWoodAmount} odun eklendi. Atolye Odun Bakiyesi: {WorkshopManager.Instance.MevcutKuyruk}");
    }


    [ContextMenu("Test 2: Atolyede Uretim Baslat")]
    public void TestStartProduction()
    {
        if (WorkshopManager.Instance == null || testRecipe == null)
        {
            GameLogger.LogError("[Test 2] WorkshopManager veya testRecipe bulunamadi!");
            return;
        }
        if (CloudSaveManager.Instance?.CurrentData == null)
        {
            GameLogger.LogError("[Test 2] CloudSaveManager.CurrentData null! Oyun basladi mi?");
            return;
        }

        int odunBakiye = WorkshopManager.Instance.MevcutKuyruk;
        int gerekenOdun = testRecipe.RequiredWood;

        if (odunBakiye < gerekenOdun)
        {
            GameLogger.LogWarning($"[Test 2] Yetersiz odun! Gereken: {gerekenOdun}, Mevcut: {odunBakiye}. Once 'Test 1: Odun Ekle' calistir.");
            return;
        }

        bool basarili = WorkshopManager.Instance.TryStartProduction(testRecipe, 1);

        if (basarili)
        {

            PlayerData d = CloudSaveManager.Instance.CurrentData;
            int sirada = d.WorkshopQueue != null ? d.WorkshopQueue.Get(testRecipe.ItemID, 0) : 0;
            GameLogger.Log($"[Test 2] Siparis verildi! WorkshopQueue[{testRecipe.ItemID}] = {sirada}. Atölye zamanlayici caliştıgında uretilecek.");
        }
        else
        {
            GameLogger.LogWarning("[Test 2] Siparis verilemedi (TryStartProduction false dondu).");
        }
    }


    [ContextMenu("Test 3: Kamyon Teslimati (Dukkana Ekle)")]
    public void TestTruckDelivery()
    {
        if (CloudSaveManager.Instance?.CurrentData == null) { GameLogger.LogError("[Test 3] CurrentData null!"); return; }
        if (ShopManager.Instance == null) { GameLogger.LogError("[Test 3] ShopManager bulunamadi!"); return; }

        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data.ReadyForTruck == null) data.ReadyForTruck = new SerializableDict();

        var readyDict = data.ReadyForTruck.ToDictionary();
        bool anyLoaded = false;

        foreach (var kvp in readyDict)
        {
            string itemId = kvp.Key;
            int hazirMiktar = System.Convert.ToInt32(kvp.Value);
            
            if (hazirMiktar > 0)
            {

                data.ReadyForTruck.Set(itemId, 0);

                ShopManager.Instance.AddStock(itemId, hazirMiktar);
                GameLogger.Log($"[Test 3] Kamyon {hazirMiktar} adet '{itemId}' alip dukkana teslim etti!");
                anyLoaded = true;
            }
        }

        if (!anyLoaded)
        {
            GameLogger.LogWarning($"[Test 3] ReadyForTruck bombos! Kamyona yuklenecek hazir urun yok. Once 'Test 2: Uretim Baslat' cagir ve atolye uretmesini bekle.");
        }
    }


    [ContextMenu("Test 4: NPC Satisi (Stok Dus ve Para Ekle)")]
    public void TestNPCSell()
    {
        if (ShopManager.Instance == null || EconomyManager.Instance == null)
        {
            GameLogger.LogError("[Test 4] ShopManager veya EconomyManager bulunamadi!");
            return;
        }
        if (CloudSaveManager.Instance?.CurrentData == null) { GameLogger.LogError("[Test 4] CurrentData null!"); return; }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        if (data.ShopInventory == null) data.ShopInventory = new SerializableDict();

        var shopDict = data.ShopInventory.ToDictionary();
        bool anySold = false;

        foreach (var kvp in shopDict)
        {
            string itemId = kvp.Key;
            int shopStok = System.Convert.ToInt32(kvp.Value);

            if (shopStok > 0)
            {
                int price = 100;
                if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null)
                {
                    foreach (var r in WorkshopManager.Instance.tumTarifler)
                    {
                        if (r != null && r.ItemID == itemId) { price = r.BaseSalePrice; break; }
                    }
                }

                bool sold = ShopManager.Instance.TryRemoveStock(itemId, 1);
                if (sold)
                {
                    EconomyManager.Instance.AddMoney(price);
                    GameLogger.Log($"[Test 4] BASARILI: 1 adet '{itemId}' satildi! +{price} para. Kalan stok: {data.ShopInventory.Get(itemId, 0)}");
                    anySold = true;
                }
            }
        }

        if (!anySold)
        {
            GameLogger.LogWarning($"[Test 4] Satis basarisiz: Dukkan stokta hicbir urun yok! Once Test 3 ile kamyonu calistir.");
        }
    }


    [ContextMenu("Test 5: Yeni NPC Siparis Sistemi (Stok Yoksa BekleyenSiparise Atar)")]
    public void TestNPC_NewOrderSystem()
    {
        if (ShopManager.Instance == null)
        {
            GameLogger.LogError("[Test 5] ShopManager bulunamadi!");
            return;
        }

        string itemId = testRecipe != null ? testRecipe.ItemID : "Chair";
        int siparisMiktari = 3;
        int fiyat = 100;

        GameLogger.Log($"[Test 5] NPC Geldi! {siparisMiktari} adet {itemId} istiyor.");
        ShopManager.Instance.ProcessCustomerOrder(itemId, siparisMiktari, fiyat);

        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            PlayerData d = CloudSaveManager.Instance.CurrentData;
            int bekleyenSiparis = d.PendingCustomerOrders != null ? d.PendingCustomerOrders.Get(itemId, 0) : 0;
            GameLogger.Log($"[Test 5] Islem sonrasi '{itemId}' bekleyen siparis miktari: {bekleyenSiparis}");
        }
    }
}
