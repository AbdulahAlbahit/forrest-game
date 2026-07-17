using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EfeOdunTasima : MonoBehaviour
{
    [Header("Odun Toplama")]
    [Min(1)] [SerializeField] private int tasimaKapasitesi = 6;
    [Min(0.25f)] [SerializeField] private float toplamaYaricapi = 1.15f;
    [Min(0.02f)] [SerializeField] private float toplamaTaramaAraligi = 0.08f;
    [Min(1f)] [SerializeField] private float odunYerdeKalmaSuresi = 60f;

    [Header("Sepet Gorseli")]
    [SerializeField] private string odunYuvasiAdi = "LogStackAnchor";
    [Min(0.05f)] [SerializeField] private float sepetOdunuOlcegi = 0.74f;

    [Header("Kamyon Yukleme")]
    [Min(0.02f)] [SerializeField] private float yuklemeTekrarAraligi = 0.15f;

    public int TasinanOdunSayisi { get; private set; }
    public int TasimaKapasitesi => tasimaKapasitesi;
    public bool SepetDolu => TasinanOdunSayisi >= tasimaKapasitesi;

    private readonly Collider[] toplamaSonuclari = new Collider[24];
    private readonly List<GameObject> sepetOdunuGorselleri = new List<GameObject>();
    private Transform odunYuvasi;
    private TruckRouteController aktifKamyon;
    private Collider kamyonYuklemeAlani;
    private float sonrakiToplamaTaramasi;
    private float sonrakiYuklemeZamani;

    private void Start()
    {
        tasimaKapasitesi = Mathf.Max(tasimaKapasitesi, 6);

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

        Rigidbody rigidbody = odun.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }

        odun.SetActive(false);
        TasinanOdunSayisi++;
        SepetGorselleriniGuncelle();

        Debug.Log($"<color=cyan>[EFE ODUN TOPLADI] Sepet: {TasinanOdunSayisi}/{tasimaKapasitesi}</color>");
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
        if (TasinanOdunSayisi <= 0 || !KamyonYuklemeKaresiMi(temasEdilenAlan))
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

        int yuklenenOdun = 0;
        while (TasinanOdunSayisi > 0 && aktifKamyon.AddWoodToCargo(1))
        {
            TasinanOdunSayisi--;
            yuklenenOdun++;
        }

        if (yuklenenOdun <= 0)
        {
            return;
        }

        SepetGorselleriniGuncelle();
        Debug.Log(
            $"<color=green>[EFE KAMYONA YUKLEDI] {yuklenenOdun} odun | " +
            $"Sepet: {TasinanOdunSayisi}/{tasimaKapasitesi} | " +
            $"Kamyon: {aktifKamyon.currentLoadedWood}/{aktifKamyon.requiredWoodToDepart}</color>");
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

        for (int i = 0; i < tasimaKapasitesi; i++)
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
        }
    }

    private void SepetGorselleriniGuncelle()
    {
        for (int i = 0; i < sepetOdunuGorselleri.Count; i++)
        {
            sepetOdunuGorselleri[i].SetActive(i < TasinanOdunSayisi);
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

    private void OnValidate()
    {
        tasimaKapasitesi = Mathf.Max(1, tasimaKapasitesi);
        toplamaYaricapi = Mathf.Max(0.25f, toplamaYaricapi);
        toplamaTaramaAraligi = Mathf.Max(0.02f, toplamaTaramaAraligi);
        odunYerdeKalmaSuresi = Mathf.Max(1f, odunYerdeKalmaSuresi);
        sepetOdunuOlcegi = Mathf.Max(0.05f, sepetOdunuOlcegi);
        yuklemeTekrarAraligi = Mathf.Max(0.02f, yuklemeTekrarAraligi);
    }
}
