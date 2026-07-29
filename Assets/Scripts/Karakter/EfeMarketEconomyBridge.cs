using System.Collections;
using UnityEngine;

/// <summary>
/// Efe'nin market satis eventini Abdullah'in mevcut ekonomi ve kayit
/// sistemine baglar. Kaynak ekonomi/veritabani kodlarini degistirmez.
/// </summary>
public sealed class EfeMarketEconomyBridge : MonoBehaviour
{
    public static EfeMarketEconomyBridge Instance { get; private set; }

    [Header("Kayit Guvenligi")]
    [SerializeField, Min(1f)] private float localFallbackDelay = 5f;
    [SerializeField] private bool saveImmediatelyAfterSale = true;

    private EconomyManager _economyManager;
    private CloudSaveManager _cloudSaveManager;
    private CloudSaveManager _subscribedCloudSaveManager;
    private Coroutine _bindRoutine;
    private bool _fallbackLoadRequested;
    private int _lastSynchronizedBalance = int.MinValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EfeMarketEconomyBridge] Ikinci ekonomi koprusu kapatildi.", this);
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

        EfeMarketSalesController.OnCreditRequested -= HandleCreditRequested;
        EfeMarketSalesController.OnCreditRequested += HandleCreditRequested;
        ResolveManagers();

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
        }
        _bindRoutine = StartCoroutine(BindAndSynchronizeRoutine());
    }

    private void OnDisable()
    {
        EfeMarketSalesController.OnCreditRequested -= HandleCreditRequested;
        UnsubscribeCloudSave();

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator BindAndSynchronizeRoutine()
    {
        float elapsed = 0f;

        while (isActiveAndEnabled)
        {
            ResolveManagers();

            if (_cloudSaveManager != null && _cloudSaveManager.CurrentData != null)
            {
                SynchronizeSalesBalance(_cloudSaveManager.CurrentData);
            }
            else if (!_fallbackLoadRequested &&
                     _cloudSaveManager != null &&
                     elapsed >= localFallbackDelay)
            {
                _fallbackLoadRequested = true;
                Debug.LogWarning(
                    "[EfeMarketEconomyBridge] Bulut verisi bekleme suresinde gelmedi; " +
                    "CloudSaveManager yerel yedegi deneyecek.",
                    this);
                _cloudSaveManager.LoadData();
            }

            elapsed += 0.25f;
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private bool HandleCreditRequested(
        EfeMarketSalesController.SaleRequest request,
        out int authoritativeBalance)
    {
        authoritativeBalance = 0;
        ResolveManagers();

        if (request.TotalPrice <= 0)
        {
            Debug.LogWarning("[EfeMarketEconomyBridge] Sifir veya negatif satis kayda gonderilmedi.", this);
            return false;
        }

        if (_economyManager == null ||
            _cloudSaveManager == null ||
            _cloudSaveManager.CurrentData == null)
        {
            Debug.LogWarning(
                "[EfeMarketEconomyBridge] Ekonomi verisi henuz hazir degil; " +
                "satis oturum bakiyesiyle tamamlanacak.",
                this);
            return false;
        }

        int previousBalance = _economyManager.MevcutPara;
        _economyManager.AddMoney(request.TotalPrice);
        authoritativeBalance = _economyManager.MevcutPara;
        _lastSynchronizedBalance = authoritativeBalance;

        if (saveImmediatelyAfterSale)
        {
            _cloudSaveManager.SaveData(_cloudSaveManager.CurrentData);
        }

        Debug.Log(
            $"[EfeMarketEconomyBridge] Satis kalici ekonomiye yazildi: " +
            $"{previousBalance} -> {authoritativeBalance}.",
            this);
        return true;
    }

    private void ResolveManagers()
    {
        if (_economyManager == null)
        {
            _economyManager = EconomyManager.Instance;
        }

        CloudSaveManager currentCloudSave = CloudSaveManager.Instance;
        if (_cloudSaveManager != currentCloudSave)
        {
            UnsubscribeCloudSave();
            _cloudSaveManager = currentCloudSave;
        }

        if (_cloudSaveManager != null && _subscribedCloudSaveManager != _cloudSaveManager)
        {
            _cloudSaveManager.OnDataLoaded += HandleDataLoaded;
            _subscribedCloudSaveManager = _cloudSaveManager;
        }
    }

    private void HandleDataLoaded(PlayerData data)
    {
        SynchronizeSalesBalance(data);
    }

    private void SynchronizeSalesBalance(PlayerData data)
    {
        if (data != null &&
            data.Para != _lastSynchronizedBalance &&
            EfeMarketSalesController.Instance != null)
        {
            EfeMarketSalesController.Instance.SynchronizeBalance(data.Para);
            _lastSynchronizedBalance = data.Para;
        }
    }

    private void UnsubscribeCloudSave()
    {
        if (_subscribedCloudSaveManager != null)
        {
            _subscribedCloudSaveManager.OnDataLoaded -= HandleDataLoaded;
            _subscribedCloudSaveManager = null;
        }
    }
}
