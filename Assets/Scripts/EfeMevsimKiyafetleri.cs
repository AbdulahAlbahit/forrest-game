using System;
using UnityEngine;
using ForrestGame.Core;

public enum EfeKiyafetMevsimi
{
    Ilkbahar = 0,
    Yaz = 1,
    Sonbahar = 2,
    Kis = 3
}

[DisallowMultipleComponent]
public sealed class EfeMevsimKiyafetleri : MonoBehaviour
{
    [Header("Mevsim Kiyafet Kokleri")]
    [Tooltip("Uzun kollu gomlek gibi ilkbahar kiyafetinin kok nesnesi.")]
    [SerializeField] private GameObject ilkbaharKiyafeti;

    [Tooltip("Kisa kollu yaz kiyafetinin kok nesnesi.")]
    [SerializeField] private GameObject yazKiyafeti;

    [Tooltip("Ince montlu sonbahar kiyafetinin kok nesnesi.")]
    [SerializeField] private GameObject sonbaharKiyafeti;

    [Tooltip("Kalin mont ve bere iceren kis kiyafetinin kok nesnesi.")]
    [SerializeField] private GameObject kisKiyafeti;

    [Header("Onizleme / Baslangic")]
    [SerializeField] private EfeKiyafetMevsimi baslangicMevsimi = EfeKiyafetMevsimi.Yaz;
    [SerializeField] private bool baslangictaUygula = true;

    public static event Action<Transform, EfeKiyafetMevsimi> KiyafetDegisti;

    public EfeKiyafetMevsimi AktifMevsim { get; private set; }

    private void Awake()
    {
        if (baslangictaUygula)
        {
            MevsimiUygula(baslangicMevsimi);
        }
    }

    private void OnEnable()
    {
        SeasonManager.OnMevsimDegisti += SeasonManager_OnMevsimDegisti;

        // SeasonManager sahnede zaten kurulduysa gercek mevsimi hemen uygula.
        if (SeasonManager.Instance != null)
        {
            MevsimiUygula((int)SeasonManager.Instance.aktifMevsim);
        }
    }

    private void OnDisable()
    {
        SeasonManager.OnMevsimDegisti -= SeasonManager_OnMevsimDegisti;
    }

    private void SeasonManager_OnMevsimDegisti(MevsimTuru mevsim)
    {
        MevsimiUygula((int)mevsim);
    }

    private void OnValidate()
    {
        if (!baslangictaUygula)
        {
            return;
        }

        GameObject secilenKiyafet = KiyafetKokunuBul(baslangicMevsimi);
        KiyafetKokunuAyarla(ilkbaharKiyafeti, ilkbaharKiyafeti == secilenKiyafet);
        KiyafetKokunuAyarla(yazKiyafeti, yazKiyafeti == secilenKiyafet);
        KiyafetKokunuAyarla(sonbaharKiyafeti, sonbaharKiyafeti == secilenKiyafet);
        KiyafetKokunuAyarla(kisKiyafeti, kisKiyafeti == secilenKiyafet);
        AktifMevsim = baslangicMevsimi;
    }

    public void MevsimiUygula(EfeKiyafetMevsimi mevsim)
    {
        GameObject secilenKiyafet = KiyafetKokunuBul(mevsim);

        KiyafetKokunuAyarla(ilkbaharKiyafeti, ilkbaharKiyafeti == secilenKiyafet);
        KiyafetKokunuAyarla(yazKiyafeti, yazKiyafeti == secilenKiyafet);
        KiyafetKokunuAyarla(sonbaharKiyafeti, sonbaharKiyafeti == secilenKiyafet);
        KiyafetKokunuAyarla(kisKiyafeti, kisKiyafeti == secilenKiyafet);

        AktifMevsim = mevsim;
        GuvenliYayinla(mevsim);
    }

    // SeasonManager tarafindaki enum degeri 0-3 olarak dogrudan baglanabilir.
    public void MevsimiUygula(int mevsimDegeri)
    {
        if (!Enum.IsDefined(typeof(EfeKiyafetMevsimi), mevsimDegeri))
        {
            Debug.LogWarning($"[Efe Kiyafet] Gecersiz mevsim degeri: {mevsimDegeri}", this);
            return;
        }

        MevsimiUygula((EfeKiyafetMevsimi)mevsimDegeri);
    }

    // Kodlar birbirine enum bagimliligi olmadan isimle de baglanabilir.
    public void MevsimiUygula(string mevsimAdi)
    {
        if (!Enum.TryParse(mevsimAdi, true, out EfeKiyafetMevsimi mevsim))
        {
            Debug.LogWarning($"[Efe Kiyafet] Tanimlanamayan mevsim adi: {mevsimAdi}", this);
            return;
        }

        MevsimiUygula(mevsim);
    }

    private GameObject KiyafetKokunuBul(EfeKiyafetMevsimi mevsim)
    {
        switch (mevsim)
        {
            case EfeKiyafetMevsimi.Ilkbahar:
                return ilkbaharKiyafeti;
            case EfeKiyafetMevsimi.Yaz:
                return yazKiyafeti;
            case EfeKiyafetMevsimi.Sonbahar:
                return sonbaharKiyafeti;
            case EfeKiyafetMevsimi.Kis:
                return kisKiyafeti;
            default:
                return null;
        }
    }

    private static void KiyafetKokunuAyarla(GameObject kiyafetKoku, bool aktif)
    {
        if (kiyafetKoku != null && kiyafetKoku.activeSelf != aktif)
        {
            kiyafetKoku.SetActive(aktif);
        }
    }

    private void GuvenliYayinla(EfeKiyafetMevsimi mevsim)
    {
        Action<Transform, EfeKiyafetMevsimi> olay = KiyafetDegisti;
        if (olay == null)
        {
            return;
        }

        foreach (Action<Transform, EfeKiyafetMevsimi> dinleyici in olay.GetInvocationList())
        {
            try
            {
                dinleyici(transform, mevsim);
            }
            catch (Exception hata)
            {
                Debug.LogException(hata, this);
            }
        }
    }
}
