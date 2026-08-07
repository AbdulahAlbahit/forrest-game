using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Bulut'un CoinBar prefabindaki CoinValue metnini mevcut ekonomi ve kayit
/// eventleriyle gunceller. Bulut'un prefabini veya MainHUD kodunu degistirmez.
/// </summary>
public sealed class EfeCoinHudAdapter : MonoBehaviour
{
    [SerializeField] private TMP_Text coinValueText;

    private EconomyManager _economyManager;
    private CloudSaveManager _cloudSaveManager;
    private Coroutine _bindRoutine;

    private void OnEnable()
    {
        EfeMarketSalesController.OnBalanceChanged += HandleBalanceChanged;
        BindManagers();

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
        }
        _bindRoutine = StartCoroutine(BindManagersRoutine());
        RefreshValue();
    }

    private void OnDisable()
    {
        EfeMarketSalesController.OnBalanceChanged -= HandleBalanceChanged;
        UnbindManagers();

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }
    }

    private IEnumerator BindManagersRoutine()
    {
        while (isActiveAndEnabled)
        {
            BindManagers();
            RefreshValue();
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private void BindManagers()
    {
        EconomyManager currentEconomy = EconomyManager.Instance;
        if (_economyManager != currentEconomy)
        {
            if (_economyManager != null)
            {
                _economyManager.OnMoneyChanged -= HandleBalanceChanged;
            }

            _economyManager = currentEconomy;
            if (_economyManager != null)
            {
                _economyManager.OnMoneyChanged += HandleBalanceChanged;
            }
        }

        CloudSaveManager currentCloudSave = CloudSaveManager.Instance;
        if (_cloudSaveManager != currentCloudSave)
        {
            if (_cloudSaveManager != null)
            {
                _cloudSaveManager.OnDataLoaded -= HandleDataLoaded;
            }

            _cloudSaveManager = currentCloudSave;
            if (_cloudSaveManager != null)
            {
                _cloudSaveManager.OnDataLoaded += HandleDataLoaded;
            }
        }
    }

    private void UnbindManagers()
    {
        if (_economyManager != null)
        {
            _economyManager.OnMoneyChanged -= HandleBalanceChanged;
            _economyManager = null;
        }

        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= HandleDataLoaded;
            _cloudSaveManager = null;
        }
    }

    private void HandleDataLoaded(PlayerData data)
    {
        if (data != null)
        {
            SetValue(data.Gold);
        }
    }

    private void HandleBalanceChanged(int amount)
    {
        SetValue(amount);
    }

    private void RefreshValue()
    {
        if (_cloudSaveManager != null && _cloudSaveManager.CurrentData != null)
        {
            SetValue(_cloudSaveManager.CurrentData.Gold);
        }
        else if (_economyManager != null)
        {
            SetValue(_economyManager.MevcutPara);
        }
        else if (EfeMarketSalesController.Instance != null)
        {
            SetValue(EfeMarketSalesController.Instance.CurrentBalance);
        }
        else
        {
            SetValue(0);
        }
    }

    private void SetValue(int amount)
    {
        if (coinValueText != null)
        {
            coinValueText.text = Mathf.Max(0, amount).ToString("N0");
        }
    }
}
