using UnityEngine;

public class KameraTakip : MonoBehaviour
{
    [Header("Takip Ayarlarý")]
    public Transform hedef;
    public float yumusaklik = 5f; // Kameranýn süzülme hýzý

    // Oyun baþladýðýndaki mesafeyi hafýzada tutacaðýz
    private Vector3 baslangicMesafesi;

    void Start()
    {
        if (hedef != null)
        {
            // Sen kamerayý editörde nereye koyduysan, o mesafeyi otomatik olarak kaydeder
            baslangicMesafesi = transform.position - hedef.position;
        }
    }

    void LateUpdate()
    {
        if (hedef == null) return;

        // Kamera sadece karakterin pozisyonunu takip eder, kendi dönüþ açýsýný (rotation) asla deðiþtirmez!
        Vector3 yeniPozisyon = hedef.position + baslangicMesafesi;
        transform.position = Vector3.Lerp(transform.position, yeniPozisyon, yumusaklik * Time.deltaTime);
    }
}