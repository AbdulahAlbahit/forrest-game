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

    [Header("Efe Karakter Akisi")]
    [SerializeField] private EfeOdunTasima odunTasima;

    [Header("Main UI")]
    [SerializeField] private Button depositWoodButton;
    [SerializeField] private TMP_Text playerWoodStockText;

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
        RefreshWoodStockText();

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
            RefreshWoodStockText();
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

        RefreshWoodStockText();
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
        RefreshWoodStockText();
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
