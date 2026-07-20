using System;
using UnityEngine;

public class BasketManager : MonoBehaviour
{
    public static BasketManager Instance { get; private set; }

    [Header("Sepet Ayarlari")]
    [SerializeField] private BasketUpgradeConfig config;

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
        maksKapasite = config != null ? config.GetLevelData(1).BasketCapacity : 5;
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
    /// Buluttan veri yuklendiginde sepet kapasitesini SepetSeviyesi'ne gore hesaplar.
    /// </summary>
    private void HandleDataLoaded(PlayerData data)
    {
        UpdateCapacity(data.SepetSeviyesi);
        Debug.Log($"BasketManager: Bulut verisi ile senkronize edildi. Kapasite: {maksKapasite} (Sepet Seviye: {data.SepetSeviyesi})");
    }

    /// <summary>
    /// Sepet seviyesine gore config uzerinden maksimum kapasiteyi gunceller.
    /// </summary>
    public void UpdateCapacity(int sepetSeviyesi)
    {
        if (config != null)
        {
            maksKapasite = config.GetLevelData(sepetSeviyesi).BasketCapacity;
        }

        // Doluluk kapasiteyi asamaz
        if (mevcutDoluluk > maksKapasite)
        {
            mevcutDoluluk = maksKapasite;
        }

        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);
    }

    public int GetBasketCapacity()
    {
        if (config == null)
        {
            Debug.LogError("BasketManager: BasketUpgradeConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return 5;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("BasketManager: CloudSaveManager veya CurrentData hazir degil, varsayilan kapasite donduruluyor.");
            return config.GetLevelData(1).BasketCapacity;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.SepetSeviyesi;
        return config.GetLevelData(seviye).BasketCapacity;
    }

    public bool UpgradeBasket()
    {
        if (config == null)
        {
            Debug.LogError("BasketManager: BasketUpgradeConfig atanmamis! Lutfen Inspector'dan config dosyasini atayin.");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogError("BasketManager: CloudSaveManager veya CurrentData hazir degil. Yukseltme iptal edildi!");
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogError("BasketManager: EconomyManager bulunamadi!");
            return false;
        }

        int seviye = CloudSaveManager.Instance.CurrentData.SepetSeviyesi;
        int cost = config.GetLevelData(seviye).UpgradeCost;

        if (EconomyManager.Instance.SpendMoney(cost))
        {
            PlayerData data = CloudSaveManager.Instance.CurrentData;
            data.SepetSeviyesi++;
            
            UpdateCapacity(data.SepetSeviyesi);
            CloudSaveManager.Instance.SaveData(data);

            Debug.Log($"BasketManager: Sepet yukseltildi! Yeni Seviye: {data.SepetSeviyesi}, Yeni Kapasite: {GetBasketCapacity()}");
            return true;
        }
        else
        {
            Debug.LogWarning($"BasketManager: Sepet yukseltmek icin yeterli para yok! Gereken: {cost}");
            return false;
        }
    }

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
