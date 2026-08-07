using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterAttackUpgradeLevel
{
    [Min(1)] public int Level = 1;

    [Tooltip("Iki balta sallama arasindaki bekleme suresi. Daha hizli saldiri icin dusurulur.")]
    [Min(0.1f)] public float AttackCooldown = 0.95f;

    [Tooltip("Butona basildiktan sonra hasarin uygulanacagi an. Animasyon hizlandikca dusurulur.")]
    [Range(0f, 1f)] public float HitDelay = 0.42f;

    [Tooltip("Saldiri animasyonunun oynatma hizi carpani. Daha hizli animasyon icin artirilir.")]
    [Min(0.1f)] public float AnimationSpeed = 1f;
}

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

public enum UpgradePriceFormula
{
    Exponential,
    Linear
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

    [Tooltip("Fiyatin katlanarak mi yoksa her seviyede sabit miktarda artarak mi hesaplanacagi.")]
    public UpgradePriceFormula PriceFormula = UpgradePriceFormula.Exponential;

    [Tooltip("Her seviyede fiyatin katlanma carpani. 1.5 = her seviye %50 daha pahali.")]
    public float PriceMultiplier = 1.5f;

    [Tooltip("Linear formulde her seviyede fiyata eklenecek sabit miktar.")]
    public float LinearPriceIncrease = 50f;

    [Header("--- Deger Formulu ---")]
    [Tooltip("Seviye 1'deki baslangic stat degeri (guc, kapasite, hiz vb.).")]
    public float BaseValue = 10f;

    [Tooltip("Her seviyede stat degerinin ne kadar artacagi.")]
    public float ValueGrowth = 5f;

    [Header("--- Sinir ---")]
    [Tooltip("Maksimum ulasilabilecek seviye. 0 = sinirsiz.")]
    public int MaxLevel = 100;

    [Header("--- Karakter Saldiri Ayarlari ---")]
    [Tooltip("Bu config karakterin saldiri zamanlamalarini yonetiyorsa acilir.")]
    public bool UseCharacterAttackTimings;

    [Tooltip("Her karakter seviyesi icin saldiri bekleme, vurus ani ve animasyon hizi degerleri.")]
    public List<CharacterAttackUpgradeLevel> CharacterAttackLevels = new List<CharacterAttackUpgradeLevel>();

    // ═══════════════════════════════════════════
    //  Hesaplama Metotlari
    // ═══════════════════════════════════════════

    /// <summary>
    /// Verilen seviyeden bir sonraki seviyeye gecis maliyetini hesaplar.
    /// Exponential: BasePrice * Pow(PriceMultiplier, currentLevel - 1)
    /// Linear: BasePrice + LinearPriceIncrease * (currentLevel - 1)
    /// </summary>
    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel <= 0) currentLevel = 1;

        float cost = PriceFormula == UpgradePriceFormula.Linear
            ? BasePrice + LinearPriceIncrease * (currentLevel - 1)
            : BasePrice * Mathf.Pow(PriceMultiplier, currentLevel - 1);
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

    public bool TryGetCharacterAttackTiming(
        int level,
        out float attackCooldown,
        out float hitDelay,
        out float animationSpeed)
    {
        attackCooldown = 0f;
        hitDelay = 0f;
        animationSpeed = 1f;

        if (!UseCharacterAttackTimings || CharacterAttackLevels == null)
        {
            return false;
        }

        CharacterAttackUpgradeLevel selected = null;
        foreach (CharacterAttackUpgradeLevel setting in CharacterAttackLevels)
        {
            if (setting != null && setting.Level <= level &&
                (selected == null || setting.Level > selected.Level))
            {
                selected = setting;
            }
        }

        if (selected == null)
        {
            return false;
        }

        attackCooldown = Mathf.Max(0.1f, selected.AttackCooldown);
        hitDelay = Mathf.Clamp(selected.HitDelay, 0f, attackCooldown);
        animationSpeed = Mathf.Max(0.1f, selected.AnimationSpeed);
        return true;
    }

    private void OnValidate()
    {
        if (CharacterAttackLevels == null)
        {
            return;
        }

        foreach (CharacterAttackUpgradeLevel setting in CharacterAttackLevels)
        {
            if (setting == null) continue;
            setting.Level = Mathf.Max(1, setting.Level);
            setting.AttackCooldown = Mathf.Max(0.1f, setting.AttackCooldown);
            setting.HitDelay = Mathf.Clamp(setting.HitDelay, 0f, setting.AttackCooldown);
            setting.AnimationSpeed = Mathf.Max(0.1f, setting.AnimationSpeed);
        }
    }
}
