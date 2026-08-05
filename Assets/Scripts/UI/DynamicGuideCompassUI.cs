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
            int currentLevel = GetPlayerLevel();

            // Seviye 5 ve üzeriyse dinamik rehberi kapat
            if (currentLevel >= maxGuideLevel)
            {
                if (showInteractionPrompts && InteractionPromptUI.Instance != null && InteractionPromptUI.Instance.IsVisible)
                {
                    InteractionPromptUI.Instance.HidePrompt();
                }
                return;
            }

            Transform newTarget = null;
            string newPrompt = "";

            int money = GetPlayerMoney();
            bool isBasketFull = IsBasketFull();
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

            // 🟢 DURUM 2: Sepet dolu -> Kamyona / Yükleme alanına götür
            if (newTarget == null && isBasketFull)
            {
                var cargoZone = FindFirstObjectByType<CargoTransferZone>();
                if (cargoZone != null)
                {
                    newTarget = cargoZone.transform;
                }
                else
                {
                    var truck = FindFirstObjectByType<TruckRouteController>();
                    if (truck != null) newTarget = truck.transform;
                }
                newPrompt = "Odunları Kamyona Yükle!";
            }

            // 🟢 DURUM 3: Sepet boş veya odun toplanıyor -> En yakın Ağaca git
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
                {
                    compassMarker.SetTarget(activeTarget);
                }
            }

            if (showInteractionPrompts && !string.IsNullOrEmpty(newPrompt) && newPrompt != currentPromptMessage)
            {
                currentPromptMessage = newPrompt;
                InteractionPromptUI.Instance?.ShowPrompt(currentPromptMessage);
            }
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
