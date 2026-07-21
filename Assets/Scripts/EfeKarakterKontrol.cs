using UnityEngine;

public class EfeKarakterKontrol : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [Min(0f)] public float yuruHizi = 1.75f;
    [Min(0f)] public float kosuyaGecisHizi = 2.4f;
    [Min(0f)] public float kosuHizi = 4.6f;
    [Min(0f)] public float hizlanma = 12f;
    [Min(0f)] public float durmaSertligi = 40f;
    [Min(0f)] public float aniDonusSertligi = 45f;
    [Min(0f)] public float donmeHizi = 12f;
    [Min(0f)] public float yercekimi = 20f;

    [Header("Su Yuzeyinde Yurume")]
    [SerializeField] private string suNesnesiAdi = "water_river";
    [Min(0f)] [SerializeField] private float suBacakDerinligi = 1.25f;
    [Min(0.1f)] [SerializeField] private float suYukseklikDuzeltmeHizi = 5f;
    [Min(0.1f)] [SerializeField] private float suIcindekiAzamiSure = 5f;

    [Header("Mobil Kontrol")]
    public VirtualJoystick joystick;

    [Range(0f, 0.3f)]
    [Tooltip("Bu değerin altındaki küçük joystick hareketleri yok sayılır.")]
    public float hareketEsigi = 0.12f;

    [Range(0.5f, 0.95f)]
    [Tooltip("Joystick bu değeri geçince yürüyüşten koşuya karışmaya başlar.")]
    public float kosuEsigi = 0.75f;

    [Header("Animasyon Ayarları")]
    [Range(0.01f, 0.5f)]
    public float animasyonGecisSuresi = 0.12f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 yatayHiz;
    private float dikeyHiz;
    private Collider aktifSuAlani;
    private float aktifSuYuzeyi;
    private float suIcindeGecenSure;
    private bool ilkZeminTemasiGoruldu;
    private bool suModuAktif;
    private Vector3 baslangicKonumu;
    private Quaternion baslangicDonusu;

    private static readonly int HizParametresi = Animator.StringToHash("Hiz");
    private const float SenkronYurumeUstHizi = 1.75f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        baslangicKonumu = transform.position;
        baslangicDonusu = transform.rotation;

        if (GetComponent<EfeOdunTasima>() == null)
        {
            gameObject.AddComponent<EfeOdunTasima>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    void Update()
    {
        if (controller == null) return;

        Vector2 joystickGirdisi = joystick != null
            ? Vector2.ClampMagnitude(joystick.InputDirection, 1f)
            : Vector2.zero;
        float girdiMiktari = joystickGirdisi.magnitude;
        bool hareketEdiyor = girdiMiktari > hareketEsigi;

        Vector3 hedefYatayHiz = Vector3.zero;
        float animatorHizi = 0f;

        if (hareketEdiyor)
        {
            // Kameranın baktığı yatay (X-Z) yönü al (y ekseni sıfırlanır ki yere saplanmasın)
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;
            if (Camera.main != null)
            {
                camForward = Camera.main.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                camRight = Camera.main.transform.right;
                camRight.y = 0f;
                camRight.Normalize();
            }

            // Joystick girdisini kameranın yönüyle harmanla (İleri = Kameranın İlerisi)
            Vector3 hareketYonu = (camForward * joystickGirdisi.y + camRight * joystickGirdisi.x).normalized;

            Quaternion hedefDonus = Quaternion.LookRotation(hareketYonu);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedefDonus, Time.deltaTime * donmeHizi);

            float hedefHareketHizi;
            if (girdiMiktari < kosuEsigi)
            {
                float yurumeOrani = Mathf.InverseLerp(hareketEsigi, kosuEsigi, girdiMiktari);
                float senkronYurumeHizi = Mathf.Min(yuruHizi, SenkronYurumeUstHizi);
                hedefHareketHizi = Mathf.Lerp(senkronYurumeHizi * 0.55f, senkronYurumeHizi, yurumeOrani);
                animatorHizi = Mathf.Lerp(0.2f, 0.5f, yurumeOrani);
            }
            else
            {
                float kosmaOrani = Mathf.InverseLerp(kosuEsigi, 1f, girdiMiktari);
                hedefHareketHizi = Mathf.Lerp(kosuyaGecisHizi, kosuHizi, kosmaOrani);
                animatorHizi = Mathf.Lerp(0.5f, 1f, kosmaOrani);
            }

            hedefYatayHiz = hareketYonu * hedefHareketHizi;
        }

        bool keskinYonDegisimi = hareketEdiyor
                                && yatayHiz.sqrMagnitude > 0.01f
                                && Vector3.Dot(
                                    yatayHiz.normalized,
                                    hedefYatayHiz.normalized) < 0.25f;
        float hizDegisimSertligi = !hareketEdiyor
            ? durmaSertligi
            : keskinYonDegisimi
                ? aniDonusSertligi
                : hizlanma;
        yatayHiz = Vector3.MoveTowards(
            yatayHiz,
            hedefYatayHiz,
            hizDegisimSertligi * Time.deltaTime);

        if (!hareketEdiyor && yatayHiz.sqrMagnitude < 0.01f)
        {
            yatayHiz = Vector3.zero;
        }

        if (controller.isGrounded)
        {
            ilkZeminTemasiGoruldu = true;
        }

        if (!suModuAktif && SuyaGercektenGirdiMi())
        {
            suModuAktif = true;
        }

        if (suModuAktif && AktifSuAlaniGecerliMi())
        {
            suIcindeGecenSure += Time.deltaTime;
            if (suIcindeGecenSure >= suIcindekiAzamiSure)
            {
                BaslangicaDon();
                return;
            }

            aktifSuYuzeyi = aktifSuAlani.bounds.max.y;
            float hedefAyakYuksekligi = aktifSuYuzeyi - suBacakDerinligi;
            float sonrakiAyakYuksekligi = Mathf.MoveTowards(
                transform.position.y,
                hedefAyakYuksekligi,
                suYukseklikDuzeltmeHizi * Time.deltaTime);
            dikeyHiz = (sonrakiAyakYuksekligi - transform.position.y)
                        / Mathf.Max(Time.deltaTime, 0.0001f);
        }
        else if (controller.isGrounded && dikeyHiz < 0f)
        {
            suIcindeGecenSure = 0f;
            dikeyHiz = -2f;
        }
        else
        {
            suIcindeGecenSure = 0f;
            dikeyHiz -= yercekimi * Time.deltaTime;
        }

        Vector3 hareket = yatayHiz + Vector3.up * dikeyHiz;
        controller.Move(hareket * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat(
                HizParametresi,
                animatorHizi,
                animasyonGecisSuresi,
                Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        SuAlaniniGuncelle(other);
    }

    private void OnTriggerStay(Collider other)
    {
        SuAlaniniGuncelle(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == aktifSuAlani)
        {
            aktifSuAlani = null;
            suModuAktif = false;
            suIcindeGecenSure = 0f;
            dikeyHiz = -2f;
        }
    }

    private void SuAlaniniGuncelle(Collider alan)
    {
        if (!alan.isTrigger || !SuAlaniMi(alan.transform))
        {
            return;
        }

        aktifSuAlani = alan;
        aktifSuYuzeyi = alan.bounds.max.y;
    }

    private bool SuAlaniMi(Transform alan)
    {
        Transform mevcut = alan;
        while (mevcut != null)
        {
            if (mevcut.name == suNesnesiAdi)
            {
                return true;
            }

            mevcut = mevcut.parent;
        }

        return false;
    }

    private bool AktifSuAlaniGecerliMi()
    {
        return aktifSuAlani != null
               && aktifSuAlani.enabled
               && aktifSuAlani.gameObject.activeInHierarchy;
    }

    private bool SuyaGercektenGirdiMi()
    {
        if (!ilkZeminTemasiGoruldu || !AktifSuAlaniGecerliMi() || controller.isGrounded)
        {
            return false;
        }

        aktifSuYuzeyi = aktifSuAlani.bounds.max.y;
        return transform.position.y <= aktifSuYuzeyi + 0.1f;
    }

    private void BaslangicaDon()
    {
        aktifSuAlani = null;
        suModuAktif = false;
        suIcindeGecenSure = 0f;
        yatayHiz = Vector3.zero;
        dikeyHiz = -2f;

        controller.enabled = false;
        transform.SetPositionAndRotation(baslangicKonumu, baslangicDonusu);
        controller.enabled = true;

        if (animator != null)
        {
            animator.SetFloat(HizParametresi, 0f);
        }
    }

    void OnValidate()
    {
        kosuyaGecisHizi = Mathf.Max(kosuyaGecisHizi, yuruHizi);
        kosuHizi = Mathf.Max(kosuHizi, kosuyaGecisHizi);
        durmaSertligi = Mathf.Max(0f, durmaSertligi);
        aniDonusSertligi = Mathf.Max(0f, aniDonusSertligi);
        suBacakDerinligi = Mathf.Max(0f, suBacakDerinligi);
        suYukseklikDuzeltmeHizi = Mathf.Max(0.1f, suYukseklikDuzeltmeHizi);
        suIcindekiAzamiSure = Mathf.Max(0.1f, suIcindekiAzamiSure);
        kosuEsigi = Mathf.Max(kosuEsigi, hareketEsigi + 0.1f);
    }
}
