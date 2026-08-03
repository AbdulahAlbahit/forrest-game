using UnityEngine;
using UnityEngine.UI;

namespace ForrestGame.UI
{
    /// <summary>
    /// Ekranın kenarında dönen 2D yön oku (Compass / Waypoint Marker) UI scripti.
    /// Oyuncu dünya nesnelerine (Kamyon, Atölye, Ağaç vb.) bakmıyorken veya hedef ekran dışındayken
    /// ekranın kenarlarında hedef yönünü gösteren oku konumlandırır ve döndürür.
    /// 
    /// KULLANIM:
    /// 1. Canvas altına bir RectTransform (ve Image ikonu) ekleyin.
    /// 2. Bu scripti ekleyin, Inspector'dan targetWorldObject ve gameCamera referanslarını atayın.
    /// </summary>
    public class CompassMarkerUI : MonoBehaviour
    {
        [Header("--- Hedef ve Kamera ---")]
        [Tooltip("Takip edilecek dünya nesnesi (Kamyon, Atölye vb.).")]
        [SerializeField] private Transform targetWorldObject;

        [Tooltip("Oyun içi ana kamera.")]
        [SerializeField] private Camera gameCamera;

        [Header("--- UI Elemanları ---")]
        [Tooltip("Ekran kenarında dönecek olan ok / marker RectTransform'u.")]
        [SerializeField] private RectTransform markerRectTransform;

        [Tooltip("Ok görseli.")]
        [SerializeField] private Image markerImage;

        [Header("--- Ayarlar ---")]
        [Tooltip("Ekran kenarından içe doğru güvenlik payı (Margin).")]
        [SerializeField] private float edgeMargin = 40f;

        [Tooltip("Hedef ekranda görünür durumdayken ok gizlensin mi?")]
        [SerializeField] private bool hideWhenOnScreen = true;

        [Header("--- Otomatik Hedef Arama ---")]
        [Tooltip("Inspector'dan hedef atanmazsa sahnedeki Kamyon/Atölye'yi otomatik hedef alsın mı?")]
        [SerializeField] private bool autoFindTargetIfNull = true;

        [Tooltip("Otomatik arama türü.")]
        [SerializeField] private AutoTargetType autoTarget = AutoTargetType.Truck;

        public enum AutoTargetType
        {
            Truck,
            Workshop,
            Player
        }

        private Canvas parentCanvas;

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (gameCamera == null)
                gameCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            if (markerRectTransform == null)
                markerRectTransform = GetComponent<RectTransform>();

            if (markerImage == null)
                markerImage = GetComponent<Image>();

            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>();
        }

        private void LateUpdate()
        {
            if (gameCamera == null)
                gameCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            if (targetWorldObject == null && autoFindTargetIfNull)
            {
                TryAutoFindTarget();
            }

            UpdateMarkerPositionAndRotation();
        }

        public void SetTarget(Transform newTarget)
        {
            targetWorldObject = newTarget;
        }

        private void TryAutoFindTarget()
        {
            switch (autoTarget)
            {
                case AutoTargetType.Truck:
                    var truck = FindFirstObjectByType<TruckRouteController>();
                    if (truck != null) targetWorldObject = truck.transform;
                    break;
                case AutoTargetType.Workshop:
                    var workshop = FindFirstObjectByType<WorkshopManager>();
                    if (workshop != null) targetWorldObject = workshop.transform;
                    break;
            }
        }

        private void UpdateMarkerPositionAndRotation()
        {
            if (targetWorldObject == null || gameCamera == null || markerRectTransform == null)
            {
                SetVisibility(false);
                return;
            }

            Vector3 worldPos = targetWorldObject.position;
            Vector3 screenPos = gameCamera.WorldToScreenPoint(worldPos);
            bool isBehindCamera = screenPos.z < 0;

            // Kamera arkasındaysa koordinatı tersle (ayna etkisi)
            if (isBehindCamera)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            float minX = edgeMargin;
            float maxX = Screen.width - edgeMargin;
            float minY = edgeMargin;
            float maxY = Screen.height - edgeMargin;

            bool isOnScreen = !isBehindCamera && 
                              screenPos.x >= minX && screenPos.x <= maxX &&
                              screenPos.y >= minY && screenPos.y <= maxY;

            if (isOnScreen && hideWhenOnScreen)
            {
                SetVisibility(false);
                return;
            }

            SetVisibility(true);

            // Ekran merkezinden hedef yönünü hesapla
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = ((Vector2)screenPos - screenCenter).normalized;

            // Ekran kenarına kelepçele (Clamping)
            Vector2 clampedScreenPos = screenPos;
            clampedScreenPos.x = Mathf.Clamp(clampedScreenPos.x, minX, maxX);
            clampedScreenPos.y = Mathf.Clamp(clampedScreenPos.y, minY, maxY);

            // Canvas tipine göre pozisyon belirle (Overlay veya Camera)
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransform canvasRect = parentCanvas.transform as RectTransform;
                if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, clampedScreenPos, parentCanvas.worldCamera, out Vector2 localPoint))
                {
                    markerRectTransform.anchoredPosition = localPoint;
                }
            }
            else
            {
                markerRectTransform.position = clampedScreenPos;
            }

            // Ok dönüş açısını hesapla (Z rotasyonu)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            markerRectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        private void SetVisibility(bool visible)
        {
            if (markerImage != null)
                markerImage.enabled = visible;
            else if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        [ContextMenu("TEST: Sahnedeki Kamyonu Hedef Al")]
        public void TestTargetTruck()
        {
            autoTarget = AutoTargetType.Truck;
            TryAutoFindTarget();
        }

        [ContextMenu("TEST: Sahnedeki Atölyeyi Hedef Al")]
        public void TestTargetWorkshop()
        {
            autoTarget = AutoTargetType.Workshop;
            TryAutoFindTarget();
        }
    }
}
