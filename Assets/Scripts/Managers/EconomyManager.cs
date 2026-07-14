using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Oyunun ekonomi sistemini yöneten Singleton manager.
/// Oyuncunun Coin ve XP değerlerini tutar, artırır ve azaltır.
/// CloudSaveManager ile haberleşerek verileri Firebase'e kaydeder ve yükler.
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

    // ── Bulut Bağlantı Durumu ──
    private bool _isCloudReady = false;

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

        // Başlangıç değerlerini ata (buluttan veri gelene kadar geçici)
        _currentCoins = startingCoins;
        _currentXP = startingXP;

        Debug.Log($"[EconomyManager] Başlatıldı → Coin: {_currentCoins}, XP: {_currentXP}");
    }

    private void Start()
    {
        // CloudSaveManager hazır olduğunda bulut verisini dinlemeye başla
        if (CloudSaveManager.Instance != null)
        {
            SubscribeToCloud();
        }
        else
        {
            StartCoroutine(WaitForCloudSaveManager());
        }
    }

    /// <summary>
    /// CloudSaveManager henüz başlatılmamışsa hazır olana kadar bekler.
    /// </summary>
    private IEnumerator WaitForCloudSaveManager()
    {
        Debug.Log("[EconomyManager] CloudSaveManager bekleniyor...");

        while (CloudSaveManager.Instance == null)
        {
            yield return null;
        }

        SubscribeToCloud();
        Debug.Log("[EconomyManager] CloudSaveManager bulundu, bulut aboneliği yapıldı.");
    }

    /// <summary>
    /// CloudSaveManager eventlerine abone olur.
    /// Veri yüklendiğinde coin ve XP değerlerini günceller.
    /// </summary>
    private void SubscribeToCloud()
    {
        CloudSaveManager.Instance.OnDataLoaded += HandleCloudDataLoaded;
        _isCloudReady = true;
    }

    private void OnDestroy()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleCloudDataLoaded;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ═══════════════════════════════════════════
    //  Firebase Bulut Entegrasyonu
    // ═══════════════════════════════════════════

    /// <summary>
    /// CloudSaveManager buluttan veri yüklediğinde çağrılır.
    /// Firebase'deki Para ve XP değerlerini EconomyManager'a aktarır.
    /// </summary>
    private void HandleCloudDataLoaded(PlayerData data)
    {
        _currentCoins = data.Para;
        _currentXP = data.XP;

        Debug.Log($"[EconomyManager] Bulut verisi yüklendi → Coin: {_currentCoins}, XP: {_currentXP}");
        OnCoinsChanged?.Invoke(_currentCoins);
        OnXPChanged?.Invoke(_currentXP);
    }

    /// <summary>
    /// Güncel coin ve XP değerlerini CloudSaveManager aracılığıyla Firestore'a kaydeder.
    /// </summary>
    private void SaveToCloud()
    {
        if (!_isCloudReady || CloudSaveManager.Instance == null)
        {
            Debug.LogWarning("[EconomyManager] Bulut bağlantısı henüz hazır değil, kayıt atlandı.");
            return;
        }

        // CloudSaveManager'ın mevcut verisini al, coin/XP güncelle, geri kaydet
        PlayerData data = CloudSaveManager.Instance.CurrentData;

        if (data == null)
        {
            data = new PlayerData();
        }

        data.Para = _currentCoins;
        data.XP = _currentXP;

        CloudSaveManager.Instance.SaveData(data);
        Debug.Log($"[EconomyManager] Değerler buluta kaydedildi → Coin: {_currentCoins}, XP: {_currentXP}");
    }

    // ═══════════════════════════════════════════
    //  Coin İşlemleri
    // ═══════════════════════════════════════════

    /// <summary>
    /// Oyuncuya belirtilen miktarda coin ekler ve buluta kaydeder.
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

        SaveToCloud();
    }

    /// <summary>
    /// Oyuncunun parasını harcamaya çalışır ve buluta kaydeder.
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

        SaveToCloud();
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
    /// Oyuncuya belirtilen miktarda XP ekler ve buluta kaydeder.
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

        SaveToCloud();
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
