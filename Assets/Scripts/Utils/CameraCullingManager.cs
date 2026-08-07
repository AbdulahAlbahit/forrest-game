using System;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;

namespace ForrestGame.CameraControl
{
    [Serializable]
    public struct LayerCullSetting
    {
        [Tooltip("Name of the Layer (e.g. 'SmallObjects')")]
        public string layerName;
        
        [Tooltip("Distance at which objects in this layer stop rendering")]
        public float cullDistance;
    }

    [RequireComponent(typeof(Camera))]
    public class CameraCullingManager : MonoBehaviour
    {
        [Header("Culling Settings")]
        [Tooltip("Default far clip plane for the camera (for large objects like terrain)")]
        public float defaultFarClipDistance = 200f;

        [Tooltip("List of custom layers and their cull distances")]
        public List<LayerCullSetting> layerCullSettings = new List<LayerCullSetting>();

        private Camera _mainCam;

        private void Start()
        {
            _mainCam = GetComponent<Camera>();
            if (_mainCam == null)
            {
                GameLogger.LogError("[CameraCulling] No Camera found on this GameObject!", this);
                return;
            }

            ApplyCullingDistances();
        }

        public void ApplyCullingDistances()
        {
            _mainCam.farClipPlane = defaultFarClipDistance;
            
            // Unity layerCullDistances is a float array of 32 elements (one for each layer)
            float[] distances = new float[32];

            bool hasCustomCull = false;

            foreach (var setting in layerCullSettings)
            {
                int layerIndex = LayerMask.NameToLayer(setting.layerName);
                if (layerIndex >= 0 && layerIndex < 32)
                {
                    distances[layerIndex] = setting.cullDistance;
                    hasCustomCull = true;
                    GameLogger.Log($"[CameraCulling] Set layer '{setting.layerName}' (Index: {layerIndex}) cull distance to {setting.cullDistance}m.", this);
                }
                else
                {
                    GameLogger.LogWarning($"[CameraCulling] Layer '{setting.layerName}' does not exist! Please add it in Unity Tags & Layers.", this);
                }
            }

            if (hasCustomCull)
            {
                _mainCam.layerCullDistances = distances;
                GameLogger.Log("[CameraCulling] Layer Culling applied successfully.", this);
            }
        }
    }
}
