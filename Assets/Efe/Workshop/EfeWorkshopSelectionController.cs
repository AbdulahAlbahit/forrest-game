using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kamyon atolyeye geldiginde Mert'in otomatik uretimini guvenli bir secim
/// kapisindan gecirir. UI hazir olana kadar 1 ve 2 tuslari test girisidir.
/// Firebase veya WorkshopManager'a dogrudan bagli degildir.
/// </summary>
public class EfeWorkshopSelectionController : MonoBehaviour
{
    public static EfeWorkshopSelectionController Instance { get; private set; }

    [Header("Atolye Baglantisi")]
    [SerializeField] private WorkshopProductionManager workshopProduction;

    [Header("Gecici Klavye Secenekleri")]
    [SerializeField] private RecipeConfig option1Recipe;
    [SerializeField] private RecipeConfig option2Recipe;
    [SerializeField] private bool enableKeyboardTest = true;

    public static event Action<TruckRouteController, int> OnSelectionRequested;
    public static event Action<RecipeConfig, int, int> OnRecipeSelected;
    public static event Action<string> OnSelectionRejected;
    public static event Action<RecipeConfig, int> OnSelectedProductionCompleted;

    public bool IsAwaitingSelection => _isAwaitingSelection;
    public bool HasPendingTruck => _pendingTruck != null;
    public int DeliveredWood => _deliveredWood;
    public int RemainingWood => _remainingWood;
    public RecipeConfig SelectedRecipe => _selectedRecipe;
    public int SelectedAmount => _selectedAmount;
    public RecipeConfig Option1Recipe => option1Recipe;
    public RecipeConfig Option2Recipe => option2Recipe;

    private TruckRouteController _pendingTruck;
    private bool _isAwaitingSelection;
    private bool _productionStarted;
    private int _deliveredWood;
    private int _remainingWood;
    private RecipeConfig _selectedRecipe;
    private int _selectedAmount;
    private Coroutine _completionRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EfeWorkshopSelection] Ayni sahnede ikinci secim yoneticisi kapatildi.", this);
            enabled = false;
            return;
        }

        Instance = this;
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

        TruckRouteController.OnWorkshopSelectionRequested += HandleWorkshopSelectionRequest;
    }

    private void OnDisable()
    {
        TruckRouteController.OnWorkshopSelectionRequested -= HandleWorkshopSelectionRequest;

        if (Application.isPlaying && _pendingTruck != null && !_pendingTruck.isWorkshopProcessDone)
        {
            ContinueWithAutomaticFallback();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!enableKeyboardTest || !_isAwaitingSelection) return;

        if (WasOptionOnePressed())
        {
            SelectOption(1);
        }
        else if (WasOptionTwoPressed())
        {
            SelectOption(2);
        }
    }

    /// <summary>
    /// Gelecekteki UI bu metodu 1 veya 2 ile cagirabilir.
    /// </summary>
    public bool SelectOption(int optionNumber)
    {
        RecipeConfig recipe = optionNumber == 1
            ? option1Recipe
            : optionNumber == 2
                ? option2Recipe
                : null;

        if (recipe == null)
        {
            return Reject($"Gecersiz veya atanmamis secenek: {optionNumber}");
        }

        return SelectRecipe(recipe);
    }

    /// <summary>
    /// Gelecekteki tarif arayuzu dogrudan RecipeConfig ile bu metodu cagirabilir.
    /// </summary>
    public bool SelectRecipe(RecipeConfig recipe)
    {
        if (!_isAwaitingSelection || _pendingTruck == null)
        {
            return Reject("Atolyede secim bekleyen bir kamyon yok.");
        }

        if (_productionStarted)
        {
            return Reject("Secilen paket zaten uretiliyor.");
        }

        if (recipe == null || string.IsNullOrWhiteSpace(recipe.ItemID) || recipe.RequiredWood <= 0)
        {
            return Reject("Tarif bilgisi gecersiz.");
        }

        int amount = _deliveredWood / recipe.RequiredWood;
        if (amount <= 0)
        {
            return Reject(
                $"{recipe.ItemID} icin yetersiz odun. Gereken: {recipe.RequiredWood}, mevcut: {_deliveredWood}");
        }

        if (workshopProduction == null)
        {
            return Reject("WorkshopProductionManager baglantisi bulunamadi.");
        }

        if (!workshopProduction.StartProductionCycle(_pendingTruck.gameObject))
        {
            return Reject("Atolye su anda baska bir uretimle mesgul.");
        }

        _selectedRecipe = recipe;
        _selectedAmount = amount;
        _remainingWood = _deliveredWood - amount * recipe.RequiredWood;
        _isAwaitingSelection = false;
        _productionStarted = true;

        Debug.Log(
            $"[EfeWorkshopSelection] Secim kabul edildi: {amount}x {recipe.ItemID}. " +
            $"Kullanilan odun: {amount * recipe.RequiredWood}, kalan: {_remainingWood}.",
            this);
        InvokeSafely(OnRecipeSelected, recipe, amount, _remainingWood);

        if (_completionRoutine != null)
        {
            StopCoroutine(_completionRoutine);
        }
        _completionRoutine = StartCoroutine(WaitForSelectedProduction());
        return true;
    }

    private bool HandleWorkshopSelectionRequest(TruckRouteController truck)
    {
        if (!isActiveAndEnabled || truck == null || workshopProduction == null)
        {
            return false;
        }

        if (_pendingTruck != null || _productionStarted)
        {
            return false;
        }

        if (option1Recipe == null && option2Recipe == null)
        {
            Debug.LogWarning(
                "[EfeWorkshopSelection] Tarif secenegi yok; Mert'in otomatik akisi kullanilacak.",
                this);
            return false;
        }

        int incomingWood = Mathf.Max(0, truck.currentLoadedWood);
        if (incomingWood <= 0)
        {
            return false;
        }

        _pendingTruck = truck;
        _deliveredWood = incomingWood;
        _remainingWood = incomingWood;
        _selectedRecipe = null;
        _selectedAmount = 0;
        _isAwaitingSelection = true;

        string option1 = DescribeOption(1, option1Recipe);
        string option2 = DescribeOption(2, option2Recipe);
        Debug.Log(
            $"[EfeWorkshopSelection] Kamyon atolyede. Uretim secimi bekleniyor. " +
            $"Odun: {_deliveredWood}. {option1} | {option2}",
            this);
        InvokeSafely(OnSelectionRequested, truck, _deliveredWood);
        return true;
    }

    private IEnumerator WaitForSelectedProduction()
    {
        TruckRouteController truck = _pendingTruck;
        RecipeConfig recipe = _selectedRecipe;
        int amount = _selectedAmount;

        while (truck != null && !truck.isWorkshopProcessDone)
        {
            yield return null;
        }

        if (truck != null)
        {
            Debug.Log(
                $"[EfeWorkshopSelection] Secilen paket hazir: {amount}x {recipe.ItemID}. Kamyon serbest.",
                this);
            InvokeSafely(OnSelectedProductionCompleted, recipe, amount);
        }

        ResetPendingState();
    }

    private void ContinueWithAutomaticFallback()
    {
        _isAwaitingSelection = false;

        if (workshopProduction != null &&
            !_productionStarted &&
            workshopProduction.StartProductionCycle(_pendingTruck.gameObject))
        {
            _productionStarted = true;
            Debug.LogWarning(
                "[EfeWorkshopSelection] Secim yoneticisi kapatildi; kamyon kilitlenmesin diye eski otomatik uretime donuldu.",
                this);
            return;
        }

        _pendingTruck.OnWorkshopProcessComplete();
        ResetPendingState();
    }

    private void ResetPendingState()
    {
        _pendingTruck = null;
        _isAwaitingSelection = false;
        _productionStarted = false;
        _completionRoutine = null;
    }

    private bool Reject(string reason)
    {
        Debug.LogWarning($"[EfeWorkshopSelection] Secim reddedildi: {reason}", this);
        InvokeSafely(OnSelectionRejected, reason);
        return false;
    }

    private static string DescribeOption(int number, RecipeConfig recipe)
    {
        return recipe == null
            ? $"{number}=Atanmadi"
            : $"{number}={recipe.ItemID} ({recipe.RequiredWood} odun/adet)";
    }

    private static bool WasOptionOnePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#else
        return false;
#endif
    }

    private static bool WasOptionTwoPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#else
        return false;
#endif
    }

    private static void InvokeSafely<T1, T2>(Action<T1, T2> handlers, T1 first, T2 second)
    {
        if (handlers == null) return;
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

    private static void InvokeSafely<T1, T2, T3>(
        Action<T1, T2, T3> handlers,
        T1 first,
        T2 second,
        T3 third)
    {
        if (handlers == null) return;
        foreach (Action<T1, T2, T3> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(first, second, third);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static void InvokeSafely(Action<string> handlers, string value)
    {
        if (handlers == null) return;
        foreach (Action<string> handler in handlers.GetInvocationList())
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
}
