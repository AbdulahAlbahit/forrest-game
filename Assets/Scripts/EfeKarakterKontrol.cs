using UnityEngine;

public class EfeKarakterKontrol : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [Min(0f)] public float yuruHizi = 1.75f;
    [Min(0f)] public float kosuyaGecisHizi = 2.4f;
    [Min(0f)] public float kosuHizi = 5.5f;
    [Min(0f)] public float hizlanma = 12f;
    [Min(0f)] public float donmeHizi = 12f;
    [Min(0f)] public float yercekimi = 20f;

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

    private static readonly int HizParametresi = Animator.StringToHash("Hiz");
    private const float SenkronYurumeUstHizi = 1.75f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

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

        yatayHiz = Vector3.MoveTowards(yatayHiz, hedefYatayHiz, hizlanma * Time.deltaTime);

        if (controller.isGrounded && dikeyHiz < 0f)
        {
            dikeyHiz = -2f;
        }
        else
        {
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

    void OnValidate()
    {
        kosuyaGecisHizi = Mathf.Max(kosuyaGecisHizi, yuruHizi);
        kosuHizi = Mathf.Max(kosuHizi, kosuyaGecisHizi);
        kosuEsigi = Mathf.Max(kosuEsigi, hareketEsigi + 0.1f);
    }
}
