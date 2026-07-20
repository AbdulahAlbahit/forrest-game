using UnityEngine;
using ForrestGame.Utils;

namespace ForrestGame.CameraControl
{
    public class FadeableObstacle : MonoBehaviour
    {
        [Header("Hedef Ayarları")]
        [Tooltip("Oyuncu karakteri (Boş bırakırsan 'Player' tagiyle arar)")]
        public Transform player;

        [Header("Materyal Ayarları")]
        [Tooltip("Bu objenin boyanmasını sağlayan Renderer (Boş bırakırsanız otomatik bulur)")]
        public Renderer targetRenderer;
        
        [Tooltip("Karakter arkaya geçtiğinde kullanılacak SAYDAM materyalleri buraya sürükleyin. (Orjinal materyal sayısıyla eşit olmalıdır)")]
        public Material[] transparentMaterials;

        private Material[] _originalMaterials;
        private Transform _camera;
        private bool _isFaded = false;

        private void Start()
        {
            _camera = Camera.main.transform;
            
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            // Renderer belirtilmediyse bul
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            
            if (targetRenderer != null)
            {
                // Oyun başladığında orjinal (opak) materyalleri hafızaya al
                _originalMaterials = targetRenderer.materials;
            }
        }

        private void Update()
        {
            // Hata tespiti için zorunlu alanlar yoksa çalışmayı durdur
            if (player == null || _camera == null || targetRenderer == null) return;
            if (transparentMaterials == null || transparentMaterials.Length == 0) return;

            Vector3 camPos = _camera.position;
            Vector3 targetPos = player.position + Vector3.up * 1f; // Göğüs hizası
            Vector3 dir = targetPos - camPos;
            float distToPlayer = dir.magnitude;
            
            bool isBlocking = false;

            Ray ray = new Ray(camPos, dir.normalized);
            float hitDistance;

            // Kameradan çıkan ışın, duvarın görsel sınırlarına (kutusuna) değiyor mu?
            if (targetRenderer.bounds.IntersectRay(ray, out hitDistance))
            {
                // Değiyorsa, bu duvar oyuncudan daha mı YAKINDA? (Yani araya mı girmiş)
                if (hitDistance < distToPlayer)
                {
                    isBlocking = true;
                }
            }

            // Eğer engelliyorsak ve henüz saydamlaşmadıysak
            if (isBlocking && !_isFaded)
            {
                _isFaded = true;
                
                if (transparentMaterials.Length == _originalMaterials.Length)
                {
                    targetRenderer.materials = transparentMaterials;
                }
                else
                {
                    Material[] newMats = new Material[_originalMaterials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = transparentMaterials[0];
                    }
                    targetRenderer.materials = newMats;
                }
            }
            // Artık engellemiyorsak ve saydamsak
            else if (!isBlocking && _isFaded)
            {
                _isFaded = false;
                targetRenderer.materials = _originalMaterials;
            }
        }
    }
}
