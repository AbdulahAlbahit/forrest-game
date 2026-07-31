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

    [Tooltip("Sepet dolum görseli (Image Type = Filled). Boşsa sadece metin güncellenir.")]
    [SerializeField] private Image basketFillImage;

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

    [Tooltip("Sol alttaki seviye göstergesi açma butonu (İstatistik panelini açar).")]
    [SerializeField] private Button levelButton;

    [Tooltip("CharacterStatsPanelController scripti içeren İstatistik paneli objesi.")]
    [SerializeField] private GameObject statsPanel;

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

    // Dahili animasyon durumları
    private int currentDisplayedCoins = -1;
    private int lastObservedLevel = -1;
    private float currentAnimTargetFill = -1f;
    private Coroutine coinRollCoroutine;
    private Coroutine coinPulseCoroutine;
    private Coroutine levelPulseCoroutine;
    private Coroutine basketPulseCoroutine;
    private Coroutine xpFillCoroutine;
    private float targetXpFill = 0f;
    private float targetBasketFill = 0f;
    private System.Collections.Generic.Dictionary<Transform, Vector3> initialScaleMap = new System.Collections.Generic.Dictionary<Transform, Vector3>();

    private Vector3 GetOriginalScale(Transform t)
    {
        if (t == null) return Vector3.one;
        if (!initialScaleMap.TryGetValue(t, out Vector3 scale))
        {
            scale = t.localScale;
            initialScaleMap[t] = scale;
        }
        return scale;
    }

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Buton listener'ları
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseButtonClicked);

        if (workshopButton != null)
            workshopButton.onClick.AddListener(OnWorkshopButtonClicked);

        if (depositWoodButton != null)
            depositWoodButton.onClick.AddListener(OnDepositWoodButtonClicked);

        if (levelButton != null)
            levelButton.onClick.AddListener(OnLevelButtonClicked);

        // Başlangıçta panelleri gizle
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (workshopPanel != null)
            workshopPanel.SetActive(false);
        if (statsPanel != null)
            statsPanel.SetActive(false);

        // Tüm UI değerlerini başlangıçta güncelle
        RefreshAllHUD();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        RefreshAllHUD();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        // Sepet Bar Yumuşak Lerp Dolumu
        if (basketFillImage != null && Mathf.Abs(basketFillImage.fillAmount - targetBasketFill) > 0.001f)
        {
            basketFillImage.fillAmount = Mathf.MoveTowards(basketFillImage.fillAmount, targetBasketFill, Time.unscaledDeltaTime * 2.5f);
        }
    }

    // ═══════════════════════════════════════════
    //  Event Abonelikleri
    // ═══════════════════════════════════════════

    private void SubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += UpdateCoinText;

        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged += UpdateXPText;
            XPManager.Instance.OnLevelUp += UpdateLevelText;
        }

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged += UpdateBasketWoodText;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged += UpdateWorkshopWoodText;

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
            lastObservedLevel = XPManager.Instance.MevcutSeviye;
        }

        if (BasketManager.Instance != null)
        {
            UpdateBasketWoodText(BasketManager.Instance.MevcutDoluluk, BasketManager.Instance.MaksKapasite);
        }

        if (WorkshopManager.Instance != null)
        {
            UpdateWorkshopWoodText(WorkshopManager.Instance.MevcutKuyruk, WorkshopManager.Instance.GetWorkshopCapacity());
        }
    }

    private void UpdateCoinText(int targetAmount)
    {
        if (coinValueText == null) return;

        if (currentDisplayedCoins < 0)
        {
            currentDisplayedCoins = targetAmount;
            coinValueText.text = targetAmount.ToString("N0");
            return;
        }

        if (coinRollCoroutine != null)
            StopCoroutine(coinRollCoroutine);

        coinRollCoroutine = StartCoroutine(CoinRollRoutine(targetAmount));

        if (targetAmount > currentDisplayedCoins)
        {
            TriggerPulse(coinValueText.transform, ref coinPulseCoroutine, 1.18f);
        }
    }

    private System.Collections.IEnumerator CoinRollRoutine(int targetAmount)
    {
        int startAmount = currentDisplayedCoins;
        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            currentDisplayedCoins = Mathf.RoundToInt(Mathf.Lerp(startAmount, targetAmount, t));
            coinValueText.text = currentDisplayedCoins.ToString("N0");
            yield return null;
        }

        currentDisplayedCoins = targetAmount;
        coinValueText.text = targetAmount.ToString("N0");
        coinRollCoroutine = null;
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

        float targetFill = Mathf.Clamp01(progress);
        if (xpFillImage != null)
        {
            if (!gameObject.activeInHierarchy)
            {
                xpFillImage.fillAmount = targetFill;
                currentAnimTargetFill = targetFill;
            }
            else
            {
                // Eğer zaten aynı hedefe doğru animasyon yapıyorsak (Örn: SaveData event'i tetiklendiğinde), coroutine'i kesme!
                if (xpFillCoroutine != null && Mathf.Approximately(currentAnimTargetFill, targetFill))
                {
                    return;
                }

                if (xpFillCoroutine != null)
                    StopCoroutine(xpFillCoroutine);

                currentAnimTargetFill = targetFill;
                xpFillCoroutine = StartCoroutine(XPFillRoutine(targetFill));
            }
        }
    }

    private System.Collections.IEnumerator XPFillRoutine(float newTargetFill)
    {
        if (xpFillImage == null) yield break;

        float startFill = xpFillImage.fillAmount;
        float animDuration = 0.65f;
        float elapsed = 0f;

        // Level Up durumu (yeni hedef başlangıçtan belirgin şekilde küçükse)
        if (newTargetFill < startFill - 0.05f)
        {
            float fillUpDuration = animDuration * 0.5f;
            while (elapsed < fillUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fillUpDuration);
                t = t * (2f - t); // EaseOutQuad
                xpFillImage.fillAmount = Mathf.Lerp(startFill, 1.0f, t);
                yield return null;
            }

            xpFillImage.fillAmount = 0f;
            startFill = 0f;
            elapsed = 0f;

            while (elapsed < fillUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fillUpDuration);
                t = t * (2f - t);
                xpFillImage.fillAmount = Mathf.Lerp(0f, newTargetFill, t);
                yield return null;
            }
        }
        else
        {
            // Normal XP kazanımı: 0.65 saniyede akıcı EaseOut dolum
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);
                t = t * (2f - t);
                xpFillImage.fillAmount = Mathf.Lerp(startFill, newTargetFill, t);
                yield return null;
            }
        }

        xpFillImage.fillAmount = newTargetFill;
        xpFillCoroutine = null;
    }

    private void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {level}";
            if (lastObservedLevel > 0 && level > lastObservedLevel)
            {
                TriggerPulse(levelText.transform, ref levelPulseCoroutine, 1.25f);
            }
            lastObservedLevel = level;
        }
    }

    private void UpdateBasketWoodText(int current, int max)
    {
        if (woodValueText != null)
            woodValueText.text = $"{current} / {max}";

        targetBasketFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        if (basketFillImage != null && !gameObject.activeInHierarchy)
        {
            basketFillImage.fillAmount = targetBasketFill;
        }

        bool isFull = max > 0 && current >= max;
        if (woodValueText != null)
        {
            woodValueText.color = isFull ? new Color(1f, 0.4f, 0.2f, 1f) : Color.white;
            if (isFull)
            {
                TriggerPulse(woodValueText.transform, ref basketPulseCoroutine, 1.15f);
            }
        }
    }

    private void TriggerPulse(Transform targetTransform, ref Coroutine coroutineTracker, float targetScaleFactor)
    {
        if (targetTransform == null || !gameObject.activeInHierarchy) return;

        if (coroutineTracker != null)
            StopCoroutine(coroutineTracker);

        coroutineTracker = StartCoroutine(PulseRoutine(targetTransform, targetScaleFactor));
    }

    private System.Collections.IEnumerator PulseRoutine(Transform targetTransform, float targetScaleFactor)
    {
        Vector3 origScale = GetOriginalScale(targetTransform);
        Vector3 maxScale = origScale * targetScaleFactor;
        float halfDuration = 0.12f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            targetTransform.localScale = Vector3.Lerp(origScale, maxScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            targetTransform.localScale = Vector3.Lerp(maxScale, origScale, t);
            yield return null;
        }

        targetTransform.localScale = origScale;
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

        PauseMenuController pauseController = pausePanel != null 
            ? pausePanel.GetComponent<PauseMenuController>() 
            : PauseMenuController.Instance;

        if (pauseController != null)
        {
            pauseController.TogglePause();
        }
        else if (pausePanel != null)
        {
            pausePanel.SetActive(!pausePanel.activeSelf);
        }
    }

    // ═══════════════════════════════════════════
    //  İstatistik Paneli Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnLevelButtonClicked()
    {
        PlayButtonClickSFX();

        if (CharacterStatsPanelController.Instance != null)
        {
            CharacterStatsPanelController.Instance.TogglePanel();
        }
        else if (statsPanel != null)
        {
            CharacterStatsPanelController controller = statsPanel.GetComponent<CharacterStatsPanelController>();
            if (controller != null)
                controller.TogglePanel();
            else
                statsPanel.SetActive(!statsPanel.activeSelf);
        }
    }

    // ═══════════════════════════════════════════
    //  Atölye Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnWorkshopButtonClicked()
    {
        PlayButtonClickSFX();

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

            workshopPanel.SetActive(!workshopPanel.activeSelf);
        }
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
