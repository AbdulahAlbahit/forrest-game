using System.Collections;
using UnityEngine;

/// <summary>
/// Yükleme ekranı (Loading Overlay) bağımsız kontrolcüsü.
/// Bu scripti doğrudan 'LoadingOverlay' paneli objesine ekleyin.
/// Böylece bu paneli istediğiniz sahnede tek başına Prefab olarak kullanabilirsiniz.
///
/// KULLANIM:
/// 1. LoadingOverlay objesine bu scripti ekleyin.
/// 2. Inspector'dan loadingText0, loadingText1, loadingText2 objelerini bağlayın.
/// 3. Obje aktif olduğunda (SetActive(true)) nokta animasyonu otomatik başlar,
///    kapandığında (SetActive(false)) otomatik durur.
/// </summary>
public class LoadingOverlayController : MonoBehaviour
{
    public static LoadingOverlayController Instance { get; private set; }

    [Header("--- Nokta Animasyonu Text Objeleri ---")]
    [Tooltip("Noktasız metin ('Loading')")]
    [SerializeField] private GameObject loadingText0;

    [Tooltip("Tek noktalı metin ('Loading.')")]
    [SerializeField] private GameObject loadingText1;

    [Tooltip("İki noktalı metin ('Loading..')")]
    [SerializeField] private GameObject loadingText2;

    [Header("--- Animasyon Ayarları ---")]
    [Tooltip("Noktalar arası geçiş süresi (saniye).")]
    [SerializeField] private float dotAnimationInterval = 0.4f;

    private Coroutine dotAnimCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Sahne bazlı tekil kalması için
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Obje ekranda görünür olduğunda animasyonu otomatik başlat
        StartAnimation();
    }

    private void OnDisable()
    {
        // Obje gizlendiğinde animasyonu durdur
        StopAnimation();
    }

    public void ShowLoading(bool show)
    {
        gameObject.SetActive(show);
    }

    public void StartAnimation()
    {
        StopAnimation();
        dotAnimCoroutine = StartCoroutine(DotAnimationLoop());
    }

    public void StopAnimation()
    {
        if (dotAnimCoroutine != null)
        {
            StopCoroutine(dotAnimCoroutine);
            dotAnimCoroutine = null;
        }
    }

    private IEnumerator DotAnimationLoop()
    {
        GameObject[] loadingTexts = { loadingText0, loadingText1, loadingText2 };
        int currentIndex = 0;

        while (true)
        {
            for (int i = 0; i < loadingTexts.Length; i++)
            {
                if (loadingTexts[i] != null)
                    loadingTexts[i].SetActive(i == currentIndex);
            }

            yield return new WaitForSecondsRealtime(dotAnimationInterval);
            currentIndex = (currentIndex + 1) % loadingTexts.Length;
        }
    }
}
