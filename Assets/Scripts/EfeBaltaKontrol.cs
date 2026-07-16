using UnityEngine;

public class EfeBaltaKontrol : MonoBehaviour
{
    [Header("Balta Saldırısı")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject balta;
    [Min(0.1f)] [SerializeField] private float saldiriBeklemeSuresi = 0.65f;

    private static readonly int SaldirParametresi = Animator.StringToHash("Saldir");
    private float sonrakiSaldiriZamani;

    public void BaltaSalla()
    {
        if (animator == null || Time.time < sonrakiSaldiriZamani)
        {
            return;
        }

        sonrakiSaldiriZamani = Time.time + saldiriBeklemeSuresi;
        animator.ResetTrigger(SaldirParametresi);
        animator.SetTrigger(SaldirParametresi);
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

        if (balta != null)
        {
            balta.SetActive(true);
        }
    }
}
