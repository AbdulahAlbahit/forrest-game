using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CargoTransferZone : MonoBehaviour
{
    public enum StationMode
    {
        PlayerToTruck_WoodLoading,      // Zone_Truck_Wood_Loading (Sırttan araca odun)
        TruckToWorkshop_CraftAndReturn  // Zone_Workshop_Upload (Araçtan atölyeye -> Üretim -> Araca mobilya)
    }

    [Header("--- İSTASYON MODU VE AYARLARI ---")]
    public StationMode mode = StationMode.PlayerToTruck_WoodLoading;
    public float transferRatePerSecond = 10f;
    public float workshopCraftingTime = 5f;

    [Header("--- GÖRSEL GERİ BİLDİRİM (URP & COLOR LERP) ---")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);      // Koyu Gri (Boş)
    public Color completedColor = new Color(0.1f, 0.9f, 0.2f, 1f);    // Parlak Yeşil (Doluyor/Hazır)
    public Color craftingColor = new Color(0.9f, 0.8f, 0.1f, 1f);     // Parlak Sarı (Atölye Çalışıyor)
    public Color readyColor = new Color(0f, 0.9f, 0.9f, 1f);          // Hazır Turkuazı

    [Tooltip("Dolan bar efekti için World Space Canvas içindeki Filled Image'ı sürükleyin.")]
    public Image fillImage;

    [Header("--- ANLIK DEPO DURUMU ---")]
    public int currentWoodCount = 0;
    public int currentFurnitureCount = 0;
    public bool isCraftingInProgress = false;

    [Header("--- EVENT PRİZLERİ ---")]
    public UnityEvent OnItemTransferredTick;
    public UnityEvent OnTransferProcessCompleted;
    public UnityEvent OnCraftingFinished;

    private float nextTickTime = 0f;
    private MeshRenderer padRenderer;
    private Material padMaterialInstance;
    private TruckRouteController activeTruck;

    void Start()
    {
        padRenderer = GetComponent<MeshRenderer>();
        if (padRenderer != null)
        {
            padMaterialInstance = padRenderer.material;
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

    void OnTriggerStay(Collider other)
    {
        if (Time.time < nextTickTime) return;
        nextTickTime = Time.time + (1f / transferRatePerSecond);

        // 1. MOD: SIRTTAN ARACA ODUN YÜKLEME (Zone_Truck_Wood_Loading)
        if (mode == StationMode.PlayerToTruck_WoodLoading)
        {
            if (activeTruck == null) activeTruck = FindObjectOfType<TruckRouteController>();

            if (activeTruck == null || !activeTruck.isAtHomePosition) return;

            if (other.CompareTag("Player") || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("fake"))
            {
                bool success = activeTruck.AddWoodToCargo(1);
                if (success)
                {
                    Debug.Log($"<color=green>[ODUN YÜKLENDİ] Kamyonetteki kargo: {activeTruck.currentLoadedWood}/{activeTruck.requiredWoodToDepart}</color>");
                    
                    float progress = Mathf.Clamp01((float)activeTruck.currentLoadedWood / activeTruck.requiredWoodToDepart);
                    
                    if (fillImage != null) {
                        fillImage.fillAmount = progress;
                        fillImage.color = completedColor;
                    } else {
                        UpdatePadColor(Color.Lerp(emptyColor, completedColor, progress));
                    }

                    OnItemTransferredTick?.Invoke();

                    // 30/30 olunca levhayı sıfırla ki araç gidince boş görünsün
                    if (activeTruck.currentLoadedWood >= activeTruck.requiredWoodToDepart)
                    {
                        StartCoroutine(ResetColorAfterDelay(1.5f));
                    }
                }
            }
        }
        // 2. MOD: ARAÇTAN ATÖLYEYE İNDİR -> ÜRET -> MOBİLYA YÜKLE (Zone_Workshop_Upload)
        else if (mode == StationMode.TruckToWorkshop_CraftAndReturn)
        {
            bool isTruck = other.GetComponentInParent<TruckRouteController>() != null || other.name.ToLower().Contains("truck");
            bool isPlayerOrFake = other.CompareTag("Player") || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("fake");

            if (!isCraftingInProgress && (isTruck || isPlayerOrFake))
            {
                if (currentWoodCount < 10)
                {
                    currentWoodCount++;
                    Debug.Log($"[ATÖLYEYE İNDİ] Atölyeye giren odun: {currentWoodCount}/10");
                    
                    float progress = Mathf.Clamp01((float)currentWoodCount / 10f);
                    
                    if (fillImage != null) {
                        fillImage.fillAmount = progress;
                        fillImage.color = completedColor;
                    } else {
                        UpdatePadColor(Color.Lerp(emptyColor, completedColor, progress));
                    }
                    
                    OnItemTransferredTick?.Invoke();
                }
                else
                {
                    StartCoroutine(WorkshopCraftingRoutine());
                }
            }
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
        Debug.Log($"<color=yellow>[ATÖLYE ÇALIŞIYOR] {currentWoodCount} odun mobilyaya dönüştürülüyor... ({workshopCraftingTime} saniye)</color>");

        if (fillImage != null) {
            fillImage.fillAmount = 1f;
            fillImage.color = craftingColor; // Doluyken sarı olsun
        }
        UpdatePadColor(craftingColor); // Parlak Sarı

        yield return new WaitForSeconds(workshopCraftingTime);

        currentFurnitureCount += currentWoodCount;
        currentWoodCount = 0;
        isCraftingInProgress = false;

        if (fillImage != null) fillImage.color = readyColor; // Bitince turkuaz
        UpdatePadColor(readyColor); // Hazır Turkuazı

        Debug.Log($"<color=cyan>[ÜRETİM TAMAMLANDI] Atölyede {currentFurnitureCount} adet Mobilya hazır!</color>");
        OnCraftingFinished?.Invoke();
        OnTransferProcessCompleted?.Invoke();
    }

    private void UpdatePadColor(Color c)
    {
        if (padMaterialInstance != null)
        {
            padMaterialInstance.color = c;
            if (padMaterialInstance.HasProperty("_BaseColor"))
            {
                padMaterialInstance.SetColor("_BaseColor", c);
            }
        }
    }
}
