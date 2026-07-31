using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Core;

[Serializable]
public class AxeDamageUpgradeLevel
{
    [Min(1)] public int level = 1;
    [Min(1)] public int damage = 1;
}

[Serializable]
public class HousePowerAttackUpgradeLevel
{
    [Min(1)] public int level = 1;
    [Min(0.1f)] public float attackCooldown = 0.95f;
    [Range(0f, 1f)] public float hitDelay = 0.42f;
    [Min(0.1f)] public float animationSpeed = 1f;
}

public class EfeBaltaKontrol : MonoBehaviour
{
    [Header("Balta Saldırısı")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject balta;
    [Min(0.1f)] [SerializeField] private float saldiriBeklemeSuresi = 0.95f;

    [Header("Ağaç Etkileşimi")]
    [Min(0.5f)] [SerializeField] private float agacEtkilesimMesafesi = 3f;
    [Min(1)] [SerializeField] private int agacHasari = 1;
    [Range(0f, 1f)] [SerializeField] private float vurusAnGecikmesi = 0.42f;

    [Header("Upgrade Baglantilari")]
    [SerializeField] private bool useUpgradeLevels = true;
    [SerializeField] private string axeUpgradeItemId = "Axe";
    [Tooltip("Zone_house_Power icin kullanilan ana level. Bu level sepet kapasitesini ve vurma hizini birlikte yonetir.")]
    [SerializeField] private string housePowerUpgradeItemId = "Basket";
    [SerializeField] private string attackSpeedAnimatorParameter = "SaldiriHizi";
    [SerializeField] private List<AxeDamageUpgradeLevel> axeDamageLevels = new List<AxeDamageUpgradeLevel>
    {
        new AxeDamageUpgradeLevel { level = 1, damage = 1 },
        new AxeDamageUpgradeLevel { level = 2, damage = 2 },
        new AxeDamageUpgradeLevel { level = 3, damage = 3 }
    };
    [SerializeField] private List<HousePowerAttackUpgradeLevel> housePowerAttackLevels = new List<HousePowerAttackUpgradeLevel>
    {
        new HousePowerAttackUpgradeLevel { level = 1, attackCooldown = 0.95f, hitDelay = 0.42f, animationSpeed = 1f },
        new HousePowerAttackUpgradeLevel { level = 2, attackCooldown = 0.82f, hitDelay = 0.36f, animationSpeed = 1.12f },
        new HousePowerAttackUpgradeLevel { level = 3, attackCooldown = 0.70f, hitDelay = 0.31f, animationSpeed = 1.25f },
        new HousePowerAttackUpgradeLevel { level = 4, attackCooldown = 0.60f, hitDelay = 0.27f, animationSpeed = 1.38f },
        new HousePowerAttackUpgradeLevel { level = 5, attackCooldown = 0.52f, hitDelay = 0.23f, animationSpeed = 1.50f }
    };

    [Header("Saldırı Dengeli Ayak Duruşu")]
    [Min(0f)] [SerializeField] private float sagAyakIleriMesafesi = 0.17f;
    [Min(0f)] [SerializeField] private float solAyakGeriMesafesi = 0.11f;
    [Min(0f)] [SerializeField] private float ayakYanalMesafesi = 0.11f;
    [Range(0.01f, 0.25f)] [SerializeField] private float durusaGecisSuresi = 0.1f;
    [Range(0.01f, 0.3f)] [SerializeField] private float durustanCikisSuresi = 0.15f;

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
    private int aktifAgacHasari;
    private float aktifSaldiriBeklemeSuresi;
    private float aktifVurusGecikmesi;
    private float aktifSaldiriAnimasyonHizi = 1f;
    private int saldiriHiziParametresi;
    private bool animatorSaldiriHiziParametresiVar;
    private bool upgradeEventlerineAbone;
    private bool dataEventineAbone;

    public int AktifAgacHasari => aktifAgacHasari;
    public float AktifSaldiriBeklemeSuresi => aktifSaldiriBeklemeSuresi;
    public float AktifSaldiriAnimasyonHizi => aktifSaldiriAnimasyonHizi;

    public void BaltaSalla()
    {
        if (animator == null || Time.time < sonrakiSaldiriZamani)
        {
            return;
        }

        sonrakiSaldiriZamani = Time.time + aktifSaldiriBeklemeSuresi;

        bool yakindaAgacVar = EnYakinTerrainAgaciniBul(out Vector3 agacKonumu);
        if (yakindaAgacVar)
        {
            AgacaDon(agacKonumu);
        }

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
        yield return new WaitForSeconds(aktifVurusGecikmesi);

        Vector3 karakterdenAgaca = agacKonumu - transform.position;
        karakterdenAgaca.y = 0f;

        if (karakterdenAgaca.sqrMagnitude > agacEtkilesimMesafesi * agacEtkilesimMesafesi)
        {
            yield break;
        }

        if (TreeManager.Instance != null)
        {
            TreeManager.Instance.HitTreeAt(agacKonumu, GetEffectiveAxeDamage());
            EfeOynanisEventleri.BaltaAgacaVurdugunuYayinla(transform);
        }
    }

    private int GetEffectiveAxeDamage()
    {
        if (SeasonManager.Instance == null || SeasonManager.Instance.ayarlar == null) return aktifAgacHasari;

        float multiplier;
        switch (SeasonManager.Instance.aktifMevsim)
        {
            case MevsimTuru.Ilkbahar:
                multiplier = SeasonManager.Instance.ayarlar.ilkbaharBaltaHasari;
                break;
            case MevsimTuru.Yaz:
                multiplier = SeasonManager.Instance.ayarlar.yazBaltaHasari;
                break;
            case MevsimTuru.Sonbahar:
                multiplier = SeasonManager.Instance.ayarlar.sonbaharBaltaHasari;
                break;
            case MevsimTuru.Kis:
                multiplier = SeasonManager.Instance.ayarlar.kisBaltaHasari;
                break;
            default:
                multiplier = 1f;
                break;
        }

        return Mathf.Max(1, Mathf.RoundToInt(aktifAgacHasari * multiplier));
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
                if (TreeManager.Instance != null && !TreeManager.Instance.IsChoppablePrototype(agac.prototypeIndex))
                {
                    continue;
                }

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
        aktifAgacHasari = agacHasari;
        aktifSaldiriBeklemeSuresi = saldiriBeklemeSuresi;
        aktifVurusGecikmesi = vurusAnGecikmesi;
        saldiriHiziParametresi = Animator.StringToHash(attackSpeedAnimatorParameter);

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            saldiriKatmaniIndexi = animator.GetLayerIndex("Upper Body Attack");
            if (saldiriKatmaniIndexi >= 0)
            {
                animator.SetLayerWeight(saldiriKatmaniIndexi, 0f);
            }

            animatorSaldiriHiziParametresiVar = AnimatorParametresiVar(
                animator,
                saldiriHiziParametresi,
                AnimatorControllerParameterType.Float);
        }

        if (balta != null)
        {
            balta.SetActive(true);
            baltaBaslangicLocalPozisyonu = balta.transform.localPosition;
            baltaBaslangicLocalDonusu = balta.transform.localRotation;
            baltaBaslangicPozuHazir = true;
        }
    }

    private void Start()
    {
        UpgradeEventlerineAboneOl();
        UpgradeAyarlariniUygula();
    }

    private void OnEnable()
    {
        AxeTablePanelController.OnAxeEquipped -= BaltaKusanildiginda;
        AxeTablePanelController.OnAxeEquipped += BaltaKusanildiginda;
        UpgradeEventlerineAboneOl();
        UpgradeAyarlariniUygula();
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
    }

    private void OnDisable()
    {
        AxeTablePanelController.OnAxeEquipped -= BaltaKusanildiginda;
        UpgradeEventlerindenCik();

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

    private void UpgradeEventlerineAboneOl()
    {
        if (!useUpgradeLevels) return;

        if (!upgradeEventlerineAbone && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += UpgradeYapildiginda;
            upgradeEventlerineAbone = true;
        }

        if (!dataEventineAbone && CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += DataYuklendiginde;
            dataEventineAbone = true;
        }
    }

    private void UpgradeEventlerindenCik()
    {
        if (upgradeEventlerineAbone && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded -= UpgradeYapildiginda;
        }

        if (dataEventineAbone && CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= DataYuklendiginde;
        }

        upgradeEventlerineAbone = false;
        dataEventineAbone = false;
    }

    private void UpgradeYapildiginda(string itemId, int newLevel)
    {
        if (itemId == AktifBaltaItemId() || itemId == housePowerUpgradeItemId)
        {
            UpgradeAyarlariniUygula();
        }
    }

    private void BaltaKusanildiginda()
    {
        UpgradeAyarlariniUygula();
    }

    private void DataYuklendiginde(PlayerData data)
    {
        UpgradeAyarlariniUygula();
    }

    public void UpgradeAyarlariniUygula()
    {
        aktifAgacHasari = agacHasari;
        aktifSaldiriBeklemeSuresi = saldiriBeklemeSuresi;
        aktifVurusGecikmesi = vurusAnGecikmesi;
        aktifSaldiriAnimasyonHizi = 1f;

        if (useUpgradeLevels && UpgradeManager.Instance != null)
        {
            string aktifBaltaItemId = AktifBaltaItemId();
            if (UpgradeManager.Instance.TryGetConfig(aktifBaltaItemId, out _))
            {
                aktifAgacHasari = Mathf.Max(
                    1,
                    Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue(aktifBaltaItemId)));
            }
            else
            {
                int axeLevel = UpgradeManager.Instance.GetItemLevel(axeUpgradeItemId);
                AxeDamageUpgradeLevel damageSetting = UygunHasarAyariniBul(axeLevel);
                if (damageSetting != null)
                {
                    aktifAgacHasari = Mathf.Max(1, damageSetting.damage);
                }
            }

            int powerLevel = UpgradeManager.Instance.GetItemLevel(housePowerUpgradeItemId);
            HousePowerAttackUpgradeLevel speedSetting = UygunHizAyariniBul(powerLevel);
            if (speedSetting != null)
            {
                aktifSaldiriBeklemeSuresi = Mathf.Max(0.1f, speedSetting.attackCooldown);
                aktifVurusGecikmesi = Mathf.Clamp(speedSetting.hitDelay, 0f, aktifSaldiriBeklemeSuresi);
                aktifSaldiriAnimasyonHizi = Mathf.Max(0.1f, speedSetting.animationSpeed);
            }
        }

        if (animator != null && animatorSaldiriHiziParametresiVar)
        {
            animator.SetFloat(saldiriHiziParametresi, aktifSaldiriAnimasyonHizi);
        }
    }

    private string AktifBaltaItemId()
    {
        if (AxeTablePanelController.Instance != null &&
            !string.IsNullOrWhiteSpace(AxeTablePanelController.Instance.CurrentlyEquippedItemId))
        {
            return AxeTablePanelController.Instance.CurrentlyEquippedItemId;
        }

        return axeUpgradeItemId;
    }

    private AxeDamageUpgradeLevel UygunHasarAyariniBul(int level)
    {
        AxeDamageUpgradeLevel result = null;
        foreach (AxeDamageUpgradeLevel setting in axeDamageLevels)
        {
            if (setting != null && setting.level <= level && (result == null || setting.level > result.level))
            {
                result = setting;
            }
        }

        return result;
    }

    private HousePowerAttackUpgradeLevel UygunHizAyariniBul(int level)
    {
        HousePowerAttackUpgradeLevel result = null;
        foreach (HousePowerAttackUpgradeLevel setting in housePowerAttackLevels)
        {
            if (setting != null && setting.level <= level && (result == null || setting.level > result.level))
            {
                result = setting;
            }
        }

        return result;
    }

    private static bool AnimatorParametresiVar(
        Animator targetAnimator,
        int parameterHash,
        AnimatorControllerParameterType parameterType)
    {
        if (targetAnimator == null) return false;
        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
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

        foreach (AxeDamageUpgradeLevel setting in axeDamageLevels)
        {
            if (setting == null) continue;
            setting.level = Mathf.Max(1, setting.level);
            setting.damage = Mathf.Max(1, setting.damage);
        }

        foreach (HousePowerAttackUpgradeLevel setting in housePowerAttackLevels)
        {
            if (setting == null) continue;
            setting.level = Mathf.Max(1, setting.level);
            setting.attackCooldown = Mathf.Max(0.1f, setting.attackCooldown);
            setting.hitDelay = Mathf.Clamp(setting.hitDelay, 0f, setting.attackCooldown);
            setting.animationSpeed = Mathf.Max(0.1f, setting.animationSpeed);
        }
    }
}
