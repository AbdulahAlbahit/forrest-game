using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ForrestGame.Utils;

namespace ForrestGame.Managers
{
    [Serializable]
    public class AxeVisualEntry
    {
        public string axeId;
        public GameObject model;
    }

    public class AxeVisualManager : MonoBehaviour
    {
        private const string EquippedAxeSaveKey = "EquippedAxe";
        private const string AxeOwnershipSavePrefix = "AxeOwned_";

        public static AxeVisualManager Instance { get; private set; }

        [Header("Axe Data")]
        public string currentAxeId = "lowpoly_axe";
        [SerializeField] private List<AxeVisualEntry> axeVisuals = new List<AxeVisualEntry>();

        public static event Action<string> OnAxeChanged;

        private CloudSaveManager subscribedCloudSaveManager;
        private Coroutine cloudSaveSubscriptionRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                ApplyCurrentVisual();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
                return;

            cloudSaveSubscriptionRoutine = StartCoroutine(SubscribeToCloudSaveWhenReady());
        }

        private void OnDisable()
        {
            if (cloudSaveSubscriptionRoutine != null)
            {
                StopCoroutine(cloudSaveSubscriptionRoutine);
                cloudSaveSubscriptionRoutine = null;
            }

            UnsubscribeFromCloudSave();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCloudSave();
            if (Instance == this)
                Instance = null;
        }

        private IEnumerator SubscribeToCloudSaveWhenReady()
        {
            while (isActiveAndEnabled && CloudSaveManager.Instance == null)
                yield return null;

            if (!isActiveAndEnabled)
                yield break;

            SubscribeToCloudSave(CloudSaveManager.Instance);
            cloudSaveSubscriptionRoutine = null;
        }

        private void SubscribeToCloudSave(CloudSaveManager cloudSaveManager)
        {
            if (cloudSaveManager == null || subscribedCloudSaveManager == cloudSaveManager)
                return;

            UnsubscribeFromCloudSave();
            subscribedCloudSaveManager = cloudSaveManager;
            subscribedCloudSaveManager.OnDataLoaded += HandleDataLoaded;

            if (subscribedCloudSaveManager.CurrentData != null)
                ApplySavedAxe(subscribedCloudSaveManager.CurrentData);
        }

        private void UnsubscribeFromCloudSave()
        {
            if (subscribedCloudSaveManager == null)
                return;

            subscribedCloudSaveManager.OnDataLoaded -= HandleDataLoaded;
            subscribedCloudSaveManager = null;
        }

        private void HandleDataLoaded(PlayerData data)
        {
            ApplySavedAxe(data);
        }

        private void ApplySavedAxe(PlayerData data)
        {
            if (data == null || axeVisuals == null || axeVisuals.Count == 0)
                return;

            int savedTier = data.ItemLevels != null && data.ItemLevels.ContainsKey(EquippedAxeSaveKey)
                ? data.GetItemLevel(EquippedAxeSaveKey)
                : 1;
            int visualIndex = Mathf.Clamp(savedTier - 1, 0, axeVisuals.Count - 1);
            AxeVisualEntry savedVisual = axeVisuals[visualIndex];

            string savedItemId = savedTier <= 1 ? "Axe" : $"Axe{savedTier}";
            if (!IsAxeOwned(data, savedTier, savedItemId))
            {
                savedTier = 1;
                visualIndex = 0;
                savedVisual = axeVisuals[visualIndex];
                data.SetItemLevel(EquippedAxeSaveKey, savedTier);
                subscribedCloudSaveManager?.SaveData(data);
            }

            if (savedVisual == null || savedVisual.model == null || string.IsNullOrWhiteSpace(savedVisual.axeId))
                savedVisual = axeVisuals.FirstOrDefault(entry => entry != null && entry.model != null);

            if (savedVisual != null)
                EquipAxe(savedVisual.axeId);
        }

        public static string GetOwnershipSaveKey(string itemId)
        {
            return AxeOwnershipSavePrefix + (string.IsNullOrWhiteSpace(itemId) ? "Axe" : itemId.Trim());
        }

        public static bool IsAxeOwned(PlayerData data, int tier, string itemId)
        {
            if (tier <= 1)
                return true;

            string ownershipKey = GetOwnershipSaveKey(itemId);
            return data != null &&
                   data.ItemLevels != null &&
                   data.ItemLevels.ContainsKey(ownershipKey) &&
                   data.GetItemLevel(ownershipKey) > 0;
        }

        public void EquipAxe(string newAxeId)
        {
            if (string.IsNullOrWhiteSpace(newAxeId)) return;

            currentAxeId = newAxeId;
            ApplyCurrentVisual();
            GameLogger.Log($"[AxeVisualManager] Equipped new axe: {newAxeId}", this);

            OnAxeChanged?.Invoke(currentAxeId);
        }

        public void ApplyCurrentVisual()
        {
            if (axeVisuals == null || axeVisuals.Count == 0) return;

            AxeVisualEntry selected = axeVisuals.FirstOrDefault(entry =>
                entry != null && entry.model != null &&
                string.Equals(entry.axeId, currentAxeId, StringComparison.OrdinalIgnoreCase));
            selected ??= axeVisuals.FirstOrDefault(entry => entry != null && entry.model != null);
            if (selected == null) return;

            currentAxeId = selected.axeId;
            foreach (AxeVisualEntry entry in axeVisuals)
            {
                if (entry?.model != null)
                    entry.model.SetActive(entry == selected);
            }
        }
    }
}
