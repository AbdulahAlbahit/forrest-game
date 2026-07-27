using System;
using UnityEngine;
using ForrestGame.Utils;

public class BasketManager : MonoBehaviour
{
    public static BasketManager Instance { get; private set; }

    private int mevcutDoluluk;
    private int maksKapasite;

    /
    /
    /
    /
    public event Action<int, int> OnBasketChanged;

    /
    /
    /
    /
    public event Action OnBasketFull;

    /
    /
    /
    public event Action<int> OnBasketEmptied;

    /
    /
    /
    public int MevcutDoluluk => mevcutDoluluk;

    /
    /
    /
    public int MaksKapasite => maksKapasite;

    /
    /
    /
    public bool IsFull => mevcutDoluluk >= maksKapasite;

    /
    /
    /
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
        maksKapasite = 5;
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
        }


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

    /
    /
    /
    private void HandleDataLoaded(PlayerData data)
    {
        RefreshCapacity();
        GameLogger.Log($"BasketManager: Bulut verisi ile senkronize edildi. Kapasite: {maksKapasite}");
    }

    /
    /
    /
    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == "Basket")
        {
            RefreshCapacity();
            GameLogger.Log($"BasketManager: Sepet yukseltildi! Yeni Kapasite: {maksKapasite} (Seviye: {newLevel})");
        }
    }

    /
    /
    /
    private void RefreshCapacity()
    {
        if (UpgradeManager.Instance != null)
        {
            maksKapasite = Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue("Basket"));
        }


        if (mevcutDoluluk > maksKapasite)
        {
            mevcutDoluluk = maksKapasite;
        }

        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);
    }

    /
    /
    /
    public int GetBasketCapacity()
    {
        if (UpgradeManager.Instance != null)
        {
            return Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue("Basket"));
        }

        return maksKapasite;
    }





    /
    /
    /
    /
    /
    /
    public int AddWood(int amount)
    {
        if (amount <= 0)
        {
            GameLogger.LogWarning("BasketManager: Eklenecek miktar pozitif olmalidir.");
            return 0;
        }

        if (IsFull)
        {
            GameLogger.LogWarning("BasketManager: Sepet zaten dolu! Daha fazla odun eklenemiyor.");
            OnBasketFull?.Invoke();
            return 0;
        }


        int bosAlan = maksKapasite - mevcutDoluluk;
        int eklenenMiktar = Mathf.Min(amount, bosAlan);

        mevcutDoluluk += eklenenMiktar;

        GameLogger.Log($"BasketManager: +{eklenenMiktar} odun eklendi. Doluluk: {mevcutDoluluk}/{maksKapasite}");
        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);

        if (IsFull)
        {
            GameLogger.Log("BasketManager: Sepet doldu!");
            OnBasketFull?.Invoke();
        }

        return eklenenMiktar;
    }

    /
    /
    /
    /
    public int EmptyBasket()
    {
        if (IsEmpty)
        {
            GameLogger.LogWarning("BasketManager: Sepet zaten bos!");
            return 0;
        }

        int bosaltilanMiktar = mevcutDoluluk;
        mevcutDoluluk = 0;

        GameLogger.Log($"BasketManager: Sepet bosaltildi. {bosaltilanMiktar} odun cikarildi.");
        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);
        OnBasketEmptied?.Invoke(bosaltilanMiktar);

        return bosaltilanMiktar;
    }

    /
    /
    /
    /
    /
    public int RemoveWood(int amount)
    {
        if (amount <= 0)
        {
            GameLogger.LogWarning("BasketManager: Cikarilacak miktar pozitif olmalidir.");
            return 0;
        }

        if (IsEmpty)
        {
            GameLogger.LogWarning("BasketManager: Sepet bos, odun cikarilamiyor.");
            return 0;
        }

        int cikarilanMiktar = Mathf.Min(amount, mevcutDoluluk);
        mevcutDoluluk -= cikarilanMiktar;

        GameLogger.Log($"BasketManager: -{cikarilanMiktar} odun cikarildi. Doluluk: {mevcutDoluluk}/{maksKapasite}");
        OnBasketChanged?.Invoke(mevcutDoluluk, maksKapasite);

        return cikarilanMiktar;
    }

    /
    /
    /
    /
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
