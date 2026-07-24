using System;
using UnityEngine;

/// <summary>
/// Efe'nin secimli atolye prototipinde uretilen paketleri, kamyon markete
/// ulastiginda satar. UI ve kalici veri sistemi bu sinifin eventlerine
/// abone olabilir; harici para sistemi bagli degilse oturumluk bakiye tutulur.
/// </summary>
public class EfeMarketSalesController : MonoBehaviour
{
    [Serializable]
    public struct SaleRequest
    {
        public string ItemId;
        public int Quantity;
        public int UnitPrice;
        public int TotalPrice;
    }

    [Serializable]
    public struct SaleResult
    {
        public string ItemId;
        public int Quantity;
        public int UnitPrice;
        public int EarnedAmount;
        public int PreviousBalance;
        public int NewBalance;
        public bool UsedExternalBalance;
    }

    public delegate bool SaleCreditHandler(SaleRequest request, out int authoritativeBalance);

    public static EfeMarketSalesController Instance { get; private set; }

    /// <summary>
    /// Veritabani/ekonomi katmani bu evente abone olup parayi kalici olarak
    /// ekleyebilir ve yeni bakiyeyi out parametresiyle dondurebilir.
    /// Hicbir dinleyici islemi devralmazsa yerel oturum bakiyesi kullanilir.
    /// </summary>
    public static event SaleCreditHandler OnCreditRequested;

    /// <summary>Arayuz, satis bildirimi ve ses sistemi bu eventi dinleyebilir.</summary>
    public static event Action<SaleResult> OnSaleCompleted;

    /// <summary>Arayuz bakiye metnini bu eventle yenileyebilir.</summary>
    public static event Action<int> OnBalanceChanged;

    /// <summary>Urun kamyona hazir oldugunda item ve miktari bildirir.</summary>
    public static event Action<RecipeConfig, int> OnCargoReadyForMarket;

    [Header("Gecici Bakiye")]
    [SerializeField, Min(0)] private int initialSessionBalance;
    [SerializeField] private bool logEmptyMarketArrival;

    public int CurrentBalance => _currentBalance;
    public bool HasPendingCargo => _pendingRecipe != null && _pendingAmount > 0;
    public RecipeConfig PendingRecipe => _pendingRecipe;
    public int PendingAmount => _pendingAmount;

    private int _currentBalance;
    private RecipeConfig _pendingRecipe;
    private int _pendingAmount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EfeMarketSales] Ayni sahnede ikinci satis yoneticisi kapatildi.", this);
            enabled = false;
            return;
        }

        Instance = this;
        _currentBalance = Mathf.Max(0, initialSessionBalance);
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            return;
        }

        EfeWorkshopSelectionController.OnSelectedProductionCompleted += RegisterProducedCargo;
        TruckRouteController.OnMarketSale += HandleMarketSale;
    }

    private void OnDisable()
    {
        EfeWorkshopSelectionController.OnSelectedProductionCompleted -= RegisterProducedCargo;
        TruckRouteController.OnMarketSale -= HandleMarketSale;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Veritabani yuklendikten sonra yerel gorunumu gercek bakiyeyle
    /// esitlemek icin kullanilabilir.
    /// </summary>
    public void SynchronizeBalance(int authoritativeBalance)
    {
        _currentBalance = Mathf.Max(0, authoritativeBalance);
        InvokeSafely(OnBalanceChanged, _currentBalance);
    }

    /// <summary>
    /// Atolye veya ileride farkli bir uretim sistemi markete gidecek urunu
    /// bu acik API uzerinden kaydedebilir.
    /// </summary>
    public void RegisterProducedCargo(RecipeConfig recipe, int amount)
    {
        if (recipe == null || string.IsNullOrWhiteSpace(recipe.ItemID) || amount <= 0)
        {
            Debug.LogWarning("[EfeMarketSales] Gecersiz urun market yukune eklenmedi.", this);
            return;
        }

        _pendingRecipe = recipe;
        _pendingAmount = amount;

        Debug.Log(
            $"[EfeMarketSales] Market yuku hazir: {amount}x {recipe.ItemID}. " +
            $"Birim satis fiyati: {Mathf.Max(0, recipe.BaseSalePrice)}.",
            this);
        InvokeSafely(OnCargoReadyForMarket, recipe, amount);
    }

    /// <summary>
    /// TruckRouteController.OnMarketSale tarafindan otomatik cagrilir.
    /// Test veya gelecekteki farkli rota sistemi de dogrudan cagirabilir.
    /// </summary>
    public bool CompleteMarketSale()
    {
        if (!HasPendingCargo)
        {
            if (logEmptyMarketArrival)
            {
                Debug.Log("[EfeMarketSales] Kamyon markete geldi fakat satilacak secimli urun yok.", this);
            }
            return false;
        }

        int quantity = _pendingAmount;
        string itemId = _pendingRecipe.ItemID;
        int unitPrice = Mathf.Max(0, _pendingRecipe.BaseSalePrice);
        int earnedAmount = unitPrice * quantity;
        int previousBalance = _currentBalance;

        SaleRequest request = new SaleRequest
        {
            ItemId = itemId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = earnedAmount
        };

        bool usedExternalBalance = TryCreditExternalBalance(request, out int newBalance);
        if (!usedExternalBalance)
        {
            newBalance = previousBalance + earnedAmount;
        }

        _currentBalance = Mathf.Max(0, newBalance);
        _pendingRecipe = null;
        _pendingAmount = 0;

        SaleResult result = new SaleResult
        {
            ItemId = itemId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            EarnedAmount = earnedAmount,
            PreviousBalance = previousBalance,
            NewBalance = _currentBalance,
            UsedExternalBalance = usedExternalBalance
        };

        Debug.Log(
            $"[MARKET SATISI] {quantity}x {itemId} satildi. " +
            $"Kazanilan para: +{earnedAmount}. Guncel bakiye: {_currentBalance}.",
            this);

        InvokeSafely(OnBalanceChanged, _currentBalance);
        InvokeSafely(OnSaleCompleted, result);
        return true;
    }

    private void HandleMarketSale()
    {
        CompleteMarketSale();
    }

    private bool TryCreditExternalBalance(SaleRequest request, out int authoritativeBalance)
    {
        authoritativeBalance = 0;
        SaleCreditHandler handlers = OnCreditRequested;
        if (handlers == null)
            return false;

        foreach (SaleCreditHandler handler in handlers.GetInvocationList())
        {
            try
            {
                if (handler(request, out authoritativeBalance))
                    return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        return false;
    }

    private static void InvokeSafely<T>(Action<T> handlers, T value)
    {
        if (handlers == null)
            return;

        foreach (Action<T> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static void InvokeSafely<T1, T2>(Action<T1, T2> handlers, T1 first, T2 second)
    {
        if (handlers == null)
            return;

        foreach (Action<T1, T2> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(first, second);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
