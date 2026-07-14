using System;
using UnityEngine;

/// <summary>
/// Oyunun ekonomi sistemini yöneten Singleton manager.
/// Oyuncunun Coin ve XP değerlerini tutar, artırır ve azaltır.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    // ── Inspector'dan Ayarlanabilir Başlangıç Değerleri ──
    [Header("Başlangıç Değerleri")]
    [SerializeField] private int startingCoins = 100;
    [SerializeField] private int startingXP = 0;

    // ── Güncel Değerler ──
    private int _currentCoins;
    private int _currentXP;

    // ── Public Erişim (Sadece Okunabilir) ──
    public int CurrentCoins => _currentCoins;
    public int CurrentXP => _currentXP;

    // ── Eventler ──
    /// <summary>Coin miktarı değiştiğinde tetiklenir. Parametre: yeni coin miktarı.</summary>
    public event Action<int> OnCoinsChanged;

    /// <summary>XP miktarı değiştiğinde tetiklenir. Parametre: yeni XP miktarı.</summary>
    public event Action<int> OnXPChanged;

    /// <summary>Yetersiz bakiye nedeniyle harcama reddedildiğinde tetiklenir. Parametre: gereken miktar.</summary>
    public event Action<int> OnInsufficientFunds;

    // ═══════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        // Singleton kontrolü
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Başlangıç değerlerini ata
        _currentCoins = startingCoins;
        _currentXP = startingXP;

        Debug.Log($"[EconomyManager] Başlatıldı → Coin: {_currentCoins}, XP: {_currentXP}");
    }

    // ═══════════════════════════════════════════
    //  Coin İşlemleri
    // ═══════════════════════════════════════════

    /// <summary>
    /// Oyuncuya belirtilen miktarda coin ekler.
    /// </summary>
    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[EconomyManager] AddCoins: Miktar 0 veya negatif olamaz.");
            return;
        }

        _currentCoins += amount;
        Debug.Log($"[EconomyManager] +{amount} coin eklendi → Toplam: {_currentCoins}");
        OnCoinsChanged?.Invoke(_currentCoins);
    }

    /// <summary>
    /// Oyuncunun parasını harcamaya çalışır.
    /// Yeterli bakiye varsa true döner ve parayı düşer, yoksa false döner.
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[EconomyManager] SpendCoins: Miktar 0 veya negatif olamaz.");
            return false;
        }

        if (_currentCoins < amount)
        {
            Debug.LogWarning($"[EconomyManager] Yetersiz bakiye! Gereken: {amount}, Mevcut: {_currentCoins}");
            OnInsufficientFunds?.Invoke(amount);
            return false;
        }

        _currentCoins -= amount;
        Debug.Log($"[EconomyManager] -{amount} coin harcandı → Kalan: {_currentCoins}");
        OnCoinsChanged?.Invoke(_currentCoins);
        return true;
    }

    /// <summary>
    /// Paranın yetip yetmediğini kontrol eder (harcama yapmadan).
    /// </summary>
    public bool HasEnoughCoins(int amount)
    {
        return _currentCoins >= amount;
    }

    // ═══════════════════════════════════════════
    //  XP İşlemleri
    // ═══════════════════════════════════════════

    /// <summary>
    /// Oyuncuya belirtilen miktarda XP ekler.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[EconomyManager] AddXP: Miktar 0 veya negatif olamaz.");
            return;
        }

        _currentXP += amount;
        Debug.Log($"[EconomyManager] +{amount} XP eklendi → Toplam: {_currentXP}");
        OnXPChanged?.Invoke(_currentXP);
    }

    // ═══════════════════════════════════════════
    //  Değerleri Dışarıdan Yükleme (Save/Load için)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Kayıtlı verileri yüklerken coin ve XP değerlerini doğrudan set eder.
    /// </summary>
    public void SetValues(int coins, int xp)
    {
        _currentCoins = Mathf.Max(0, coins);
        _currentXP = Mathf.Max(0, xp);

        Debug.Log($"[EconomyManager] Değerler yüklendi → Coin: {_currentCoins}, XP: {_currentXP}");
        OnCoinsChanged?.Invoke(_currentCoins);
        OnXPChanged?.Invoke(_currentXP);
    }
}
