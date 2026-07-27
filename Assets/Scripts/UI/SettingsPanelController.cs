using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ForrestGame.Core;

/// <summary>
/// Ayarlar panelinin kontrolcusu.
/// Muzik ve SFX acma/kapama, cikis yapma ve versiyon gosterme islerini yonetir.
/// Ayarlar PlayerPrefs'e kaydedilir ve sahne degisse bile korunur.
///
/// KULLANIM:
/// 1. Bu scripti Settings paneli objesine ekle.
/// 2. Inspector'dan buton ve text referanslarini surukle-birak.
/// 3. Muzik/SFX toggle butonlarina basinca otomatik PlayerPrefs'e kaydeder.
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [Header("--- Toggle Butonlari ---")]
    [Tooltip("Muzik acma/kapama butonu.")]
    [SerializeField] private Button musicToggleButton;

    [Tooltip("Ses efektleri acma/kapama butonu.")]
    [SerializeField] private Button sfxToggleButton;

    [Header("--- Toggle Metinleri ---")]
    [Tooltip("Muzik butonunun text'i. Duruma gore 'Muzik: Acik/Kapali' yazar.")]
    [SerializeField] private TextMeshProUGUI musicToggleText;

    [Tooltip("SFX butonunun text'i. Duruma gore 'Ses Efektleri: Acik/Kapali' yazar.")]
    [SerializeField] private TextMeshProUGUI sfxToggleText;

    [Header("--- Diger ---")]
    [Tooltip("Ayarlar panelini kapatan buton.")]
    [SerializeField] private Button closeButton;

    [Tooltip("Oyun versiyonunu gosteren text. Otomatik olarak Application.version'dan doldurulur.")]
    [SerializeField] private TextMeshProUGUI versionText;
    
    [Header("--- Ses Ayarlari ---")]
    [Tooltip("Butonlara basildiginda calacak SFX sesinin ismi (AudioManager'daki Sound ismi). Bos kalirsa calmaz.")]
    [SerializeField] private string buttonSfxName = "";

    // PlayerPrefs anahtarlari
    private const string MUSIC_PREF_KEY = "Settings_MusicOn";
    private const string SFX_PREF_KEY = "Settings_SFXOn";

    private bool isMusicOn = true;
    private bool isSfxOn = true;

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        // Ilk calismada varsayilan olarak sesleri ACIK (1) yap
        if (!PlayerPrefs.HasKey(MUSIC_PREF_KEY))
        {
            PlayerPrefs.SetInt(MUSIC_PREF_KEY, 1);
            PlayerPrefs.Save();
        }

        if (!PlayerPrefs.HasKey(SFX_PREF_KEY))
        {
            PlayerPrefs.SetInt(SFX_PREF_KEY, 1);
            PlayerPrefs.Save();
        }

        // Kayitli ayarlari yukle
        isMusicOn = PlayerPrefs.GetInt(MUSIC_PREF_KEY, 1) == 1;
        isSfxOn = PlayerPrefs.GetInt(SFX_PREF_KEY, 1) == 1;
    }

    private void Start()
    {
        // Buton listener'lari
        if (musicToggleButton != null)
            musicToggleButton.onClick.AddListener(ToggleMusic);

        if (sfxToggleButton != null)
            sfxToggleButton.onClick.AddListener(ToggleSFX);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // Versiyon metnini ayarla
        if (versionText != null)
            versionText.text = $"v{Application.version}";

        // Kayitli ayarlari uygula ve buton metinlerini guncelle
        ApplyAudioSettings();
        UpdateToggleTexts();
    }

    /// <summary>
    /// Panel her aktif olduğunda audio ayarlarını uygular.
    /// Boylece AudioManager sonradan sahnede olusturulsa bile ayarlar uygulanir.
    /// </summary>
    private void OnEnable()
    {
        ApplyAudioSettings();
        UpdateToggleTexts();
    }

    // ═══════════════════════════════════════════
    //  Muzik Kontrolu
    // ═══════════════════════════════════════════

    private void ToggleMusic()
    {
        PlayButtonClickSFX();
        isMusicOn = !isMusicOn;

        // PlayerPrefs'e kaydet
        PlayerPrefs.SetInt(MUSIC_PREF_KEY, isMusicOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMusicSetting();
        UpdateToggleTexts();

        Debug.Log($"SettingsPanelController: Muzik {(isMusicOn ? "acildi" : "kapatildi")}");
    }

    // ═══════════════════════════════════════════
    //  SFX Kontrolu
    // ═══════════════════════════════════════════

    private void ToggleSFX()
    {
        PlayButtonClickSFX();
        isSfxOn = !isSfxOn;

        // PlayerPrefs'e kaydet
        PlayerPrefs.SetInt(SFX_PREF_KEY, isSfxOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplySFXSetting();
        UpdateToggleTexts();

        Debug.Log($"SettingsPanelController: SFX {(isSfxOn ? "acildi" : "kapatildi")}");
    }



    // ═══════════════════════════════════════════
    //  Panel Kapat
    // ═══════════════════════════════════════════

    /// <summary>
    /// Ayarlar panelini kapatir. LoginUIController tekrar acabilir.
    /// </summary>
    private void ClosePanel()
    {
        PlayButtonClickSFX();
        gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  Yardimci Metodlar
    // ═══════════════════════════════════════════

    /// <summary>
    /// Hem muzik hem SFX ayarlarini AudioManager'a uygular.
    /// AudioManager sahnede yoksa sessizce atlar.
    /// </summary>
    private void ApplyAudioSettings()
    {
        ApplyMusicSetting();
        ApplySFXSetting();
    }

    /// <summary>
    /// Muzik ayarini AudioManager'daki musicSource'a uygular.
    /// </summary>
    private void ApplyMusicSetting()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.musicSource != null)
        {
            AudioManager.Instance.musicSource.mute = !isMusicOn;
        }
    }

    /// <summary>
    /// SFX ayarini AudioManager'daki sfxSource, ambienceSource ve vehicleSource'a uygular.
    /// "Cevre sesi" olarak SFX, ambiyans ve arac seslerinin tumu kontrol edilir.
    /// </summary>
    private void ApplySFXSetting()
    {
        if (AudioManager.Instance == null) return;

        if (AudioManager.Instance.sfxSource != null)
            AudioManager.Instance.sfxSource.mute = !isSfxOn;

        if (AudioManager.Instance.ambienceSource != null)
            AudioManager.Instance.ambienceSource.mute = !isSfxOn;

        if (AudioManager.Instance.vehicleSource != null)
            AudioManager.Instance.vehicleSource.mute = !isSfxOn;
    }

    /// <summary>
    /// Toggle butonlarinin metinlerini mevcut duruma gore gunceller.
    /// </summary>
    private void UpdateToggleTexts()
    {
        if (musicToggleText != null)
            musicToggleText.text = isMusicOn ? "Music: On" : "Music: Off";

        if (sfxToggleText != null)
            sfxToggleText.text = isSfxOn ? "SFX: On" : "SFX: Off";
    }
}
