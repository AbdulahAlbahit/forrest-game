using System;
using UnityEngine;

public class BasketManager : MonoBehaviour
{
    public static BasketManager Instance { get; private set; }

    private int mevcutDoluluk;
    private int maksKapasite;

    /// <summary>
    /// Sepet dolulugu degistiginde tetiklenir. (mevcutDoluluk, maksKapasite) parametreleri verir.
    /// HUD'daki sepet gostergesi bu event'i dinleyerek guncellenir.
    /// </summary>
    public event Action<int, int> OnBasketChanged;

    /// <summary>
    /// Sepet tamamen dolduğunda tetiklenir.
    /// Bildirim sistemi "Sepet dolu!" mesaji gostermek icin kullanabilir.
    /// </summary>
    public event Action OnBasketFull;

    /// <summary>
    /// Sepet bosaltildiginda tetiklenir. Bosaltilan odun miktarini parametre olarak verir.
    /// </summary>
    public event Action<int> OnBasketEmptied;

    /// <summary>
    /// Mevcut sepet dolulugunu dondurur.
    /// </summary>
    public int MevcutDoluluk => mevcutDoluluk;

    /// <summary>
    /// Mevcut maksimum kapasiteyi dondurur.
    /// </summary>
    public int MaksKapasite => maksKapasite;

    /// <summary>
    /// Sepetin dolu olup olmadigini dondurur.
    /// </summary>
    public bool IsFull => mevcutDoluluk >= maksKapasite;

    /// <summary>
    /// Sepetin bos olup olmadigini dondurur.
    /// </summary>
    public bool IsEmpty => mevcutDoluluk <= 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        mevcutDoluluk = 0;
        maksKapasite = 5; // Varsayilan kapasite, UpgradeManager hazir olunca guncellenecek
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
        }

        // UpgradeManager uzerinden sepet yukseltme event'ini dinle
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;
        }
    }

    private void OnDisable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;
        }
    }

    /// <summary>
    /// Buluttan veri yuklendiginde sepet kapasitesini UpgradeManager uzerinden gunceller.
    /// </summary>
    private void HandleDataLoaded(PlayerData data)
    {
        RefreshCapacity();
        Debug.Log($"BasketManager: Bulut verisi ile senkronize edildi. Kapasite: {maksKapasite}");
    }

    /// <summary>
    /// Herhangi bir esya yukseltildiginde, eger "Basket" ise kapasiteyi gunceller.
    /// </summary>
    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == "Basket")
        {
            RefreshCapacity();
            Debug.Log($"BasketManager: Sepet yukseltildi! Yeni Kapasite: {maksKapasite} (Seviye: {newLevel})");
        }
    }

    /// <summary>
    /// UpgradeManager uzerinden sepet kapasitesini yeniden hesaplar.
    /// </summary>
    private void RefreshCapacity()
    {
        if (UpgradeManager.Instance != null)
        {
            maksKapasite = Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue("Basket"));
        }

        // Doluluk kapasiteyi asamaz
        if (mevcutDoluluk > maksKapasite)
        {
            mevcutDoluluk = maksKapasite;
        }

        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);
    }

    /// <summary>
    /// Sepet kapasitesini dondurur. UpgradeManager uzerinden hesaplanir.
    /// </summary>
    public int GetBasketCapacity()
    {
        if (UpgradeManager.Instance != null)
        {
            return Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue("Basket"));
        }

        return maksKapasite;
    }

    // ═══════════════════════════════════════════
    //  Oyun Mekanikleri (Degistirilmedi)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Sepete belirtilen miktarda odun ekler.
    /// Kapasite asildiysa sadece siğan kadar ekler.
    /// Eklenen gercek miktari dondurur.
    /// Ornek kullanim: BasketManager.Instance.AddWood(1);
    /// </summary>
    public int AddWood(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("BasketManager: Eklenecek miktar pozitif olmalidir.");
            return 0;
        }

        if (IsFull)
        {
            Debug.LogWarning("BasketManager: Sepet zaten dolu! Daha fazla odun eklenemiyor.");
            OnBasketFull?.Invoke();
            return 0;
        }

        // Kapasiteyi asmayacak sekilde ekle
        int bosAlan = maksKapasite - mevcutDoluluk;
        int eklenenMiktar = Mathf.Min(amount, bosAlan);

        mevcutDoluluk += eklenenMiktar;

        Debug.Log($"BasketManager: +{eklenenMiktar} odun eklendi. Doluluk: {mevcutDoluluk}/{maksKapasite}");
        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);

        if (IsFull)
        {
            Debug.Log("BasketManager: Sepet doldu!");
            OnBasketFull?.Invoke();
        }

        return eklenenMiktar;
    }

    /// <summary>
    /// Sepetteki tum odunlari bosaltir ve bosaltilan miktari dondurur.
    /// Genellikle kamyona veya atolyeye teslim ederken kullanilir.
    /// </summary>
    public int EmptyBasket()
    {
        if (IsEmpty)
        {
            Debug.LogWarning("BasketManager: Sepet zaten bos!");
            return 0;
        }

        int bosaltilanMiktar = mevcutDoluluk;
        mevcutDoluluk = 0;

        Debug.Log($"BasketManager: Sepet bosaltildi. {bosaltilanMiktar} odun cikarildi.");
        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);
        OnBasketEmptied?.Invoke(bosaltilanMiktar);

        return bosaltilanMiktar;
    }

    /// <summary>
    /// Sepetten belirtilen miktarda odun cikarir.
    /// Yeterli odun yoksa mevcut kadar cikarir.
    /// Cikarilan gercek miktari dondurur.
    /// </summary>
    public int RemoveWood(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("BasketManager: Cikarilacak miktar pozitif olmalidir.");
            return 0;
        }

        if (IsEmpty)
        {
            Debug.LogWarning("BasketManager: Sepet bos, odun cikarilamiyor.");
            return 0;
        }

        int cikarilanMiktar = Mathf.Min(amount, mevcutDoluluk);
        mevcutDoluluk -= cikarilanMiktar;

        Debug.Log($"BasketManager: -{cikarilanMiktar} odun cikarildi. Doluluk: {mevcutDoluluk}/{maksKapasite}");
        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);

        return cikarilanMiktar;
    }

    /// <summary>
    /// Sepet doluluk yuzdesini dondurur (0.0 ~ 1.0).
    /// HUD'daki doluluk gostergesi icin kullanilabilir.
    /// </summary>
    public float GetFillPercentage()
    {
        if (maksKapasite <= 0) return 0f;
        return Mathf.Clamp01((float)mevcutDoluluk / maksKapasite);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
