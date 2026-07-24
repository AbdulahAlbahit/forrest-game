using System;
using UnityEngine;

/// <summary>
/// Efe'ye ait oynanis sistemlerinin ses/VFX gibi bagimsiz sistemlere acik olay sozlesmesi.
/// Dinleyiciler OnEnable icinde abone olup OnDisable icinde abonelikten cikmalidir.
/// </summary>
public static class EfeOynanisEventleri
{
    public static event Action<Transform> JoystickBasildi;
    public static event Action<Transform> JoystickBirakildi;
    public static event Action<Transform> HareketBasladi;
    public static event Action<Transform> HareketBitti;
    public static event Action<Transform> KosuBasladi;
    public static event Action<Transform> KosuBitti;
    public static event Action<Transform> BaltaSallandi;
    public static event Action<Transform> BaltaAgacaVurdu;
    public static event Action<Transform, int, int> OdunToplandi;
    public static event Action<Transform> SepetDoldu;
    public static event Action<Transform> KamyonYuklemeBasladi;
    public static event Action<Transform, int, int> OdunKamyonaYuklendi;
    public static event Action<Transform> KamyonYuklemeBitti;
    public static event Action<Transform> SepetBosaldi;
    public static event Action<Transform> SuyaGirdi;
    public static event Action<Transform> SudanCikti;
    public static event Action<Transform> SudanKurtarildi;

    internal static void JoystickBasildiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(JoystickBasildi, kaynak, nameof(JoystickBasildi));

    internal static void JoystickBirakildiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(JoystickBirakildi, kaynak, nameof(JoystickBirakildi));

    internal static void HareketBasladiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(HareketBasladi, kaynak, nameof(HareketBasladi));

    internal static void HareketBittiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(HareketBitti, kaynak, nameof(HareketBitti));

    internal static void KosuBasladiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(KosuBasladi, kaynak, nameof(KosuBasladi));

    internal static void KosuBittiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(KosuBitti, kaynak, nameof(KosuBitti));

    internal static void BaltaSallandiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(BaltaSallandi, kaynak, nameof(BaltaSallandi));

    internal static void BaltaAgacaVurdugunuYayinla(Transform kaynak) =>
        GuvenliYayinla(BaltaAgacaVurdu, kaynak, nameof(BaltaAgacaVurdu));

    internal static void OdunToplandiginiYayinla(Transform kaynak, int mevcut, int kapasite) =>
        GuvenliYayinla(OdunToplandi, kaynak, mevcut, kapasite, nameof(OdunToplandi));

    internal static void SepetDoldugunuYayinla(Transform kaynak) =>
        GuvenliYayinla(SepetDoldu, kaynak, nameof(SepetDoldu));

    internal static void KamyonYuklemeBasladiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(KamyonYuklemeBasladi, kaynak, nameof(KamyonYuklemeBasladi));

    internal static void OdunKamyonaYuklendiginiYayinla(
        Transform kaynak,
        int mevcut,
        int kapasite) =>
        GuvenliYayinla(
            OdunKamyonaYuklendi,
            kaynak,
            mevcut,
            kapasite,
            nameof(OdunKamyonaYuklendi));

    internal static void KamyonYuklemeBittiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(KamyonYuklemeBitti, kaynak, nameof(KamyonYuklemeBitti));

    internal static void SepetBosaldiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(SepetBosaldi, kaynak, nameof(SepetBosaldi));

    internal static void SuyaGirdiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(SuyaGirdi, kaynak, nameof(SuyaGirdi));

    internal static void SudanCiktiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(SudanCikti, kaynak, nameof(SudanCikti));

    internal static void SudanKurtarildiginiYayinla(Transform kaynak) =>
        GuvenliYayinla(SudanKurtarildi, kaynak, nameof(SudanKurtarildi));

    private static void GuvenliYayinla(Action<Transform> olay, Transform kaynak, string olayAdi)
    {
        if (olay == null)
        {
            return;
        }

        foreach (Action<Transform> dinleyici in olay.GetInvocationList())
        {
            try
            {
                dinleyici(kaynak);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EfeOynanisEventleri] {olayAdi} dinleyicisi hata verdi: {exception}");
            }
        }
    }

    private static void GuvenliYayinla(
        Action<Transform, int, int> olay,
        Transform kaynak,
        int mevcut,
        int kapasite,
        string olayAdi)
    {
        if (olay == null)
        {
            return;
        }

        foreach (Action<Transform, int, int> dinleyici in olay.GetInvocationList())
        {
            try
            {
                dinleyici(kaynak, mevcut, kapasite);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EfeOynanisEventleri] {olayAdi} dinleyicisi hata verdi: {exception}");
            }
        }
    }
}
