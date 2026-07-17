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
        int upgradeCost = 100;

        if (EconomyManager.Instance == null || CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null || BasketManager.Instance == null)
        {
            Debug.LogError("[Test 3] Gerekli Manager'lardan biri veya CurrentData eksik!");
            return;
        }

        if (EconomyManager.Instance.SpendMoney(upgradeCost))
        {
            PlayerData data = CloudSaveManager.Instance.CurrentData;
            
            // Sepet seviyesini artir
            data.SepetSeviyesi++;
            
            // BasketManager'a yeni seviyeyi bildirerek kapasiteyi guncelle
            BasketManager.Instance.UpdateCapacity(data.SepetSeviyesi);
            
            // Guncel parayi data icine esitle ve kaydet
            data.Para = EconomyManager.Instance.MevcutPara;
            CloudSaveManager.Instance.SaveData(data);

            Debug.Log($"[Test 3] Sepet basariyla gelistirildi! Yeni Sepet Seviyesi: {data.SepetSeviyesi}, Yeni Kapasite: {BasketManager.Instance.MaksKapasite}, Kalan Para: {EconomyManager.Instance.MevcutPara}");
        }
        else
        {
            Debug.LogWarning($"[Test 3] Sepeti gelistirmek icin yeterli paraniz yok! (Gereken: {upgradeCost}, Mevcut: {EconomyManager.Instance.MevcutPara})");
        }
    }

    [ContextMenu("Test 4: Bolge 1'i Ac (Maliyet 150)")]
    public void TestUnlockRegion()
    {
        if (RegionManager.Instance != null && EconomyManager.Instance != null)
        {
            bool basarili = RegionManager.Instance.UnlockRegion("Bolge 1", 150);
            if (basarili)
            {
                Debug.Log($"[Test 4] 'Bolge 1' kilidi acildi! Kalan Para: {EconomyManager.Instance.MevcutPara}");
            }
            else
            {
                Debug.LogWarning($"[Test 4] Bolge acilamadi. (Belki onceden acildi veya para yetersiz - Mevcut: {EconomyManager.Instance.MevcutPara})");
            }
        }
        else
        {
            Debug.LogError("[Test 4] RegionManager veya EconomyManager bulunamadi!");
        }
    }
    [ContextMenu("Test: Bolge 2'yi Ac (Maliyet 200)")]
    public void TestUnlockRegion2()
    {
        if (RegionManager.Instance != null && EconomyManager.Instance != null)
        {
            bool basarili = RegionManager.Instance.UnlockRegion("Bolge_2", 200);
            if (basarili)
            {
                Debug.Log($"[Test] 'Bolge_2' kilidi acildi! Kalan Para: {EconomyManager.Instance.MevcutPara}");
            }
            else
            {
                Debug.LogWarning($"[Test] Bolge_2 acilamadi. (Onceden acildi veya para yetersiz - Mevcut: {EconomyManager.Instance.MevcutPara})");
            }
        }
        else
        {
            Debug.LogError("[Test] RegionManager veya EconomyManager bulunamadi!");
        }
    }

    [ContextMenu("Test: Bolge 3'u Ac (Maliyet 500)")]
    public void TestUnlockRegion3()
    {
        if (RegionManager.Instance != null && EconomyManager.Instance != null)
        {
            bool basarili = RegionManager.Instance.UnlockRegion("Bolge_3", 500);
            if (basarili)
            {
                Debug.Log($"[Test] 'Bolge_3' kilidi acildi! Kalan Para: {EconomyManager.Instance.MevcutPara}");
            }
            else
            {
                Debug.LogWarning($"[Test] Bolge_3 acilamadi. (Onceden acildi veya para yetersiz - Mevcut: {EconomyManager.Instance.MevcutPara})");
            }
        }
        else
        {
            Debug.LogError("[Test] RegionManager veya EconomyManager bulunamadi!");
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
