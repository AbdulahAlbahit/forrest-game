using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;

namespace ForrestGame.UI
{
    /// <summary>
    /// Atölye üretimi sürerken, Atölye Paneli kapalı olsa dahi HUD üzerinde mini
    /// canlı ilerleme widget'ı gösteren UI bileşeni.
    /// 
    /// KULLANIM:
    /// 1. HUD Canvas üzerine (örneğin sol üst veya sağ üst köşe) yerleştirin.
    /// 2. Mini progress bar (Image - Filled) ve metin referanslarını bağlayın.
    /// 3. Atölyede aktif üretim olduğunda otomatik belirir, bittiğinde veya panel açıldığında gizlenir.
    /// </summary>
    public class WorkshopStatusHUD : MonoBehaviour
    {
        [Header("--- UI Referansları ---")]
        [Tooltip("Mini widget ana konteyneri (Boş bırakılırsa bu scriptin olduğu çocuk objeleri gizler/gösterir).")]
        [SerializeField] private GameObject miniHUDContainer;

        [Tooltip("Canlı yüzde doluluk görseli (Image Type = Filled, Fill Method = Horizontal).")]
        [SerializeField] private Image fillImage;

        [Tooltip("İlerleme metni (Örn: %45).")]
        [SerializeField] private TextMeshProUGUI progressText;

        [Tooltip("Üretilen mobilya ikon görseli (Opsiyonel).")]
        [SerializeField] private Image furnitureIcon;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            EnsureCanvasGroup();
            SetVisibility(false);
        }

        private void OnEnable()
        {
            EnsureCanvasGroup();
        }

        private void EnsureCanvasGroup()
        {
            GameObject targetObj = (miniHUDContainer != null && miniHUDContainer != gameObject) ? miniHUDContainer : gameObject;
            if (canvasGroup == null)
            {
                canvasGroup = targetObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = targetObj.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Update()
        {
            UpdateMiniHUD();
        }

        private void UpdateMiniHUD()
        {
            // Eğer büyük atölye paneli zaten açıksa, ekranda kalabalık etmesin gizle
            if (WorkshopPanelController.Instance != null && WorkshopPanelController.Instance.gameObject.activeInHierarchy)
            {
                SetVisibility(false);
                return;
            }

            if (WorkshopManager.Instance == null)
            {
                SetVisibility(false);
                return;
            }

            float progress = WorkshopManager.Instance.GetProcessingProgress();
            // Üretim aktif mi kontrol et (ilerleme 0'dan büyük ve 1'den küçük veya sıradaki üretilen eşya var)
            bool isProcessing = progress > 0f || (CloudSaveManager.Instance?.CurrentData?.WorkshopQueue != null && CloudSaveManager.Instance.CurrentData.WorkshopQueue.ToDictionary().Count > 0);

            if (!isProcessing)
            {
                SetVisibility(false);
                return;
            }

            SetVisibility(true);

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(progress);

            if (progressText != null)
            {
                int percentage = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f);
                progressText.text = $"%{percentage}";
            }
        }

        private void SetVisibility(bool visible)
        {
            EnsureCanvasGroup();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = false;
            }

            // miniHUDContainer ayrı bir çocuk obje ise onun SetActive'ini de güncelle, ama kendi gameObject'imizi KAPATMA (Update çalışsın)
            if (miniHUDContainer != null && miniHUDContainer != gameObject)
            {
                miniHUDContainer.SetActive(visible);
            }
        }

        [ContextMenu("TEST: Görünürlüğü Aç/Kapat")]
        public void TestToggleVisibility()
        {
            SetVisibility(canvasGroup == null || canvasGroup.alpha < 0.5f);
        }
    }
}
