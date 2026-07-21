using UnityEngine;
using UnityEngine.Events;
using ForrestGame.Managers;
using ForrestGame.Utils;

namespace ForrestGame.Stations
{
    public class AxeTableInteractable : MonoBehaviour
    {
        [Header("Visual Settings")]
        public Transform axeModelContainer;

        [Header("Interaction Events")]
        public UnityEvent OnTableClicked;

        private void OnEnable()
        {
            AxeVisualManager.OnAxeChanged += HandleAxeChanged;
        }

        private void OnDisable()
        {
            AxeVisualManager.OnAxeChanged -= HandleAxeChanged;
        }

        private void Start()
        {
            if (AxeVisualManager.Instance != null)
            {
                HandleAxeChanged(AxeVisualManager.Instance.currentAxeId);
            }
        }

        private void HandleAxeChanged(string newAxeId)
        {
            if (axeModelContainer == null) return;

            bool isAxeFound = false;

            foreach (Transform child in axeModelContainer)
            {
                if (child.name.ToLower().Contains(newAxeId.ToLower()))
                {
                    child.gameObject.SetActive(true);
                    isAxeFound = true;
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }

            if (!isAxeFound)
            {
                GameLogger.LogWarning($"[AxeTable] Axe model '{newAxeId}' not found in container!", this);
            }
            else
            {
                GameLogger.Log($"[AxeTable] Visual updated to: {newAxeId}", this);
            }
        }

        private void OnMouseDown()
        {
            GameLogger.Log("[AxeTable] Clicked! Requesting UI Panel...", this);
            OnTableClicked?.Invoke();
        }
    }
}
