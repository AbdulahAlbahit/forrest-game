using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SADECE TEST İÇİN - Efe'nin sistemi gelince silinecek!
/// Play modunda T tuşuna bas = Kameranın baktığı yöndeki ağaca vur
/// </summary>
public class TreeTestHelper : MonoBehaviour
{
    [Header("--- TEST AYARLARI ---")]
    [Tooltip("Her T tuşuna basınca kaç hasar versin?")]
    public int damagePerPress = 1;

    [Tooltip("Kameradan ne kadar uzağa Raycast atsın? (metre)")]
    public float raycastDistance = 20f;

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        Debug.Log("<color=cyan>[TEST] T tuşuna bas → Kameranın önündeki ağaca vur!</color>");
    }

    void Update()
    {
        // Yeni Input System ile T tuşu kontrolü
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Ray ray = mainCam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            {
                Debug.Log($"<color=yellow>[TEST] Raycast çarptı: {hit.collider.name} | Nokta: {hit.point}</color>");
                TreeManager.Instance.HitTreeAt(hit.point, damagePerPress);
            }
            else
            {
                Debug.Log("<color=red>[TEST] Raycast hiçbir şeye çarpmadı. Kamerayı bir ağaca doğru çevir!</color>");
            }
        }
    }
}
