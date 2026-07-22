using System.Collections;
using UnityEngine;

public class EfeBaltaKontrol : MonoBehaviour
{
    [Header("Balta Saldırısı")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject balta;
    [Min(0.1f)] [SerializeField] private float saldiriBeklemeSuresi = 0.65f;

    [Header("Ağaç Etkileşimi")]
    [Min(0.5f)] [SerializeField] private float agacEtkilesimMesafesi = 3f;
    [Min(1)] [SerializeField] private int agacHasari = 1;
    [Range(0f, 1f)] [SerializeField] private float vurusAnGecikmesi = 0.3f;

    [Header("Saldırı Dengeli Ayak Duruşu")]
    [Min(0f)] [SerializeField] private float sagAyakIleriMesafesi = 0.17f;
    [Min(0f)] [SerializeField] private float solAyakGeriMesafesi = 0.11f;
    [Min(0f)] [SerializeField] private float ayakYanalMesafesi = 0.11f;
    [Range(0.01f, 0.25f)] [SerializeField] private float durusaGecisSuresi = 0.1f;
    [Range(0.01f, 0.3f)] [SerializeField] private float durustanCikisSuresi = 0.15f;

    [Header("Saldiri Gorsel Cesitliligi")]
    [Range(0f, 35f)] [SerializeField] private float yanalVurusDonusAcisi = 24f;
    [Range(0f, 20f)] [SerializeField] private float yanalVurusYatisAcisi = 10f;
    [Range(0f, 25f)] [SerializeField] private float usttenVurusEgilmeAcisi = 16f;
    [Range(0f, 25f)] [SerializeField] private float baltaYanalVurguAcisi = 14f;
    [Range(0f, 30f)] [SerializeField] private float baltaUsttenVurguAcisi = 18f;
    [Range(0f, 0.6f)] [SerializeField] private float yanalElVurusOfseti = 0.38f;
    [Range(0f, 0.8f)] [SerializeField] private float usttenElVurusOfseti = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float elVurusIkAgirligi = 0.85f;

    private static readonly int SaldirParametresi = Animator.StringToHash("Saldir");
    private static readonly int HizParametresi = Animator.StringToHash("Hiz");
    private const float YurumePozisyonSalinimi = 0.025f;
    private const float KosmaPozisyonSalinimi = 0.055f;
    private const float YurumeDonusAcisi = 12f;
    private const float KosmaDonusAcisi = 24f;
    private const float BaltaSalinimYumusakligi = 14f;
    private const float SaldiriKatmaniGecisHizi = 12f;
    private float sonrakiSaldiriZamani;
    private float durusBaslangicZamani = float.NegativeInfinity;
    private float solAyakZeminOfseti;
    private float sagAyakZeminOfseti;
    private Vector3 baltaBaslangicLocalPozisyonu;
    private Quaternion baltaBaslangicLocalDonusu;
    private Vector3 mevcutBaltaPozisyonOfseti;
    private Quaternion mevcutBaltaDonusOfseti = Quaternion.identity;
    private bool baltaBaslangicPozuHazir;
    private int saldiriKatmaniIndexi = -1;
    private float saldiriKatmaniAgirligi;
    private Transform ustGovdeKemigi;
    private int saldiriCesidi = -1;

    public void BaltaSalla()
    {
        if (animator == null || Time.time < sonrakiSaldiriZamani)
        {
            return;
        }

        sonrakiSaldiriZamani = Time.time + saldiriBeklemeSuresi;

        bool yakindaAgacVar = EnYakinTerrainAgaciniBul(out Vector3 agacKonumu);
        if (yakindaAgacVar)
        {
            AgacaDon(agacKonumu);
        }

        saldiriCesidi = (saldiriCesidi + 1) % 3;
        SaldiriAyakDurusunuBaslat();

        if (saldiriKatmaniIndexi >= 0)
        {
            saldiriKatmaniAgirligi = 1f;
            animator.SetLayerWeight(saldiriKatmaniIndexi, saldiriKatmaniAgirligi);
        }

        animator.ResetTrigger(SaldirParametresi);
        animator.SetTrigger(SaldirParametresi);
        EfeOynanisEventleri.BaltaSallandiginiYayinla(transform);

        if (yakindaAgacVar)
        {
            StartCoroutine(VurusuGecikmeliUygula(agacKonumu));
        }
    }

    private IEnumerator VurusuGecikmeliUygula(Vector3 agacKonumu)
    {
        yield return new WaitForSeconds(vurusAnGecikmesi);

        Vector3 karakterdenAgaca = agacKonumu - transform.position;
        karakterdenAgaca.y = 0f;

        if (karakterdenAgaca.sqrMagnitude > agacEtkilesimMesafesi * agacEtkilesimMesafesi)
        {
            yield break;
        }

        if (TreeManager.Instance != null)
        {
            TreeManager.Instance.HitTreeAt(agacKonumu, agacHasari);
            EfeOynanisEventleri.BaltaAgacaVurdugunuYayinla(transform);
        }
    }

    private bool EnYakinTerrainAgaciniBul(out Vector3 enYakinAgacKonumu)
    {
        enYakinAgacKonumu = Vector3.zero;
        float enYakinMesafeKaresi = agacEtkilesimMesafesi * agacEtkilesimMesafesi;
        bool agacBulundu = false;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainBoyutu = terrain.terrainData.size;
            Vector3 terrainKonumu = terrain.transform.position;

            foreach (TreeInstance agac in terrain.terrainData.treeInstances)
            {
                Vector3 agacDunyaKonumu = new Vector3(
                    agac.position.x * terrainBoyutu.x + terrainKonumu.x,
                    agac.position.y * terrainBoyutu.y + terrainKonumu.y,
                    agac.position.z * terrainBoyutu.z + terrainKonumu.z);

                Vector3 yatayFark = agacDunyaKonumu - transform.position;
                yatayFark.y = 0f;
                float mesafeKaresi = yatayFark.sqrMagnitude;

                if (mesafeKaresi <= enYakinMesafeKaresi)
                {
                    enYakinMesafeKaresi = mesafeKaresi;
                    enYakinAgacKonumu = agacDunyaKonumu;
                    agacBulundu = true;
                }
            }
        }

        return agacBulundu;
    }

    private void AgacaDon(Vector3 agacKonumu)
    {
        Vector3 bakisYonu = agacKonumu - transform.position;
        bakisYonu.y = 0f;

        if (bakisYonu.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(bakisYonu.normalized, Vector3.up);
        }
    }

    private void SaldiriAyakDurusunuBaslat()
    {
        Transform solAyak = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform sagAyak = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        solAyakZeminOfseti = AyakZeminOfsetiniHesapla(solAyak);
        sagAyakZeminOfseti = AyakZeminOfsetiniHesapla(sagAyak);
        durusBaslangicZamani = Time.time;
    }

    private float AyakZeminOfsetiniHesapla(Transform ayak)
    {
        if (ayak == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, ayak.position.y - ZeminYuksekliginiBul(ayak.position));
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
        {
            return;
        }

        float durusAgirligi = SaldiriDurusAgirliginiHesapla();
        if (layerIndex == 0)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, durusAgirligi);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, durusAgirligi);

            if (durusAgirligi > 0f)
            {
                Vector3 ileri = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 sag = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

                Vector3 solAyakHedefi = transform.position
                                       - ileri * solAyakGeriMesafesi
                                       - sag * ayakYanalMesafesi;
                Vector3 sagAyakHedefi = transform.position
                                       + ileri * sagAyakIleriMesafesi
                                       + sag * ayakYanalMesafesi;

                solAyakHedefi.y = ZeminYuksekliginiBul(solAyakHedefi) + solAyakZeminOfseti;
                sagAyakHedefi.y = ZeminYuksekliginiBul(sagAyakHedefi) + sagAyakZeminOfseti;

                animator.SetIKPosition(AvatarIKGoal.LeftFoot, solAyakHedefi);
                animator.SetIKPosition(AvatarIKGoal.RightFoot, sagAyakHedefi);
            }
        }

        int elIkKatmani = saldiriKatmaniIndexi >= 0 ? saldiriKatmaniIndexi : 0;
        if (layerIndex == elIkKatmani)
        {
            SaldiriElYolunuUygula(durusAgirligi);
        }
    }

    private void SaldiriElYolunuUygula(float durusAgirligi)
    {
        Transform sagEl = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (sagEl == null || saldiriCesidi < 0 || durusAgirligi <= 0f)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            return;
        }

        float gecenSure = Time.time - durusBaslangicZamani;
        float ilerleme = Mathf.Clamp01(gecenSure / saldiriBeklemeSuresi);
        float yolVurgusu = Mathf.Sin(ilerleme * Mathf.PI) * durusAgirligi;
        float ikAgirligi = yolVurgusu * elVurusIkAgirligi;
        Vector3 hedefElKonumu = sagEl.position;

        switch (saldiriCesidi)
        {
            case 0:
                hedefElKonumu -= transform.right * yanalElVurusOfseti;
                hedefElKonumu += transform.up * 0.06f;
                break;
            case 1:
                hedefElKonumu += transform.right * yanalElVurusOfseti;
                hedefElKonumu += transform.up * 0.06f;
                break;
            default:
                Transform sagOmuz = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                if (sagOmuz == null)
                {
                    sagOmuz = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                }

                hedefElKonumu = sagOmuz != null
                    ? sagOmuz.position
                      + transform.up * usttenElVurusOfseti
                      + transform.forward * 0.08f
                    : hedefElKonumu + transform.up * (usttenElVurusOfseti + 0.25f);
                break;
        }

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikAgirligi);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetIKPosition(AvatarIKGoal.RightHand, hedefElKonumu);
    }

    private float SaldiriDurusAgirliginiHesapla()
    {
        float gecenSure = Time.time - durusBaslangicZamani;
        if (gecenSure < 0f || gecenSure >= saldiriBeklemeSuresi)
        {
            return 0f;
        }

        float girisAgirligi = Mathf.Clamp01(gecenSure / durusaGecisSuresi);
        float kalanSure = saldiriBeklemeSuresi - gecenSure;
        float cikisAgirligi = Mathf.Clamp01(kalanSure / durustanCikisSuresi);
        return Mathf.SmoothStep(0f, 1f, Mathf.Min(girisAgirligi, cikisAgirligi));
    }

    private float ZeminYuksekliginiBul(Vector3 dunyaKonumu)
    {
        Terrain enYakinTerrain = null;
        float enYakinMesafeKaresi = float.PositiveInfinity;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 merkez = terrain.transform.position + terrain.terrainData.size * 0.5f;
            Vector2 fark = new Vector2(dunyaKonumu.x - merkez.x, dunyaKonumu.z - merkez.z);
            if (fark.sqrMagnitude < enYakinMesafeKaresi)
            {
                enYakinMesafeKaresi = fark.sqrMagnitude;
                enYakinTerrain = terrain;
            }
        }

        return enYakinTerrain != null
            ? enYakinTerrain.SampleHeight(dunyaKonumu) + enYakinTerrain.transform.position.y
            : transform.position.y;
    }

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            saldiriKatmaniIndexi = animator.GetLayerIndex("Upper Body Attack");
            ustGovdeKemigi = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (ustGovdeKemigi == null)
            {
                ustGovdeKemigi = animator.GetBoneTransform(HumanBodyBones.Chest);
            }
            if (ustGovdeKemigi == null)
            {
                ustGovdeKemigi = animator.GetBoneTransform(HumanBodyBones.Spine);
            }

            if (saldiriKatmaniIndexi >= 0)
            {
                animator.SetLayerWeight(saldiriKatmaniIndexi, 0f);
            }
        }

        if (balta != null)
        {
            balta.SetActive(true);
            baltaBaslangicLocalPozisyonu = balta.transform.localPosition;
            baltaBaslangicLocalDonusu = balta.transform.localRotation;
            baltaBaslangicPozuHazir = true;
        }
    }

    private void LateUpdate()
    {
        if (animator == null || balta == null || !baltaBaslangicPozuHazir)
        {
            return;
        }

        if (saldiriKatmaniIndexi >= 0)
        {
            float hedefKatmanAgirligi = Time.time < sonrakiSaldiriZamani ? 1f : 0f;
            saldiriKatmaniAgirligi = Mathf.MoveTowards(
                saldiriKatmaniAgirligi,
                hedefKatmanAgirligi,
                SaldiriKatmaniGecisHizi * Time.deltaTime);
            animator.SetLayerWeight(saldiriKatmaniIndexi, saldiriKatmaniAgirligi);
        }

        float hiz = Mathf.Clamp01(animator.GetFloat(HizParametresi));
        float hareketAgirligi = Time.time < sonrakiSaldiriZamani
            ? 0f
            : Mathf.InverseLerp(0.05f, 0.5f, hiz);
        float kosuOrani = Mathf.InverseLerp(0.5f, 1f, hiz);
        float animasyonFazi = animator.GetCurrentAnimatorStateInfo(0).normalizedTime * Mathf.PI * 2f;

        float pozisyonGenligi = Mathf.Lerp(YurumePozisyonSalinimi, KosmaPozisyonSalinimi, kosuOrani) * hareketAgirligi;
        float donusGenligi = Mathf.Lerp(YurumeDonusAcisi, KosmaDonusAcisi, kosuOrani) * hareketAgirligi;

        Vector3 hedefPozisyonOfseti = new Vector3(
            Mathf.Cos(animasyonFazi) * pozisyonGenligi * 0.25f,
            Mathf.Sin(animasyonFazi * 2f) * pozisyonGenligi * 0.35f,
            Mathf.Sin(animasyonFazi) * pozisyonGenligi * 0.45f);
        Quaternion hedefDonusOfseti = Quaternion.Euler(
            Mathf.Sin(animasyonFazi) * donusGenligi,
            Mathf.Cos(animasyonFazi) * donusGenligi * 0.3f,
            -Mathf.Sin(animasyonFazi) * donusGenligi * 0.5f);

        float yumusatma = 1f - Mathf.Exp(-BaltaSalinimYumusakligi * Time.deltaTime);
        mevcutBaltaPozisyonOfseti = Vector3.Lerp(mevcutBaltaPozisyonOfseti, hedefPozisyonOfseti, yumusatma);
        mevcutBaltaDonusOfseti = Quaternion.Slerp(mevcutBaltaDonusOfseti, hedefDonusOfseti, yumusatma);

        balta.transform.localPosition = baltaBaslangicLocalPozisyonu + mevcutBaltaPozisyonOfseti;
        balta.transform.localRotation = baltaBaslangicLocalDonusu * mevcutBaltaDonusOfseti;
        SaldiriCesitliliginiUygula();
    }

    private void SaldiriCesitliliginiUygula()
    {
        if (ustGovdeKemigi == null || saldiriCesidi < 0)
        {
            return;
        }

        float gecenSure = Time.time - durusBaslangicZamani;
        if (gecenSure < 0f || gecenSure >= saldiriBeklemeSuresi)
        {
            return;
        }

        float ilerleme = Mathf.Clamp01(gecenSure / saldiriBeklemeSuresi);
        float hareketVurgusu = Mathf.Sin(ilerleme * Mathf.PI)
                              * SaldiriDurusAgirliginiHesapla();
        float yatayDonus = 0f;
        float yanalYatis = 0f;
        float ileriEgilme = 0f;
        float baltaYatayVurgusu = 0f;
        float baltaUstVurgusu = 0f;

        switch (saldiriCesidi)
        {
            case 0:
                yatayDonus = -yanalVurusDonusAcisi;
                yanalYatis = yanalVurusYatisAcisi;
                baltaYatayVurgusu = -baltaYanalVurguAcisi;
                break;
            case 1:
                yatayDonus = yanalVurusDonusAcisi;
                yanalYatis = -yanalVurusYatisAcisi;
                baltaYatayVurgusu = baltaYanalVurguAcisi;
                break;
            default:
                ileriEgilme = -usttenVurusEgilmeAcisi;
                baltaUstVurgusu = -baltaUsttenVurguAcisi;
                break;
        }

        Quaternion govdeVurgusu = Quaternion.AngleAxis(
                                      yatayDonus * hareketVurgusu,
                                      transform.up)
                                  * Quaternion.AngleAxis(
                                      ileriEgilme * hareketVurgusu,
                                      transform.right)
                                  * Quaternion.AngleAxis(
                                      yanalYatis * hareketVurgusu,
                                      transform.forward);
        ustGovdeKemigi.rotation = govdeVurgusu * ustGovdeKemigi.rotation;

        Quaternion baltaVurgusu = Quaternion.AngleAxis(
                                      baltaYatayVurgusu * hareketVurgusu,
                                      transform.up)
                                  * Quaternion.AngleAxis(
                                      baltaUstVurgusu * hareketVurgusu,
                                      transform.right);
        balta.transform.rotation = baltaVurgusu * balta.transform.rotation;
    }

    private void OnDisable()
    {
        if (balta == null || !baltaBaslangicPozuHazir)
        {
            return;
        }

        balta.transform.localPosition = baltaBaslangicLocalPozisyonu;
        balta.transform.localRotation = baltaBaslangicLocalDonusu;
        mevcutBaltaPozisyonOfseti = Vector3.zero;
        mevcutBaltaDonusOfseti = Quaternion.identity;

        if (animator != null && saldiriKatmaniIndexi >= 0)
        {
            saldiriKatmaniAgirligi = 0f;
            animator.SetLayerWeight(saldiriKatmaniIndexi, 0f);
        }
    }

    private void OnValidate()
    {
        agacEtkilesimMesafesi = Mathf.Max(0.5f, agacEtkilesimMesafesi);
        agacHasari = Mathf.Max(1, agacHasari);
        vurusAnGecikmesi = Mathf.Clamp(vurusAnGecikmesi, 0f, saldiriBeklemeSuresi);
        sagAyakIleriMesafesi = Mathf.Max(0f, sagAyakIleriMesafesi);
        solAyakGeriMesafesi = Mathf.Max(0f, solAyakGeriMesafesi);
        ayakYanalMesafesi = Mathf.Max(0f, ayakYanalMesafesi);
        durusaGecisSuresi = Mathf.Clamp(durusaGecisSuresi, 0.01f, saldiriBeklemeSuresi);
        durustanCikisSuresi = Mathf.Clamp(durustanCikisSuresi, 0.01f, saldiriBeklemeSuresi);
        yanalVurusDonusAcisi = Mathf.Clamp(yanalVurusDonusAcisi, 0f, 35f);
        yanalVurusYatisAcisi = Mathf.Clamp(yanalVurusYatisAcisi, 0f, 20f);
        usttenVurusEgilmeAcisi = Mathf.Clamp(usttenVurusEgilmeAcisi, 0f, 25f);
        baltaYanalVurguAcisi = Mathf.Clamp(baltaYanalVurguAcisi, 0f, 25f);
        baltaUsttenVurguAcisi = Mathf.Clamp(baltaUsttenVurguAcisi, 0f, 30f);
        yanalElVurusOfseti = Mathf.Clamp(yanalElVurusOfseti, 0f, 0.6f);
        usttenElVurusOfseti = Mathf.Clamp(usttenElVurusOfseti, 0f, 0.8f);
        elVurusIkAgirligi = Mathf.Clamp01(elVurusIkAgirligi);
    }
}
