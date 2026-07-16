using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Baslangic Ayarlari")]
    [SerializeField] private int baslangicParasi = 100;

    private int mevcutPara;

    /// <summary>
    /// Para miktari degistiginde tetiklenir. Yeni para miktarini parametre olarak verir.
    /// </summary>
    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        mevcutPara = baslangicParasi;
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void OnDisable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
        }
    }

    /// <summary>
    /// Buluttan veri yuklendiginde para miktarini senkronize eder.
    /// </summary>
    private void HandleDataLoaded(PlayerData data)
    {
        SetMoney(data.Para);
        Debug.Log($"EconomyManager: Bulut verisi ile senkronize edildi. Para: {mevcutPara}");
    }

    /// <summary>
    /// Mevcut para miktarini dondurur.
    /// </summary>
    public int MevcutPara => mevcutPara;

    /// <summary>
    /// Belirtilen miktarda para ekler.
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("EconomyManager: Eklenecek miktar pozitif olmalidir.");
            return;
        }

        mevcutPara += amount;
        OnMoneyChanged?.Invoke(mevcutPara);
        Debug.Log($"EconomyManager: {amount} para eklendi. Mevcut: {mevcutPara}");
    }

    /// <summary>
    /// Belirtilen miktarda para harcar. Yeterli para varsa true, yoksa false dondurur.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("EconomyManager: Harcanacak miktar pozitif olmalidir.");
            return false;
        }

        if (mevcutPara < amount)
        {
            Debug.LogWarning($"EconomyManager: Yetersiz bakiye! Gereken: {amount}, Mevcut: {mevcutPara}");
            return false;
        }

        mevcutPara -= amount;
        OnMoneyChanged?.Invoke(mevcutPara);
        Debug.Log($"EconomyManager: {amount} para harcandi. Mevcut: {mevcutPara}");
        return true;
    }

    /// <summary>
    /// Para miktarini dogrudan ayarlar (save/load islemi icin).
    /// </summary>
    public void SetMoney(int amount)
    {
        mevcutPara = amount;
        OnMoneyChanged?.Invoke(mevcutPara);
    }
}
