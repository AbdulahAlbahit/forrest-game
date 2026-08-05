using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Managers;
using ForrestGame.Core;
using ForrestGame.Data;

/// <summary>
/// Karakter İstatistik Paneli Kontrolcüsü.
/// Sol alttaki Seviye butonuna basıldığında açılır.
/// Oyuncunun mevcut tüm stat'larını (Karakter, Ekonomi, Balta, Envanter, Atölye, Mevsim Buff/Debuff'ları)
/// kategorilere ayrılmış koyu renkli şık satırlar halinde listeler.
/// </summary>
public class CharacterStatsPanelController : MonoBehaviour
{
    public static CharacterStatsPanelController Instance { get; private set; }

    [Header("--- Panel Referansları ---")]
    [Tooltip("İstatistik paneli ana GameObject'i.")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Kapat (X) butonu.")]
    [SerializeField] private Button closeButton;

    [Tooltip("HUD üzerindeki Seviye butonu (Opsiyonel - Inspector'dan bağlanabilir veya HUD otomatik bağlar).")]
    [SerializeField] private Button levelButton;

    [Header("--- Liste İçeriği ---")]
    [Tooltip("Kaydırma alanı (ScrollRect).")]
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("Stat satırlarının ekleneceği Content transform'u.")]
    [SerializeField] private Transform statListContent;

    [Tooltip("Stat satırı prefab'ı (StatRowUI bileşeni içermelidir).")]
    [SerializeField] private GameObject statRowPrefab;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX sesi.")]
    [SerializeField] private string buttonSfxName = "";

    [Header("--- Renk Tema Ayarları ---")]
    [SerializeField] private Color headerColorCharacter = new Color(0.4f, 0.7f, 1f, 1f);   // Mavi
    [SerializeField] private Color headerColorEconomy   = new Color(1f, 0.85f, 0.2f, 1f);  // Altın
    [SerializeField] private Color headerColorAxe       = new Color(1f, 0.4f, 0.3f, 1f);   // Kırmızı / Hasar
    [SerializeField] private Color headerColorInventory = new Color(0.4f, 0.9f, 0.4f, 1f);   // Yeşil
    [SerializeField] private Color headerColorWorkshop  = new Color(0.9f, 0.5f, 0.9f, 1f);   // Mor
    [SerializeField] private Color headerColorTruck     = new Color(1f, 0.65f, 0.25f, 1f);   // Turuncu
    [SerializeField] private Color headerColorSeason    = new Color(0.3f, 0.9f, 0.9f, 1f);   // Turkuaz
    [SerializeField] private Color headerColorStats     = new Color(0.8f, 0.8f, 0.8f, 1f);   // Gri / Gümüş

    [SerializeField] private Color normalValueColor     = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color buffValueColor       = new Color(0.3f, 0.95f, 0.4f, 1f);  // Pozitif Buff (Yeşil)
    [SerializeField] private Color debuffValueColor     = new Color(0.95f, 0.3f, 0.3f, 1f);  // Negatif Debuff (Kırmızı)

    private List<StatRowUI> spawnedRows = new List<StatRowUI>();
    private Dictionary<string, bool> categoryFoldStates = new Dictionary<string, bool>();
    private string currentBuildingCategory = "";
    private bool isPanelOpen = false;
    private float lastToggleTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += HandleStatChanged;

        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged += HandleStatChanged;
            XPManager.Instance.OnLevelUp += HandleStatChanged;
        }

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged += HandleBasketChanged;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged += HandleQueueChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;

        SeasonManager.OnMevsimDegisti += HandleMevsimDegisti;
        AxeVisualManager.OnAxeChanged += HandleAxeChanged;
        AxeTablePanelController.OnAxeEquipped += HandleAxeEquipped;
    }

    private void UnsubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleStatChanged;

        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged -= HandleStatChanged;
            XPManager.Instance.OnLevelUp -= HandleStatChanged;
        }

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged -= HandleBasketChanged;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged -= HandleQueueChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;

        SeasonManager.OnMevsimDegisti -= HandleMevsimDegisti;
        AxeVisualManager.OnAxeChanged -= HandleAxeChanged;
        AxeTablePanelController.OnAxeEquipped -= HandleAxeEquipped;
    }

    private void HandleStatChanged(int val) => TriggerRefreshIfActive();
    private void HandleBasketChanged(int cur, int max) => TriggerRefreshIfActive();
    private void HandleQueueChanged(int cur, int max) => TriggerRefreshIfActive();
    private void HandleItemUpgraded(string id, int lvl) => TriggerRefreshIfActive();
    private void HandleDataLoaded(PlayerData data) => TriggerRefreshIfActive();
    private void HandleMevsimDegisti(MevsimTuru mevsim) => TriggerRefreshIfActive();
    private void HandleAxeChanged(string id) => TriggerRefreshIfActive();
    private void HandleAxeEquipped() => TriggerRefreshIfActive();

    private void TriggerRefreshIfActive()
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void Update()
    {
    }

    // ═══════════════════════════════════════════
    //  Panel Aç / Kapat
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        isPanelOpen = true;
        PlayButtonClickSFX();

        // 1. Script objesini ve mainPanel'i aktif et
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(true);

        // 2. Üst ebeveynlerin (parent transform'ların) ve CanvasGroup'ların aktif olduğundan emin ol
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            CanvasGroup parentCG = current.GetComponent<CanvasGroup>();
            if (parentCG != null)
            {
                parentCG.alpha = 1f;
                parentCG.interactable = true;
                parentCG.blocksRaycasts = true;
            }

            current = current.parent;
        }

        // 3. Animator varsa animasyonu sıfırla/açık duruma getir
        Animator anim = GetComponent<Animator>() ?? (mainPanel != null ? mainPanel.GetComponent<Animator>() : null);
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 5. Scale ve Pop-up animasyonu kontrolü
        GameObject targetObj = mainPanel != null ? mainPanel : gameObject;
        var uiPanelAnim = targetObj.GetComponent<ForrestGame.UI.UIPanelAnimator>();
        if (uiPanelAnim != null)
        {
            uiPanelAnim.Show();
        }
        else
        {
            RectTransform rt = targetObj.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;
        }

        RefreshPanel();

        // Paneli ilk açışta scroll'u en üste al
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ClosePanel()
    {
        isPanelOpen = false;
        PlayButtonClickSFX();

        GameObject targetObj = mainPanel != null ? mainPanel : gameObject;
        var uiPanelAnim = targetObj.GetComponent<ForrestGame.UI.UIPanelAnimator>();

        if (uiPanelAnim != null)
        {
            uiPanelAnim.Hide(() =>
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

            gameObject.SetActive(false);
        }
    }

    public void TogglePanel()
    {
        if (Time.unscaledTime - lastToggleTime < 0.2f) return;
        lastToggleTime = Time.unscaledTime;

        if (isPanelOpen || (mainPanel != null && mainPanel.activeInHierarchy) || gameObject.activeInHierarchy)
            ClosePanel();
        else
            OpenPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Yenileme & Stat Listesi Doldurma
    // ═══════════════════════════════════════════

    public void RefreshPanel()
    {
        ClearSpawnedRows();

        // Fallback: statListContent atanmamışsa ScrollView içindeki Content'i otomatik bulmaya çalış
        if (statListContent == null)
        {
            if (scrollRect != null && scrollRect.content != null)
            {
                statListContent = scrollRect.content;
            }
            else
            {
                var contentObj = transform.Find("Scroll View/Viewport/Content") ?? transform.Find("Panel/Scroll View/Viewport/Content");
                if (contentObj != null) statListContent = contentObj;
            }
        }

        if (statListContent == null)
        {
            Debug.LogWarning("[CharacterStatsPanelController] 'statListContent' (Content Transform) Inspector'dan atanmamış! Statlar listelenemiyor.");
            return;
        }

        if (statRowPrefab == null)
        {
            Debug.LogWarning("[CharacterStatsPanelController] 'statRowPrefab' (StatRowUI Prefab) Inspector'dan atanmamış! Statlar listelenemiyor.");
            return;
        }

        // Layout bileşenlerini kontrol et (VerticalLayoutGroup & ContentSizeFitter yoksa koddan ekle)
        EnsureLayoutComponents(statListContent);

        // Verileri hazırla
        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        // 1. KARAKTER
        AddHeader("KARAKTER", headerColorCharacter);
        int playerLevel = XPManager.Instance != null ? XPManager.Instance.MevcutSeviye : (data?.OyuncuSeviyesi ?? 1);
        int currentXP = XPManager.Instance != null ? XPManager.Instance.MevcutXP : (data?.XP ?? 0);
        int reqXP = XPManager.Instance != null ? XPManager.Instance.GetRequiredXPForLevel(playerLevel) : 100;
        float xpPercent = reqXP > 0 ? ((float)currentXP / reqXP) * 100f : 100f;

        AddRow("Oyuncu Seviyesi", $"Level {playerLevel}", normalValueColor);
        AddRow("Mevcut XP", $"{currentXP:N0} / {reqXP:N0}", normalValueColor);
        AddRow("XP İlerleme", $"%{xpPercent:F1}", normalValueColor);

        int characterAttackLevel = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel("Character") : 1;
        AddRow("Karakter Vuruş Seviyesi", $"Seviye {characterAttackLevel}", normalValueColor);

        string hitsPerSecondText = "—";
        if (UpgradeManager.Instance != null &&
            UpgradeManager.Instance.TryGetConfig("Character", out ItemUpgradeConfig characterAttackConfig) &&
            characterAttackConfig.TryGetCharacterAttackTiming(characterAttackLevel, out float attackCooldown, out _, out _) &&
            attackCooldown > 0f)
        {
            hitsPerSecondText = $"{1f / attackCooldown:F2} vuruş/sn";
        }
        AddRow("Vuruş Hızı (Karakter)", hitsPerSecondText, headerColorCharacter);

        // 2. EKONOMİ
        AddHeader("EKONOMİ", headerColorEconomy);
        int gold = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : (data?.Para ?? 0);
        AddRow("Altın Bakiyesi", $"{gold:N0} Gold", headerColorEconomy);

        // 3. BALTA
        AddHeader("BALTA İSTATİSTİKLERİ", headerColorAxe);
        string currentAxeId = AxeVisualManager.Instance != null ? AxeVisualManager.Instance.currentAxeId : "lowpoly_axe";
        int equippedTier = GetEquippedAxeTier(currentAxeId);
        string equippedItemId = GetAxeItemId(equippedTier);
        string equippedAxeName = GetEquippedAxeName(equippedTier);
        int equippedLevel = GetAxeItemLevel(equippedItemId, data);
        float equippedPower = GetAxePower(equippedItemId, equippedLevel);
        bool equippedAxeIsMaxLevel = UpgradeManager.Instance != null &&
                                     UpgradeManager.Instance.IsMaxLevel(equippedItemId);
        float nextLevelPower = equippedAxeIsMaxLevel
            ? equippedPower
            : GetAxePower(equippedItemId, equippedLevel + 1);

        MevsimTuru currentSeason = SeasonManager.Instance != null ? SeasonManager.Instance.aktifMevsim : MevsimTuru.Ilkbahar;
        SeasonSettings currentSettings = SeasonManager.Instance != null ? SeasonManager.Instance.ayarlar : null;
        float seasonAxeMult = GetAxeMultiplierForSeason(currentSeason, currentSettings);
        float effectiveEquippedDamage = equippedPower * seasonAxeMult;

        AddRow("Kuşanılan Balta", equippedAxeName, headerColorAxe);
        AddRow("Balta Seviyesi", $"Seviye {equippedLevel}", normalValueColor);
        AddRow("Kuşanılan Balta Gücü", $"{equippedPower:F1} Hasar", headerColorAxe);

        Color netDamageColor = seasonAxeMult > 1.0f ? buffValueColor : (seasonAxeMult < 1.0f ? debuffValueColor : normalValueColor);
        AddRow("Net Hasar (Mevsim Etkisiyle)", $"{effectiveEquippedDamage:F1} Hasar", netDamageColor);

        AddRow(
            "Sonraki Seviye",
            equippedAxeIsMaxLevel ? "Maksimum Seviye" : $"Seviye {equippedLevel + 1}",
            normalValueColor);
        AddRow(
            "Sonraki Seviye Gücü",
            equippedAxeIsMaxLevel ? "Maksimum Seviye" : $"{nextLevelPower:F1} Hasar",
            normalValueColor);

        // 4. ENVANTER & DEPO
        AddHeader("ENVANTER & DEPO", headerColorInventory);
        int basketLvl = GetBasketLevel(data);
        int configuredBasketCapacity = GetBasketCapacity(basketLvl);
        // Gercek oynanis kapasitesi EfeOdunTasima tarafinda tutuluyor. BasketManager
        // eski/yardimci sistem oldugu icin yuksek seviyelerde 5 degerinde kalabiliyor.
        EfeOdunTasima woodCarrier = FindFirstObjectByType<EfeOdunTasima>();
        int basketCur = woodCarrier != null
            ? woodCarrier.TasinanOdunSayisi
            : (BasketManager.Instance != null ? BasketManager.Instance.MevcutDoluluk : 0);
        int basketMax = woodCarrier != null && woodCarrier.TasimaKapasitesi > 0
            ? woodCarrier.TasimaKapasitesi
            : configuredBasketCapacity;
        bool basketIsMaxLevel = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.IsMaxLevel("Basket")
            : basketLvl >= 5;
        int nextBasketCapacity = GetBasketCapacity(Mathf.Min(5, basketLvl + 1));
        int basketUpgradePrice = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetUpgradeCost("Basket")
            : Mathf.Max(100, 100 + (basketLvl - 1) * 50);
        int workshopRawWood = data != null ? data.WorkshopRawWood : 0;
        int workshopCap = WorkshopManager.Instance != null ? WorkshopManager.Instance.GetWorkshopCapacity() : 10;

        AddRow("Sepet Odun Miktarı", $"{basketCur} / {basketMax}", normalValueColor);
        AddRow("Sepet Seviyesi", $"Seviye {basketLvl}", normalValueColor);
        AddRow("Sepet Kapasitesi", $"{basketMax} Odun", headerColorInventory);
        AddRow(
            "Sonraki Seviye Kapasitesi",
            basketIsMaxLevel ? "Maksimum Seviye" : $"{nextBasketCapacity} Odun",
            normalValueColor);
        AddRow(
            "Yükseltme Fiyatı",
            basketIsMaxLevel ? "Maksimum Seviye" : $"{basketUpgradePrice:N0} Para",
            basketIsMaxLevel ? normalValueColor : headerColorInventory);
        AddRow("Atölye Odun Stoku", $"{workshopRawWood} / {workshopCap}", headerColorInventory);

        string woodTransferSpeedText = "—";
        if (woodCarrier != null && woodCarrier.YuklemeTekrarAraligi > 0f)
        {
            woodTransferSpeedText = $"{1f / woodCarrier.YuklemeTekrarAraligi:F2} odun/sn";
        }
        AddRow("Odun Aktarma Hızı (Kamyona)", woodTransferSpeedText, headerColorInventory);

        // 5. ATÖLYE
        AddHeader("ATÖLYE", headerColorWorkshop);
        int workshopLvl = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel("Workshop") : 1;
        int queueLvl = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel("WorkshopQueue") : 1;
        float prodTime = WorkshopManager.Instance != null ? WorkshopManager.Instance.GetEstimatedProductionTime() : 0f;

        AddRow("Atölye Seviyesi", $"Seviye {workshopLvl}", normalValueColor);
        AddRow("Kuyruk Seviyesi", $"Seviye {queueLvl}", normalValueColor);
        AddRow("Üretim Hızı", $"{prodTime:F2} sn/ürün", headerColorWorkshop);
        AddRow("Atölye Kapasitesi", $"{workshopCap} Ürün", normalValueColor);

        // 6. KAMYON GELİŞTİRME
        AddHeader("KAMYON GELİŞTİRME", headerColorTruck);
        int truckLevel = GetTruckLevel(data);
        int truckCapacity = GetTruckCapacity(truckLevel);
        int truckModel = GetTruckModel(truckLevel);
        bool truckIsMaxLevel = UpgradeManager.Instance != null && UpgradeManager.Instance.IsMaxLevel("Truck");
        int truckUpgradePrice = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetUpgradeCost("Truck")
            : Mathf.Max(100, 100 + (truckLevel - 1) * 50);

        AddRow("Kamyon Seviyesi", $"Seviye {truckLevel}", normalValueColor);
        AddRow("Taşıma Kapasitesi", $"{truckCapacity} Odun", headerColorTruck);
        AddRow("Kamyon Modeli", $"Model {truckModel}", normalValueColor);
        AddRow(
            "Yükseltme Fiyatı",
            truckIsMaxLevel ? "Maksimum Seviye" : $"{truckUpgradePrice:N0} Para",
            truckIsMaxLevel ? normalValueColor : headerColorTruck);

        // 7. MEVSİM & BUFF / DEBUFFS
        AddHeader("MEVSİM & ETKİLERİ", headerColorSeason);
        MevsimTuru activeSeason = SeasonManager.Instance != null ? SeasonManager.Instance.aktifMevsim : MevsimTuru.Ilkbahar;
        float remainingSec = SeasonManager.Instance != null ? SeasonManager.Instance.kalanSaniye : 0f;
        TimeSpan st = TimeSpan.FromSeconds(Mathf.Max(0, remainingSec));
        string timerStr = $"{st.Hours}s {st.Minutes}dk {st.Seconds}sn";

        AddRow("Aktif Mevsim", activeSeason.ToString(), headerColorSeason);
        AddRow("Mevsim Kalan Süre", timerStr, normalValueColor);

        // Mevsimsel Buff / Debuff göstergeleri
        SeasonSettings settings = SeasonManager.Instance != null ? SeasonManager.Instance.ayarlar : null;
        if (settings != null)
        {
            // Balta Hasarı Çarpanı
            float axeMult = GetAxeMultiplierForSeason(activeSeason, settings);
            Color axeMultColor = axeMult > 1.0f ? buffValueColor : (axeMult < 1.0f ? debuffValueColor : normalValueColor);
            AddRow("Balta Hasar Çarpanı", $"x{axeMult:F2}", axeMultColor);

            // Altın Çarpanı (Sonbahar)
            float goldMult = activeSeason == MevsimTuru.Sonbahar ? settings.sonbaharAltinCarpani : 1.0f;
            Color goldMultColor = goldMult > 1.0f ? buffValueColor : normalValueColor;
            AddRow("Altın Kazanma Çarpanı", $"x{goldMult:F2}", goldMultColor);

            // Üretim Hızı Çarpanı (Yaz)
            float prodMult = activeSeason == MevsimTuru.Yaz ? settings.summerProductionSpeed : 1.0f;
            Color prodMultColor = prodMult > 1.0f ? buffValueColor : normalValueColor;
            AddRow("Üretim Hızı Çarpanı", $"x{prodMult:F2}", prodMultColor);

            // Ağaç Büyüme Çarpanı (İlkbahar / Kış)
            float treeMult = GetTreeGrowthMultiplierForSeason(activeSeason, settings);
            Color treeMultColor = treeMult > 1.0f ? buffValueColor : (treeMult < 1.0f ? debuffValueColor : normalValueColor);
            AddRow("Ağaç Büyüme Çarpanı", $"x{treeMult:F2}", treeMultColor);
        }

        // 8. GENEL İSTATİSTİKLER
        AddHeader("GENEL İSTATİSTİKLER", headerColorStats);
        float playTime = data != null ? data.OyunSuresi : 0f;
        TimeSpan pt = TimeSpan.FromSeconds(playTime);
        string playTimeStr = $"{pt.Hours}s {pt.Minutes}dk {pt.Seconds}sn";

        int activeQuestCount = 0;
        int completedQuestCount = (data != null && data.CompletedQuests != null) ? data.CompletedQuests.Count : 0;

        if (QuestManager.Instance != null && QuestManager.Instance.tumGorevler != null)
        {
            foreach (var quest in QuestManager.Instance.tumGorevler)
            {
                if (quest == null) continue;
                bool isCompleted = data?.CompletedQuests != null && data.CompletedQuests.Contains(quest.QuestID);
                if (!isCompleted)
                    activeQuestCount++;
            }
        }
        else if (data != null && data.ActiveQuests != null)
        {
            activeQuestCount = data.ActiveQuests.keys.Count;
        }

        AddRow("Toplam Oyun Süresi", playTimeStr, normalValueColor);
        AddRow("Aktif Görevler", $"{activeQuestCount} Görev", normalValueColor);
        AddRow("Tamamlanan Görevler", $"{completedQuestCount} Görev", buffValueColor);

        // Rebuild layout ve canvas güncelle
        if (statListContent is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void EnsureLayoutComponents(Transform contentTransform)
    {
        if (contentTransform == null) return;

        LayoutGroup existingLayout = contentTransform.GetComponent<LayoutGroup>();
        if (existingLayout is VerticalLayoutGroup vLayout)
        {
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing = 4f;
        }
        else if (existingLayout == null)
        {
            VerticalLayoutGroup newLayout = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            if (newLayout != null)
            {
                newLayout.childControlWidth = true;
                newLayout.childControlHeight = true;
                newLayout.childForceExpandWidth = true;
                newLayout.childForceExpandHeight = false;
                newLayout.spacing = 4f;
            }
        }

        ContentSizeFitter fitter = contentTransform.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentTransform.gameObject.AddComponent<ContentSizeFitter>();
        }

        if (fitter != null)
        {
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }

    private bool IsCategoryExpanded(string category)
    {
        if (categoryFoldStates.TryGetValue(category, out bool expanded))
            return expanded;
        return true; // Varsayılan olarak açık
    }

    private void ToggleCategory(string category)
    {
        bool currentState = IsCategoryExpanded(category);
        categoryFoldStates[category] = !currentState;
        Debug.Log($"[CharacterStatsPanelController] '{category}' kategorisi katlama durumu değiştirildi: {currentState} -> {!currentState}");
        PlayButtonClickSFX();
        RefreshPanel();
    }

    private void AddHeader(string title, Color color)
    {
        currentBuildingCategory = title;
        bool isExpanded = IsCategoryExpanded(title);

        GameObject obj = Instantiate(statRowPrefab, statListContent, false);
        obj.SetActive(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
        }

        LayoutElement layoutElement = obj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = obj.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 36f;
        layoutElement.preferredHeight = 40f;

        StatRowUI row = obj.GetComponent<StatRowUI>();
        if (row != null)
        {
            row.SetupAsHeader(title, color, () => ToggleCategory(title), isExpanded);
            spawnedRows.Add(row);
        }
    }

    private void AddRow(string label, string value, Color valColor)
    {
        bool isExpanded = IsCategoryExpanded(currentBuildingCategory);

        GameObject obj = Instantiate(statRowPrefab, statListContent, false);
        obj.SetActive(isExpanded);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
        }

        LayoutElement layoutElement = obj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = obj.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 32f;
        layoutElement.preferredHeight = 36f;

        StatRowUI row = obj.GetComponent<StatRowUI>();
        if (row != null)
        {
            row.Setup(label, value, valColor);
            spawnedRows.Add(row);
        }
    }

    private float GetAxeMultiplierForSeason(MevsimTuru season, SeasonSettings settings)
    {
        if (settings == null) return 1f;
        switch (season)
        {
            case MevsimTuru.Ilkbahar: return settings.ilkbaharBaltaHasari;
            case MevsimTuru.Yaz: return settings.yazBaltaHasari;
            case MevsimTuru.Sonbahar: return settings.sonbaharBaltaHasari;
            case MevsimTuru.Kis: return settings.kisBaltaHasari;
            default: return 1f;
        }
    }

    private int GetEquippedAxeTier(string axeId)
    {
        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        // 1. Kaydedilmiş kuşanılan balta tercihi bir seviye değil, 1 tabanlı balta indeksidir.
        if (data != null && data.ItemLevels != null && data.ItemLevels.ContainsKey("EquippedAxe"))
        {
            return Mathf.Clamp(data.GetItemLevel("EquippedAxe"), 1, 5);
        }

        // 2. Balta masası çalışıyorsa onun güncel seçimini kullan.
        if (AxeTablePanelController.Instance != null)
        {
            return Mathf.Clamp(AxeTablePanelController.Instance.CurrentlyEquippedTier, 1, 5);
        }

        // 3. Görsel yöneticisinin gerçek model ID'sinden çözümle.
        if (!string.IsNullOrWhiteSpace(axeId))
        {
            if (axeId.Equals("lowpoly_axe", StringComparison.OrdinalIgnoreCase) ||
                axeId.Equals("Axe", StringComparison.OrdinalIgnoreCase))
                return 1;

            string normalizedId = axeId
                .Replace("axe_replace_", "", StringComparison.OrdinalIgnoreCase)
                .Replace("axe_tier_", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Axe", "", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(normalizedId, out int tier))
                return Mathf.Clamp(tier, 1, 5);
        }

        return 1;
    }

    private string GetAxeItemId(int tier)
    {
        return tier <= 1 ? "Axe" : $"Axe{Mathf.Clamp(tier, 2, 5)}";
    }

    private string GetEquippedAxeName(int tier)
    {
        if (AxeTablePanelController.Instance != null &&
            AxeTablePanelController.Instance.CurrentlyEquippedTier == tier &&
            !string.IsNullOrWhiteSpace(AxeTablePanelController.Instance.CurrentlyEquippedDisplayName))
        {
            return AxeTablePanelController.Instance.CurrentlyEquippedDisplayName;
        }

        return $"Balta {Mathf.Clamp(tier, 1, 5)}";
    }

    private int GetAxeItemLevel(string itemId, PlayerData data)
    {
        if (UpgradeManager.Instance != null)
            return Mathf.Max(1, UpgradeManager.Instance.GetItemLevel(itemId));

        return data != null ? Mathf.Max(1, data.GetItemLevel(itemId)) : 1;
    }

    private float GetAxePower(string itemId, int level)
    {
        if (UpgradeManager.Instance != null)
            return UpgradeManager.Instance.GetItemValueAtLevel(itemId, Mathf.Max(1, level));

        return 1f;
    }

    private int GetTruckLevel(PlayerData data)
    {
        if (UpgradeManager.Instance != null)
            return Mathf.Clamp(UpgradeManager.Instance.GetItemLevel("Truck"), 1, 10);

        return data != null ? Mathf.Clamp(data.GetItemLevel("Truck"), 1, 10) : 1;
    }

    private int GetBasketLevel(PlayerData data)
    {
        if (UpgradeManager.Instance != null)
            return Mathf.Clamp(UpgradeManager.Instance.GetItemLevel("Basket"), 1, 5);

        return data != null ? Mathf.Clamp(data.GetItemLevel("Basket"), 1, 5) : 1;
    }

    private int GetBasketCapacity(int level)
    {
        if (UpgradeManager.Instance != null)
            return Mathf.RoundToInt(UpgradeManager.Instance.GetItemValueAtLevel("Basket", Mathf.Clamp(level, 1, 5)));

        return 6 + (Mathf.Clamp(level, 1, 5) - 1) * 2;
    }

    private int GetTruckCapacity(int level)
    {
        return 30 + (Mathf.Clamp(level, 1, 10) - 1) * 5;
    }

    private int GetTruckModel(int level)
    {
        return Mathf.Clamp(((Mathf.Clamp(level, 1, 10) - 1) / 2) + 1, 1, 5);
    }

    private float GetTreeGrowthMultiplierForSeason(MevsimTuru season, SeasonSettings settings)
    {
        if (settings == null) return 1f;
        switch (season)
        {
            case MevsimTuru.Ilkbahar: return settings.ilkbaharAgacBuyumeHizi;
            case MevsimTuru.Kis: return settings.kisAgacBuyumeHizi;
            default: return 1.0f;
        }
    }

    private void ClearSpawnedRows()
    {
        if (statListContent != null)
        {
            for (int i = statListContent.childCount - 1; i >= 0; i--)
            {
                Transform child = statListContent.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
        spawnedRows.Clear();
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }
}
