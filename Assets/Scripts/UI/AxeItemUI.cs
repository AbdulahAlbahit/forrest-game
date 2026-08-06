using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Balta Masası Paneli (AxeTablePanel) içindeki tek bir balta seviye kartını yönetir.
/// Mobil cihazlar için hem aksiyon butonundan hem de kartın HERHANGİ bir yerine tıklanarak etkileşime geçilmesini sağlar.
/// </summary>
public class AxeItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Kartın tamamına tıklamayı sağlayan buton (Opsiyonel: Kök objede Button varsa otomatik alınır).")]
    [SerializeField] private Button cardButton;

    [Tooltip("Balta ikonu.")]
    [SerializeField] private Image axeIcon;

    [Tooltip("Balta kademe adı metni (Örn: 'Seviye 2 Balta').")]
    [SerializeField] private TextMeshProUGUI axeNameText;

    [Tooltip("Balta güç / hasar statüsü metni (Örn: 'Güç: 15').")]
    [SerializeField] private TextMeshProUGUI axeStatText;

    [Header("--- Aksiyon Butonu ---")]
    [Tooltip("Kuşanma / Yükseltme aksiyon butonu.")]
    [SerializeField] private Button actionButton;

    [Tooltip("Buton üzerindeki metin (Örn: 'Kullanılıyor', 'Kuşan', '75 Altın').")]
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [Header("--- Durum Görselleri ---")]
    [Tooltip("Seçili / Kullanılıyor vurgu çerçevesi.")]
    [SerializeField] private GameObject equippedHighlight;

    [Tooltip("Kilitli (Henüz ulaşılmamış seviye) overlay paneli.")]
    [SerializeField] private GameObject lockOverlay;

    private System.Action onClickCallback;
    private bool isInteractableState = false;
    private float lastClickTime = 0f;

    // ─── Animasyon State ───
    private bool previouslyEquipped = false;
    private Coroutine equipAnimCoroutine;

    private void Awake()
    {
        // Kök objede Button varsa al
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
        }

        // Eğer actionButton ayrı bir objeyse ve cardButton ile aynı değilse ona da bağla
        if (actionButton != null && actionButton != cardButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnCardClicked);
        }
    }

    /// <summary>
    /// Kartı verilen kademe verisi ve oyuncunun UpgradeManager seviyesiyle doldurur.
    /// </summary>
    public void Setup(
        int tierLevel,
        string tierName,
        Sprite icon,
        float statValue,
        int upgradeCost,
        bool isUnlocked,
        bool isEquipped,
        bool isNextUpgrade,
        int currentGold,
        System.Action onClick)
    {
        onClickCallback = onClick;

        if (axeNameText != null)
            axeNameText.text = string.IsNullOrEmpty(tierName) ? $"Seviye {tierLevel} Balta" : tierName;

        if (axeIcon != null && icon != null)
        {
            axeIcon.sprite = icon;
            axeIcon.gameObject.SetActive(true);
        }

        if (axeStatText != null)
            axeStatText.text = $"Güç: {statValue:F0}";

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!isUnlocked && !isNextUpgrade);
        }

        if (equippedHighlight != null)
        {
            equippedHighlight.SetActive(isEquipped);
        }

        // ─── Kuşanma Animasyonu ───
        if (isEquipped && !previouslyEquipped)
            TriggerEquipAnimation();
        previouslyEquipped = isEquipped;

        // Etkileşim durumu belirleme
        if (isEquipped)
        {
            isInteractableState = false;
            SetButtonText("Kullanılıyor", false);
        }
        else if (isUnlocked)
        {
            // Kazanılmış seviye -> Kuşanılabilir
            isInteractableState = true;
            SetButtonText("Kuşan", true);
        }
        else if (isNextUpgrade)
        {
            // Bir sonraki seviye -> Satın al / Yükselt
            bool canAfford = currentGold >= upgradeCost;
            isInteractableState = canAfford;
            string text = upgradeCost > 0 ? $"Yükselt ({upgradeCost} Altın)" : "Ücretsiz";
            SetButtonText(text, canAfford);
        }
        else
        {
            // İleriki kilitli seviyeler
            isInteractableState = false;
            SetButtonText("Kilitli", false);
        }

        // Kök kart butonunu da aktif/pasif tut
        if (cardButton != null)
        {
            cardButton.interactable = isInteractableState;
        }
    }

    /// <summary>
    /// Beş bağımsız balta için satın alma/kuşanma kartını doldurur.
    /// Eski sıralı seviye Setup API'si UI uyumluluğu için korunur.
    /// </summary>
    public void SetupIndependent(
        int listIndex,
        string axeName,
        Sprite icon,
        float statValue,
        int currentLevel,
        bool isOwned,
        bool isEquipped,
        int purchasePrice,
        System.Action onAction)
    {
        onClickCallback = onAction;

        if (axeNameText != null)
            axeNameText.text = string.IsNullOrWhiteSpace(axeName) ? $"Balta {listIndex}" : axeName;

        if (axeIcon != null)
        {
            axeIcon.sprite = icon;
            axeIcon.gameObject.SetActive(icon != null);
        }

        if (axeStatText != null)
            axeStatText.text = $"Güç: {statValue:F0}  |  Seviye: {currentLevel}";

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!isOwned);
            foreach (Graphic graphic in lockOverlay.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        if (equippedHighlight != null)
            equippedHighlight.SetActive(isEquipped);

        // ─── Kuşanma Animasyonu ───
        if (isEquipped && !previouslyEquipped)
            TriggerEquipAnimation();
        previouslyEquipped = isEquipped;

        isInteractableState = !isEquipped;

        if (cardButton != null)
            cardButton.interactable = true;

        string actionText = !isOwned
            ? $"Satın Al\n{purchasePrice:N0} Para"
            : isEquipped ? "Kullanılıyor" : "Kuşan";
        SetButtonText(actionText, isInteractableState);
    }

    // ═══════════════════════════════════════════
    //  Animasyonlar
    // ═══════════════════════════════════════════

    /// <summary>
    /// Balta kuşanıldığında: scale-up bounce + altın parıltı çerçeve animasyonu.
    /// </summary>
    public void TriggerEquipAnimation()
    {
        if (!gameObject.activeInHierarchy) return;
        if (equipAnimCoroutine != null)
            StopCoroutine(equipAnimCoroutine);
        equipAnimCoroutine = StartCoroutine(EquipBounceRoutine());
    }

    private IEnumerator EquipBounceRoutine()
    {
        Vector3 originalScale = transform.localScale;

        // Phase 1: Scale up (0.14s — EaseOut)
        float elapsed = 0f;
        float duration = 0.14f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * (2f - t); // EaseOutQuad
            transform.localScale = Vector3.LerpUnclamped(originalScale, originalScale * 1.22f, t);
            yield return null;
        }

        // Phase 2: Scale geri (0.20s — Slight overshoot)
        elapsed = 0f;
        duration = 0.20f;
        Vector3 bigScale = transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // SmoothStep
            transform.localScale = Vector3.LerpUnclamped(bigScale, originalScale, t);
            yield return null;
        }
        transform.localScale = originalScale;

        // Phase 3: Highlight altın parıltı (0.45s)
        if (equippedHighlight != null)
        {
            Image hlImage = equippedHighlight.GetComponent<Image>();
            if (hlImage != null)
            {
                Color originalColor = hlImage.color;
                Color flashColor = new Color(1f, 0.92f, 0.25f, 1f); // Altın parıltı

                elapsed = 0f;
                duration = 0.18f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    hlImage.color = Color.Lerp(originalColor, flashColor, t);
                    yield return null;
                }

                elapsed = 0f;
                duration = 0.27f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    hlImage.color = Color.Lerp(flashColor, originalColor, t);
                    yield return null;
                }
                hlImage.color = originalColor;
            }
        }

        equipAnimCoroutine = null;
    }

    /// <summary>
    /// Yetersiz para durumunda kart kırmızı yanıp söner + yatay titreme.
    /// AxeTablePanelController tarafından çağrılır.
    /// </summary>
    public void TriggerShakeAnimation()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 originalPos = transform.localPosition;
        Image cardImg = GetComponent<Image>();
        Color originalColor = cardImg != null ? cardImg.color : Color.clear;

        // Kırmızı flash
        if (cardImg != null)
            cardImg.color = new Color(0.85f, 0.15f, 0.15f, 0.55f);

        float elapsed = 0f;
        float totalDuration = 0.38f;
        float shakeAmount = 9f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / totalDuration;
            float offset = Mathf.Sin(t * Mathf.PI * 7f) * shakeAmount * (1f - t);
            transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
            yield return null;
        }

        transform.localPosition = originalPos;
        if (cardImg != null)
            cardImg.color = originalColor;
    }

    // ═══════════════════════════════════════════
    //  Yardımcı
    // ═══════════════════════════════════════════

    private void SetButtonText(string text, bool interactable)
    {
        if (actionButton != null)
        {
            actionButton.interactable = interactable;
        }

        if (actionButtonText != null)
        {
            actionButtonText.text = text;
        }
    }

    private void OnCardClicked()
    {
        if (!isInteractableState) return;

        // Çift tıklama (double trigger) engelleme
        if (Time.unscaledTime - lastClickTime < 0.25f) return;
        lastClickTime = Time.unscaledTime;

        onClickCallback?.Invoke();
    }
}
