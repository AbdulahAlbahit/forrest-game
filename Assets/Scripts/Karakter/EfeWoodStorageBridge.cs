using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efe'nin karakter sepeti ile kalici PlayerWoodStock verisi arasindaki kopru.
/// Ortak BasketManager ve WorkshopManager akislarina dokunmaz.
/// </summary>
[DefaultExecutionOrder(200)]
public sealed class EfeWoodStorageBridge : MonoBehaviour
{
    public static event Action<int, int> OnWoodStored;
    public static event Action<int, int> OnWoodWithdrawn;

    [Header("Efe Karakter Akisi")]
    [SerializeField] private EfeOdunTasima odunTasima;

    [Header("Main UI")]
    [SerializeField] private Button depositWoodButton;
    [SerializeField] private TMP_Text playerWoodStockText;

    [Header("Ambar Gorseli")]
    [SerializeField] private EfeWoodStorageVisual storageVisual;

    [Header("Kayit")]
    [SerializeField] private bool saveImmediately = true;

    private CloudSaveManager _cloudSaveManager;
    private CloudSaveManager _subscribedCloudSaveManager;
    private Coroutine _bindRoutine;

    private void OnEnable()
    {
        if (depositWoodButton != null)
        {
            depositWoodButton.onClick.RemoveListener(StoreCarriedWood);
            depositWoodButton.onClick.AddListener(StoreCarriedWood);
        }

        ResolveCloudSaveManager();
        RefreshWoodStockPresentation();

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
        }

        _bindRoutine = StartCoroutine(BindRoutine());
    }

    private void OnDisable()
    {
        if (depositWoodButton != null)
        {
            depositWoodButton.onClick.RemoveListener(StoreCarriedWood);
        }

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        UnsubscribeCloudSaveManager();
    }

    private IEnumerator BindRoutine()
    {
        while (isActiveAndEnabled)
        {
            ResolveCloudSaveManager();
            RefreshWoodStockPresentation();
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    public void StoreCarriedWood()
    {
        ResolveCloudSaveManager();

        if (odunTasima == null)
        {
            Debug.LogError("[EfeWoodStorageBridge] EfeOdunTasima referansi eksik.", this);
            return;
        }

        if (odunTasima.TasinanOdunSayisi <= 0)
        {
            NotificationUIController.Instance?.ShowNotification("Sepetiniz boş!");
            return;
        }

        if (_cloudSaveManager == null || _cloudSaveManager.CurrentData == null)
        {
            NotificationUIController.Instance?.ShowNotification("Kayıt sistemi hazırlanıyor, tekrar deneyin.");
            Debug.LogWarning(
                "[EfeWoodStorageBridge] PlayerData hazır olmadığı için odunlar sepetten silinmedi.",
                this);
            return;
        }

        int aktarilanMiktar = odunTasima.TasinanOdunlariAmbaraAktar();
        if (aktarilanMiktar <= 0)
        {
            return;
        }

        PlayerData data = _cloudSaveManager.CurrentData;
        data.PlayerWoodStock = Mathf.Max(0, data.PlayerWoodStock) + aktarilanMiktar;
        int toplamStok = data.PlayerWoodStock;

        RefreshWoodStockPresentation();
        OnWoodStored?.Invoke(aktarilanMiktar, toplamStok);

        if (saveImmediately)
        {
            _cloudSaveManager.SaveData(data);
        }

        NotificationUIController.Instance?.ShowNotification(
            $"{aktarilanMiktar} odun ambara aktarıldı. Toplam: {toplamStok}");
        Debug.Log(
            $"[EfeWoodStorageBridge] PlayerWoodStock güncellendi: +{aktarilanMiktar}, toplam {toplamStok}.",
            this);
    }

    public void TakeStoredWood(EfeOdunTasima requestingCarrier, Vector3 storageWorldPosition)
    {
        ResolveCloudSaveManager();

        if (requestingCarrier == null || requestingCarrier != odunTasima)
        {
            return;
        }

        if (_cloudSaveManager == null || _cloudSaveManager.CurrentData == null)
        {
            NotificationUIController.Instance?.ShowNotification("Kayıt sistemi hazırlanıyor, tekrar deneyin.");
            return;
        }

        PlayerData data = _cloudSaveManager.CurrentData;
        int mevcutStok = Mathf.Max(0, data.PlayerWoodStock);
        if (mevcutStok <= 0)
        {
            NotificationUIController.Instance?.ShowNotification("Ambarda odun yok.");
            return;
        }

        int bosKapasite = Mathf.Max(0, requestingCarrier.TasimaKapasitesi - requestingCarrier.TasinanOdunSayisi);
        if (bosKapasite <= 0)
        {
            NotificationUIController.Instance?.ShowNotification("Sepetiniz dolu!");
            return;
        }

        int istenenMiktar = Mathf.Min(mevcutStok, bosKapasite);
        int alinanMiktar = requestingCarrier.AmbardanOdunAl(istenenMiktar, storageWorldPosition);
        if (alinanMiktar <= 0)
        {
            return;
        }

        data.PlayerWoodStock = Mathf.Max(0, mevcutStok - alinanMiktar);
        int kalanStok = data.PlayerWoodStock;
        RefreshWoodStockPresentation();
        OnWoodWithdrawn?.Invoke(alinanMiktar, kalanStok);

        if (saveImmediately)
        {
            _cloudSaveManager.SaveData(data);
        }

        NotificationUIController.Instance?.ShowNotification(
            $"{alinanMiktar} odun ambardan sepete alındı. Kalan: {kalanStok}");
        Debug.Log(
            $"[EfeWoodStorageBridge] PlayerWoodStock güncellendi: -{alinanMiktar}, kalan {kalanStok}.",
            this);
    }

    private void ResolveCloudSaveManager()
    {
        CloudSaveManager current = CloudSaveManager.Instance;
        if (_cloudSaveManager == current)
        {
            return;
        }

        UnsubscribeCloudSaveManager();
        _cloudSaveManager = current;

        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded += HandleDataLoaded;
            _subscribedCloudSaveManager = _cloudSaveManager;
        }
    }

    private void HandleDataLoaded(PlayerData data)
    {
        RefreshWoodStockPresentation();
    }

    private void RefreshWoodStockPresentation()
    {
        RefreshWoodStockText();

        if (storageVisual != null)
        {
            int woodStock = _cloudSaveManager != null && _cloudSaveManager.CurrentData != null
                ? Mathf.Max(0, _cloudSaveManager.CurrentData.PlayerWoodStock)
                : 0;
            storageVisual.SetStoredAmount(woodStock);
        }
    }

    private void RefreshWoodStockText()
    {
        if (playerWoodStockText == null)
        {
            return;
        }

        int woodStock = 0;
        if (_cloudSaveManager != null && _cloudSaveManager.CurrentData != null)
        {
            woodStock = Mathf.Max(0, _cloudSaveManager.CurrentData.PlayerWoodStock);
        }

        playerWoodStockText.text = woodStock.ToString();
    }

    private void UnsubscribeCloudSaveManager()
    {
        if (_subscribedCloudSaveManager != null)
        {
            _subscribedCloudSaveManager.OnDataLoaded -= HandleDataLoaded;
            _subscribedCloudSaveManager = null;
        }
    }
}
