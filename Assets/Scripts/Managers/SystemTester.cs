using UnityEngine;

public class SystemTester : MonoBehaviour
{
    [Header("Bolge Test Ayarlari")]
    public int testRegionId = 1;

    [Header("Upgrade Test Ayarlari")]
    [Tooltip("Test edilecek esyanin ItemID'si (Axe, Basket, Truck, Workshop, WorkshopQueue vb.)")]
    public string testItemId = "Axe";

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
}
