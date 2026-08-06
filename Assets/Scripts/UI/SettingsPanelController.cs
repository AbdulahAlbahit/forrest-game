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

    [Header("--- Toggle Metinleri (Opsiyonel) ---")]
    [Tooltip("Muzik butonunun text'i. Duruma gore 'Muzik: Acik/Kapali' yazar.")]
    [SerializeField] private TextMeshProUGUI musicToggleText;

    [Tooltip("SFX butonunun text'i. Duruma gore 'Ses Efektleri: Acik/Kapali' yazar.")]
    [SerializeField] private TextMeshProUGUI sfxToggleText;

    [Header("--- Toggle İkonları / Görselleri (2 Farklı Obje) ---")]
    [Tooltip("Müzik AÇIK olduğunda görünecek resim / obje.")]
    [SerializeField] private GameObject musicOnImage;

    [Tooltip("Müzik KAPALI olduğunda görünecek resim / obje.")]
    [SerializeField] private GameObject musicOffImage;

    [Tooltip("SFX AÇIK olduğunda görünecek resim / obje.")]
    [SerializeField] private GameObject sfxOnImage;

    [Tooltip("SFX KAPALI olduğunda görünecek resim / obje.")]
    [SerializeField] private GameObject sfxOffImage;

    [Header("--- Toggle İkonları / Görselleri (Tek Image Sprite Değiştirme) ---")]
    [Tooltip("Müzik butonunun varsayılan Image bileşeni.")]
    [SerializeField] private Image musicImage;
    [Tooltip("Müzik AÇIK iken gösterilecek Sprite.")]
    [SerializeField] private Sprite musicOnSprite;
    [Tooltip("Müzik KAPALI iken gösterilecek Sprite.")]
    [SerializeField] private Sprite musicOffSprite;

    [Tooltip("SFX butonunun varsayılan Image bileşeni.")]
    [SerializeField] private Image sfxImage;
    [Tooltip("SFX AÇIK iken gösterilecek Sprite.")]
    [SerializeField] private Sprite sfxOnSprite;
    [Tooltip("SFX KAPALI iken gösterilecek Sprite.")]
    [SerializeField] private Sprite sfxOffSprite;

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
    private ForrestGame.UI.UIPanelAnimator panelAnimator;

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName))
        {
            AudioManager audioMgr = GetAudioManager();
            if (audioMgr != null)
                audioMgr.PlaySFX(buttonSfxName);
        }
    }

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        panelAnimator = GetComponent<ForrestGame.UI.UIPanelAnimator>();

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

        if (panelAnimator != null)
        {
            panelAnimator.Show();
        }
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

        if (panelAnimator != null)
        {
            panelAnimator.Hide(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ═══════════════════════════════════════════
    //  Yardimci Metodlar
    // ═══════════════════════════════════════════

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedAudioSettingsOnBoot()
    {
        bool musicOn = PlayerPrefs.GetInt(MUSIC_PREF_KEY, 1) == 1;
        bool sfxOn = PlayerPrefs.GetInt(SFX_PREF_KEY, 1) == 1;

        AudioManager audioMgr = AudioManager.Instance != null 
            ? AudioManager.Instance 
            : Object.FindFirstObjectByType<AudioManager>();

        if (audioMgr != null)
        {
            if (audioMgr.musicSource != null)
                audioMgr.musicSource.mute = !musicOn;

            if (audioMgr.sfxSource != null)
                audioMgr.sfxSource.mute = !sfxOn;

            if (audioMgr.ambienceSource != null)
                audioMgr.ambienceSource.mute = !sfxOn;

            if (audioMgr.vehicleSource != null)
                audioMgr.vehicleSource.mute = !sfxOn;
        }
    }

    /// <summary>
    /// Panel her aktif olduğunda ses ayarlarını kontrol eder ve uygular.
    /// </summary>
    private void ApplyAudioSettings()
    {
        ApplyMusicSetting();
        ApplySFXSetting();
    }

    private AudioManager GetAudioManager()
    {
        if (AudioManager.Instance != null) return AudioManager.Instance;
        return FindFirstObjectByType<AudioManager>();
    }

    /// <summary>
    /// Muzik ayarini AudioManager'daki musicSource'a uygular.
    /// </summary>
    private void ApplyMusicSetting()
    {
        AudioManager audioMgr = GetAudioManager();
        if (audioMgr != null && audioMgr.musicSource != null)
        {
            audioMgr.musicSource.mute = !isMusicOn;
        }
    }

    /// <summary>
    /// SFX ayarini AudioManager'daki sfxSource, ambienceSource ve vehicleSource'a uygular.
    /// "Cevre sesi" olarak SFX, ambiyans ve arac seslerinin tumu kontrol edilir.
    /// </summary>
    private void ApplySFXSetting()
    {
        AudioManager audioMgr = GetAudioManager();
        if (audioMgr == null) return;

        if (audioMgr.sfxSource != null)
            audioMgr.sfxSource.mute = !isSfxOn;

        if (audioMgr.ambienceSource != null)
            audioMgr.ambienceSource.mute = !isSfxOn;

        if (audioMgr.vehicleSource != null)
            audioMgr.vehicleSource.mute = !isSfxOn;
    }

    /// <summary>
    /// Toggle butonlarının metinlerini, ikonlarını ve görsellerini mevcut duruma göre günceller.
    /// </summary>
    private void UpdateToggleTexts()
    {
        // 1. Text Güncellemesi (Opsiyonel)
        if (musicToggleText != null)
            musicToggleText.text = isMusicOn ? "Music: On" : "Music: Off";

        if (sfxToggleText != null)
            sfxToggleText.text = isSfxOn ? "SFX: On" : "SFX: Off";

        // 2. Müzik 2 Farklı Resim / Obje Göster / Gizle
        if (musicOnImage != null)
            musicOnImage.SetActive(isMusicOn);

        if (musicOffImage != null)
            musicOffImage.SetActive(!isMusicOn);

        // Müzik Tek Image için Sprite Değiştirme
        if (musicImage != null)
        {
            if (isMusicOn && musicOnSprite != null) musicImage.sprite = musicOnSprite;
            else if (!isMusicOn && musicOffSprite != null) musicImage.sprite = musicOffSprite;
        }

        // 3. SFX 2 Farklı Resim / Obje Göster / Gizle
        if (sfxOnImage != null)
            sfxOnImage.SetActive(isSfxOn);

        if (sfxOffImage != null)
            sfxOffImage.SetActive(!isSfxOn);

        // SFX Tek Image için Sprite Değiştirme
        if (sfxImage != null)
        {
            if (isSfxOn && sfxOnSprite != null) sfxImage.sprite = sfxOnSprite;
            else if (!isSfxOn && sfxOffSprite != null) sfxImage.sprite = sfxOffSprite;
        }
    }
}
