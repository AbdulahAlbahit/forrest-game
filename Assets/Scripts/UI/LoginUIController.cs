using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using ForrestGame.Core;

/// <summary>
/// Login ekraninin tum UI etkilesimlerini yoneten kontrolcu.
/// Inspector'dan buton ve panel referanslari atanarak kullanilir.
/// FirebaseAuthManager event'lerini dinleyerek giris akisini kontrol eder.
///
/// KULLANIM:
/// 1. Bu scripti sahneye ekle (ornegin UI_Controller objesine).
/// 2. Inspector'dan LoginPanel, LoadingOverlay, butonlar ve loading text'lerini surukleBirak.
/// 3. TargetSceneName alanina basarili giris sonrasi yuklenecek sahne adini yaz.
/// </summary>
public class LoginUIController : MonoBehaviour
{
    [Header("--- Panel Referanslari ---")]
    [Tooltip("Login butonlarini iceren ana panel.")]
    [SerializeField] private GameObject loginPanel;

    [Tooltip("Yukleme sirasinda gosterilecek overlay paneli.")]
    [SerializeField] private GameObject loadingOverlay;

    [Tooltip("Ayarlar paneli. SettingsPanelController scripti bu obje uzerine atanmalidir.")]
    [SerializeField] private GameObject settingsPanel;

    [Header("--- Butonlar ---")]
    [Tooltip("Misafir girisi butonu.")]
    [SerializeField] private Button guestLoginButton;

    [Tooltip("Google girisi butonu (suanlik deaktif).")]
    [SerializeField] private Button googleLoginButton;

    [Tooltip("Ayarlar butonunu acma butonu.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Ayarlar butonunu kapama butonu.")]
    [SerializeField] private Button settingsCloseButton;

    [Header("--- Loading Nokta Animasyonu ---")]
    [Tooltip("LoadingOverlay altindaki 'Loading' text objesi.")]
    [SerializeField] private GameObject loadingText0;

    [Tooltip("LoadingOverlay altindaki 'Loading.' text objesi.")]
    [SerializeField] private GameObject loadingText1;

    [Tooltip("LoadingOverlay altindaki 'Loading..' text objesi.")]
    [SerializeField] private GameObject loadingText2;

    [Tooltip("Nokta animasyonu gecis suresi (saniye).")]
    [SerializeField] private float dotAnimationInterval = 0.4f;

    [Header("--- Sahne Gecisi ---")]
    [Tooltip("Basarili giris sonrasi yuklenecek sahne adi. Inspector'dan degistirilebilir.")]
    [SerializeField] private string targetSceneName = "Bulut_UI";

    [Tooltip("Firebase giris icin maksimum bekleme suresi (saniye). Sure dolunca yine de sahneye gecer.")]
    [SerializeField] private float firebaseTimeout = 5f;

    [Header("--- Ses Ayarlari ---")]
    [Tooltip("Login ekraninda calacak arka plan muziginin ismi (AudioManager'daki Sound ismi). Bos kalirsa calmaz.")]
    [SerializeField] private string bgMusicName = "";

    [Tooltip("Butonlara basildiginda calacak SFX sesinin ismi (AudioManager'daki Sound ismi). Bos kalirsa calmaz.")]
    [SerializeField] private string buttonSfxName = "";

    private bool isTransitioning = false;
    private Coroutine dotAnimCoroutine;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Start()
    {
        // Baslangic durumlari
        SetPanelActive(loadingOverlay, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(loginPanel, true);

        // Google login suanlik deaktif
        if (googleLoginButton != null)
            googleLoginButton.interactable = false;

        // Buton listener'lari
        if (guestLoginButton != null)
            guestLoginButton.onClick.AddListener(OnGuestLoginClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsToggle);

        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(OnSettingsCloseToggle);

        // Firebase event aboneligi
        SubscribeToAuth();

        // Muzik baslat
        TryPlayBackgroundMusic();
    }

    private void TryPlayBackgroundMusic()
    {
        if (!string.IsNullOrEmpty(bgMusicName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(bgMusicName);
        }
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromAuth();
    }

    // ═══════════════════════════════════════════
    //  Firebase Event Yonetimi
    // ═══════════════════════════════════════════

    private void SubscribeToAuth()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
            FirebaseAuthManager.Instance.OnLoginFailed += HandleLoginFailed;
        }
        else
        {
            // FirebaseAuthManager henuz hazir degil, bekle
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (FirebaseAuthManager.Instance == null)
            yield return null;

        FirebaseAuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        FirebaseAuthManager.Instance.OnLoginFailed += HandleLoginFailed;
        Debug.Log("LoginUIController: FirebaseAuthManager'a abone olundu.");
    }

    private void UnsubscribeFromAuth()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
            FirebaseAuthManager.Instance.OnLoginFailed -= HandleLoginFailed;
        }
    }

    // ═══════════════════════════════════════════
    //  Buton Islemleri
    // ═══════════════════════════════════════════

    /// <summary>
    /// "Play as guest" butonuna basildiginda calisir.
    /// Firebase zaten otomatik anonim giris yapiyor.
    /// Eger giris tamamlanmissa direkt sahneye gecer,
    /// tamamlanmamissa belirli bir sure bekleyip yine de sahneye gecer.
    /// </summary>
    private void OnGuestLoginClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        PlayButtonClickSFX();

        Debug.Log("LoginUIController: Misafir girisi istendi.");

        // Loading overlay'i goster
        ShowLoadingOverlay(true);

        // Firebase zaten otomatik giris yapiyor.
        // Giris tamamlanmissa direkt sahneye gec.
        if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsLoggedIn)
        {
            Debug.Log("LoginUIController: Firebase zaten giris yapmis, sahne yukleniyor...");
            StartCoroutine(LoadTargetSceneAsync());
        }
        else
        {
            // Firebase henuz giris yapmamis, timeout ile bekle
            Debug.Log($"LoginUIController: Firebase girisi bekleniyor (max {firebaseTimeout}sn)...");
            StartCoroutine(WaitForLoginThenLoadScene());
        }
    }

    /// <summary>
    /// Firebase girisini belirli bir sure bekler.
    /// Sure dolmadan giris olursa HandleLoginSuccess sahneyi yukler.
    /// Sure dolarsa yine de sahneye gecer (Firebase arka planda devam eder).
    /// </summary>
    private IEnumerator WaitForLoginThenLoadScene()
    {
        float elapsed = 0f;

        while (elapsed < firebaseTimeout)
        {
            // Firebase giris yaptiysa direkt sahneye gec
            if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsLoggedIn)
            {
                Debug.Log("LoginUIController: Firebase girisi tamamlandi, sahne yukleniyor...");
                StartCoroutine(LoadTargetSceneAsync());
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Timeout doldu, yine de sahneye gec
        Debug.LogWarning($"LoginUIController: Firebase {firebaseTimeout}sn icinde giris yapamadi. Sahne yine de yukleniyor...");
        StartCoroutine(LoadTargetSceneAsync());
    }

    /// <summary>
    /// Ayarlar butonuna basildiginda settingsPanel'i acar/kapatir.
    /// </summary>
    private void OnSettingsToggle()
    {
        PlayButtonClickSFX();
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void OnSettingsCloseToggle()
    {
        PlayButtonClickSFX();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  Firebase Event Handler'lari
    // ═══════════════════════════════════════════

    private void HandleLoginSuccess()
    {
        Debug.Log("LoginUIController: Firebase giris basarili!");

        // WaitForLoginThenLoadScene zaten kontrol ediyor,
        // ama buton basilmadan once giris olduysa burasi calismaz (isTransitioning = false).
    }

    private void HandleLoginFailed(string errorMessage)
    {
        Debug.LogError($"LoginUIController: Giris basarisiz - {errorMessage}");

        // Firebase basarisiz olsa bile sahneye gecmeyi dene
        if (isTransitioning)
        {
            Debug.LogWarning("LoginUIController: Firebase basarisiz ama sahne yine de yukleniyor...");
            StartCoroutine(LoadTargetSceneAsync());
        }
    }

    // ═══════════════════════════════════════════
    //  Loading Overlay ve Nokta Animasyonu
    // ═══════════════════════════════════════════

    private void ShowLoadingOverlay(bool show)
    {
        SetPanelActive(loginPanel, !show);
        SetPanelActive(loadingOverlay, show);

        if (show)
        {
            // Nokta animasyonunu baslat
            if (dotAnimCoroutine != null)
                StopCoroutine(dotAnimCoroutine);
            dotAnimCoroutine = StartCoroutine(DotAnimationLoop());
        }
        else
        {
            // Nokta animasyonunu durdur
            if (dotAnimCoroutine != null)
            {
                StopCoroutine(dotAnimCoroutine);
                dotAnimCoroutine = null;
            }
        }
    }

    /// <summary>
    /// "Loading", "Loading.", "Loading.." textleri arasinda dongusel gecis yapar.
    /// Her seferinde sadece bir text aktif olur, nokta efekti olusturur.
    /// </summary>
    private IEnumerator DotAnimationLoop()
    {
        GameObject[] loadingTexts = { loadingText0, loadingText1, loadingText2 };
        int currentIndex = 0;

        while (true)
        {
            // Sadece aktif index'teki text'i goster
            for (int i = 0; i < loadingTexts.Length; i++)
            {
                if (loadingTexts[i] != null)
                    loadingTexts[i].SetActive(i == currentIndex);
            }

            yield return new WaitForSeconds(dotAnimationInterval);
            currentIndex = (currentIndex + 1) % loadingTexts.Length;
        }
    }

    // ═══════════════════════════════════════════
    //  Sahne Yukleme (Async)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Hedef sahneyi asenkron olarak yukler.
    /// Yukleme sirasinda loading overlay aktif kalir.
    /// </summary>
    private IEnumerator LoadTargetSceneAsync()
    {
        Debug.Log($"LoginUIController: '{targetSceneName}' sahnesi yukleniyor...");

        // Loading overlay'in gorunur oldugunu garantile
        ShowLoadingOverlay(true);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);

        if (asyncLoad == null)
        {
            Debug.LogError($"LoginUIController: '{targetSceneName}' sahnesi bulunamadi! Build Settings'e eklenmis mi?");
            isTransitioning = false;
            ShowLoadingOverlay(false);
            yield break;
        }

        // Sahne hazir olana kadar bekle (loading overlay gorunur kalir)
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Sahneyi aktiflestir
        Debug.Log("LoginUIController: Sahne hazir, gecis yapiliyor...");
        asyncLoad.allowSceneActivation = true;
    }

    // ═══════════════════════════════════════════
    //  Yardimci
    // ═══════════════════════════════════════════

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
