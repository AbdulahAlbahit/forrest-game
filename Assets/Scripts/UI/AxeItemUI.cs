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
