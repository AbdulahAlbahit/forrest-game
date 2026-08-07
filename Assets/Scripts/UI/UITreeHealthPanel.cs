using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ForrestGame.UI
{
    public class UITreeHealthPanel : MonoBehaviour
    {
        [Header("--- Ayarlar ---")]
        [Tooltip("Hasar yazısı panelinin ne kadar süre ekranda kalacağı (saniye).")]
        [SerializeField] private float displayDuration = 3f;
        [Tooltip("Panelin belirme/kaybolma hızı.")]
        [SerializeField] private float fadeSpeed = 5f;

        [Header("--- Referanslar ---")]
        [Tooltip("Panelin genel saydamlığını kontrol etmek için CanvasGroup.")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Tooltip("Yüzdelik değeri gösterecek metin. (Örn: %75)")]
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Tooltip("Can barını gösterecek slider. (Opsiyonel)")]
        [SerializeField] private Slider healthSlider;

        private float targetAlpha = 0f;
        private float hideTimer = 0f;
        private float targetSliderValue = 1f;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.value = 1f;
            }

            // Başlangıçta görünmez yap
            canvasGroup.alpha = 0f;
            targetAlpha = 0f;
        }

        private void OnEnable()
        {
            TreeManager.OnTreeHealthChanged += HandleTreeHealthChanged;
        }

        private void OnDisable()
        {
            TreeManager.OnTreeHealthChanged -= HandleTreeHealthChanged;
        }

        private void Update()
        {
            // Zamanlayıcıyı düşür
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                {
                    targetAlpha = 0f; // Süre dolduğunda gizle
                }
            }

            // Smooth Fade işlemi
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }

            // Slider için yumuşak azalış animasyonu
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Lerp(healthSlider.value, targetSliderValue, Time.deltaTime * 10f);
            }
        }

        private void HandleTreeHealthChanged(int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0) return;

            // Paneli görünür yap ve zamanlayıcıyı sıfırla
            targetAlpha = 1f;
            hideTimer = displayDuration;

            // Can yüzdesini hesapla
            float healthPercent = Mathf.Clamp01((float)currentHealth / maxHealth);
            int displayPercent = Mathf.RoundToInt(healthPercent * 100f);

            // İlk kez belirdiğinde slider'ı anında doldur
            if (canvasGroup != null && canvasGroup.alpha < 0.05f && healthSlider != null)
            {
                healthSlider.value = healthPercent + ((float)1 / maxHealth); // Bir önceki vuruşun canından başlasın
            }

            // Hedef slider değerini güncelle (Update içinde yumuşakça azalacak)
            targetSliderValue = healthPercent;

            // Metni güncelle
            if (healthText != null)
            {
                if (currentHealth <= 0)
                {
                    healthText.text = "Ağaç Devrildi!";
                }
                else
                {
                    healthText.text = $"% {displayPercent}";
                }
            }

            // Ağaç öldüyse süreyi kısa tutarak hızlıca gizle
            if (currentHealth <= 0)
            {
                hideTimer = 1.5f; 
            }
        }
    }
}
