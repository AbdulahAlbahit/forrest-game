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

    [Header("--- Odun Göstergesi (Sağ Üst) ---")]
    [Tooltip("Mevcut odun miktarını veya sepet doluluğunu gösteren metin.")]
    [SerializeField] private TextMeshProUGUI woodValueText;

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

        // Başlangıçta paneli gizle
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

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
        // Atölye Paneli kapalı olsa bile arka plan üretimini saniye saniye yürüt
        WorkshopPanelController.TickBackgroundProduction(Time.deltaTime);
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

        // Sepet / Odun event'i
        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged += UpdateWoodTextFromBasket;

        // Atölye kuyruğu event'i (varsa)
        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged += UpdateWoodTextFromWorkshop;

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
            BasketManager.Instance.OnBasketChanged -= UpdateWoodTextFromBasket;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged -= UpdateWoodTextFromWorkshop;

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

        if (WorkshopManager.Instance != null)
        {
            UpdateWoodTextFromWorkshop(WorkshopManager.Instance.MevcutKuyruk, WorkshopManager.Instance.GetWorkshopCapacity());
        }
        else if (BasketManager.Instance != null)
        {
            UpdateWoodTextFromBasket(BasketManager.Instance.MevcutDoluluk, BasketManager.Instance.MaksKapasite);
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

    private void UpdateWoodTextFromBasket(int current, int max)
    {
        if (woodValueText != null)
            woodValueText.text = $"{current} / {max}";
    }

    private void UpdateWoodTextFromWorkshop(int current, int max)
    {
        SetWoodTextDirect(current, max);
    }

    public void SetWoodTextDirect(int current, int max)
    {
        if (woodValueText != null)
            woodValueText.text = $"{current} / {max}";
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
