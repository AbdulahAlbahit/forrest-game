using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Managers;
using ForrestGame.Core;

/// <summary>
/// Tüm Görevler Paneli (FullQuestPanel) kontrolcüsü.
/// Sol tarafta sekmesine göre görev isimlerini listeler, sağ tarafta seçilen görevin detaylarını gösterir.
/// 
/// SEKMELER:
/// 1. "Mevcut Questler" (Aktif görevler)
/// 2. "Tamamlanan Questler" (Bitmiş görevler)
/// </summary>
public class FullQuestPanelController : MonoBehaviour
{
    public static FullQuestPanelController Instance { get; private set; }

    public enum QuestTab
    {
        Active,
        Completed
    }

    [Header("--- Panel Referansları ---")]
    [Tooltip("Tüm görevler penceresi ana paneli.")]
    [SerializeField] private GameObject mainPanel;

    [Header("--- Sekme Butonları ---")]
    [Tooltip("'Mevcut Questler' sekme butonu.")]
    [SerializeField] private Button activeQuestsTabButton;

    [Tooltip("'Tamamlanan Questler' sekme butonu.")]
    [SerializeField] private Button completedQuestsTabButton;

    [Tooltip("'Mevcut Questler' sekme butonunun renk/görsel vurgusu için Image (İsteğe bağlı).")]
    [SerializeField] private Image activeTabImage;

    [Tooltip("'Tamamlanan Questler' sekme butonunun renk/görsel vurgusu için Image (İsteğe bağlı).")]
    [SerializeField] private Image completedTabImage;

    [Header("--- Kapat Butonu ---")]
    [Tooltip("Sağ üstteki (X) kapatma butonu.")]
    [SerializeField] private Button closeButton;

    [Header("--- Sol Taraf: Görev Liste İçeriği ---")]
    [Tooltip("Görev öğelerinin dizileceği ScrollRect/Content transform'u.")]
    [SerializeField] private Transform questListContent;

    [Tooltip("Görev öğesi prefab'ı (FullQuestItemUI bileşeni içermelidir).")]
    [SerializeField] private GameObject questItemPrefab;

    [Header("--- Sağ Taraf: Seçili Görev Detayları ---")]
    [Tooltip("Sağ taraftaki detay panelinin kapsayıcı objesi (Opsiyonel).")]
    [SerializeField] private GameObject detailPanelContainer;

    [Tooltip("Seçili görevin adı metni.")]
    [SerializeField] private TextMeshProUGUI selectedQuestNameText;

    [Tooltip("Seçili görevin açıklama metni.")]
    [SerializeField] private TextMeshProUGUI selectedQuestDescriptionText;

    [Tooltip("Seçili görevin ilerleme miktarı metni (Örn: '2 / 5' veya 'Tamamlandı!').")]
    [SerializeField] private TextMeshProUGUI selectedQuestProgressText;

    [Tooltip("Seçili görevin ödül bilgisi metni (Örn: '+100 Altın, +50 XP').")]
    [SerializeField] private TextMeshProUGUI selectedQuestRewardText;

    [Tooltip("Seçili görevin canlı dolan ilerleme barı.")]
    [SerializeField] private Image selectedQuestProgressFillImage;

    [Tooltip("Seçili görev tamamlandı rozeti.")]
    [SerializeField] private GameObject selectedQuestCompletedBadge;

    [Header("--- Boş Liste Durum Metni ---")]
    [Tooltip("Hiç görev olmadığında gösterilecek bilgilendirme metni.")]
    [SerializeField] private TextMeshProUGUI emptyListText;

    [Header("--- Tab Vurgu Renkleri ---")]
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    // Dahili Durum
    private QuestTab currentTab = QuestTab.Active;
    private List<FullQuestItemUI> spawnedItems = new List<FullQuestItemUI>();
    private QuestDataWrapper selectedQuest;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (activeQuestsTabButton != null)
            activeQuestsTabButton.onClick.AddListener(() => SwitchTab(QuestTab.Active));

        if (completedQuestsTabButton != null)
            completedQuestsTabButton.onClick.AddListener(() => SwitchTab(QuestTab.Completed));

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void Start()
    {
        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;

        ShopManager.OnItemSold += HandleItemSold;
        if (QuestManager.Instance != null)
            QuestManager.OnQuestsReset += RefreshList;

        SwitchTab(QuestTab.Active);
    }

    private void OnDisable()
    {
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;

        ShopManager.OnItemSold -= HandleItemSold;
        if (QuestManager.Instance != null)
            QuestManager.OnQuestsReset -= RefreshList;
    }

    private void HandleDataLoaded(PlayerData data)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshList();
    }

    private void HandleItemSold(string itemId, int amount)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshList();
    }

    // ═══════════════════════════════════════════
    //  Panel Aç / Kapat
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        PlayButtonClickSFX();

        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        GameObject targetObj = mainPanel != null ? mainPanel : gameObject;
        var anim = targetObj.GetComponent<ForrestGame.UI.UIPanelAnimator>();
        if (anim != null) anim.Show();

        SwitchTab(QuestTab.Active);
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();

        GameObject targetObj = mainPanel != null ? mainPanel : gameObject;
        var anim = targetObj.GetComponent<ForrestGame.UI.UIPanelAnimator>();

        if (anim != null)
        {
            anim.Hide(() =>
            {
                if (mainPanel != null)
                    mainPanel.SetActive(false);
                gameObject.SetActive(false);
            });
        }
        else
        {
            if (mainPanel != null)
                mainPanel.SetActive(false);
            else
                gameObject.SetActive(false);
        }
    }

    public void TogglePanel()
    {
        bool isActive = mainPanel != null ? mainPanel.activeSelf : gameObject.activeSelf;
        if (isActive)
            ClosePanel();
        else
            OpenPanel();
    }

    // ═══════════════════════════════════════════
    //  Sekme Değiştirme
    // ═══════════════════════════════════════════

    public void SwitchTab(QuestTab tab)
    {
        PlayButtonClickSFX();
        currentTab = tab;

        UpdateTabButtonStyles();
        RefreshList();
    }

    private void UpdateTabButtonStyles()
    {
        if (activeTabImage != null)
            activeTabImage.color = (currentTab == QuestTab.Active) ? activeTabColor : inactiveTabColor;

        if (completedTabImage != null)
            completedTabImage.color = (currentTab == QuestTab.Completed) ? activeTabColor : inactiveTabColor;

        if (activeQuestsTabButton != null)
            activeQuestsTabButton.interactable = (currentTab != QuestTab.Active);

        if (completedQuestsTabButton != null)
            completedQuestsTabButton.interactable = (currentTab != QuestTab.Completed);
    }

    // ═══════════════════════════════════════════
    //  Görev Listesi Doldurma
    // ═══════════════════════════════════════════

    public void RefreshList()
    {
        ClearSpawnedItems();

        List<QuestDataWrapper> questList = CollectQuestsForCurrentTab();
        Debug.Log($"[FullQuestPanel] Sekme: {currentTab}, Bulunan Görev Sayısı: {questList.Count}");

        string emptyMessage = currentTab == QuestTab.Active 
            ? "Şu an aktif görev bulunmuyor." 
            : "Henüz tamamlanan görev yok.";

        if (emptyListText != null)
        {
            if (questList.Count == 0)
            {
                emptyListText.gameObject.SetActive(true);
                emptyListText.text = emptyMessage;
            }
            else
            {
                emptyListText.gameObject.SetActive(false);
            }
        }

        if (questListContent == null) return;

        if (questList.Count == 0)
        {
            ClearRightDetailPanel(emptyMessage);
            return;
        }

        if (questItemPrefab == null) return;

        FullQuestItemUI firstItemUI = null;
        QuestDataWrapper firstQuestData = null;

        for (int i = 0; i < questList.Count; i++)
        {
            var quest = questList[i];

            GameObject obj = Instantiate(questItemPrefab, questListContent, false);
            obj.SetActive(true);

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }

            FullQuestItemUI itemUI = obj.GetComponent<FullQuestItemUI>();

            if (itemUI != null)
            {
                FullQuestItemUI capturedItemUI = itemUI;
                QuestDataWrapper capturedQuestData = quest;

                itemUI.Setup(
                    quest.Name,
                    quest.Description,
                    quest.CurrentProgress,
                    quest.TargetAmount,
                    quest.RewardGold,
                    quest.RewardXP,
                    quest.IsCompleted,
                    () => OnQuestSelected(capturedQuestData, capturedItemUI)
                );

                spawnedItems.Add(itemUI);

                if (i == 0)
                {
                    firstItemUI = itemUI;
                    firstQuestData = quest;
                }
            }
        }

        if (questListContent is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        // İlk görevi otomatik seç
        if (firstQuestData != null && firstItemUI != null)
        {
            OnQuestSelected(firstQuestData, firstItemUI);
        }
    }

    private void OnQuestSelected(QuestDataWrapper quest, FullQuestItemUI itemUI)
    {
        PlayButtonClickSFX();
        selectedQuest = quest;

        // Sol listedeki diğer öğelerin vurgusunu kaldır
        foreach (var item in spawnedItems)
        {
            if (item != null) item.SetSelected(false);
        }

        if (itemUI != null)
            itemUI.SetSelected(true);

        // Sağ Taraf Detay Panelini Güncelle
        UpdateRightDetailPanel(quest);
    }

    private void UpdateRightDetailPanel(QuestDataWrapper quest)
    {
        if (detailPanelContainer != null)
            detailPanelContainer.SetActive(true);

        if (quest == null) return;

        if (selectedQuestNameText != null)
            selectedQuestNameText.text = quest.IsCompleted ? $"{quest.Name} (Tamamlandı)" : quest.Name;

        if (selectedQuestDescriptionText != null)
            selectedQuestDescriptionText.text = string.IsNullOrEmpty(quest.Description) ? "Görev hedefini tamamla." : quest.Description;

        if (selectedQuestProgressText != null)
        {
            if (quest.IsCompleted)
                selectedQuestProgressText.text = "Tamamlandı!";
            else
                selectedQuestProgressText.text = $"{quest.CurrentProgress} / {quest.TargetAmount}";
        }

        if (selectedQuestRewardText != null)
        {
            string rewardStr = "";
            if (quest.RewardGold > 0) rewardStr += $"+{quest.RewardGold} Altın ";
            if (quest.RewardXP > 0) rewardStr += $"+{quest.RewardXP} XP";
            selectedQuestRewardText.text = string.IsNullOrEmpty(rewardStr) ? "Ödül Yok" : rewardStr.Trim();
        }

        if (selectedQuestProgressFillImage != null)
        {
            float ratio = quest.IsCompleted ? 1f : (quest.TargetAmount > 0 ? Mathf.Clamp01((float)quest.CurrentProgress / quest.TargetAmount) : 0f);
            selectedQuestProgressFillImage.fillAmount = ratio;
        }

        if (selectedQuestCompletedBadge != null)
        {
            selectedQuestCompletedBadge.SetActive(quest.IsCompleted);
        }
    }

    private void ClearRightDetailPanel(string emptyMessage)
    {
        if (selectedQuestNameText != null)
            selectedQuestNameText.text = "";

        if (selectedQuestDescriptionText != null)
            selectedQuestDescriptionText.text = emptyMessage;

        if (selectedQuestProgressText != null)
            selectedQuestProgressText.text = "";

        if (selectedQuestRewardText != null)
            selectedQuestRewardText.text = "";

        if (selectedQuestProgressFillImage != null)
            selectedQuestProgressFillImage.fillAmount = 0f;

        if (selectedQuestCompletedBadge != null)
            selectedQuestCompletedBadge.SetActive(false);
    }

    private void ClearSpawnedItems()
    {
        if (questListContent != null)
        {
            for (int i = questListContent.childCount - 1; i >= 0; i--)
            {
                Transform child = questListContent.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
        spawnedItems.Clear();
    }

    // ═══════════════════════════════════════════
    //  Görev Verisi Toplama
    // ═══════════════════════════════════════════

    private List<QuestDataWrapper> CollectQuestsForCurrentTab()
    {
        List<QuestDataWrapper> list = new List<QuestDataWrapper>();

        if (QuestManager.Instance != null && QuestManager.Instance.tumGorevler != null)
        {
            PlayerData data = CloudSaveManager.Instance?.CurrentData;

            foreach (QuestConfig quest in QuestManager.Instance.tumGorevler)
            {
                if (quest == null) continue;

                bool isCompleted = data?.CompletedQuests != null && data.CompletedQuests.Contains(quest.QuestID);
                int progress = data?.ActiveQuests != null ? data.ActiveQuests.Get(quest.QuestID, 0) : 0;

                if (currentTab == QuestTab.Active && !isCompleted)
                {
                    Debug.Log($"[FullQuestPanel] Aktif Görev Eklendi: {quest.QuestName} ({progress}/{quest.TargetAmount})");
                    list.Add(new QuestDataWrapper
                    {
                        Name = quest.QuestName,
                        Description = quest.QuestDescription,
                        CurrentProgress = progress,
                        TargetAmount = quest.TargetAmount,
                        RewardGold = quest.RewardGold,
                        RewardXP = quest.RewardXP,
                        IsCompleted = false
                    });
                }
                else if (currentTab == QuestTab.Completed && isCompleted)
                {
                    list.Add(new QuestDataWrapper
                    {
                        Name = quest.QuestName,
                        Description = quest.QuestDescription,
                        CurrentProgress = quest.TargetAmount,
                        TargetAmount = quest.TargetAmount,
                        RewardGold = quest.RewardGold,
                        RewardXP = quest.RewardXP,
                        IsCompleted = true
                    });
                }
            }
        }

        return list;
    }

    // ═══════════════════════════════════════════
    //  Ses
    // ═══════════════════════════════════════════

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }

    // ═══════════════════════════════════════════
    //  Yardımcı Veri Sınıfı
    // ═══════════════════════════════════════════

    private class QuestDataWrapper
    {
        public string Name;
        public string Description;
        public int CurrentProgress;
        public int TargetAmount;
        public int RewardGold;
        public int RewardXP;
        public bool IsCompleted;
    }
}
