using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ForrestGame.Utils;

public class WorkshopProductionManager : MonoBehaviour 
{
    [Header("Spawn Points")]
    public Transform workshopTableTop;
    public Transform boxSpawnPoint;
    public Transform truckCargoPoint;

    [Header("Log & Box Models")]
    public GameObject smallLogStack;
    public GameObject largeLogStack;
    public GameObject normalBoxes;
    public GameObject largeBoxes;

    [Header("Worker & UI")]
    public Animator workerAnimator;
    public GameObject loadingRingCanvas;
    public Image loadingRingProgress;

    [Header("Durations")]
    public float unloadDuration = 2f;
    public float craftingDuration = 3f;
    public float loadingDuration = 2f;

    // SFX Actions for AudioManager
    public static event Action OnHammerStrike;
    public static event Action OnProductionSuccess;
    public static event Action OnCraftingStartedGlobal;
    public static event Action OnCraftingFinishedGlobal;

    private bool _isProducing = false;

    private void Start() 
    { 
        ResetVisuals(); 
    }

    public void ResetVisuals() 
    {
        if (smallLogStack) smallLogStack.SetActive(false);
        if (largeLogStack) largeLogStack.SetActive(false);
        if (normalBoxes) normalBoxes.SetActive(false);
        if (largeBoxes) largeBoxes.SetActive(false);
        if (loadingRingCanvas) loadingRingCanvas.SetActive(false);
        if (workerAnimator) workerAnimator.SetBool("isCrafting", false);
    }

    public bool StartProductionCycle(GameObject truck) 
    {
        if (_isProducing) return false;
        StartCoroutine(ProductionRoutine(truck));
        return true;
    }

    private void SnapToPoint(GameObject obj, Transform targetPoint) 
    {
        if (obj != null && targetPoint != null) 
        {
            obj.transform.position = targetPoint.position;
            obj.transform.rotation = targetPoint.rotation;
        }
    }

    private IEnumerator ProductionRoutine(GameObject truck) 
    {
        _isProducing = true; 
        ResetVisuals();

        GameLogger.Log("[WorkshopProduction] Unloading logs to table.", this);

        SnapToPoint(smallLogStack, workshopTableTop);
        SnapToPoint(largeLogStack, workshopTableTop);
        Transform targetBoxPoint = (boxSpawnPoint != null) ? boxSpawnPoint : workshopTableTop;
        SnapToPoint(normalBoxes, targetBoxPoint);
        SnapToPoint(largeBoxes, targetBoxPoint);

        // 1. KAMYONDAN MASAYA TEK TEK UÇAN ODUNLAR (Efe'nin sistemine benzer)
        TruckRouteController truckController = truck != null ? truck.GetComponent<TruckRouteController>() : null;
        GameObject logPrefab = TreeManager.Instance != null ? TreeManager.Instance.woodLogPrefab : null;
        Transform startPoint = truckCargoPoint != null ? truckCargoPoint : (truck != null ? truck.transform : null);
        Transform endPoint = workshopTableTop;

        if (truckController != null && truckController.currentLoadedWood > 0 && logPrefab != null && startPoint != null && endPoint != null)
        {
            int totalLogs = truckController.currentLoadedWood;
            float timePerLog = unloadDuration / totalLogs;
            float flightDuration = Mathf.Clamp(timePerLog * 1.5f, 0.2f, 0.5f); // Her birinin havada kalma süresi

            for (int i = 0; i < totalLogs; i++)
            {
                // Kamyonun kasasındaki odun sayısını 1 düşür (Efe'nin scripti anında 1 tane görseli silecek)
                truckController.currentLoadedWood--;

                // Havada uçacak sahte bir kütük oluştur
                GameObject flyingLog = Instantiate(logPrefab, startPoint.position, startPoint.rotation);
                
                // Kütük fiziksel olarak düşmesin diye Rigidbody'sini sil
                Rigidbody rb = flyingLog.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);
                
                // Çarpışmaları engelle
                foreach (Collider col in flyingLog.GetComponentsInChildren<Collider>()) col.enabled = false;

                // Uçuşu başlat ve ulaştığında yok et
                StartCoroutine(AnimateBoxFlight(flyingLog, startPoint, endPoint, flightDuration, true));

                // Bir sonraki kütüğün fırlaması için biraz bekle
                yield return new WaitForSeconds(timePerLog);
            }
        }
        else
        {
            // Eğer prefab yoksa veya sistem eksikse eski usül toptan sil
            if (truckController != null) truckController.currentLoadedWood = 0;
            yield return new WaitForSeconds(unloadDuration);
        }

        // Tüm odunlar uçtuktan sonra masadaki küçük yığın görünür olsun
        if (smallLogStack) smallLogStack.SetActive(true);
        yield return new WaitForSeconds(0.5f); // Yığın oluştuktan sonra yarım saniye es payı

        if (smallLogStack) smallLogStack.SetActive(false);
        if (largeLogStack) largeLogStack.SetActive(true);
        yield return new WaitForSeconds(unloadDuration * 0.5f);

        if (workerAnimator) workerAnimator.SetBool("isCrafting", true);
        if (loadingRingCanvas) 
        { 
            loadingRingCanvas.SetActive(true); 
            if (loadingRingProgress) loadingRingProgress.fillAmount = 0f; 
        }

        GameLogger.Log("[WorkshopProduction] Crafting started.", this);
        OnCraftingStartedGlobal?.Invoke();

        float elapsed = 0f;
        float sfxTimer = 0f;
        float hammerInterval = 0.54f;

        while (elapsed < craftingDuration) 
        {
            elapsed += Time.deltaTime;
            sfxTimer += Time.deltaTime;

            if (sfxTimer >= hammerInterval) 
            {
                OnHammerStrike?.Invoke();
                sfxTimer = 0f;
            }

            if (loadingRingProgress) loadingRingProgress.fillAmount = Mathf.Clamp01(elapsed / craftingDuration);
            yield return null;
        }

        if (workerAnimator) workerAnimator.SetBool("isCrafting", false);
        if (loadingRingCanvas) loadingRingCanvas.SetActive(false);
        if (largeLogStack) largeLogStack.SetActive(false);

        OnProductionSuccess?.Invoke();
        OnCraftingFinishedGlobal?.Invoke();
        GameLogger.Log("[WorkshopProduction] Crafting finished. Animating box flight.", this);

        if (normalBoxes) 
        {
            normalBoxes.SetActive(true);
            Transform boxStartPoint = (boxSpawnPoint != null) ? boxSpawnPoint : workshopTableTop;
            Transform targetTruckCargo = (truckCargoPoint != null) ? truckCargoPoint : (truck != null ? truck.transform : null);
            
            if (boxStartPoint != null && targetTruckCargo != null) 
            {
                yield return StartCoroutine(AnimateBoxFlight(normalBoxes, boxStartPoint, targetTruckCargo, 0.75f));
            } 
            else 
            {
                yield return new WaitForSeconds(loadingDuration);
            }
            normalBoxes.SetActive(false);
        } 
        else 
        {
            yield return new WaitForSeconds(loadingDuration);
        }

        if (largeBoxes != null) 
        {
            Transform targetTruckCargo = (truckCargoPoint != null) ? truckCargoPoint : (truck != null ? truck.transform : null);
            SnapToPoint(largeBoxes, targetTruckCargo);
            largeBoxes.SetActive(true);
        } 
        else if (truck != null) 
        {
            Transform tc = truck.transform.Find("Cargo_Boxes"); 
            if (tc == null && truckCargoPoint != null) tc = truckCargoPoint;
            if (tc != null) tc.gameObject.SetActive(true);
        }

        if (truck != null) 
        {
            MonoBehaviour rc = truck.GetComponent("TruckRouteController") as MonoBehaviour;
            if (rc != null) rc.SendMessage("OnWorkshopProcessComplete", SendMessageOptions.DontRequireReceiver);
        }

        _isProducing = false;
        GameLogger.Log("[WorkshopProduction] Cycle complete. Truck departing.", this);
    }

    private IEnumerator AnimateBoxFlight(GameObject box, Transform startTransform, Transform endTransform, float duration, bool destroyAtEnd = false) 
    {
        float elapsed = 0f;
        Vector3 startPos = startTransform.position;
        Vector3 endPos = endTransform.position;
        
        float arcHeight = 2.5f; 
        
        Quaternion startRot = startTransform.rotation;
        Quaternion endRot = endTransform.rotation;

        while (elapsed < duration) 
        {
            if (box == null) yield break; // Safety check

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            box.transform.position = currentPos;

            Quaternion lookRot = Quaternion.Lerp(startRot, endRot, t);
            box.transform.rotation = lookRot * Quaternion.AngleAxis(t * 360f, Vector3.right + Vector3.up);

            yield return null;
        }

        if (box != null)
        {
            box.transform.position = endPos;
            box.transform.rotation = endRot;
            if (destroyAtEnd) Destroy(box);
        }
    }
}
