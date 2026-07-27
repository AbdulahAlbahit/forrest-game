using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tüm Görevler Paneli (FullQuestPanel) içerisindeki tek bir görev kutucuğunu yönetir.
/// Hem aktif (Mevcut) hem de tamamlanmış (Completed) görev verileriyle doldurulabilir.
/// </summary>
public class FullQuestItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Görev adı metni.")]
    [SerializeField] private TextMeshProUGUI questNameText;

    [Tooltip("Görev açıklama metni.")]
    [SerializeField] private TextMeshProUGUI questDescriptionText;

    [Tooltip("İlerleme miktar metni (ör: '2 / 5' veya 'Tamamlandı!').")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Tooltip("Ödül bilgisi metni (ör: '+100 Altın, +50 XP').")]
    [SerializeField] private TextMeshProUGUI rewardText;

    [Header("--- İlerleme Görselleri ---")]
    [Tooltip("Canlı dolan ilerleme barı (Image Type = Filled, Fill Method = Horizontal).")]
    [SerializeField] private Image progressFillImage;

    [Tooltip("Tamamlandı rozeti / ikonu (İsteğe bağlı).")]
    [SerializeField] private GameObject completedBadge;

    /// <summary>
    /// Kutucuğu verilen görev verisi ile yapılandırır.
    /// </summary>
    public void Setup(string questName, string description, int currentProgress, int targetAmount, 
                      int rewardGold, int rewardXP, bool isCompleted)
    {
        if (questNameText != null)
            questNameText.text = isCompleted ? $"✅ {questName}" : questName;

        if (questDescriptionText != null)
            questDescriptionText.text = string.IsNullOrEmpty(description) ? "Görev hedefini tamamla." : description;

        if (progressText != null)
        {
            if (isCompleted)
                progressText.text = "Tamamlandı!";
            else
                progressText.text = $"{currentProgress} / {targetAmount}";
        }

        if (rewardText != null)
        {
            string rewardStr = "";
            if (rewardGold > 0) rewardStr += $"+{rewardGold} Altın ";
            if (rewardXP > 0) rewardStr += $"+{rewardXP} XP";
            rewardText.text = string.IsNullOrEmpty(rewardStr) ? "Ödül Yok" : rewardStr.Trim();
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
    }
}
