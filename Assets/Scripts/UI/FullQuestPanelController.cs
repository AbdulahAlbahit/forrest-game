using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Managers;
using ForrestGame.Core;

/// <summary>
/// Tüm Görevler Paneli (FullQuestPanel) kontrolcüsü.
/// Ekran görüntüsündeki çizime uygun olarak 2 sekme içerir:
/// 1. "Mevcut Questler" (Aktif görevler)
/// 2. "Tamamlanan Questler" (Bitmiş görevler)
/// Ayrıca sağ üstte Kapat (X) butonu yer alır.
///
/// KULLANIM:
/// 1. CanvasUI altına FullQuestPanel prefab/objesini koyun.
/// 2. Bu scripti o panel objesine ekleyin ve Inspector referanslarını sürükleyin.
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

    [Header("--- Görev Liste İçeriği ---")]
    [Tooltip("Görev öğelerinin dizileceği ScrollRect/Content transform'u.")]
    [SerializeField] private Transform questListContent;

    [Tooltip("Görev öğesi prefab'ı (FullQuestItemUI bileşeni içermelidir).")]
    [SerializeField] private GameObject questItemPrefab;

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

        // Dinleyicileri Awake'te bağla ki panel açıldığında tıklamalar kesinleşmiş olsun
        if (activeQuestsTabButton != null)
            activeQuestsTabButton.onClick.AddListener(() => SwitchTab(QuestTab.Active));

        if (completedQuestsTabButton != null)
            completedQuestsTabButton.onClick.AddListener(() => SwitchTab(QuestTab.Completed));

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void Start()
    {
        // Başlangıçta paneli sadece oyun esnasında kapalıysa gizle
        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;

        // Panel her aktif olduğunda varsayılan olarak 'Mevcut Questler' sekmesini aç ve doldur
        SwitchTab(QuestTab.Active);
    }

    private void OnDisable()
    {
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded(PlayerData data)
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

        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(true);
        else if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        else
            SwitchTab(QuestTab.Active);
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();

        if (mainPanel != null)
            mainPanel.SetActive(false);
        else
            gameObject.SetActive(false);
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

        // Sekme Buton Görsellerini Güncelle
        UpdateTabButtonStyles();

        // Görev Listesini Yenile
        RefreshList();
    }

    private void UpdateTabButtonStyles()
    {
        if (activeTabImage != null)
            activeTabImage.color = (currentTab == QuestTab.Active) ? activeTabColor : inactiveTabColor;

        if (completedTabImage != null)
            completedTabImage.color = (currentTab == QuestTab.Completed) ? activeTabColor : inactiveTabColor;

        // Sekme butonlarının tıklanabilirlik durumlarını ayarla
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
        // Eski objeleri temizle
        ClearSpawnedItems();

        List<QuestDataWrapper> questList = CollectQuestsForCurrentTab();

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

        // Liste boşsa ve prefab varsa (ya da emptyListText bağlanmamışsa) bilgilendirme satırı ekle
        if (questList.Count == 0 && emptyListText == null && questItemPrefab != null)
        {
            GameObject emptyObj = Instantiate(questItemPrefab, questListContent);
            FullQuestItemUI itemUI = emptyObj.GetComponent<FullQuestItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup("Bilgilendirme", emptyMessage, 0, 0, 0, 0, false);
                spawnedItems.Add(itemUI);
            }
            return;
        }

        if (questItemPrefab == null) return;

        foreach (var quest in questList)
        {
            GameObject obj = Instantiate(questItemPrefab, questListContent);
            FullQuestItemUI itemUI = obj.GetComponent<FullQuestItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(
                    quest.Name,
                    quest.Description,
                    quest.CurrentProgress,
                    quest.TargetAmount,
                    quest.RewardGold,
                    quest.RewardXP,
                    quest.IsCompleted
                );
                spawnedItems.Add(itemUI);
            }
        }
    }

    private void ClearSpawnedItems()
    {
        // questListContent altındaki TÜM çocuk objeleri derhal listeden ayırıp sil
        if (questListContent != null)
        {
            for (int i = questListContent.childCount - 1; i >= 0; i--)
            {
                Transform child = questListContent.GetChild(i);
                child.SetParent(null); // Layout gruptan anında çıkar
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

        // 1. Gerçek QuestManager sistemi
        if (QuestManager.Instance != null && QuestManager.Instance.tumGorevler != null)
        {
            PlayerData data = CloudSaveManager.Instance?.CurrentData;

            foreach (QuestConfig quest in QuestManager.Instance.tumGorevler)
            {
                if (quest == null) continue;

                bool isCompleted = data?.CompletedQuests != null && data.CompletedQuests.Contains(quest.QuestID);
                int progress = data?.ActiveQuests != null ? data.ActiveQuests.Get(quest.QuestID, 0) : 0;

                // Sekme filtresi: Active tab → tamamlanmamışlar, Completed tab → tamamlanmışlar
                if (currentTab == QuestTab.Active && !isCompleted)
                {
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

        // 2. Fallback: QuestManager yoksa test verisi üret
        if (list.Count == 0 && (QuestManager.Instance == null || QuestManager.Instance.tumGorevler == null || QuestManager.Instance.tumGorevler.Count == 0))
        {
            if (currentTab == QuestTab.Active)
            {
                list.Add(new QuestDataWrapper { Name = "İlk Masanı Üret", Description = "Atölyede 1 adet masa üret.", CurrentProgress = 0, TargetAmount = 1, RewardGold = 100, RewardXP = 50, IsCompleted = false });
                list.Add(new QuestDataWrapper { Name = "3 Sandalye Sat", Description = "Markette 3 sandalye satışı yap.", CurrentProgress = 1, TargetAmount = 3, RewardGold = 250, RewardXP = 100, IsCompleted = false });
            }
            else
            {
                list.Add(new QuestDataWrapper { Name = "5 Odun Topla", Description = "Ormandan 5 adet ham odun topla.", CurrentProgress = 5, TargetAmount = 5, RewardGold = 50, RewardXP = 20, IsCompleted = true });
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
