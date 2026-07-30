using System;
using System.Collections;
using System.Collections.Generic;
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
    private readonly Queue<GameObject> _flyingLogPool = new Queue<GameObject>();

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

    /// <summary>
    /// WorkshopManager.ProcessQueue() kamyondan bagimsiz, arka planda bir tarif
    /// uretmeye basladiginda cagirir. Kamyon iceren eski ProductionRoutine'den
    /// farkli olarak sadece isci/halka gorsellerini yonetir.
    /// </summary>
    public void PlayQueueCraftingStart()
    {
        if (normalBoxes) normalBoxes.SetActive(false);
        if (workerAnimator) workerAnimator.SetBool("isCrafting", true);
        if (loadingRingCanvas)
        {
            loadingRingCanvas.SetActive(true);
            if (loadingRingProgress) loadingRingProgress.fillAmount = 0f;
        }
        OnCraftingStartedGlobal?.Invoke();
    }

    /// <summary>
    /// Aktif kuyruk uretiminin 0-1 arasi ilerlemesini halka gorseline yansitir.
    /// </summary>
    public void PlayQueueCraftingProgress(float progress01)
    {
        if (loadingRingProgress) loadingRingProgress.fillAmount = Mathf.Clamp01(progress01);
    }

    /// <summary>
    /// Kuyruktaki bir tarif uretimi bittiginde cagirir: isci animasyonunu durdurur,
    /// masaya (kamyon bekleniyormus gibi) bir kutu birakir.
    /// </summary>
    public void PlayQueueCraftingComplete()
    {
        if (workerAnimator) workerAnimator.SetBool("isCrafting", false);
        if (loadingRingCanvas) loadingRingCanvas.SetActive(false);

        if (normalBoxes != null)
        {
            Transform point = boxSpawnPoint != null ? boxSpawnPoint : workshopTableTop;
            if (point != null) SnapToPoint(normalBoxes, point);
            normalBoxes.SetActive(true);
        }

        OnProductionSuccess?.Invoke();
        OnCraftingFinishedGlobal?.Invoke();
    }

    /// <summary>
    /// Kamyon atolyeden ayrilmadan hemen once, masada bekleyen kutuyu (varsa)
    /// kamyonun kasasina ucurur ve yerine kalici bir kargo gorseli birakir.
    /// Kargo gorseli sadece gercekten yuklenmis urun varsa (hasCargo) gosterilir.
    /// </summary>
    public IEnumerator LoadFinishedBoxesOntoTruck(Transform truckTransform, bool hasCargo)
    {
        Transform target = truckCargoPoint != null ? truckCargoPoint : truckTransform;
        if (target == null) yield break;

        if (normalBoxes != null && normalBoxes.activeSelf)
        {
            Transform startPoint = boxSpawnPoint != null ? boxSpawnPoint : workshopTableTop;
            if (startPoint != null)
            {
                yield return StartCoroutine(AnimateBoxFlight(normalBoxes, startPoint, target, 0.75f));
            }
            normalBoxes.SetActive(false);
        }

        if (largeBoxes != null)
        {
            if (hasCargo)
            {
                SnapToPoint(largeBoxes, target);
                largeBoxes.SetActive(true);
            }
            else
            {
                largeBoxes.SetActive(false);
            }
        }
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

        int deliveredWoodAmount = 0;

        if (truckController != null && truckController.currentLoadedWood > 0 && logPrefab != null && startPoint != null && endPoint != null)
        {
            int totalLogs = truckController.currentLoadedWood;
            deliveredWoodAmount = totalLogs; // Kaç odun teslim edildi — backend için saklıyoruz
            float timePerLog = unloadDuration / totalLogs;
            float flightDuration = Mathf.Clamp(timePerLog * 1.5f, 0.2f, 0.5f); // Her birinin havada kalma süresi

            for (int i = 0; i < totalLogs; i++)
            {
                // Kamyonun kasasındaki odun sayısını 1 düşür (Efe'nin scripti anında 1 tane görseli silecek)
                truckController.currentLoadedWood--;

                // Havada uçacak sahte bir kütük al (havuzdan, yoksa oluştur)
                GameObject flyingLog = GetPooledFlyingLog(logPrefab, startPoint.position, startPoint.rotation);

                // Uçuşu başlat ve ulaştığında havuza iade et
                StartCoroutine(AnimateBoxFlight(flyingLog, startPoint, endPoint, flightDuration, true, ReturnFlyingLogToPool));

                // Bir sonraki kütüğün fırlaması için biraz bekle
                yield return new WaitForSeconds(timePerLog);
            }
        }
        else
        {
            // Eğer prefab yoksa veya sistem eksikse eski usül toptan sil
            if (truckController != null)
            {
                deliveredWoodAmount = truckController.currentLoadedWood;
                truckController.currentLoadedWood = 0;
            }
            yield return new WaitForSeconds(unloadDuration);
        }

        // ─── BACKEND: Teslim edilen odunları atölye stoguna ekle ───
        // Animasyon bitti, odunlar masaya indi → WorkshopManager'a bildir.
        // WorkshopRawWood bu sayede dolar ve uretim kuyruğu çalışabilir.
        if (deliveredWoodAmount > 0 && WorkshopManager.Instance != null)
        {
            WorkshopManager.Instance.AddWood(deliveredWoodAmount);
            GameLogger.Log($"[WorkshopProduction] {deliveredWoodAmount} odun atolye stoguna eklendi. Yeni bakiye: {WorkshopManager.Instance.MevcutKuyruk}", this);
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

    private IEnumerator AnimateBoxFlight(GameObject box, Transform startTransform, Transform endTransform, float duration, bool destroyAtEnd = false, Action<GameObject> onDestroyAtEnd = null)
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
            if (destroyAtEnd)
            {
                if (onDestroyAtEnd != null) onDestroyAtEnd(box);
                else Destroy(box);
            }
        }
    }

    private GameObject GetPooledFlyingLog(GameObject logPrefab, Vector3 position, Quaternion rotation)
    {
        GameObject flyingLog;

        if (_flyingLogPool.Count > 0)
        {
            flyingLog = _flyingLogPool.Dequeue();
            flyingLog.transform.SetPositionAndRotation(position, rotation);
            flyingLog.SetActive(true);
        }
        else
        {
            flyingLog = Instantiate(logPrefab, position, rotation);

            // Kütük fiziksel olarak düşmesin diye Rigidbody'sini sil (havuzdaki her kütükte tek seferlik)
            Rigidbody rb = flyingLog.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            // Çarpışmaları engelle
            foreach (Collider col in flyingLog.GetComponentsInChildren<Collider>()) col.enabled = false;
        }

        return flyingLog;
    }

    private void ReturnFlyingLogToPool(GameObject flyingLog)
    {
        flyingLog.SetActive(false);
        _flyingLogPool.Enqueue(flyingLog);
    }
}
