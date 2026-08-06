using UnityEngine;

namespace ForrestGame.UI
{
    /// <summary>
    /// Mobil cihazlar için dokunsal geri bildirim (Haptic Feedback) yöneticisi.
    /// Android ve iOS platformlarında çalışır, Editör veya desteklenmeyen cihazlarda güvenle yutulur.
    /// 
    /// KULLANIM:
    /// UIHapticFeedback.Light();   // Balta vuruşları, buton tıklamaları
    /// UIHapticFeedback.Medium();  // Sepet dolumu, eşya toplama
    /// UIHapticFeedback.Heavy();   // Level up, satın alımlar
    /// </summary>
    public static class UIHapticFeedback
    {
        public static bool HapticEnabled { get; set; } = true;

        public static void Light()
        {
            if (!HapticEnabled) return;
            Vibrate();
        }

        public static void Medium()
        {
            if (!HapticEnabled) return;
            Vibrate();
        }

        public static void Heavy()
        {
            if (!HapticEnabled) return;
            Vibrate();
        }

        private static void Vibrate()
        {
            try
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
            catch
            {
                // Standalone veya cihaz desteksizse hata vermesin
            }
        }
    }
}
