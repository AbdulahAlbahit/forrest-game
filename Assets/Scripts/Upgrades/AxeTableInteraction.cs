using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Evdeki balta masasini yakin mesafeden fare veya mobil dokunusla acar.
/// Satin alma islemini hazir AxeTablePanelController ve UpgradeManager yapar.
/// </summary>
[DisallowMultipleComponent]
public class AxeTableInteraction : MonoBehaviour
{
    [Header("Hazir Sistem Baglantilari")]
    [SerializeField] private AxeTablePanelController panelController;
    [SerializeField] private UpgradeZoneController axeUpgradeZone;
    [SerializeField] private Transform player;
    [SerializeField] private Camera interactionCamera;

    [Header("Etkilesim")]
    [Min(0.5f)] [SerializeField] private float interactionDistance = 2.5f;
    [Min(1)] [SerializeField] private int baseTreeHitPoints = 3;

    [Header("Panel Bilgi Alanlari")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text currentLevelText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text priceText;

    private int _lastPointerFrame = -1;
    private bool _upgradeSubscribed;
    private bool _economySubscribed;

    private void Awake()
    {
        ResolveMissingReferences();
    }

    private void Start()
    {
        ResolveMissingReferences();
        Subscribe();
        RefreshPanelInformation();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        Subscribe();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
            !IsPointerOverUi())
        {
            TryOpenFromScreenPoint(Mouse.current.position.ReadValue());
        }

        if (Touchscreen.current == null) return;
        foreach (UnityEngine.InputSystem.Controls.TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.wasPressedThisFrame) continue;
            int touchId = touch.touchId.ReadValue();
            if (!IsPointerOverUi(touchId))
            {
                TryOpenFromScreenPoint(touch.position.ReadValue());
            }
            break;
        }
    }

    public bool TryOpenPanel()
    {
        ResolveMissingReferences();
        if (panelController == null || player == null)
        {
            return false;
        }

        Vector3 tablePoint = GetComponent<Collider>() != null
            ? GetComponent<Collider>().ClosestPoint(player.position)
            : transform.position;
        if (Vector3.Distance(player.position, tablePoint) > interactionDistance)
        {
            NotificationUIController.Instance?.ShowNotification("Balta masasina biraz daha yaklas.");
            return false;
        }

        panelController.OpenPanel();
        RefreshPanelInformation();
        return true;
    }

    public void RefreshPanelInformation()
    {
        if (axeUpgradeZone == null) return;

        int currentLevel = Mathf.Max(1, axeUpgradeZone.CurrentLevel);
        bool isMaxLevel = UpgradeManager.Instance != null &&
                          UpgradeManager.Instance.IsMaxLevel(axeUpgradeZone.ItemId);
        int currentHits = CalculateHitsForLevel(currentLevel);

        if (titleText != null)
            titleText.text = "Balta Gelistirme";
        if (currentLevelText != null)
            currentLevelText.text = $"Seviye {currentLevel} - Agac: {currentHits} vurus";

        if (nextLevelText != null)
        {
            nextLevelText.text = isMaxLevel
                ? "Maksimum seviyeye ulasildi"
                : $"Sonraki: Agac {CalculateHitsForLevel(currentLevel + 1)} vurusta kirilir";
        }

        if (priceText != null)
        {
            priceText.text = isMaxLevel
                ? "MAKSIMUM SEVIYE"
                : $"Fiyat: {axeUpgradeZone.CurrentPrice} Para";
        }
    }

    private void TryOpenFromScreenPoint(Vector2 screenPoint)
    {
        if (_lastPointerFrame == Time.frameCount) return;
        _lastPointerFrame = Time.frameCount;

        ResolveMissingReferences();
        if (interactionCamera == null) return;

        Ray ray = interactionCamera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) &&
            (hit.transform == transform || hit.transform.IsChildOf(transform)))
        {
            TryOpenPanel();
        }
    }

    private int CalculateHitsForLevel(int level)
    {
        float damage = 1f;
        if (UpgradeManager.Instance != null)
        {
            damage = UpgradeManager.Instance.GetItemValueAtLevel("Axe", Mathf.Max(1, level));
        }

        return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, baseTreeHitPoints) / Mathf.Max(1f, damage)));
    }

    private void ResolveMissingReferences()
    {
        if (interactionCamera == null) interactionCamera = Camera.main;
        if (player == null)
        {
            EfeBaltaKontrol axeControl = FindAnyObjectByType<EfeBaltaKontrol>();
            if (axeControl != null) player = axeControl.transform;
        }
        if (panelController == null)
            panelController = FindAnyObjectByType<AxeTablePanelController>(FindObjectsInactive.Include);
    }

    private void Subscribe()
    {
        if (!_upgradeSubscribed && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;
            _upgradeSubscribed = true;
        }

        if (!_economySubscribed && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            _economySubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (_upgradeSubscribed && UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;
        if (_economySubscribed && EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;

        _upgradeSubscribed = false;
        _economySubscribed = false;
    }

    private void HandleItemUpgraded(string itemId, int newLevel)
    {
        if (itemId == "Axe") RefreshPanelInformation();
    }

    private void HandleMoneyChanged(int newMoney)
    {
        RefreshPanelInformation();
    }

    private static bool IsPointerOverUi(int pointerId = -1)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        if (pointerId != -1 && Touchscreen.current != null)
        {
            // Yeni input sisteminde Touch Id direkt islesemiyor, o yuzden Touch posizyonunu kullanmak en dogrusu
            eventData.position = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            eventData.position = Mouse.current.position.ReadValue();
        }
        else
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}
