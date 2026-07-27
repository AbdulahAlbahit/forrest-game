using System;
using System.Collections.Generic;
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
    public bool HasPendingCargo => _pendingCargo.Count > 0;
    public RecipeConfig PendingRecipe => HasPendingCargo ? _pendingCargo[0].Recipe : null;
    public int PendingAmount
    {
        get
        {
            int total = 0;
            foreach (PendingCargoEntry entry in _pendingCargo) total += entry.Amount;
            return total;
        }
    }

    private int _currentBalance;
    private readonly List<PendingCargoEntry> _pendingCargo = new List<PendingCargoEntry>();

    private sealed class PendingCargoEntry
    {
        public RecipeConfig Recipe;
        public int Amount;
    }

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

        PendingCargoEntry existing = _pendingCargo.Find(
            entry => string.Equals(entry.Recipe.ItemID, recipe.ItemID, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Amount += amount;
        }
        else
        {
            _pendingCargo.Add(new PendingCargoEntry { Recipe = recipe, Amount = amount });
        }

        int unitPrice = ResolveUnitPrice(recipe);

        Debug.Log(
            $"[EfeMarketSales] Market yuku hazir: {amount}x {recipe.ItemID}. " +
            $"Birim satis fiyati: {unitPrice}. Kamyondaki toplam koli: {PendingAmount}.",
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

        int saleStartBalance = _currentBalance;
        int totalEarned = 0;
        int totalQuantity = 0;

        foreach (PendingCargoEntry cargo in _pendingCargo)
        {
            int quantity = cargo.Amount;
            string itemId = cargo.Recipe.ItemID;
            int unitPrice = ResolveUnitPrice(cargo.Recipe);
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
            totalEarned += earnedAmount;
            totalQuantity += quantity;

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
                $"[MARKET SATISI] {quantity}x {itemId} satildi. Birim fiyat: {unitPrice}. " +
                $"Kazanilan para: +{earnedAmount}. Guncel bakiye: {_currentBalance}.",
                this);
            InvokeSafely(OnSaleCompleted, result);
        }

        _pendingCargo.Clear();
        Debug.Log(
            $"[MARKET SATISI OZET] {totalQuantity} koli teslim edildi. " +
            $"Toplam kazanc: +{totalEarned}. Bakiye: {saleStartBalance} -> {_currentBalance}.",
            this);
        InvokeSafely(OnBalanceChanged, _currentBalance);
        return true;
    }

    private static int ResolveUnitPrice(RecipeConfig recipe)
    {
        if (recipe == null) return 0;
        if (recipe.BaseSalePrice > 0) return recipe.BaseSalePrice;

        string itemId = recipe.ItemID == null ? string.Empty : recipe.ItemID.Trim().ToLowerInvariant();
        switch (itemId)
        {
            case "masa":
                return 500;
            case "sandalye":
                return 250;
            case "ahşap dolap":
            case "ahsap dolap":
                return 450;
            case "koltuk":
                return 400;
            default:
                Debug.LogWarning($"[EfeMarketSales] '{recipe.ItemID}' icin satis fiyati tanimli degil.");
                return 0;
        }
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
