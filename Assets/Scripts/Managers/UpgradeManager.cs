using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Upgrade Fiyat Ayarlari")]
    [SerializeField] private int temelFiyat = 50;

    [Tooltip("Her seviye icin fiyat bu carpan ile katlanir. Ornek: 1.5 ise her seviye %50 daha pahali olur.")]
    [SerializeField] private float seviyeCarpani = 1.5f;

    /// <summary>
    /// Belirtilen seviye icin upgrade maliyetini hesaplar.
    /// Formul: temelFiyat * (seviyeCarpani ^ mevcutSeviye)
    /// </summary>
    public int GetUpgradeCost(int currentLevel)
    {
        float cost = temelFiyat * Mathf.Pow(seviyeCarpani, currentLevel);
        return Mathf.RoundToInt(cost);
    }

    /// <summary>
    /// Upgrade islemi yapmaya calisir.
    /// EconomyManager'dan parayi ceker, basarili olursa seviyeyi artirip true dondurur.
    /// </summary>
    /// <param name="upgradeType">Upgrade tipi (log icin kullanilir: Balta, Sepet, Kamyon vb.)</param>
    /// <param name="currentLevel">Mevcut seviye referansi. Basarili olursa 1 artar.</param>
    /// <returns>Upgrade basarili ise true, yetersiz bakiye ise false.</returns>
    public bool TryUpgrade(string upgradeType, ref int currentLevel)
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("UpgradeManager: EconomyManager bulunamadi! Sahnede oldugundan emin ol.");
            return false;
        }

        int cost = GetUpgradeCost(currentLevel);

        if (EconomyManager.Instance.SpendCoins(cost))
        {
            currentLevel++;
            Debug.Log($"UpgradeManager: {upgradeType} upgrade basarili! Yeni seviye: {currentLevel}, Harcanan: {cost}");
            return true;
        }

        Debug.LogWarning($"UpgradeManager: {upgradeType} upgrade icin yetersiz bakiye. Maliyet: {cost}");
        return false;
    }
}
