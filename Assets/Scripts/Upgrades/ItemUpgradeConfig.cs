using UnityEngine;

/// <summary>
/// Upgrade kategorileri. Yeni kategori eklemek icin buraya ekleme yapilir.
/// </summary>
public enum UpgradeCategory
{
    Tool,        // Balta, Testere vb.
    Storage,     // Sepet, Depo vb.
    Transport,   // Kamyon, Forklift vb.
    Production,  // Atolye hizi, kuyruk kapasitesi vb.
    Economy      // Satis bonusu, para carpani vb.
}

/// <summary>
/// Upgrade'in oyundaki etkisinin turu.
/// </summary>
public enum EffectType
{
    Power,       // Hasar, guc
    Capacity,    // Kapasite, hacim
    Speed,       // Hiz
    QueueSize    // Kuyruk boyutu
}

/// <summary>
/// Jenerik Upgrade Config. Tek bir ScriptableObject sablonu ile
/// Balta, Sepet, Kamyon, Atolye veya gelecekte eklenecek herhangi bir
/// esyanin yukseltme verilerini tanimi yapilabilir.
///
/// Formul tabanli calisir: 100 seviye icin elle veri girmeye gerek yoktur.
/// </summary>
[CreateAssetMenu(fileName = "NewItemUpgrade", menuName = "ForrestGame/Item Upgrade Config")]
public class ItemUpgradeConfig : ScriptableObject
{
    [Header("--- Tanimlayici ---")]
    [Tooltip("Bu esyanin benzersiz kimlik anahtari. Ornek: Axe, Basket, Truck, Workshop")]
    public string ItemID = "NewItem";

    [Tooltip("Upgrade kategorisi (sadece organizasyon ve filtreleme icin).")]
    public UpgradeCategory Category = UpgradeCategory.Tool;

    [Tooltip("Bu upgrade'in oyundaki etkisinin turu.")]
    public EffectType Effect = EffectType.Power;

    [Header("--- Fiyat Formulu ---")]
    [Tooltip("Seviye 1'den 2'ye gecisteki temel fiyat.")]
    public float BasePrice = 50f;

    [Tooltip("Her seviyede fiyatin katlanma carpani. 1.5 = her seviye %50 daha pahali.")]
    public float PriceMultiplier = 1.5f;

    [Header("--- Deger Formulu ---")]
    [Tooltip("Seviye 1'deki baslangic stat degeri (guc, kapasite, hiz vb.).")]
    public float BaseValue = 10f;

    [Tooltip("Her seviyede stat degerinin ne kadar artacagi.")]
    public float ValueGrowth = 5f;

    [Header("--- Sinir ---")]
    [Tooltip("Maksimum ulasilabilecek seviye. 0 = sinirsiz.")]
    public int MaxLevel = 100;

    // ═══════════════════════════════════════════
    //  Hesaplama Metotlari
    // ═══════════════════════════════════════════

    /// <summary>
    /// Verilen seviyeden bir sonraki seviyeye gecis maliyetini hesaplar.
    /// Formul: BasePrice * Pow(PriceMultiplier, currentLevel - 1)
    /// </summary>
    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel <= 0) currentLevel = 1;

        float cost = BasePrice * Mathf.Pow(PriceMultiplier, currentLevel - 1);
        return Mathf.RoundToInt(cost);
    }

    /// <summary>
    /// Verilen seviyedeki stat degerini hesaplar (guc, kapasite, hiz vb.).
    /// Formul: BaseValue + (ValueGrowth * (currentLevel - 1))
    /// </summary>
    public float GetStatValue(int currentLevel)
    {
        if (currentLevel <= 0) currentLevel = 1;

        return BaseValue + (ValueGrowth * (currentLevel - 1));
    }

    /// <summary>
    /// Verilen seviyenin maksimum seviyeye ulasmis olup olmadigini kontrol eder.
    /// MaxLevel 0 ise sinir yoktur (sonsuz upgrade).
    /// </summary>
    public bool IsMaxLevel(int currentLevel)
    {
        if (MaxLevel <= 0) return false; // Sinirsiz
        return currentLevel >= MaxLevel;
    }
}
