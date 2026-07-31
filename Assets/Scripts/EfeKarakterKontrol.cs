using UnityEngine;

public class EfeKarakterKontrol : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float yuruHizi = 2f;
    public float kosuHizi = 6f;
    public float donmeHizi = 10f;
    public float yercekimi = 9.81f;

    [Header("Mobil Kontrol")]
    public VirtualJoystick joystick;

    [Range(0.2f, 0.95f)]
    [Tooltip("Joystick yüzde kaç itildiğinde koşmaya başlasın? (Örn: 0.8 = %80)")]
    public float kosuEsigi = 0.8f; // Varsayılan olarak %80 yaptık!

    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (joystick == null) return;

        float yatay = joystick.InputDirection.x;
        float dikey = joystick.InputDirection.y;

        Vector3 hareketYonu = new Vector3(yatay, 0, dikey).normalized;
        float girdiMiktari = new Vector2(yatay, dikey).magnitude;

        if (girdiMiktari > 0.1f)
        {
            Quaternion hedefDonus = Quaternion.LookRotation(hareketYonu);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedefDonus, Time.deltaTime * donmeHizi);

            // 🎯 Burayı güncelledik: Yeni belirlediğimiz eşiğe göre yürü/koş kararını veriyor
            float mevcutHiz = girdiMiktari > kosuEsigi ? kosuHizi : yuruHizi;

            Vector3 hareket = hareketYonu * mevcutHiz;

            if (!controller.isGrounded)
            {
                hareket.y -= yercekimi;
            }

            controller.Move(hareket * Time.deltaTime);

            // ---- 🧠 ANİMATÖRÜ TETİKLEYEN KISIM ----
            animator.SetFloat("Hiz", mevcutHiz);
        }
        else
        {
            animator.SetFloat("Hiz", 0f);

            if (!controller.isGrounded)
            {
                controller.Move(new Vector3(0, -yercekimi * Time.deltaTime, 0));
            }
        }
    }
}
