using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tüm Görevler Paneli (FullQuestPanel) içerisindeki tek bir görev kutucuğunu yönetir.
/// Sol listede görev adını gösterir, tıklandığında sağ tarafta detayların açılmasını sağlar.
/// </summary>
public class FullQuestItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Görev adı metni.")]
    [SerializeField] private TextMeshProUGUI questNameText;

    [Tooltip("Görev açıklama metni (Kutucuk içi fallback).")]
    [SerializeField] private TextMeshProUGUI questDescriptionText;

    [Tooltip("İlerleme miktar metni (ör: '2 / 5' veya 'Tamamlandı!').")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Tooltip("Ödül bilgisi metni (ör: '+100 Altın, +50 XP').")]
    [SerializeField] private TextMeshProUGUI rewardText;

    [Header("--- İlerleme & Seçim Görselleri ---")]
    [Tooltip("Canlı dolan ilerleme barı (Image Type = Filled, Fill Method = Horizontal).")]
    [SerializeField] private Image progressFillImage;

    [Tooltip("Tamamlandı rozeti / ikonu (İsteğe bağlı).")]
    [SerializeField] private GameObject completedBadge;

    [Tooltip("Seçim arka planı görseli (Seçilince parlar).")]
    [SerializeField] private GameObject selectionHighlight;

    [Tooltip("Tıklama butonu.")]
    [SerializeField] private Button clickButton;

    private System.Action onSelectedCallback;

    private void Awake()
    {
        if (clickButton != null)
        {
            clickButton.onClick.AddListener(OnClick);
        }
    }

    /// <summary>
    /// Kutucuğu verilen görev verisi ve tıklama callback'i ile yapılandırır.
    /// </summary>
    public void Setup(string questName, string description, int currentProgress, int targetAmount, 
                      int rewardGold, int rewardXP, bool isCompleted, System.Action onSelected = null)
    {
        onSelectedCallback = onSelected;

        if (questNameText != null)
            questNameText.text = isCompleted ? $"{questName} (Tamamlandı)" : questName;

        if (questDescriptionText != null)
            questDescriptionText.text = string.IsNullOrEmpty(description) ? "" : description;

        // Bilgilendirme (boş liste) durumu kontrolü
        bool isInfoMessage = targetAmount <= 0 && (rewardGold <= 0 && rewardXP <= 0);

        if (progressText != null)
        {
            if (isInfoMessage)
                progressText.text = "";
            else if (isCompleted)
                progressText.text = "Tamamlandı!";
            else
                progressText.text = $"{currentProgress} / {targetAmount}";
        }

        if (rewardText != null)
        {
            if (isInfoMessage)
            {
                rewardText.text = "";
            }
            else
            {
                string rewardStr = "";
                if (rewardGold > 0) rewardStr += $"+{rewardGold} Altın ";
                if (rewardXP > 0) rewardStr += $"+{rewardXP} XP";
                rewardText.text = string.IsNullOrEmpty(rewardStr) ? "" : rewardStr.Trim();
            }
        }

        if (progressFillImage != null)
        {
            float ratio = isCompleted ? 1f : (targetAmount > 0 ? Mathf.Clamp01((float)currentProgress / targetAmount) : 0f);
            progressFillImage.fillAmount = ratio;
        }

        if (completedBadge != null)
        {
            completedBadge.SetActive(isCompleted);
        }

        SetSelected(false);
    }

    /// <summary>
    /// Bu kutucuğun seçili olup olmadığını ayarlar (Öne çıkarma görseli).
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(isSelected);
    }

    private void OnClick()
    {
        onSelectedCallback?.Invoke();
    }
}
