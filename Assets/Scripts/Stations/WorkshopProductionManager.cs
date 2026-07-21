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

        if (smallLogStack) smallLogStack.SetActive(true);
        yield return new WaitForSeconds(unloadDuration * 0.5f);

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
        GameLogger.Log("[WorkshopProduction] Crafting finished. Animating box flight.", this);

        if (normalBoxes) 
        {
            normalBoxes.SetActive(true);
            Transform startPoint = (boxSpawnPoint != null) ? boxSpawnPoint : workshopTableTop;
            Transform targetTruckCargo = (truckCargoPoint != null) ? truckCargoPoint : (truck != null ? truck.transform : null);
            
            if (startPoint != null && targetTruckCargo != null) 
            {
                yield return StartCoroutine(AnimateBoxFlight(normalBoxes, startPoint, targetTruckCargo, 0.75f));
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

    private IEnumerator AnimateBoxFlight(GameObject box, Transform startTransform, Transform endTransform, float duration) 
    {
        float elapsed = 0f;
        Vector3 startPos = startTransform.position;
        Vector3 endPos = endTransform.position;
        
        float arcHeight = 2.5f; 
        
        Quaternion startRot = startTransform.rotation;
        Quaternion endRot = endTransform.rotation;

        while (elapsed < duration) 
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            box.transform.position = currentPos;

            Quaternion lookRot = Quaternion.Lerp(startRot, endRot, t);
            box.transform.rotation = lookRot * Quaternion.AngleAxis(t * 360f, Vector3.right + Vector3.up);

            yield return null;
        }

        box.transform.position = endPos;
        box.transform.rotation = endRot;
    }
}
