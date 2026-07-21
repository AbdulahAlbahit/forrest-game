using UnityEngine;

namespace ForrestGame.Data
{
    /// <summary>
    /// Yasir'in oyundaki mevsim sürelerini ve etkilerini (Buff/Debuff) ayarlayacağı dosya.
    /// </summary>
    [CreateAssetMenu(fileName = "SeasonSettings", menuName = "Forrest Game/Season Settings")]
    public class SeasonSettings : ScriptableObject
    {
        [Header("Mevsim Süresi")]
        [Tooltip("Bir mevsimin kaç dakika süreceği (Çevrimdışıyken de sayar)")]
        public int mevsimSuresiDakika = 20;

        [Header("İlkbahar (Spring) Etkileri")]
        public float ilkbaharAgacBuyumeHizi = 1.20f; // %20 daha hızlı
        public float ilkbaharBaltaHasari = 1.0f; // Normal

        [Header("Yaz (Summer) Etkileri")]
        public float yazOduncuHizi = 1.10f; // %10 daha hızlı
        public float yazBaltaHasari = 1.0f; // Normal

        [Header("Sonbahar (Autumn) Etkileri")]
        public float sonbaharAltinCarpani = 1.15f; // Odun satışından %15 daha fazla altın
        public float sonbaharBaltaHasari = 1.0f; // Normal

        [Header("Kış (Winter) Etkileri")]
        public float kisBaltaHasari = 0.75f; // Balta %25 yavaş vurur (Zorluk)
        public float kisAgacBuyumeHizi = 0.80f; // Ağaçlar %20 daha yavaş büyür
    }
}
