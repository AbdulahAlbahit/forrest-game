using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ForrestGame.Utils;

public class CargoTransferZone : MonoBehaviour
{
    public enum StationMode
    {
        PlayerToTruck_WoodLoading,
        TruckToWorkshop_CraftAndReturn
    }

    [Header("Station Settings")]
    public StationMode mode = StationMode.PlayerToTruck_WoodLoading;
    public float transferRatePerSecond = 10f;
    public float workshopCraftingTime = 3f; // Atölye animasyonuyla senkronize olması için 5'ten 3'e düşürdük

    [Header("Visual Feedback")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color completedColor = new Color(0.1f, 0.9f, 0.2f, 1f);
    public Color craftingColor = new Color(0.9f, 0.8f, 0.1f, 1f);
    public Color readyColor = new Color(0f, 0.9f, 0.9f, 1f);
    public Image fillImage;

    [Header("Storage Status")]
    public int currentWoodCount = 0;
    public int currentFurnitureCount = 0;
    public bool isCraftingInProgress = false;

    [Header("Unity Events")]
    public UnityEvent OnItemTransferredTick;
    public UnityEvent OnTransferProcessCompleted;
    public UnityEvent OnCraftingFinished;

    // SFX Actions for AudioManager
    public static event Action OnWoodTransferred;
    public static event Action OnWorkshopCraftingStarted;
    public static event Action OnWorkshopCraftingFinished;

    private float _nextTickTime = 0f;
    private MeshRenderer _padRenderer;
    private Material _padMaterialInstance;
    private TruckRouteController _activeTruck;

    private void Start()
    {
        _padRenderer = GetComponent<MeshRenderer>();
        if (_padRenderer != null)
        {
            _padMaterialInstance = _padRenderer.material;
            UpdatePadColor(emptyColor);
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = completedColor;
        }

        if (gameObject.name.ToLower().Contains("wood_loading"))
        {
            mode = StationMode.PlayerToTruck_WoodLoading;
        }
        else if (gameObject.name.ToLower().Contains("workshop_upload"))
        {
            mode = StationMode.TruckToWorkshop_CraftAndReturn;
        }
    }

    private void Update()
    {
        // Kamyon (Araç) atölye alanına (Zone) yeterince yaklaştıysa sistemi tetikle!
        if (mode == StationMode.TruckToWorkshop_CraftAndReturn && !isCraftingInProgress)
        {
            if (_activeTruck == null) _activeTruck = FindObjectOfType<TruckRouteController>();
            
            // 8 metreyi 4 metreye düşürdük. 
            if (_activeTruck != null && !_activeTruck.isAtHomePosition && Vector3.Distance(transform.position, _activeTruck.transform.position) < 4f)
            {
                // FİLTRE: Kamyon atölyedeki işini bitirmişse (yani markete gidiyor veya eve dönüyorsa) ÇALIŞTIRMA!
                if (!_activeTruck.isWorkshopProcessDone)
                {
                    if (Time.time >= _nextTickTime)
                    {
                        _nextTickTime = Time.time + (1f / transferRatePerSecond);
                        ProcessTruckToWorkshopLogic();
                    }
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < _nextTickTime) return;
        
        if (mode == StationMode.PlayerToTruck_WoodLoading)
        {
            _nextTickTime = Time.time + (1f / transferRatePerSecond);
            ProcessPlayerToTruckMode();
        }
        else if (mode == StationMode.TruckToWorkshop_CraftAndReturn)
        {
            // Player karakteri zone'a basarsa tetikle (Kamyonu zaten Update içinden mesafe ile tetikliyoruz)
            bool isPlayerOrFake = other.CompareTag("Player") || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("fake");
            if (isPlayerOrFake && !isCraftingInProgress)
            {
                _nextTickTime = Time.time + (1f / transferRatePerSecond);
                ProcessTruckToWorkshopLogic();
            }
        }
    }

    private void ProcessPlayerToTruckMode()
    {
        if (_activeTruck == null) _activeTruck = FindObjectOfType<TruckRouteController>();
        if (_activeTruck == null || !_activeTruck.isAtHomePosition) return;

        float progress = Mathf.Clamp01((float)_activeTruck.currentLoadedWood / _activeTruck.requiredWoodToDepart);
        
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
            fillImage.color = completedColor;
        }
        else
        {
            UpdatePadColor(Color.Lerp(emptyColor, completedColor, progress));
        }

        if (_activeTruck.currentLoadedWood >= _activeTruck.requiredWoodToDepart)
        {
            StartCoroutine(ResetColorAfterDelay(1.5f));
        }
    }

    private void ProcessTruckToWorkshopLogic()
    {
        if (currentWoodCount < 10)
        {
            currentWoodCount++;
            GameLogger.Log($"[Workshop Zone] Zemin Rengi Doluyor: {currentWoodCount}/10", this);
            
            float progress = Mathf.Clamp01((float)currentWoodCount / 10f);
            
            if (fillImage != null) 
            {
                fillImage.fillAmount = progress;
                fillImage.color = completedColor;
            } 
            else 
            {
                UpdatePadColor(Color.Lerp(emptyColor, completedColor, progress));
            }
            
            OnItemTransferredTick?.Invoke();
            if (OnWoodTransferred != null) OnWoodTransferred.Invoke();
        }
        else
        {
            StartCoroutine(WorkshopCraftingRoutine());
        }
    }

    private IEnumerator ResetColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (fillImage != null) fillImage.fillAmount = 0f;
        UpdatePadColor(emptyColor);
    }

    private IEnumerator WorkshopCraftingRoutine()
    {
        isCraftingInProgress = true;
        
        float unloadTime = 2f;
        float craftTime = 3f;
        float loadTime = 2f;

        WorkshopProductionManager wp = FindObjectOfType<WorkshopProductionManager>();
        if (wp != null)
        {
            unloadTime = wp.unloadDuration;
            craftTime = wp.craftingDuration;
            loadTime = wp.loadingDuration;
        }

        GameLogger.Log($"[Workshop Zone] Kamyon odunları indiriyor... ({unloadTime} sn)", this);
        // 1. Kamyon atölyeye yanaştı, Efe kütükleri masaya koyana kadar bekle. (Bu sürede renk yeşil veya dolmaya devam eder)
        yield return new WaitForSeconds(unloadTime);

        // 2. Kütükler masaya indi, testere (Crafting) çalışmaya başladı (Renk SARI olur)
        GameLogger.Log($"[Workshop Zone] Üretim Başladı! Renk SARI. ({craftTime} sn)", this);
        if (fillImage != null) 
        {
            fillImage.fillAmount = 1f;
            fillImage.color = craftingColor; 
        }
        UpdatePadColor(craftingColor);

        // Testerenin işini bitirmesini bekle
        yield return new WaitForSeconds(craftTime);

        // 3. Üretim Bitti, mobilyalar hazır! (Renk TURKUAZ olur)
        currentFurnitureCount += currentWoodCount;
        currentWoodCount = 0;
        
        GameLogger.Log($"[Workshop Zone] Üretim Bitti! Renk TURKUAZ.", this);
        if (fillImage != null) fillImage.color = readyColor; 
        UpdatePadColor(readyColor); 

        // 4. Kamyon kutuları alıp yola çıkıyor. Kamyon tamamen gittikten sonra zemini sıfırla (GRİ)
        yield return new WaitForSeconds(loadTime + 1f); // Kamyonun hareket etme payı için 1 saniye eklendi
        
        isCraftingInProgress = false;
        
        if (fillImage != null) 
        { 
            fillImage.fillAmount = 0f; 
            fillImage.color = emptyColor; // İşi bitince orjinal boş renge geri dönsün
        }
        UpdatePadColor(emptyColor);

        OnCraftingFinished?.Invoke();
        OnTransferProcessCompleted?.Invoke();
        if (OnWorkshopCraftingFinished != null) OnWorkshopCraftingFinished.Invoke();
    }

    private void UpdatePadColor(Color c)
    {
        if (_padMaterialInstance != null)
        {
            _padMaterialInstance.color = c;
            if (_padMaterialInstance.HasProperty("_BaseColor"))
            {
                _padMaterialInstance.SetColor("_BaseColor", c);
            }
        }
    }
}
