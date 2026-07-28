using System;
using System.Collections;
using System.Collections.Generic;
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
    public static event Action OnProductionQueueCompleted;

    public bool IsAwaitingSelection => _isAwaitingSelection;
    public bool IsProductionStarted => _productionStarted;
    public bool HasPendingTruck => _pendingTruck != null;
    public int DeliveredWood => _deliveredWood;
    public int RemainingWood => _remainingWood;
    public RecipeConfig SelectedRecipe => _selectedRecipe;
    public int SelectedAmount => _selectedAmount;
    public RecipeConfig Option1Recipe => option1Recipe;
    public RecipeConfig Option2Recipe => option2Recipe;
    public IReadOnlyList<RecipeConfig> QueuedRecipes => _queuedRecipes;
    public int QueuedProductCount => _queuedRecipes.Count;

    private TruckRouteController _pendingTruck;
    private bool _isAwaitingSelection;
    private bool _productionStarted;
    private int _deliveredWood;
    private int _remainingWood;
    private RecipeConfig _selectedRecipe;
    private int _selectedAmount;
    private Coroutine _completionRoutine;
    private readonly List<RecipeConfig> _queuedRecipes = new List<RecipeConfig>();

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
        else if (WasConfirmPressed())
        {
            ConfirmSelection();
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

        if (recipe == null || string.IsNullOrWhiteSpace(recipe.ItemID) || recipe.RequiredWood <= 0)
        {
            return Reject("Tarif bilgisi gecersiz.");
        }

        if (_remainingWood < recipe.RequiredWood)
        {
            return Reject(
                $"{recipe.ItemID} icin yetersiz odun. Gereken: {recipe.RequiredWood}, kalan: {_remainingWood}");
        }

        if (workshopProduction == null)
        {
            return Reject("WorkshopProductionManager baglantisi bulunamadi.");
        }

        _selectedRecipe = recipe;
        _selectedAmount = 1;
        _remainingWood -= recipe.RequiredWood;
        _queuedRecipes.Add(recipe);

        Debug.Log(
            $"[EfeWorkshopSelection] Kuyruga eklendi: 1x {recipe.ItemID}. " +
            $"Kuyruk: {_queuedRecipes.Count} urun, kalan odun: {_remainingWood}. " +
            "Uretim otomatik olarak devam ediyor.",
            this);
        InvokeSafely(OnRecipeSelected, recipe, 1, _remainingWood);

        if (!_productionStarted)
        {
            ConfirmSelection();
        }

        return true;
    }

    public bool ConfirmSelection()
    {
        if (!_isAwaitingSelection || _pendingTruck == null)
        {
            return Reject("Onaylanacak bir atolye secimi yok.");
        }

        if (_queuedRecipes.Count == 0)
        {
            return Reject("Uretime baslamadan once en az bir urun secin.");
        }

        if (_productionStarted)
        {
            return Reject("Uretim zaten basladi.");
        }

        _productionStarted = true;

        if (_completionRoutine != null)
        {
            StopCoroutine(_completionRoutine);
        }
        _completionRoutine = StartCoroutine(ProcessSelectedQueue());

        Debug.Log(
            $"[EfeWorkshopSelection] Otomatik uretim basladi. Kuyruk: {_queuedRecipes.Count} urun. " +
            $"Kullanilan odun: {_deliveredWood - _remainingWood}, kalan: {_remainingWood}.",
            this);
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
        _queuedRecipes.Clear();
        _isAwaitingSelection = true;

        string option1 = DescribeOption(1, option1Recipe);
        string option2 = DescribeOption(2, option2Recipe);
        Debug.LogWarning(
            $"[ATOLYE SECIMI] Kamyon atolyeye geldi. Urun secin: " +
            $"klavyeden [{option1}] veya [{option2}]. Teslim edilen odun: {_deliveredWood}. " +
            "Her secim urunu kuyruga ekler ve uretimi otomatik baslatir. " +
            "Mobil arayuz SelectRecipe metodunu kullanir.",
            this);
        InvokeSafely(OnSelectionRequested, truck, _deliveredWood);
        return true;
    }

    private IEnumerator ProcessSelectedQueue()
    {
        TruckRouteController truck = _pendingTruck;
        int completedOrderCount = 0;

        if (truck == null || workshopProduction == null)
        {
            Reject("Uretim baslarken kamyon veya atolye baglantisi kayboldu.");
            if (truck != null) truck.OnWorkshopProcessComplete();
            ResetPendingState();
            yield break;
        }

        yield return StartCoroutine(UnloadTruckWood(truck));

        while (completedOrderCount < _queuedRecipes.Count)
        {
            RecipeConfig recipe = _queuedRecipes[completedOrderCount];
            yield return StartCoroutine(CraftSingleProduct(
                recipe,
                truck,
                completedOrderCount + 1,
                _queuedRecipes.Count));

            completedOrderCount++;

            Debug.Log(
                $"[EfeWorkshopSelection] Paket hazir: 1x {recipe.ItemID} " +
                $"({completedOrderCount}/{_queuedRecipes.Count}).",
                this);
            InvokeSafely(OnSelectedProductionCompleted, recipe, 1);
        }

        _isAwaitingSelection = false;
        PrepareTruckCargoVisual(truck);
        truck.OnWorkshopProcessComplete();
        Debug.Log(
            $"[EfeWorkshopSelection] Tum kuyruk tamamlandi: {completedOrderCount} koli. Kamyon serbest.",
            this);
        InvokeSafely(OnProductionQueueCompleted);
        ResetPendingState();
    }

    private IEnumerator UnloadTruckWood(TruckRouteController truck)
    {
        workshopProduction.ResetVisuals();

        int totalLogs = Mathf.Max(0, truck.currentLoadedWood);
        Transform startPoint = workshopProduction.truckCargoPoint != null
            ? workshopProduction.truckCargoPoint
            : truck.transform;
        Transform endPoint = workshopProduction.workshopTableTop;
        GameObject logPrefab = TreeManager.Instance != null ? TreeManager.Instance.woodLogPrefab : null;
        float unloadDuration = Mathf.Max(0.1f, workshopProduction.unloadDuration);

        if (totalLogs > 0 && startPoint != null && endPoint != null && logPrefab != null)
        {
            float delay = unloadDuration / totalLogs;
            float flightDuration = Mathf.Clamp(delay * 1.5f, 0.2f, 0.5f);
            for (int i = 0; i < totalLogs; i++)
            {
                truck.currentLoadedWood = Mathf.Max(0, truck.currentLoadedWood - 1);
                GameObject flyingLog = Instantiate(logPrefab, startPoint.position, startPoint.rotation);
                Rigidbody rigidbody = flyingLog.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;
                    rigidbody.useGravity = false;
                }
                foreach (Collider collider in flyingLog.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }

                StartCoroutine(AnimateAndDestroy(
                    flyingLog,
                    startPoint.position,
                    startPoint.rotation,
                    endPoint.position,
                    endPoint.rotation,
                    flightDuration));
                yield return new WaitForSeconds(delay);
            }
        }
        else
        {
            truck.currentLoadedWood = 0;
            yield return new WaitForSeconds(unloadDuration);
        }

        truck.currentLoadedWood = 0;
        if (workshopProduction.smallLogStack != null)
        {
            SnapToPoint(workshopProduction.smallLogStack, workshopProduction.workshopTableTop);
            workshopProduction.smallLogStack.SetActive(true);
        }
        yield return new WaitForSeconds(0.5f);

        if (workshopProduction.smallLogStack != null) workshopProduction.smallLogStack.SetActive(false);
        if (workshopProduction.largeLogStack != null)
        {
            SnapToPoint(workshopProduction.largeLogStack, workshopProduction.workshopTableTop);
            workshopProduction.largeLogStack.SetActive(true);
        }
    }

    private IEnumerator CraftSingleProduct(
        RecipeConfig recipe,
        TruckRouteController truck,
        int orderNumber,
        int totalOrders)
    {
        if (workshopProduction.workerAnimator != null)
        {
            workshopProduction.workerAnimator.SetBool("isCrafting", true);
        }
        if (workshopProduction.loadingRingCanvas != null)
        {
            workshopProduction.loadingRingCanvas.SetActive(true);
        }
        if (workshopProduction.loadingRingProgress != null)
        {
            workshopProduction.loadingRingProgress.fillAmount = 0f;
        }

        float duration = recipe != null && recipe.BaseProductionTime > 0f
            ? recipe.BaseProductionTime
            : Mathf.Max(0.1f, workshopProduction.craftingDuration);
        float elapsed = 0f;
        Debug.Log(
            $"[EfeWorkshopSelection] Uretim basladi: {recipe.ItemID} ({orderNumber}/{totalOrders}).",
            this);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (workshopProduction.loadingRingProgress != null)
            {
                workshopProduction.loadingRingProgress.fillAmount = Mathf.Clamp01(elapsed / duration);
            }
            yield return null;
        }

        if (workshopProduction.workerAnimator != null)
        {
            workshopProduction.workerAnimator.SetBool("isCrafting", false);
        }
        if (workshopProduction.loadingRingCanvas != null)
        {
            workshopProduction.loadingRingCanvas.SetActive(false);
        }

        GameObject boxes = workshopProduction.normalBoxes;
        Transform startPoint = workshopProduction.boxSpawnPoint != null
            ? workshopProduction.boxSpawnPoint
            : workshopProduction.workshopTableTop;
        Transform endPoint = workshopProduction.truckCargoPoint != null
            ? workshopProduction.truckCargoPoint
            : truck.transform;

        if (boxes != null && startPoint != null && endPoint != null)
        {
            boxes.SetActive(true);
            yield return StartCoroutine(AnimateObject(
                boxes,
                startPoint.position,
                startPoint.rotation,
                endPoint.position,
                endPoint.rotation,
                0.75f));
            boxes.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, workshopProduction.loadingDuration));
        }
    }

    private void PrepareTruckCargoVisual(TruckRouteController truck)
    {
        if (workshopProduction.largeLogStack != null)
        {
            workshopProduction.largeLogStack.SetActive(false);
        }

        Transform target = workshopProduction.truckCargoPoint != null
            ? workshopProduction.truckCargoPoint
            : truck.transform;
        if (workshopProduction.largeBoxes != null && target != null)
        {
            SnapToPoint(workshopProduction.largeBoxes, target);
            workshopProduction.largeBoxes.SetActive(true);
        }
    }

    private IEnumerator AnimateAndDestroy(
        GameObject item,
        Vector3 startPosition,
        Quaternion startRotation,
        Vector3 endPosition,
        Quaternion endRotation,
        float duration)
    {
        yield return StartCoroutine(AnimateObject(
            item,
            startPosition,
            startRotation,
            endPosition,
            endRotation,
            duration));
        if (item != null) Destroy(item);
    }

    private static IEnumerator AnimateObject(
        GameObject item,
        Vector3 startPosition,
        Quaternion startRotation,
        Vector3 endPosition,
        Quaternion endRotation,
        float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            if (item == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 2.5f;
            item.transform.position = position;
            item.transform.rotation = Quaternion.Lerp(startRotation, endRotation, t) *
                                      Quaternion.AngleAxis(t * 360f, Vector3.right + Vector3.up);
            yield return null;
        }

        if (item != null)
        {
            item.transform.SetPositionAndRotation(endPosition, endRotation);
        }
    }

    private static void SnapToPoint(GameObject item, Transform point)
    {
        if (item != null && point != null)
        {
            item.transform.SetPositionAndRotation(point.position, point.rotation);
        }
    }

    private void ContinueWithAutomaticFallback()
    {
        _isAwaitingSelection = false;

        if (workshopProduction != null)
        {
            workshopProduction.ResetVisuals();
        }

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
        _queuedRecipes.Clear();
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

    private static bool WasConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
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

    private static void InvokeSafely(Action handlers)
    {
        if (handlers == null) return;
        foreach (Action handler in handlers.GetInvocationList())
        {
            try
            {
                handler();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
