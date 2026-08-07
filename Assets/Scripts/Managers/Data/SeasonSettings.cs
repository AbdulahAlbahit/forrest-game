using UnityEngine;
using UnityEngine.Serialization;

namespace ForrestGame.Data
{
    [CreateAssetMenu(fileName = "SeasonSettings", menuName = "Forrest Game/Season Settings")]
    public class SeasonSettings : ScriptableObject
    {
        [Header("Mevsim Süresi")]
        [Tooltip("Bir mevsimin kaç dakika süreceği (Çevrimdışıyken de sayar)")]
        public int mevsimSuresiDakika = 20;
        [Range(0f, 1f)]
        [Tooltip("Mevsim başladığında ışık/kar/yaprak rengi gibi görsel efektlerin kaç oranında yumuşak geçişle geleceği. Örn: 0.2 ve 20 dakikalık mevsimde geçiş ilk 4 dakika sürer.")]
        public float gecisOrani = 0.2f;

        [Header("İlkbahar (Spring) Etkileri")]
        public float ilkbaharAgacBuyumeHizi = 1.20f;
        public float ilkbaharBaltaHasari = 1.0f;

        [Header("Yaz (Summer) Etkileri")]
        [FormerlySerializedAs("yazUretimHizi")]
        public float summerProductionSpeed = 1.20f;
        public float yazBaltaHasari = 1.0f;

        [Header("Sonbahar (Autumn) Etkileri")]
        public float sonbaharAltinCarpani = 1.15f;
        public float sonbaharBaltaHasari = 1.0f;

        [Header("Kış (Winter) Etkileri")]
        public float kisBaltaHasari = 0.75f;
        public float kisAgacBuyumeHizi = 0.80f;
    }
}
