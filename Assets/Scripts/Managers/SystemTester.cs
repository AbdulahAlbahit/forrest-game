using UnityEngine;
using ForrestGame.Managers;

public class SystemTester : MonoBehaviour
{
    [Header("Bolge Test Ayarlari")]
    public int testRegionId = 1;

    [Header("Upgrade Test Ayarlari")]
    [Tooltip("Test edilecek esyanin ItemID'si (Axe, Basket, Truck, Workshop, WorkshopQueue vb.)")]
    public string testItemId = "Axe";

    [Header("Uretim ve Dukkan Test Ayarlari")]
    public RecipeConfig testRecipe;
    public int testWoodAmount = 50;
    public int testDeliveryAmount = 5;

    // ═══════════════════════════════════════════
    //  Ekonomi Testleri
    // ═══════════════════════════════════════════

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

    // ═══════════════════════════════════════════
    //  XP Testleri
    // ═══════════════════════════════════════════

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

    // ═══════════════════════════════════════════
    //  Jenerik Upgrade Testleri (Yeni Sistem)
    // ═══════════════════════════════════════════

    [ContextMenu("Test: Secili Esyayi Gelistir")]
    public void TestUpgradeItem()
    {
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError($"[Test Upgrade] UpgradeManager bulunamadi!");
            return;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError($"[Test Upgrade] EconomyManager bulunamadi!");
            return;
        }

        int eskiSeviye = UpgradeManager.Instance.GetItemLevel(testItemId);
        int maliyet = UpgradeManager.Instance.GetUpgradeCost(testItemId);
        Debug.Log($"[Test Upgrade] '{testItemId}' yukseltme deneniyor. Mevcut Seviye: {eskiSeviye}, Maliyet: {maliyet}, Para: {EconomyManager.Instance.MevcutPara}");

        bool basarili = UpgradeManager.Instance.UpgradeItem(testItemId);
        if (basarili)
        {
            int yeniSeviye = UpgradeManager.Instance.GetItemLevel(testItemId);
            float yeniDeger = UpgradeManager.Instance.GetItemValue(testItemId);
            Debug.Log($"[Test Upgrade] '{testItemId}' yukseltildi! Seviye: {eskiSeviye} -> {yeniSeviye}, Deger: {yeniDeger:F1}, Kalan Para: {EconomyManager.Instance.MevcutPara}");
        }
        else
        {
            Debug.LogWarning($"[Test Upgrade] '{testItemId}' yukseltme basarisiz. (Max seviye, yetersiz para veya config eksik)");
        }
    }

    [ContextMenu("Test: Secili Esyanin Bilgilerini Goster")]
    public void TestShowItemInfo()
    {
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("[Test Info] UpgradeManager bulunamadi!");
            return;
        }

        int seviye = UpgradeManager.Instance.GetItemLevel(testItemId);
        float deger = UpgradeManager.Instance.GetItemValue(testItemId);
        int sonrakiMaliyet = UpgradeManager.Instance.GetUpgradeCost(testItemId);
        bool maxMi = UpgradeManager.Instance.IsMaxLevel(testItemId);

        Debug.Log($"[Test Info] === {testItemId} ===\n" +
                  $"  Seviye      : {seviye}\n" +
                  $"  Guncel Deger: {deger:F1}\n" +
                  $"  Sonraki Maliyet: {sonrakiMaliyet}\n" +
                  $"  Max Seviye Mi: {maxMi}");
    }

    // ═══════════════════════════════════════════
    //  Bolge Testleri (Degistirilmedi)
    // ═══════════════════════════════════════════

    [ContextMenu("Test: Secili Bolgeyi Ac (Config)")]
    public void TestUnlockSelectedRegion()
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

        int maliyet = RegionManager.Instance.GetRegionCost(testRegionId);
        Debug.Log($"Bolge {testRegionId} acilis testi baslatildi. Config'den okunan maliyet: {maliyet}");

        bool basarili = RegionManager.Instance.UnlockRegion(testRegionId);
        if (basarili)
        {
            Debug.Log($"[Test Bolge] Bolge {testRegionId} kilidi acildi! Kalan Para: {EconomyManager.Instance.MevcutPara}");
        }
        else
        {
            Debug.LogWarning($"[Test Bolge] Bolge {testRegionId} acilamadi. (Onceden acildi veya para yetersiz - Mevcut: {EconomyManager.Instance.MevcutPara})");
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

    // ═══════════════════════════════════════════
    //  Atolye ve Dukkan Testleri (YENI SISTEM)
    // ═══════════════════════════════════════════

    // Oncelikle bu metodu cagirarak sistemlerin hazir olup olmadigini kontrol et!
    [ContextMenu("TEST 0: Sistem Durumunu Kontrol Et")]
    public void TestDiagnostic()
    {
        if (CloudSaveManager.Instance == null) { Debug.LogError("[DIAGNOZ] CloudSaveManager YOK!"); return; }
        if (CloudSaveManager.Instance.CurrentData == null) { Debug.LogError("[DIAGNOZ] CurrentData NULL - Firebase login oldu mu?"); return; }
        if (WorkshopManager.Instance == null) { Debug.LogError("[DIAGNOZ] WorkshopManager YOK!"); return; }
        if (ShopManager.Instance == null) { Debug.LogError("[DIAGNOZ] ShopManager YOK!"); return; }
        if (testRecipe == null) { Debug.LogError("[DIAGNOZ] testRecipe atanmamis! Inspector'dan sandalye asset'ini sur."); return; }

        PlayerData d = CloudSaveManager.Instance.CurrentData;
        string itemId = testRecipe.ItemID;

        int workshopQ = d.WorkshopQueue  != null ? d.WorkshopQueue.Get(itemId, 0)  : -1;
        int readyQ    = d.ReadyForTruck  != null ? d.ReadyForTruck.Get(itemId, 0)  : -1;
        int shopQ     = d.ShopInventory  != null ? d.ShopInventory.Get(itemId, 0)  : -1;

        // tumTarifler kontrolu - COKCA ONEMLI
        bool tarifVar = false;
        if (WorkshopManager.Instance != null && WorkshopManager.Instance.tumTarifler != null)
        {
            foreach (var r in WorkshopManager.Instance.tumTarifler)
            {
                if (r != null && r.ItemID == itemId) { tarifVar = true; break; }
            }
        }

        Debug.Log($"[DIAGNOZ] === {itemId} DURUM RAPORU ==="
            + $"\n  Atolye Odun Bakiyesi : {WorkshopManager.Instance.MevcutKuyruk}"
            + $"\n  WorkshopQueue (uretim sirasi) : {workshopQ}"
            + $"\n  ReadyForTruck (kamyon bekliyor) : {readyQ}"
            + $"\n  ShopInventory (dukkan stok) : {shopQ}"
            + $"\n  RecipeConfig RequiredWood : {testRecipe.RequiredWood}"
            + $"\n  RecipeConfig BaseProductionTime : {testRecipe.BaseProductionTime}s"
            + $"\n  WorkshopManager.tumTarifler'de bu tarif var mi: {tarifVar}");

        if (!tarifVar)
            Debug.LogError($"[DIAGNOZ] !!! KRITIK: WorkshopManager Inspector'unda 'Tum Tarifler' dizisine '{itemId}' RecipeConfig'ini SURUKLE! Yoksa uretim sessizce iptal ediliyor.");
    }

    // ADIM 1: Atolyenin odun deposuna odun ekle (bellekte tutulur)
    [ContextMenu("Test 1: Odun Ekle")]
    public void TestAddWoodToWorkshop()
    {
        if (WorkshopManager.Instance == null) { Debug.LogError("[Test 1] WorkshopManager bulunamadi!"); return; }
        WorkshopManager.Instance.AddWood(testWoodAmount);
        Debug.Log($"[Test 1] +{testWoodAmount} odun eklendi. Atolye Odun Bakiyesi: {WorkshopManager.Instance.MevcutKuyruk}");
    }

    // ADIM 2: Siparis ver — odun dusulur, WorkshopQueue'ya eklenir, Firestore'a yazilir
    [ContextMenu("Test 2: Atolyede Uretim Baslat")]
    public void TestStartProduction()
    {
        if (WorkshopManager.Instance == null || testRecipe == null)
        {
            Debug.LogError("[Test 2] WorkshopManager veya testRecipe bulunamadi!");
            return;
        }
        if (CloudSaveManager.Instance?.CurrentData == null)
        {
            Debug.LogError("[Test 2] CloudSaveManager.CurrentData null! Oyun basladi mi?");
            return;
        }

        int odunBakiye = WorkshopManager.Instance.MevcutKuyruk;
        int gerekenOdun = testRecipe.RequiredWood;

        if (odunBakiye < gerekenOdun)
        {
            Debug.LogWarning($"[Test 2] Yetersiz odun! Gereken: {gerekenOdun}, Mevcut: {odunBakiye}. Once 'Test 1: Odun Ekle' calistir.");
            return;
        }

        WorkshopManager.Instance.StartProduction(testRecipe, 1);

        // Sonucu hemen goster
        PlayerData d = CloudSaveManager.Instance.CurrentData;
        int sirada = d.WorkshopQueue != null ? d.WorkshopQueue.Get(testRecipe.ItemID, 0) : 0;
        Debug.Log($"[Test 2] Siparis verildi! WorkshopQueue[{testRecipe.ItemID}] = {sirada}. Atölye zamanlayici caliştıgında uretilecek.");
    }

    // ADIM 3: Uretim bitti, ReadyForTruck'taki mallari al, dukkana ekle
    [ContextMenu("Test 3: Kamyon Teslimati (Dukkana Ekle)")]
    public void TestTruckDelivery()
    {
        if (testRecipe == null) { Debug.LogError("[Test 3] testRecipe atanmamis!"); return; }
        if (CloudSaveManager.Instance?.CurrentData == null) { Debug.LogError("[Test 3] CurrentData null!"); return; }
        if (ShopManager.Instance == null) { Debug.LogError("[Test 3] ShopManager bulunamadi!"); return; }

        string itemId = testRecipe.ItemID;
        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data.ReadyForTruck == null) data.ReadyForTruck = new SerializableDict();

        int hazirMiktar = data.ReadyForTruck.Get(itemId, 0);
        int workshopSira = data.WorkshopQueue != null ? data.WorkshopQueue.Get(itemId, 0) : 0;

        Debug.Log($"[Test 3] Kamyon geldi! ReadyForTruck={hazirMiktar}, WorkshopQueue={workshopSira}");

        if (hazirMiktar > 0)
        {
            // ReadyForTruck'tan temizle
            data.ReadyForTruck.Set(itemId, 0);
            // Dukkana ekle (AddStock iceride SaveData cagiriyor)
            ShopManager.Instance.AddStock(itemId, hazirMiktar);
            Debug.Log($"[Test 3] Kamyon {hazirMiktar} adet {itemId} alip dukkana teslim etti!");
        }
        else
        {
            Debug.LogWarning($"[Test 3] ReadyForTruck'ta {itemId} yok! Once 'Test 2: Uretim Baslat' cagir ve atolye uretmesini bekle.");
        }
    }

    // ADIM 4: NPC geldi, dukkan stogunu dusurup parayı ekle
    [ContextMenu("Test 4: NPC Satisi (Stok Dus ve Para Ekle)")]
    public void TestNPCSell()
    {
        if (ShopManager.Instance == null || testRecipe == null || EconomyManager.Instance == null)
        {
            Debug.LogError("[Test 4] ShopManager, EconomyManager veya testRecipe bulunamadi!");
            return;
        }
        if (CloudSaveManager.Instance?.CurrentData == null) { Debug.LogError("[Test 4] CurrentData null!"); return; }

        string itemId = testRecipe.ItemID;
        int shopStok = CloudSaveManager.Instance.CurrentData.ShopInventory?.Get(itemId, 0) ?? 0;
        Debug.Log($"[Test 4] NPC geldi. Dukkan stok: {shopStok} {itemId}");

        bool sold = ShopManager.Instance.TryRemoveStock(itemId, 1);
        if (sold)
        {
            EconomyManager.Instance.AddMoney(testRecipe.BaseSalePrice);
            Debug.Log($"[Test 4] BASARILI: 1 {itemId} satildi! +{testRecipe.BaseSalePrice} para. Kalan stok: {CloudSaveManager.Instance.CurrentData.ShopInventory.Get(itemId, 0)}");
        }
        else
        {
            Debug.LogWarning($"[Test 4] Satis basarisiz: Dukkan stokta {itemId} yok (Mevcut: {shopStok}). Once Test 3 ile kamyonu calistir.");
        }
    }
}
