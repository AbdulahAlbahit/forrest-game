using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopProductionManager : MonoBehaviour {
    [Header("🎯 Konumlandırma Yuvaları (Spawn Points)")]
    [Tooltip("Odunların kamyondan inip duracağı Atölye Masası Noktası (Transform)")]
    public Transform workshopTableTop;
    [Tooltip("Üretim bitince paketlerin/kolilerin çıkacağı Çıkış Konumu (Transform)")]
    public Transform boxSpawnPoint;
    [Tooltip("Kolilerin yükleneceği Kamyonet Kasası Noktası (Opsiyonel / Transform)")]
    public Transform truckCargoPoint;

    [Header("🪵 Odun & Koli Modelleri")]
    public GameObject smallLogStack;
    public GameObject largeLogStack;
    public GameObject normalBoxes;
    public GameObject largeBoxes;

    [Header("👨‍🔧 Usta & Yükleme UI")]
    public Animator workerAnimator;
    public GameObject loadingRingCanvas;
    public Image loadingRingProgress;

    [Header("🔊 Ses Efektleri (SFX)")]
    [Tooltip("Usta üretim yaparken çalacak çekiç sesi")]
    public AudioClip hammerStrikeSFX;
    [Tooltip("Üretim başarıyla tamamlandığında çalacak ses")]
    public AudioClip productionSuccessSFX;
    private AudioSource audioSource;

    [Header("⏱️ Süreler")]
    public float unloadDuration = 2f;
    public float craftingDuration = 3f;
    public float loadingDuration = 2f;

    private bool isProducing = false;

    void Start() { 
        ResetVisuals(); 
        // Ses çalabilmek için dinamik AudioSource ekliyoruz
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D Ses yapıyoruz (Atölyeye yakınlaşınca duyulsun)
    }

    public void ResetVisuals() {
        if (smallLogStack) smallLogStack.SetActive(false);
        if (largeLogStack) largeLogStack.SetActive(false);
        if (normalBoxes) normalBoxes.SetActive(false);
        if (largeBoxes) largeBoxes.SetActive(false);
        if (loadingRingCanvas) loadingRingCanvas.SetActive(false);
        if (workerAnimator) workerAnimator.SetBool("isCrafting", false);
    }

    public bool StartProductionCycle(GameObject truck) {
        if (isProducing) return false;
        StartCoroutine(ProductionRoutine(truck));
        return true;
    }

    private void PlaySFX(AudioClip clip) {
        if (clip != null && audioSource != null) {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SnapToPoint(GameObject obj, Transform targetPoint) {
        if (obj != null && targetPoint != null) {
            obj.transform.position = targetPoint.position;
            obj.transform.rotation = targetPoint.rotation;
        }
    }

    private IEnumerator ProductionRoutine(GameObject truck) {
        isProducing = true; 
        ResetVisuals();

        // 1. ADIM: Odunlar Kamyondan İndi -> Masa Üstüne Oturur
        SnapToPoint(smallLogStack, workshopTableTop);
        SnapToPoint(largeLogStack, workshopTableTop);

        // Koliler çıkış noktasına (boxSpawnPoint, yoksa masa üstüne) oturur
        Transform targetBoxPoint = (boxSpawnPoint != null) ? boxSpawnPoint : workshopTableTop;
        SnapToPoint(normalBoxes, targetBoxPoint);
        SnapToPoint(largeBoxes, targetBoxPoint);

        if (smallLogStack) smallLogStack.SetActive(true);
        yield return new WaitForSeconds(unloadDuration * 0.5f);

        if (smallLogStack) smallLogStack.SetActive(false);
        if (largeLogStack) largeLogStack.SetActive(true);
        yield return new WaitForSeconds(unloadDuration * 0.5f);

        // 2. ADIM: Usta Çekiçle Üretim Yapar & Loading Halka Dolar
        if (workerAnimator) workerAnimator.SetBool("isCrafting", true);
        if (loadingRingCanvas) { 
            loadingRingCanvas.SetActive(true); 
            if (loadingRingProgress) loadingRingProgress.fillAmount = 0f; 
        }

        // Üretim süresince ritmik çekiç sesi çalar
        float elapsed = 0f;
        float sfxTimer = 0f;
        float hammerInterval = 0.54f; // 1.85 hızındaki animasyon vuruş ritmine göre çekiç sesi aralığı

        while (elapsed < craftingDuration) {
            elapsed += Time.deltaTime;
            sfxTimer += Time.deltaTime;

            if (sfxTimer >= hammerInterval) {
                PlaySFX(hammerStrikeSFX);
                sfxTimer = 0f;
            }

            if (loadingRingProgress) loadingRingProgress.fillAmount = Mathf.Clamp01(elapsed / craftingDuration);
            yield return null;
        }

        if (workerAnimator) workerAnimator.SetBool("isCrafting", false);
        if (loadingRingCanvas) loadingRingCanvas.SetActive(false);
        if (largeLogStack) largeLogStack.SetActive(false);

        // 3. ADIM: Usta Üretimi Bitirdi -> Atölye Çıkışında NORMAL BOX Belirir ve Başarı Sesi Çalar!
        PlaySFX(productionSuccessSFX);
        if (normalBoxes) normalBoxes.SetActive(true);
        yield return new WaitForSeconds(loadingDuration);

        // 4. ADIM: Atölyedeki Normal Kutu Kaybolur & Kamyonetin Kasasında LARGE BOX Belirip Araç Yola Çıkar!
        if (normalBoxes) normalBoxes.SetActive(false);

        if (largeBoxes != null) {
            Transform targetTruckCargo = (truckCargoPoint != null) ? truckCargoPoint : (truck != null ? truck.transform : null);
            SnapToPoint(largeBoxes, targetTruckCargo);
            largeBoxes.SetActive(true);
        } else if (truck != null) {
            Transform tc = truck.transform.Find("Cargo_Boxes"); 
            if (tc == null && truckCargoPoint != null) tc = truckCargoPoint;
            if (tc != null) tc.gameObject.SetActive(true);
        }

        if (truck != null) {
            MonoBehaviour rc = truck.GetComponent("TruckRouteController") as MonoBehaviour;
            if (rc != null) rc.SendMessage("OnWorkshopProcessComplete", SendMessageOptions.DontRequireReceiver);
        }

        isProducing = false;
    }
}
