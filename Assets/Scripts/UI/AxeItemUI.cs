using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Balta masası panelindeki tek bir bağımsız balta kartını yönetir.
/// Kilitli kart satın alır; açık kart kuşanır.
/// </summary>
public class AxeItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Kartın tamamına tıklamayı sağlayan buton. Boşsa kök objeden alınır.")]
    [SerializeField] private Button cardButton;

    [SerializeField] private Image axeIcon;
    [SerializeField] private TextMeshProUGUI axeNameText;
    [SerializeField] private TextMeshProUGUI axeStatText;

    [Header("--- Aksiyon Butonu ---")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [Header("--- Durum Görselleri ---")]
    [SerializeField] private GameObject equippedHighlight;
    [SerializeField] private GameObject lockOverlay;

    private System.Action onActionCallback;
    private bool canInteract;
    private float lastClickTime;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
        }

        if (actionButton != null && actionButton != cardButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }
    }

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
        onActionCallback = onAction;

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

        canInteract = !isEquipped;

        if (cardButton != null)
            cardButton.interactable = true;

        string actionText = !isOwned
            ? $"Satın Al\n{purchasePrice:N0} Para"
            : isEquipped ? "Kullanılıyor" : "Kuşan";
        SetUpgradeButton(actionText, canInteract);
    }

    private void SetUpgradeButton(string text, bool interactable)
    {
        if (actionButton != null)
            actionButton.interactable = interactable;

        if (actionButtonText != null)
            actionButtonText.text = text;
    }

    private void OnCardClicked()
    {
        if (!canInteract || IsDoubleClick()) return;
        onActionCallback?.Invoke();
    }

    private void OnActionClicked()
    {
        if (!canInteract || IsDoubleClick()) return;
        onActionCallback?.Invoke();
    }

    private bool IsDoubleClick()
    {
        if (Time.unscaledTime - lastClickTime < 0.25f)
            return true;

        lastClickTime = Time.unscaledTime;
        return false;
    }
}
