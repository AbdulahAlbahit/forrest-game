using System;
using System.Collections;
using UnityEngine;

namespace ForrestGame.Utils
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Shake Settings")]
        public float defaultDuration = 0.15f;
        public float defaultMagnitude = 0.1f;

        private Transform _cameraTransform;
        private Vector3 _originalLocalPos;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
            else
            {
                GameLogger.LogError("[CameraShake] Main Camera not found!", this);
            }
        }

        private void OnEnable()
        {
            // Ağaca vurulduğu anda otomatik tetiklenecek (Prefab Ağaçlar)
            TreeController.OnTreeHitAction += HandleTreeHit;
            // Terrain Üzerindeki Ağaçlar (Efe'nin sistemi)
            TreeManager.OnTreeHitAction += HandleTreeHitTerrain;
        }

        private void OnDisable()
        {
            TreeController.OnTreeHitAction -= HandleTreeHit;
            TreeManager.OnTreeHitAction -= HandleTreeHitTerrain;
        }

        private void HandleTreeHit()
        {
            Shake(defaultDuration, defaultMagnitude);
        }

        private void HandleTreeHitTerrain(Vector3 treePos)
        {
            Shake(defaultDuration, defaultMagnitude);
        }

        public void Shake(float duration, float magnitude)
        {
            if (_cameraTransform == null) return;

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _cameraTransform.localPosition = _originalLocalPos;
            }
            else
            {
                _originalLocalPos = _cameraTransform.localPosition;
            }

            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

                _cameraTransform.localPosition = new Vector3(_originalLocalPos.x + x, _originalLocalPos.y + y, _originalLocalPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _cameraTransform.localPosition = _originalLocalPos;
            _shakeCoroutine = null;
        }
    }
}
