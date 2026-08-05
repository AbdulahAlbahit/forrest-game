using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EfeOdunTasima : MonoBehaviour
{
    private static readonly int[] KamyonKasaSatirSirasi = { 2, 1, 3, 0, 4 };

    [Header("Odun Toplama")]
    [Min(1)] [SerializeField] private int tasimaKapasitesi = 6;
    [Min(0.25f)] [SerializeField] private float toplamaYaricapi = 1.15f;
    [Min(0.02f)] [SerializeField] private float toplamaTaramaAraligi = 0.08f;
    [Min(1f)] [SerializeField] private float odunYerdeKalmaSuresi = 60f;

    [Header("Upgrade Baglantisi")]
    [SerializeField] private bool useBasketUpgradeLevel = true;
    [SerializeField] private string basketUpgradeItemId = "Basket";

    [Header("Sepet Gorseli")]
    [SerializeField] private string odunYuvasiAdi = "LogStackAnchor";
    [Min(0.05f)] [SerializeField] private float sepetOdunuOlcegi = 0.74f;

    [Header("Odun Toplama Animasyonu")]
    [Min(0.1f)] [SerializeField] private float sepeteUcusSuresi = 0.45f;
    [Min(0f)] [SerializeField] private float sepeteUcusYuksekligi = 1.3f;
    [Min(0f)] [SerializeField] private float sepeteUcusDonusAcisi = 360f;

    [Header("Kamyon Yukleme")]
    [Min(0.02f)] [SerializeField] private float yuklemeTekrarAraligi = 0.15f;

    [Header("Kamyon Yukleme Upgrade Baglantisi")]
    [SerializeField] private bool useTruckLoadSpeedUpgrade = true;
    [SerializeField] private string truckLoadSpeedItemId = "TruckLoadSpeed";

    [Header("Kamyon Yukleme Gorseli")]
    [Min(0.1f)] [SerializeField] private float kamyonaUcusSuresi = 0.5f;
    [Min(0f)] [SerializeField] private float kamyonaUcusYuksekligi = 1.35f;
    [Min(0.05f)] [SerializeField] private float kamyonOdunuOlcegi = 0.52f;
    [Range(1, 30)] [SerializeField] private int kamyonGorselUstSiniri = 30;

    public int TasinanOdunSayisi { get; private set; }
    public int TasimaKapasitesi => tasimaKapasitesi;
    public bool SepetDolu => TasinanOdunSayisi >= tasimaKapasitesi;

    private readonly Collider[] toplamaSonuclari = new Collider[24];
    private readonly List<GameObject> sepetOdunuGorselleri = new List<GameObject>();
    private readonly List<int> sepetAnimasyonSurumleri = new List<int>();
    private readonly List<Vector3> sepetSlotYerelKonumlari = new List<Vector3>();
    private readonly List<Quaternion> sepetSlotYerelDonusleri = new List<Quaternion>();
    private readonly List<Vector3> sepetSlotYerelOlcekleri = new List<Vector3>();
    private readonly List<GameObject> kamyonOdunuGorselleri = new List<GameObject>();
    private Transform odunYuvasi;
    private Transform kamyonOdunuGorselKoku;
    private TruckRouteController aktifKamyon;
    private Collider kamyonYuklemeAlani;
    private float sonrakiToplamaTaramasi;
    private float sonrakiYuklemeZamani;
    private bool kamyonYuklemeAnimasyonuCalisiyor;
    private int sonGosterilenKamyonOdunu = -1;
    private bool upgradeEventineAbone;
    private bool dataEventineAbone;

    private void OnEnable()
    {
        UpgradeEventlerineAboneOl();
        UpgradeKapasitesiniUygula();
        TruckYuklemeHiziniUygula();
    }

    private void Start()
    {
        tasimaKapasitesi = Mathf.Max(tasimaKapasitesi, 6);
        UpgradeEventlerineAboneOl();
        UpgradeKapasitesiniUygula();
        TruckYuklemeHiziniUygula();

        if (TreeManager.Instance != null)
        {
            TreeManager.Instance.logDisappearDelay = odunYerdeKalmaSuresi;
        }

        odunYuvasi = AltNesneyiAdaGoreBul(transform, odunYuvasiAdi);
        GameObject yuklemeKaresi = GameObject.Find("Zone_Truck_Wood_Loading");
        if (yuklemeKaresi != null)
        {
            kamyonYuklemeAlani = yuklemeKaresi.GetComponent<Collider>();
        }

        SepetGorselleriniHazirla();
        SepetGorselleriniGuncelle();
    }

    private void Update()
    {
        if (!SepetDolu && Time.time >= sonrakiToplamaTaramasi)
        {
            sonrakiToplamaTaramasi = Time.time + toplamaTaramaAraligi;
            YakindakiOdunlariTopla();
        }

        if (TasinanOdunSayisi > 0
            && kamyonYuklemeAlani != null
            && Time.time >= sonrakiYuklemeZamani)
        {
            Vector3 kontrolNoktasi = transform.position + Vector3.up * 0.5f;
            Vector3 enYakinNokta = kamyonYuklemeAlani.ClosestPoint(kontrolNoktasi);
            if ((enYakinNokta - kontrolNoktasi).sqrMagnitude < 0.01f)
            {
                KamyonKaresindeyseYukle(kamyonYuklemeAlani);
            }
        }

        if (aktifKamyon != null && aktifKamyon.currentLoadedWood != sonGosterilenKamyonOdunu)
        {
            KamyonYukGorselleriniGuncelle();
        }
    }

    private void YakindakiOdunlariTopla()
    {
        Vector3 taramaMerkezi = transform.position + Vector3.up * 0.55f;
        int sonucSayisi = Physics.OverlapSphereNonAlloc(
            taramaMerkezi,
            toplamaYaricapi,
            toplamaSonuclari,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < sonucSayisi && !SepetDolu; i++)
        {
            Collider bulunanCollider = toplamaSonuclari[i];
            if (bulunanCollider == null)
            {
                continue;
            }

            GameObject odun = bulunanCollider.attachedRigidbody != null
                ? bulunanCollider.attachedRigidbody.gameObject
                : bulunanCollider.gameObject;

            if (!odun.activeInHierarchy || !odun.name.StartsWith("Pool_WoodLog_"))
            {
                continue;
            }

            OdunuSepeteAl(odun);
        }
    }

    private void OdunuSepeteAl(GameObject odun)
    {
        if (sepetOdunuGorselleri.Count == 0)
        {
            odunYuvasi = AltNesneyiAdaGoreBul(transform, odunYuvasiAdi);
            SepetGorselleriniHazirla();
        }

        Vector3 odununDunyaKonumu = odun.transform.position;
        Quaternion odununDunyaDonusu = odun.transform.rotation;

        Rigidbody rigidbody = odun.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }

        odun.SetActive(false);
        TasinanOdunSayisi++;
        EfeOynanisEventleri.OdunToplandiginiYayinla(
            transform,
            TasinanOdunSayisi,
            tasimaKapasitesi);
        if (SepetDolu)
        {
            EfeOynanisEventleri.SepetDoldugunuYayinla(transform);
        }

        int sepetSirasi = TasinanOdunSayisi - 1;
        if (odunYuvasi != null && sepetSirasi < sepetOdunuGorselleri.Count)
        {
            OdunuAnimasyonlaSepeteYerlestir(
                sepetSirasi,
                odununDunyaKonumu,
                odununDunyaDonusu);
        }
        else
        {
            SepetGorselleriniGuncelle();
        }

        Debug.Log($"<color=cyan>[EFE ODUN TOPLADI] Sepet: {TasinanOdunSayisi}/{tasimaKapasitesi}</color>");
    }

    private void OdunuAnimasyonlaSepeteYerlestir(
        int sepetSirasi,
        Vector3 baslangicKonumu,
        Quaternion baslangicDonusu)
    {
        GameObject gorsel = sepetOdunuGorselleri[sepetSirasi];
        Transform gorselTransformi = gorsel.transform;
        Vector3 hedefYerelKonum = sepetSlotYerelKonumlari[sepetSirasi];
        Quaternion hedefYerelDonus = sepetSlotYerelDonusleri[sepetSirasi];
        Vector3 hedefYerelOlcek = sepetSlotYerelOlcekleri[sepetSirasi];

        sepetAnimasyonSurumleri[sepetSirasi]++;
        int animasyonSurumu = sepetAnimasyonSurumleri[sepetSirasi];

        gorselTransformi.localScale = hedefYerelOlcek * 0.9f;
        gorselTransformi.SetPositionAndRotation(baslangicKonumu, baslangicDonusu);
        gorsel.SetActive(true);

        StartCoroutine(OdunuSepeteUcur(
            sepetSirasi,
            animasyonSurumu,
            baslangicKonumu,
            baslangicDonusu,
            hedefYerelKonum,
            hedefYerelDonus,
            hedefYerelOlcek));
    }

    private IEnumerator OdunuSepeteUcur(
        int sepetSirasi,
        int animasyonSurumu,
        Vector3 baslangicKonumu,
        Quaternion baslangicDonusu,
        Vector3 hedefYerelKonum,
        Quaternion hedefYerelDonus,
        Vector3 hedefYerelOlcek)
    {
        GameObject gorsel = sepetOdunuGorselleri[sepetSirasi];
        Transform gorselTransformi = gorsel.transform;
        float gecenSure = 0f;

        while (gecenSure < sepeteUcusSuresi)
        {
            if (sepetAnimasyonSurumleri[sepetSirasi] != animasyonSurumu)
            {
                yield break;
            }

            if (sepetSirasi >= TasinanOdunSayisi)
            {
                gorsel.SetActive(false);
                yield break;
            }

            gecenSure += Time.deltaTime;
            float zaman = Mathf.Clamp01(gecenSure / sepeteUcusSuresi);
            float yumusatilmisZaman = 1f - Mathf.Pow(1f - zaman, 3f);
            Vector3 hedefDunyaKonumu = odunYuvasi.TransformPoint(hedefYerelKonum);
            Vector3 ortaNokta = (baslangicKonumu + hedefDunyaKonumu) * 0.5f
                                + Vector3.up * sepeteUcusYuksekligi;

            float kalan = 1f - yumusatilmisZaman;
            gorselTransformi.position =
                kalan * kalan * baslangicKonumu
                + 2f * kalan * yumusatilmisZaman * ortaNokta
                + yumusatilmisZaman * yumusatilmisZaman * hedefDunyaKonumu;

            Quaternion hedefDunyaDonusu = odunYuvasi.rotation * hedefYerelDonus;
            Quaternion temelDonus = Quaternion.Slerp(
                baslangicDonusu,
                hedefDunyaDonusu,
                yumusatilmisZaman);
            Quaternion havadaDonus = Quaternion.AngleAxis(
                sepeteUcusDonusAcisi * zaman,
                Vector3.right);
            gorselTransformi.rotation = havadaDonus * temelDonus;

            float hafifBuyume = 1f + Mathf.Sin(zaman * Mathf.PI) * 0.12f;
            gorselTransformi.localScale = hedefYerelOlcek * hafifBuyume;
            yield return null;
        }

        if (sepetSirasi < TasinanOdunSayisi
            && sepetAnimasyonSurumleri[sepetSirasi] == animasyonSurumu)
        {
            gorselTransformi.localPosition = hedefYerelKonum;
            gorselTransformi.localRotation = hedefYerelDonus;
            gorselTransformi.localScale = hedefYerelOlcek;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        KamyonKaresindeyseYukle(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time >= sonrakiYuklemeZamani)
        {
            KamyonKaresindeyseYukle(other);
        }
    }

    private void KamyonKaresindeyseYukle(Collider temasEdilenAlan)
    {
        if (TasinanOdunSayisi <= 0
            || kamyonYuklemeAnimasyonuCalisiyor
            || !KamyonYuklemeKaresiMi(temasEdilenAlan))
        {
            return;
        }

        sonrakiYuklemeZamani = Time.time + yuklemeTekrarAraligi;

        if (aktifKamyon == null)
        {
            aktifKamyon = Object.FindAnyObjectByType<TruckRouteController>();
        }

        if (aktifKamyon == null || !aktifKamyon.isAtHomePosition)
        {
            return;
        }

        KamyonYukGorselleriniHazirla();
        StartCoroutine(OdunlariAnimasyonlaKamyonaYukle());
    }

    private IEnumerator OdunlariAnimasyonlaKamyonaYukle()
    {
        kamyonYuklemeAnimasyonuCalisiyor = true;
        EfeOynanisEventleri.KamyonYuklemeBasladiginiYayinla(transform);
        int yuklenenOdun = 0;

        while (TasinanOdunSayisi > 0
               && aktifKamyon != null
               && aktifKamyon.isAtHomePosition
               && aktifKamyon.currentLoadedWood < aktifKamyon.requiredWoodToDepart)
        {
            int sepetSirasi = TasinanOdunSayisi - 1;
            int hedefKamyonSirasi = KamyonGorunurOdunSayisiniHesapla(
                aktifKamyon.currentLoadedWood + 1) - 1;

            if (sepetSirasi < 0
                || sepetSirasi >= sepetOdunuGorselleri.Count
                || hedefKamyonSirasi < 0
                || hedefKamyonSirasi >= kamyonOdunuGorselleri.Count)
            {
                break;
            }

            GameObject sepetOdunu = sepetOdunuGorselleri[sepetSirasi];
            GameObject kamyonOdunu = kamyonOdunuGorselleri[hedefKamyonSirasi];
            sepetAnimasyonSurumleri[sepetSirasi]++;
            sepetOdunu.SetActive(true);

            yield return StartCoroutine(OdunuKamyonaUcur(
                sepetOdunu.transform,
                kamyonOdunu.transform));

            if (aktifKamyon == null
                || !aktifKamyon.isAtHomePosition
                || !aktifKamyon.AddWoodToCargo(1))
            {
                SepetGorselleriniGuncelle();
                break;
            }

            TasinanOdunSayisi--;
            yuklenenOdun++;
            EfeOynanisEventleri.OdunKamyonaYuklendiginiYayinla(
                transform,
                aktifKamyon.currentLoadedWood,
                aktifKamyon.requiredWoodToDepart);
            if (TasinanOdunSayisi == 0)
            {
                EfeOynanisEventleri.SepetBosaldiginiYayinla(transform);
            }
            SepetGorselleriniGuncelle();
            KamyonYukGorselleriniGuncelle();

            sonrakiYuklemeZamani = Time.time + yuklemeTekrarAraligi;
            yield return new WaitForSeconds(yuklemeTekrarAraligi);
        }

        kamyonYuklemeAnimasyonuCalisiyor = false;
        EfeOynanisEventleri.KamyonYuklemeBittiginiYayinla(transform);

        if (yuklenenOdun > 0)
        {
            Debug.Log(
                $"<color=green>[EFE KAMYONA YUKLEDI] {yuklenenOdun} odun | " +
                $"Sepet: {TasinanOdunSayisi}/{tasimaKapasitesi} | " +
                $"Kamyon: {aktifKamyon.currentLoadedWood}/{aktifKamyon.requiredWoodToDepart}</color>");
        }
    }

    private IEnumerator OdunuKamyonaUcur(Transform ucacakOdun, Transform hedefSlot)
    {
        Vector3 baslangicKonumu = ucacakOdun.position;
        Quaternion baslangicDonusu = ucacakOdun.rotation;
        Vector3 baslangicYerelOlcegi = ucacakOdun.localScale;
        float gecenSure = 0f;

        while (gecenSure < kamyonaUcusSuresi)
        {
            if (aktifKamyon == null || hedefSlot == null)
            {
                yield break;
            }

            gecenSure += Time.deltaTime;
            float zaman = Mathf.Clamp01(gecenSure / kamyonaUcusSuresi);
            float yumusatilmisZaman = 1f - Mathf.Pow(1f - zaman, 3f);
            Vector3 hedefKonumu = hedefSlot.position;
            Vector3 ortaNokta = (baslangicKonumu + hedefKonumu) * 0.5f
                                + Vector3.up * kamyonaUcusYuksekligi;
            float kalan = 1f - yumusatilmisZaman;

            ucacakOdun.position =
                kalan * kalan * baslangicKonumu
                + 2f * kalan * yumusatilmisZaman * ortaNokta
                + yumusatilmisZaman * yumusatilmisZaman * hedefKonumu;
            Quaternion temelDonus = Quaternion.Slerp(
                baslangicDonusu,
                hedefSlot.rotation,
                yumusatilmisZaman);
            ucacakOdun.rotation =
                Quaternion.AngleAxis(360f * zaman, Vector3.right) * temelDonus;
            Vector3 hedefOlcek = hedefSlot.localScale;
            ucacakOdun.localScale =
                Vector3.Lerp(baslangicYerelOlcegi, hedefOlcek, yumusatilmisZaman)
                * (1f + Mathf.Sin(zaman * Mathf.PI) * 0.1f);
            yield return null;
        }

        ucacakOdun.SetPositionAndRotation(hedefSlot.position, hedefSlot.rotation);
        ucacakOdun.localScale = hedefSlot.localScale;
    }

    private void KamyonYukGorselleriniHazirla()
    {
        if (aktifKamyon == null)
        {
            return;
        }

        if (kamyonOdunuGorselKoku != null
            && kamyonOdunuGorselKoku.parent == aktifKamyon.transform
            && kamyonOdunuGorselleri.Count > 0)
        {
            return;
        }

        if (kamyonOdunuGorselKoku != null)
        {
            Destroy(kamyonOdunuGorselKoku.gameObject);
        }

        kamyonOdunuGorselleri.Clear();
        sonGosterilenKamyonOdunu = -1;

        GameObject odunPrefab = TreeManager.Instance != null ? TreeManager.Instance.woodLogPrefab : null;
        if (odunPrefab == null)
        {
            Debug.LogWarning("[EfeOdunTasima] Kamyon odun gorseli icin woodLogPrefab bulunamadi.");
            return;
        }

        Bounds kamyonYerelSinirlari = KamyonYerelSinirlariniHesapla(aktifKamyon.transform);
        GameObject kok = new GameObject("Efe_KamyonOdunGorselleri");
        kamyonOdunuGorselKoku = kok.transform;
        kamyonOdunuGorselKoku.SetParent(aktifKamyon.transform, false);
        kamyonOdunuGorselKoku.localPosition = new Vector3(
            kamyonYerelSinirlari.center.x,
            kamyonYerelSinirlari.min.y + kamyonYerelSinirlari.size.y * 0.42f,
            Mathf.Lerp(kamyonYerelSinirlari.min.z, kamyonYerelSinirlari.max.z, 0.3f));
        kamyonOdunuGorselKoku.localRotation = Quaternion.identity;

        int gorselSayisi = Mathf.Clamp(
            aktifKamyon.requiredWoodToDepart,
            1,
            kamyonGorselUstSiniri);
        int sutunSayisi = 3;
        int satirSayisi = 5;
        int katBasinaOdun = sutunSayisi * satirSayisi;
        float yatayAralik = Mathf.Max(0.2f, kamyonYerelSinirlari.size.x * 0.16f);
        float derinlikAraligi = Mathf.Max(0.2f, kamyonYerelSinirlari.size.z * 0.075f);

        for (int i = 0; i < gorselSayisi; i++)
        {
            GameObject gorsel = Instantiate(odunPrefab, kamyonOdunuGorselKoku);
            gorsel.name = $"Efe_KamyonOdunu_{i + 1}";

            Rigidbody rigidbody = gorsel.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            foreach (Collider collider in gorsel.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            int kat = i / katBasinaOdun;
            int katSirasi = i % katBasinaOdun;
            int satir = katSirasi / sutunSayisi;
            int sutun = katSirasi % sutunSayisi;
            int kasaSatiri = KamyonKasaSatirSirasi[satir];
            gorsel.transform.localPosition = new Vector3(
                (sutun - 1) * yatayAralik,
                kat * 0.1f,
                (kasaSatiri - 2) * derinlikAraligi);
            gorsel.transform.localRotation = Quaternion.Euler(0f, 90f, kat % 2 == 0 ? -3f : 3f);
            gorsel.transform.localScale = Vector3.one * kamyonOdunuOlcegi;
            gorsel.SetActive(false);
            kamyonOdunuGorselleri.Add(gorsel);
        }

        KamyonYukGorselleriniGuncelle();
    }

    private void KamyonYukGorselleriniGuncelle()
    {
        if (aktifKamyon == null)
        {
            return;
        }

        KamyonYukGorselleriniHazirla();
        int gorunurOdun = KamyonGorunurOdunSayisiniHesapla(aktifKamyon.currentLoadedWood);
        for (int i = 0; i < kamyonOdunuGorselleri.Count; i++)
        {
            kamyonOdunuGorselleri[i].SetActive(i < gorunurOdun);
        }

        sonGosterilenKamyonOdunu = aktifKamyon.currentLoadedWood;
    }

    private int KamyonGorunurOdunSayisiniHesapla(int yuklenenOdun)
    {
        if (aktifKamyon == null
            || aktifKamyon.requiredWoodToDepart <= 0
            || kamyonOdunuGorselleri.Count == 0)
        {
            return 0;
        }

        float doluluk = Mathf.Clamp01(
            (float)yuklenenOdun / aktifKamyon.requiredWoodToDepart);
        return Mathf.Clamp(
            Mathf.CeilToInt(doluluk * kamyonOdunuGorselleri.Count),
            0,
            kamyonOdunuGorselleri.Count);
    }

    private static Bounds KamyonYerelSinirlariniHesapla(Transform kamyon)
    {
        Renderer[] renderers = kamyon.GetComponentsInChildren<Renderer>(true);
        Bounds yerelSinirlar = new Bounds(Vector3.zero, Vector3.one * 2f);
        bool ilkNokta = true;

        foreach (Renderer renderer in renderers)
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
            {
                continue;
            }

            Bounds dunyaSinirlari = renderer.bounds;
            Vector3 min = dunyaSinirlari.min;
            Vector3 max = dunyaSinirlari.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 dunyaNoktasi = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 yerelNokta = kamyon.InverseTransformPoint(dunyaNoktasi);

                        if (ilkNokta)
                        {
                            yerelSinirlar = new Bounds(yerelNokta, Vector3.zero);
                            ilkNokta = false;
                        }
                        else
                        {
                            yerelSinirlar.Encapsulate(yerelNokta);
                        }
                    }
                }
            }
        }

        return yerelSinirlar;
    }

    private static bool KamyonYuklemeKaresiMi(Collider alan)
    {
        Transform mevcut = alan.transform;
        while (mevcut != null)
        {
            if (mevcut.name == "Zone_Truck_Wood_Loading")
            {
                return true;
            }

            mevcut = mevcut.parent;
        }

        return false;
    }

    private void SepetGorselleriniHazirla()
    {
        if (odunYuvasi == null)
        {
            Debug.LogWarning("[EfeOdunTasima] LogStackAnchor bulunamadi; sayac calisir fakat odunlar sirtta gorunmez.");
            return;
        }

        GameObject odunPrefab = TreeManager.Instance != null ? TreeManager.Instance.woodLogPrefab : null;
        if (odunPrefab == null)
        {
            Debug.LogWarning("[EfeOdunTasima] TreeManager woodLogPrefab bulunamadi; sepet gorselleri olusturulamadi.");
            return;
        }

        for (int i = sepetOdunuGorselleri.Count; i < tasimaKapasitesi; i++)
        {
            GameObject gorsel = Instantiate(odunPrefab, odunYuvasi);
            gorsel.name = $"Efe_SepetOdunu_{i + 1}";

            Rigidbody rigidbody = gorsel.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            foreach (Collider collider in gorsel.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            int kat = i / 2;
            float derinlik = i % 2 == 0 ? -0.1f : 0.1f;
            float hafifYatayKaydirma = kat == 0 ? -0.025f : 0.025f;
            gorsel.transform.localPosition = new Vector3(
                hafifYatayKaydirma,
                0.43f + kat * 0.17f,
                derinlik);
            gorsel.transform.localRotation = Quaternion.Euler(0f, 90f, kat == 0 ? -4f : 4f);
            float gorunurOdunOlcegi = Mathf.Max(sepetOdunuOlcegi, 0.74f);
            gorsel.transform.localScale = Vector3.one * gorunurOdunOlcegi;
            gorsel.SetActive(false);
            sepetOdunuGorselleri.Add(gorsel);
            sepetAnimasyonSurumleri.Add(0);
            sepetSlotYerelKonumlari.Add(gorsel.transform.localPosition);
            sepetSlotYerelDonusleri.Add(gorsel.transform.localRotation);
            sepetSlotYerelOlcekleri.Add(gorsel.transform.localScale);
        }
    }

    /// <summary>
    /// Karakterin tasidigi tum odunlari harici bir depolama sistemine aktarilmak
    /// uzere sepetten cikarir. Kalici stok ve kayit islemi bu sinifin sorumlulugu
    /// degildir; basarili aktarilan miktari cagirana dondurur.
    /// </summary>
    public int TasinanOdunlariAmbaraAktar()
    {
        if (kamyonYuklemeAnimasyonuCalisiyor || TasinanOdunSayisi <= 0)
        {
            return 0;
        }

        int aktarilanMiktar = TasinanOdunSayisi;
        TasinanOdunSayisi = 0;
        SepetGorselleriniGuncelle();
        EfeOynanisEventleri.SepetBosaldiginiYayinla(transform);

        return aktarilanMiktar;
    }

    /// <summary>
    /// Kalici ambardan, sepetteki bos yer kadar odunu karaktere geri verir.
    /// Kalici stoktan dusme ve kayit islemi EfeWoodStorageBridge sorumlulugundadir.
    /// </summary>
    public int AmbardanOdunAl(int istenenMiktar, Vector3 ambarDunyaKonumu)
    {
        if (kamyonYuklemeAnimasyonuCalisiyor || istenenMiktar <= 0 || SepetDolu)
        {
            return 0;
        }

        if (sepetOdunuGorselleri.Count == 0)
        {
            odunYuvasi = AltNesneyiAdaGoreBul(transform, odunYuvasiAdi);
            SepetGorselleriniHazirla();
        }

        int bosKapasite = Mathf.Max(0, tasimaKapasitesi - TasinanOdunSayisi);
        int alinacakMiktar = Mathf.Min(istenenMiktar, bosKapasite);
        int baslangicSayisi = TasinanOdunSayisi;

        for (int i = 0; i < alinacakMiktar; i++)
        {
            int sepetSirasi = baslangicSayisi + i;
            TasinanOdunSayisi++;
            EfeOynanisEventleri.OdunToplandiginiYayinla(
                transform,
                TasinanOdunSayisi,
                tasimaKapasitesi);

            if (odunYuvasi != null && sepetSirasi < sepetOdunuGorselleri.Count)
            {
                Vector3 dagitilmisBaslangic = ambarDunyaKonumu
                    + Vector3.up * (0.12f * (i / 2))
                    + transform.right * (i % 2 == 0 ? -0.12f : 0.12f);
                OdunuAnimasyonlaSepeteYerlestir(
                    sepetSirasi,
                    dagitilmisBaslangic,
                    Quaternion.Euler(0f, 90f, 0f));
            }
        }

        if (alinacakMiktar > 0 && SepetDolu)
        {
            EfeOynanisEventleri.SepetDoldugunuYayinla(transform);
        }

        if (sepetOdunuGorselleri.Count < TasinanOdunSayisi)
        {
            SepetGorselleriniGuncelle();
        }

        return alinacakMiktar;
    }

    private void SepetGorselleriniGuncelle()
    {
        for (int i = 0; i < sepetOdunuGorselleri.Count; i++)
        {
            bool gorunurOlmali = i < TasinanOdunSayisi;
            if (!gorunurOlmali)
            {
                sepetAnimasyonSurumleri[i]++;
                sepetOdunuGorselleri[i].transform.localPosition = sepetSlotYerelKonumlari[i];
                sepetOdunuGorselleri[i].transform.localRotation = sepetSlotYerelDonusleri[i];
                sepetOdunuGorselleri[i].transform.localScale = sepetSlotYerelOlcekleri[i];
            }

            sepetOdunuGorselleri[i].SetActive(gorunurOlmali);
        }
    }

    private static Transform AltNesneyiAdaGoreBul(Transform kok, string arananAd)
    {
        foreach (Transform altNesne in kok.GetComponentsInChildren<Transform>(true))
        {
            if (altNesne.name == arananAd)
            {
                return altNesne;
            }
        }

        return null;
    }

    private void UpgradeEventlerineAboneOl()
    {
        if (!useBasketUpgradeLevel && !useTruckLoadSpeedUpgrade) return;

        if (!upgradeEventineAbone && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded += UpgradeYapildiginda;
            upgradeEventineAbone = true;
        }

        if (!dataEventineAbone && CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded += DataYuklendiginde;
            dataEventineAbone = true;
        }
    }

    private void UpgradeEventlerindenCik()
    {
        if (upgradeEventineAbone && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnItemUpgraded -= UpgradeYapildiginda;
        }

        if (dataEventineAbone && CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnDataLoaded -= DataYuklendiginde;
        }

        upgradeEventineAbone = false;
        dataEventineAbone = false;
    }

    private void UpgradeYapildiginda(string itemId, int newLevel)
    {
        if (itemId == basketUpgradeItemId)
        {
            UpgradeKapasitesiniUygula();
        }

        if (itemId == truckLoadSpeedItemId)
        {
            TruckYuklemeHiziniUygula();
        }
    }

    private void DataYuklendiginde(PlayerData data)
    {
        UpgradeKapasitesiniUygula();
        TruckYuklemeHiziniUygula();
    }

    /// <summary>
    /// TruckLoadSpeed config'i "saniyede yuklenen odun" hizi olarak tanimlanir
    /// (WorkshopManager'daki hiz upgrade'leriyle ayni format). Yukleme araligi
    /// bu hizin tersine (1 / hiz) cevrilerek uygulanir.
    /// </summary>
    public void TruckYuklemeHiziniUygula()
    {
        if (!useTruckLoadSpeedUpgrade || UpgradeManager.Instance == null)
        {
            return;
        }

        if (!UpgradeManager.Instance.TryGetConfig(truckLoadSpeedItemId, out _))
        {
            return;
        }

        float odunBasinaHiz = UpgradeManager.Instance.GetItemValue(truckLoadSpeedItemId);
        yuklemeTekrarAraligi = Mathf.Max(0.02f, 1f / Mathf.Max(0.01f, odunBasinaHiz));
    }

    public void UpgradeKapasitesiniUygula()
    {
        if (!useBasketUpgradeLevel || UpgradeManager.Instance == null)
        {
            return;
        }

        int oldCapacity = tasimaKapasitesi;
        // Kapasiteyi ayri bir tabloyla tekrar tanimlamak yerine mevcut
        // ItemUpgradeConfig -> UpgradeManager hattindan oku.
        tasimaKapasitesi = Mathf.Max(
            1,
            Mathf.RoundToInt(UpgradeManager.Instance.GetItemValue(basketUpgradeItemId)));
        if (tasimaKapasitesi > oldCapacity && odunYuvasi != null)
        {
            SepetGorselleriniHazirla();
        }

        SepetGorselleriniGuncelle();
    }

    private void OnDisable()
    {
        UpgradeEventlerindenCik();
    }

    private void OnValidate()
    {
        tasimaKapasitesi = Mathf.Max(1, tasimaKapasitesi);
        toplamaYaricapi = Mathf.Max(0.25f, toplamaYaricapi);
        toplamaTaramaAraligi = Mathf.Max(0.02f, toplamaTaramaAraligi);
        odunYerdeKalmaSuresi = Mathf.Max(1f, odunYerdeKalmaSuresi);
        sepetOdunuOlcegi = Mathf.Max(0.05f, sepetOdunuOlcegi);
        sepeteUcusSuresi = Mathf.Max(0.1f, sepeteUcusSuresi);
        sepeteUcusYuksekligi = Mathf.Max(0f, sepeteUcusYuksekligi);
        sepeteUcusDonusAcisi = Mathf.Max(0f, sepeteUcusDonusAcisi);
        yuklemeTekrarAraligi = Mathf.Max(0.02f, yuklemeTekrarAraligi);
        kamyonaUcusSuresi = Mathf.Max(0.1f, kamyonaUcusSuresi);
        kamyonaUcusYuksekligi = Mathf.Max(0f, kamyonaUcusYuksekligi);
        kamyonOdunuOlcegi = Mathf.Max(0.05f, kamyonOdunuOlcegi);
        kamyonGorselUstSiniri = Mathf.Clamp(kamyonGorselUstSiniri, 1, 30);
    }
}
