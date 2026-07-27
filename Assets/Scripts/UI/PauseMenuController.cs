using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ForrestGame.Core;

/// <summary>
/// Oyunu durduran (Time.timeScale = 0) ve devam ettiren Pause Menüsü kontrolcüsü.
/// Pause aktifken tüm Time.deltaTime bağımlı sistemler (atölye üretimi dahil) otomatik durur.
///
/// KULLANIM:
/// 1. CanvasUI altına bir PausePanel (karartmalı arka plan + butonlar) ekleyin.
/// 2. Bu scripti PausePanel objesine veya ayrı bir objeye ekleyin.
/// 3. Inspector'dan buton ve panel referanslarını sürükleyin.
/// 4. MainHUDController'daki Pause butonunu bu scriptteki TogglePause() metoduna bağlayın.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("--- UI Referansları ---")]
    [Tooltip("Pause menüsü ana paneli (karartmalı arka plan dahil).")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Oyuna devam etme butonu.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Oyundan çıkış butonu.")]
    [SerializeField] private Button quitButton;

    [Header("--- Panel İçerik Metinleri ---")]
    [Tooltip("Panel başlığı metni (ör: 'Oyun Duraklatıldı').")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    [Header("--- Sahne Ayarları ---")]
    [Tooltip("Çıkış yapıldığında dönülecek sahne adı. Boş bırakılırsa Application.Quit() çağrılır.")]
    [SerializeField] private string exitSceneName = "";

    // Dahili durum
    private bool isPaused = false;

    /// <summary>
    /// Oyunun şu an duraklatılmış olup olmadığını döndürür.
    /// </summary>
    public bool IsPaused => isPaused;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Buton listener'ları bağla
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Başlangıçta paneli sadece oyun duraklatılmamışsa gizle
        // (İlk tıkta PauseGame() çağrıldığında Start() çalışıp paneli tekrar kapatmasın)
        if (pausePanel != null && !isPaused)
            pausePanel.SetActive(false);

        // Başlık metnini ayarla
        if (titleText != null)
            titleText.text = "Oyun Duraklatıldı";
    }

    // ═══════════════════════════════════════════
    //  Pause / Resume İşlemleri
    // ═══════════════════════════════════════════

    /// <summary>
    /// Oyunu duraklatır. Time.timeScale = 0 yaparak tüm deltaTime bağımlı sistemleri durdurur.
    /// Atölye üretimi (WorkshopManager, WorkshopPanelController) Time.deltaTime kullandığı için
    /// otomatik olarak durur.
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;

        PlayButtonClickSFX();

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Debug.Log("[PauseMenu] Oyun duraklatıldı. Time.timeScale = 0");
    }

    /// <summary>
    /// Oyunu devam ettirir. Time.timeScale = 1 yaparak tüm sistemleri tekrar başlatır.
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        PlayButtonClickSFX();

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Debug.Log("[PauseMenu] Oyun devam ediyor. Time.timeScale = 1");
    }

    /// <summary>
    /// Pause durumunu tersine çevirir (toggle). HUD'daki Pause butonundan çağrılır.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Oyundan çıkış yapar. exitSceneName belirtilmişse o sahneye döner,
    /// yoksa uygulamayı tamamen kapatır.
    /// </summary>
    public void QuitGame()
    {
        PlayButtonClickSFX();

        // Önce timeScale'i normale döndür (sahne geçişlerinde sorun çıkmasın)
        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log("[PauseMenu] Oyundan çıkış yapılıyor...");

        // Belirtilen sahneye dön
        if (!string.IsNullOrEmpty(exitSceneName))
        {
            SceneManager.LoadScene(exitSceneName);
            return;
        }

        // Uygulama çıkışı
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ═══════════════════════════════════════════
    //  Ses
    // ═══════════════════════════════════════════

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }
}
