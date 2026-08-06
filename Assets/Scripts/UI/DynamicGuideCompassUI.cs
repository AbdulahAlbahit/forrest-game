using System.Collections;
using UnityEngine;
using ForrestGame.Core;
using ForrestGame.Managers;

namespace ForrestGame.UI
{
    /// <summary>
    /// Oyuncu Seviye 5 olana kadar dinamik tutorial / rehber yönlendirmesini yöneten UI kontrolcüsü.
    /// 
    /// AKIŞ (Seviye < 5 iken):
    /// 1. Balta yükseltmek için yeterli altın varsa -> Baltayı Yükseltmasası'na yönlendirir.
    /// 2. Sepet doluysa -> Kamyon Yükleme Alanı'na yönlendirir.
    /// 3. Odun gerekiyorsa -> En yakın Ağaç'a yönlendirir.
    /// 4. Seviye 5 ve üzerine ulaşılınca rehber otomatik kapanır.
    /// </summary>
    [RequireComponent(typeof(CompassMarkerUI))]
    public class DynamicGuideCompassUI : MonoBehaviour
    {
        public static DynamicGuideCompassUI Instance { get; private set; }

        [Header("--- Ayarlar ---")]
        [Tooltip("Rehberin aktif kalacağı maksimum oyuncu seviyesi.")]
        [SerializeField] private int maxGuideLevel = 5;

        [Tooltip("Ekran altında etkileşim ipucu yazıları da gösterilsin mi?")]
        [SerializeField] private bool showInteractionPrompts = true;

        [Tooltip("Hedef arama sıklığı (saniye).")]
        [SerializeField] private float updateInterval = 0.35f;

        private CompassMarkerUI compassMarker;
        private Transform activeTarget;
        private string currentPromptMessage = "";
        private Coroutine guideRoutine;

        // ─── Sepet Dolu Zinciri ───
        private bool basketWasFullLastFrame = false;
        private Coroutine basketPulseCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            compassMarker = GetComponent<CompassMarkerUI>();
        }

        private void OnEnable()
        {
            if (guideRoutine != null) StopCoroutine(guideRoutine);
            guideRoutine = StartCoroutine(GuideLoop());
        }

        private void OnDisable()
        {
            if (guideRoutine != null)
            {
                StopCoroutine(guideRoutine);
                guideRoutine = null;
            }
            if (showInteractionPrompts && InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.HidePrompt();
            }
        }

        private IEnumerator GuideLoop()
        {
            while (isActiveAndEnabled)
            {
                EvaluateGuideState();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void EvaluateGuideState()
        {
            bool isBasketFull = IsBasketFull();

            // ─────────────────────────────────────────────────────────────────
            // DURUM 0: Sepet DOLU → Kamyona yönlendir (SEVİYEDEN BAĞIMSIZ)
            // ─────────────────────────────────────────────────────────────────
            if (isBasketFull)
            {
                Transform truckTarget = FindTruckOrLoadingZone();

                if (truckTarget != activeTarget)
                {
                    activeTarget = truckTarget;
                    if (compassMarker != null)
                        compassMarker.SetTarget(activeTarget);
                }

                // İlk kez doldu → pulse başlat
                if (!basketWasFullLastFrame)
                {
                    basketWasFullLastFrame = true;
                    StartBasketPulse();
                }

                const string truckPrompt = "Odunları Kamyona Yükle!";
                if (showInteractionPrompts && truckPrompt != currentPromptMessage)
                {
                    currentPromptMessage = truckPrompt;
                    InteractionPromptUI.Instance?.ShowPrompt(currentPromptMessage);
                }
                return;
            }

            // Sepet boşaldı → pulse durdur ve ipucu gizle
            if (basketWasFullLastFrame)
            {
                basketWasFullLastFrame = false;
                StopBasketPulse();
                if (showInteractionPrompts && InteractionPromptUI.Instance != null)
                {
                    InteractionPromptUI.Instance.HidePrompt();
                    currentPromptMessage = "";
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // Seviye kısıtlı rehber (Seviye 5 ve üzerinde kapat)
            // ─────────────────────────────────────────────────────────────────
            int currentLevel = GetPlayerLevel();
            if (currentLevel >= maxGuideLevel)
            {
                if (showInteractionPrompts && InteractionPromptUI.Instance != null && InteractionPromptUI.Instance.IsVisible)
                    InteractionPromptUI.Instance.HidePrompt();
                if (activeTarget != null)
                {
                    activeTarget = null;
                    compassMarker?.SetTarget(null);
                }
                return;
            }

            Transform newTarget = null;
            string newPrompt = "";

            int money = GetPlayerMoney();
            int currentWood = GetCurrentWoodCount();

            // 🟢 DURUM 1: Yeterli para var ve Balta yükseltilebilir -> Balta Masasına git
            if (money >= 100 && IsAxeUpgradeAffordable(money))
            {
                var axeTable = FindFirstObjectByType<AxeTableInteraction>();
                if (axeTable != null)
                {
                    newTarget = axeTable.transform;
                    newPrompt = "Baltanı Yükselt!";
                }
            }

            // 🟢 DURUM 2: Sepet boş veya odun toplanıyor -> En yakın Ağaca git
            if (newTarget == null)
            {
                newTarget = FindNearestTree();
                newPrompt = currentWood > 0 ? "Ağaç Kesmeye Devam Et" : "Ağaç Kes!";
            }

            // Hedef veya mesaj değiştiyse güncelle
            if (newTarget != activeTarget)
            {
                activeTarget = newTarget;
                if (compassMarker != null)
                    compassMarker.SetTarget(activeTarget);
            }

            if (showInteractionPrompts && !string.IsNullOrEmpty(newPrompt) && newPrompt != currentPromptMessage)
            {
                currentPromptMessage = newPrompt;
                InteractionPromptUI.Instance?.ShowPrompt(currentPromptMessage);
            }
        }

        // ─── Sepet Dolu Pulse Animasyonu ───

        private Transform FindTruckOrLoadingZone()
        {
            var cargoZone = FindFirstObjectByType<CargoTransferZone>();
            if (cargoZone != null) return cargoZone.transform;
            var truck = FindFirstObjectByType<TruckRouteController>();
            return truck != null ? truck.transform : null;
        }

        private void StartBasketPulse()
        {
            if (basketPulseCoroutine != null)
                StopCoroutine(basketPulseCoroutine);
            if (compassMarker != null)
                basketPulseCoroutine = StartCoroutine(BasketPulseRoutine());
        }

        private void StopBasketPulse()
        {
            if (basketPulseCoroutine != null)
            {
                StopCoroutine(basketPulseCoroutine);
                basketPulseCoroutine = null;
            }
            // RectTransform ölçeğini sıfırla
            if (compassMarker != null)
            {
                RectTransform rt = compassMarker.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one;
            }
        }

        private System.Collections.IEnumerator BasketPulseRoutine()
        {
            RectTransform rt = compassMarker != null ? compassMarker.GetComponent<RectTransform>() : null;
            if (rt == null) yield break;

            while (isActiveAndEnabled && basketWasFullLastFrame)
            {
                // Büyüt
                float elapsed = 0f;
                float half = 0.22f;
                while (elapsed < half)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / half);
                    rt.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * 1.28f, t * (2f - t));
                    yield return null;
                }
                // Küçült
                elapsed = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / half);
                    rt.localScale = Vector3.LerpUnclamped(Vector3.one * 1.28f, Vector3.one, t * (2f - t));
                    yield return null;
                }
                rt.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(0.55f);
            }

            rt.localScale = Vector3.one;
            basketPulseCoroutine = null;
        }

        private int GetPlayerLevel()
        {
            if (XPManager.Instance != null) return XPManager.Instance.MevcutSeviye;
            if (CloudSaveManager.Instance?.CurrentData != null) return CloudSaveManager.Instance.CurrentData.OyuncuSeviyesi;
            return 1;
        }

        private int GetPlayerMoney()
        {
            if (EconomyManager.Instance != null) return EconomyManager.Instance.MevcutPara;
            if (CloudSaveManager.Instance?.CurrentData != null) return CloudSaveManager.Instance.CurrentData.Para;
            return 0;
        }

        private bool IsBasketFull()
        {
            if (BasketManager.Instance != null) return BasketManager.Instance.IsFull;
            var efeOdun = FindFirstObjectByType<EfeOdunTasima>();
            if (efeOdun != null) return efeOdun.SepetDolu;
            return false;
        }

        private int GetCurrentWoodCount()
        {
            if (BasketManager.Instance != null) return BasketManager.Instance.MevcutDoluluk;
            var efeOdun = FindFirstObjectByType<EfeOdunTasima>();
            if (efeOdun != null) return efeOdun.TasinanOdunSayisi;
            return 0;
        }

        private bool IsAxeUpgradeAffordable(int currentMoney)
        {
            if (UpgradeManager.Instance != null)
            {
                string equippedAxeId = AxeTablePanelController.Instance != null ? AxeTablePanelController.Instance.CurrentlyEquippedItemId : "Axe";
                if (string.IsNullOrEmpty(equippedAxeId)) equippedAxeId = "Axe";

                int cost = UpgradeManager.Instance.GetUpgradeCost(equippedAxeId);
                return cost > 0 && currentMoney >= cost;
            }

            return currentMoney >= 150;
        }

        private Transform FindNearestTree()
        {
            Vector3 playerPos = Vector3.zero;

            // Oyuncuyu bul (CharacterController, EfeKarakterKontrol veya Tag ile)
            CharacterController cc = FindFirstObjectByType<CharacterController>();
            if (cc != null)
            {
                playerPos = cc.transform.position;
            }
            else
            {
                try
                {
                    var pObj = GameObject.FindGameObjectWithTag("Player");
                    if (pObj != null) playerPos = pObj.transform.position;
                }
                catch { }
            }

            // Sahnedeki Ağaç / Tree tag'li nesneleri dene
            try
            {
                GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
                if (trees != null && trees.Length > 0)
                {
                    float minDistance = float.MaxValue;
                    Transform closest = null;
                    foreach (var tree in trees)
                    {
                        if (tree == null || !tree.activeInHierarchy) continue;
                        float dist = Vector3.Distance(playerPos, tree.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closest = tree.transform;
                        }
                    }
                    if (closest != null) return closest;
                }
            }
            catch { }

            // Fallback 1: TreeManager nesnesi
            if (TreeManager.Instance != null)
            {
                return TreeManager.Instance.transform;
            }

            // Fallback 2: Kamyon veya Atölye
            var truck = FindFirstObjectByType<TruckRouteController>();
            return truck != null ? truck.transform : transform;
        }
    }
}
