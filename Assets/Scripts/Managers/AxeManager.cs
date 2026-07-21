using UnityEngine;
using System;
using ForrestGame.Utils;

namespace ForrestGame.Managers
{
    public class AxeManager : MonoBehaviour
    {
        public static AxeManager Instance { get; private set; }

        [Header("Axe Data")]
        public string currentAxeId = "lowpoly_axe";

        public static event Action<string> OnAxeChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void EquipAxe(string newAxeId)
        {
            if (currentAxeId == newAxeId) return;

            currentAxeId = newAxeId;
            GameLogger.Log($"[AxeManager] Equipped new axe: {newAxeId}", this);
            
            OnAxeChanged?.Invoke(currentAxeId);
        }
    }
}
