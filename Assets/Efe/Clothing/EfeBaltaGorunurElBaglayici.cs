using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Baltayi gorunmez kaynak iskelet yerine ekranda aktif olan mevsim
/// kiyafetinin sag eline baglar. Boylece editor onizlemesi, Play Mode ve
/// mevsim degisimleri ayni gorunur avucu kullanir.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public sealed class EfeBaltaGorunurElBaglayici : MonoBehaviour
{
    private static readonly Vector3 SapTutusNoktasi = new Vector3(0f, 0.08f, 0f);

    [SerializeField] private GameObject balta;
    [SerializeField] private Animator kaynakAnimator;
    [SerializeField] private EfeMevsimKiyafetleri mevsimKiyafetleri;

    [Header("Gorunur El Tutus Pozu")]
    [SerializeField] private Quaternion tutusLocalDonusu = Quaternion.identity;
    [SerializeField] private Vector3 hedefDunyaOlcegi = Vector3.one;

    public Transform AktifGorunurEl { get; private set; }

    private void OnEnable()
    {
        EfeMevsimKiyafetleri.KiyafetDegisti -= KiyafetDegistiginde;
        EfeMevsimKiyafetleri.KiyafetDegisti += KiyafetDegistiginde;
        GorunurEleBagla(true);
    }

    private void OnDisable()
    {
        EfeMevsimKiyafetleri.KiyafetDegisti -= KiyafetDegistiginde;
    }

    private void Update()
    {
        Transform hedefEl = AktifKiyafetEliniBul();
        bool elDegisti = hedefEl != null && hedefEl != AktifGorunurEl;

        if (elDegisti || !Application.isPlaying)
        {
            GorunurEleBagla(elDegisti || !Application.isPlaying);
        }
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            // EfeBaltaKontrol'un eski iskelet olcegine gore verdigi yurume/kosma
            // ofsetlerinden sonra baltayi gorunur avuca geri sabitle.
            GorunurEleBagla(true);
        }
    }

    private void KiyafetDegistiginde(Transform karakter, EfeKiyafetMevsimi mevsim)
    {
        if (karakter == transform)
        {
            GorunurEleBagla(true);
        }
    }

    public void Yapilandir(
        GameObject baltaNesnesi,
        Animator kaynak,
        EfeMevsimKiyafetleri kiyafetler,
        Quaternion localDonus,
        Vector3 dunyaOlcegi)
    {
        balta = baltaNesnesi;
        kaynakAnimator = kaynak;
        mevsimKiyafetleri = kiyafetler;
        tutusLocalDonusu = localDonus;
        hedefDunyaOlcegi = dunyaOlcegi;
        GorunurEleBagla(true);
    }

    private void GorunurEleBagla(bool pozuYenile)
    {
        if (balta == null || kaynakAnimator == null || mevsimKiyafetleri == null)
        {
            return;
        }

        Animator hedefAnimator = AktifKiyafetAnimatorunuBul();
        Transform hedefEl = hedefAnimator != null
            ? hedefAnimator.GetBoneTransform(HumanBodyBones.RightHand)
            : null;
        if (hedefEl == null || hedefAnimator == null)
        {
            return;
        }

        if (balta.transform.parent != hedefEl)
        {
            balta.transform.SetParent(hedefEl, false);
            pozuYenile = true;
        }

        if (pozuYenile)
        {
            balta.transform.localRotation = tutusLocalDonusu;
            balta.transform.localScale = DunyaOlcegindenLocalOlcekHesapla(hedefEl);
            Vector3 avucDunya = AvucMerkeziniBul(hedefAnimator, hedefEl);
            balta.transform.localPosition =
                hedefEl.InverseTransformPoint(avucDunya) -
                tutusLocalDonusu * Vector3.Scale(SapTutusNoktasi, balta.transform.localScale);
        }

        if (!balta.activeSelf)
        {
            balta.SetActive(true);
        }

        AktifGorunurEl = hedefEl;
    }

    private Transform AktifKiyafetEliniBul()
    {
        Animator animator = AktifKiyafetAnimatorunuBul();
        return animator != null
            ? animator.GetBoneTransform(HumanBodyBones.RightHand)
            : null;
    }

    private Animator AktifKiyafetAnimatorunuBul()
    {
        return GetComponentsInChildren<Animator>(true).FirstOrDefault(animator =>
            animator != null &&
            animator != kaynakAnimator &&
            animator.gameObject.activeInHierarchy &&
            animator.isHuman);
    }

    private Vector3 DunyaOlcegindenLocalOlcekHesapla(Transform hedefEl)
    {
        Vector3 elDunyaOlcegi = hedefEl.lossyScale;
        return new Vector3(
            GuvenliBol(hedefDunyaOlcegi.x, elDunyaOlcegi.x),
            GuvenliBol(hedefDunyaOlcegi.y, elDunyaOlcegi.y),
            GuvenliBol(hedefDunyaOlcegi.z, elDunyaOlcegi.z));
    }

    private static float GuvenliBol(float pay, float payda)
    {
        return Mathf.Abs(payda) > 0.000001f ? pay / Mathf.Abs(payda) : pay;
    }

    private static Vector3 AvucMerkeziniBul(Animator animator, Transform el)
    {
        HumanBodyBones[] parmakKokleri =
        {
            HumanBodyBones.RightThumbProximal,
            HumanBodyBones.RightIndexProximal,
            HumanBodyBones.RightMiddleProximal,
            HumanBodyBones.RightRingProximal,
            HumanBodyBones.RightLittleProximal
        };

        Transform[] parmaklar = parmakKokleri
            .Select(animator.GetBoneTransform)
            .Where(parmak => parmak != null)
            .ToArray();
        if (parmaklar.Length == 0)
        {
            return el.position;
        }

        Vector3 parmakOrtalamasi = Vector3.zero;
        foreach (Transform parmak in parmaklar)
        {
            parmakOrtalamasi += parmak.position;
        }

        parmakOrtalamasi /= parmaklar.Length;
        return Vector3.Lerp(el.position, parmakOrtalamasi, 0.55f);
    }
}
