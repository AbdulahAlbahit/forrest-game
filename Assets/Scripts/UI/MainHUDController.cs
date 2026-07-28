using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;

/// <summary>
/// Ana oyun içi arayüzü (HUD) yöneten kontrolcü.
/// Bulut_UI sahnesindeki Para, XP, Oyuncu Seviyesi, Odun Miktarı ve Ayarlar Butonunu kontrol eder.
///
/// KULLANIM:
/// 1. Bulut_UI sahnesindeki CanvasUI üzerine bu scripti ekleyin.
/// 2. Inspector'dan ilgili TextMeshProUGUI ve Button referanslarını sürükleyin.
/// 3. Oyun içi event'leri (EconomyManager, XPManager, BasketManager/WorkshopManager) dinleyerek UI'ı canlı tutar.
/// </summary>
public class MainHUDController : MonoBehaviour
{
    public static MainHUDController Instance { get; private set; }

    [Header("--- Para Göstergesi (Sağ Üst) ---")]
    [Tooltip("Para miktarını gösteren metin.")]
    [SerializeField] private TextMeshProUGUI coinValueText;

    [Header("--- Sepet Odun Göstergesi ---")]
    [Tooltip("Sepetteki odun miktarını gösteren metin. Örn: 3/5")]
    [SerializeField] private TextMeshProUGUI woodValueText;

    [Header("--- Atölye Ham Odun Göstergesi ---")]
    [Tooltip("Atölyedeki ham odun miktarını gösteren metin. Örn: 10/25")]
    [SerializeField] private TextMeshProUGUI workshopWoodText;

    [Header("--- XP & Seviye Göstergesi (Sol Üst & Sol Alt) ---")]
    [Tooltip("Mevcut XP değerini gösteren metin (Sol Üst).")]
    [SerializeField] private TextMeshProUGUI xpValueText;

    [Tooltip("XP doluluk barı görseli (Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left olmalıdır).")]
    [SerializeField] private Image xpFillImage;

    [Tooltip("Oyuncu seviyesini gösteren metin (Sol Alt). Örn: Level 1")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("--- Ayarlar Paneli (Sağ Alt) ---")]
    [Tooltip("Sağ alttaki ayarlar açma butonu.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Sahnede yer alan veya prefab'ten koyulan SettingsPanel objesi.")]
    [SerializeField] private GameObject settingsPanel;

    [Header("--- Pause Menüsü ---")]
    [Tooltip("Oyunu duraklatma butonu.")]
    [SerializeField] private Button pauseButton;

    [Tooltip("PauseMenuController scripti içeren Pause paneli objesi.")]
    [SerializeField] private GameObject pausePanel;

    [Header("--- Atölye Paneli (HUD Butonu) ---")]
    [Tooltip("HUD üzerindeki Atölye açma butonu. Fiziksel atölyeye gitmeden paneli açar.")]
    [SerializeField] private Button workshopButton;

    [Tooltip("Sahnede yer alan WorkshopPanelController objesi.")]
    [SerializeField] private GameObject workshopPanel;

    [Header("--- Depoya Odun Aktarma Kısayol Butonu ---")]
    [Tooltip("Sepetteki odunları doğrudan atölye deposuna aktaran kısayol butonu.")]
    [SerializeField] private Button depositWoodButton;

    /// <summary>
    /// Sepetteki odunlar depoya aktarıldığında veya butona basıldığında tetiklenir.
    /// </summary>
    public event System.Action OnDepositWoodRequested;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX sesinin ismi.")]
    [SerializeField] private string buttonSfxName = "";

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Buton listener
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        // Pause butonu listener
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseButtonClicked);

        // Atölye butonu listener
        if (workshopButton != null)
            workshopButton.onClick.AddListener(OnWorkshopButtonClicked);

        // Depoya odun aktarma butonu listener
        if (depositWoodButton != null)
            depositWoodButton.onClick.AddListener(OnDepositWoodButtonClicked);

        // Başlangıçta panelleri gizle
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (workshopPanel != null)
            workshopPanel.SetActive(false);

        // Tüm UI değerlerini başlangıçta güncelle
        RefreshAllHUD();
    }

    private void OnEnable()
    {
        // Event dinleyicileri bağla
        SubscribeEvents();
        // Sahne aktif olduğunda verileri güncelle
        RefreshAllHUD();
    }

    private void OnDisable()
    {
        // Event dinleyicileri kaldır
        UnsubscribeEvents();
    }

    private void Update()
    {
    }

    // ═══════════════════════════════════════════
    //  Event Abonelikleri
    // ═══════════════════════════════════════════

    private void SubscribeEvents()
    {
        // Ekonomi event'i (Para değiştiğinde)
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += UpdateCoinText;

        // XP event'leri
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged += UpdateXPText;
            XPManager.Instance.OnLevelUp += UpdateLevelText;
        }

        // Sepet odun event'i
        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged += UpdateBasketWoodText;

        // Atölye ham odun event'i
        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged += UpdateWorkshopWoodText;

        // Bulut verisi yüklendiğinde
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
    }

    private void UnsubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= UpdateCoinText;

        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged -= UpdateXPText;
            XPManager.Instance.OnLevelUp -= UpdateLevelText;
        }

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged -= UpdateBasketWoodText;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged -= UpdateWorkshopWoodText;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
    }

    // ═══════════════════════════════════════════
    //  UI Güncelleme Metotları
    // ═══════════════════════════════════════════

    private void HandleDataLoaded(PlayerData data)
    {
        RefreshAllHUD();
    }

    /// <summary>
    /// Tüm HUD göstergelerini mevcut verilerle günceller.
    /// </summary>
    public void RefreshAllHUD()
    {
        if (EconomyManager.Instance != null)
            UpdateCoinText(EconomyManager.Instance.MevcutPara);

        if (XPManager.Instance != null)
        {
            UpdateXPText(XPManager.Instance.MevcutXP);
            UpdateLevelText(XPManager.Instance.MevcutSeviye);
        }

        // Sepet göstergesi
        if (BasketManager.Instance != null)
        {
            UpdateBasketWoodText(BasketManager.Instance.MevcutDoluluk, BasketManager.Instance.MaksKapasite);
        }

        // Atölye ham odun göstergesi
        if (WorkshopManager.Instance != null)
        {
            UpdateWorkshopWoodText(WorkshopManager.Instance.MevcutKuyruk, WorkshopManager.Instance.GetWorkshopCapacity());
        }
    }

    private void UpdateCoinText(int amount)
    {
        if (coinValueText != null)
            coinValueText.text = amount.ToString("N0"); // 1,000 şeklinde formatlı gösterim
    }

    private void UpdateXPText(int currentXP)
    {
        float progress = 0f;

        if (XPManager.Instance != null)
        {
            int reqXP = XPManager.Instance.GetRequiredXPForLevel(XPManager.Instance.MevcutSeviye);
            if (xpValueText != null)
                xpValueText.text = $"XP: {currentXP} / {reqXP}";

            progress = XPManager.Instance.GetXPProgress();
        }
        else
        {
            if (xpValueText != null)
                xpValueText.text = $"XP: {currentXP}";
        }

        // XP Bar dolum görselini güncelle (Image Type = Filled)
        if (xpFillImage != null)
            xpFillImage.fillAmount = progress;
    }

    private void UpdateLevelText(int level)
    {
        if (levelText != null)
            levelText.text = $"Level {level}";
    }

    private void UpdateBasketWoodText(int current, int max)
    {
        if (woodValueText != null)
            woodValueText.text = $"{current} / {max}";
    }

    private void UpdateWorkshopWoodText(int current, int max)
    {
        if (workshopWoodText != null)
            workshopWoodText.text = $"{current} / {max}";
    }

    public void SetWoodTextDirect(int current, int max)
    {
        if (workshopWoodText != null)
            workshopWoodText.text = $"{current} / {max}";
    }

    // ═══════════════════════════════════════════
    //  Ayarlar Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnSettingsButtonClicked()
    {
        PlayButtonClickSFX();

        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void OnPauseButtonClicked()
    {
        PlayButtonClickSFX();

        // PauseMenuController varsa toggle yap
        PauseMenuController pauseController = pausePanel != null 
            ? pausePanel.GetComponent<PauseMenuController>() 
            : PauseMenuController.Instance;

        if (pauseController != null)
        {
            pauseController.TogglePause();
        }
        else if (pausePanel != null)
        {
            // Fallback: PauseMenuController yoksa sadece paneli göster/gizle
            pausePanel.SetActive(!pausePanel.activeSelf);
        }
    }

    // ═══════════════════════════════════════════
    //  Atölye Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnWorkshopButtonClicked()
    {
        PlayButtonClickSFX();

        // WorkshopPanelController varsa onun OpenPanel metodunu kullan
        if (workshopPanel != null)
        {
            WorkshopPanelController controller = workshopPanel.GetComponent<WorkshopPanelController>();
            if (controller != null)
            {
                if (workshopPanel.activeSelf)
                    controller.ClosePanel();
                else
                    controller.OpenPanel();
                return;
            }

            // Fallback: WorkshopPanelController yoksa toggle yap
            workshopPanel.SetActive(!workshopPanel.activeSelf);
        }
        // Instance üzerinden de dene
        else if (WorkshopPanelController.Instance != null)
        {
            if (WorkshopPanelController.Instance.gameObject.activeSelf)
                WorkshopPanelController.Instance.ClosePanel();
            else
                WorkshopPanelController.Instance.OpenPanel();
        }
    }

    // ═══════════════════════════════════════════
    //  Depoya Odun Aktarma Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnDepositWoodButtonClicked()
    {
        PlayButtonClickSFX();

        OnDepositWoodRequested?.Invoke();

        // Varsayılan Akış: Sepetteki odunları boşaltıp atölyeye ekle
        if (BasketManager.Instance != null && WorkshopManager.Instance != null)
        {
            if (BasketManager.Instance.IsEmpty)
            {
                NotificationUIController.Instance?.ShowNotification("Sepetiniz boş!");
                return;
            }

            int amount = BasketManager.Instance.EmptyBasket();
            if (amount > 0)
            {
                WorkshopManager.Instance.AddWood(amount);
                NotificationUIController.Instance?.ShowNotification($"{amount} Odun depoya aktarıldı!");
            }
        }
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }

    // ═══════════════════════════════════════════
    //  Inspector Test Metotları (ContextMenu)
    // ═══════════════════════════════════════════

    [ContextMenu("TEST: +500 Para Ekle")]
    public void TestAddMoney()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(500);
            Debug.Log("[HUD Test] 500 Para eklendi.");
        }
        else
        {
            Debug.LogWarning("[HUD Test] EconomyManager bulunamadı! Sahneye/Manager'a ekli mi?");
        }
    }

    [ContextMenu("TEST: +100 XP Ekle (Level Up Testi)")]
    public void TestAddXP()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.AddXP(100);
            Debug.Log("[HUD Test] 100 XP eklendi.");
        }
        else
        {
            Debug.LogWarning("[HUD Test] XPManager bulunamadı!");
        }
    }

    [ContextMenu("TEST: +5 Odun Ekle")]
    public void TestAddWood()
    {
        if (BasketManager.Instance != null)
        {
            BasketManager.Instance.AddWood(5);
            Debug.Log("[HUD Test] 5 Odun sepete eklendi.");
        }
        else if (WorkshopManager.Instance != null)
        {
            WorkshopManager.Instance.AddWood(5);
            Debug.Log("[HUD Test] 5 Odun atölyeye eklendi.");
        }
        else
        {
            Debug.LogWarning("[HUD Test] BasketManager / WorkshopManager bulunamadı!");
        }
    }
}
