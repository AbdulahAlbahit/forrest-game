using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EfeKiyafetAnimatorEsleyici : MonoBehaviour
{
    [SerializeField] private List<Animator> kiyafetAnimatorlari = new List<Animator>();

    private Animator karakterAnimatoru;
    private Animator sonAktifAnimator;
    private bool ilkKareEslendi;

    private void Awake()
    {
        karakterAnimatoru = GetComponent<Animator>();
        KiyafetAnimatorlariniYenile();
    }

    private void LateUpdate()
    {
        if (!Hazirla()) return;

        Animator buKareAktifAnimator = null;
        foreach (Animator kiyafetAnimatoru in kiyafetAnimatorlari)
        {
            if (kiyafetAnimatoru == null || !kiyafetAnimatoru.gameObject.activeInHierarchy) continue;
            bool yeniAktifKiyafet = kiyafetAnimatoru != sonAktifAnimator;
            AnimatoruEsle(kiyafetAnimatoru, yeniAktifKiyafet);
            buKareAktifAnimator = kiyafetAnimatoru;
        }

        sonAktifAnimator = buKareAktifAnimator;
        ilkKareEslendi = true;
    }

    private void AnimatoruEsle(Animator kiyafetAnimatoru, bool yeniAktifKiyafet)
    {
        if (kiyafetAnimatoru.runtimeAnimatorController != karakterAnimatoru.runtimeAnimatorController)
        {
            kiyafetAnimatoru.runtimeAnimatorController = karakterAnimatoru.runtimeAnimatorController;
            kiyafetAnimatoru.Rebind();
        }

        AnimatorControllerParameter[] parametreler = karakterAnimatoru.parameters;
        foreach (AnimatorControllerParameter parametre in parametreler)
        {
            switch (parametre.type)
            {
                case AnimatorControllerParameterType.Float:
                    kiyafetAnimatoru.SetFloat(parametre.nameHash, karakterAnimatoru.GetFloat(parametre.nameHash));
                    break;
                case AnimatorControllerParameterType.Bool:
                    kiyafetAnimatoru.SetBool(parametre.nameHash, karakterAnimatoru.GetBool(parametre.nameHash));
                    break;
                case AnimatorControllerParameterType.Int:
                    kiyafetAnimatoru.SetInteger(parametre.nameHash, karakterAnimatoru.GetInteger(parametre.nameHash));
                    break;
            }
        }

        int katmanSayisi = Mathf.Min(karakterAnimatoru.layerCount, kiyafetAnimatoru.layerCount);
        for (int katman = 0; katman < katmanSayisi; katman++)
        {
            kiyafetAnimatoru.SetLayerWeight(katman, karakterAnimatoru.GetLayerWeight(katman));
            AnimatorStateInfo kaynakDurum = karakterAnimatoru.GetCurrentAnimatorStateInfo(katman);
            AnimatorStateInfo hedefDurum = kiyafetAnimatoru.GetCurrentAnimatorStateInfo(katman);
            if (!ilkKareEslendi || yeniAktifKiyafet || kaynakDurum.fullPathHash != hedefDurum.fullPathHash)
            {
                kiyafetAnimatoru.Play(kaynakDurum.fullPathHash, katman, kaynakDurum.normalizedTime % 1f);
            }
        }

    }

    public void KiyafetAnimatorunuAyarla(Animator animator)
    {
        kiyafetAnimatorlari.Clear();
        if (animator != null) kiyafetAnimatorlari.Add(animator);
        ilkKareEslendi = false;
        Hazirla();
    }

    public void KiyafetAnimatorlariniAyarla(IEnumerable<Animator> animatorlar)
    {
        kiyafetAnimatorlari = animatorlar != null
            ? animatorlar.Where(item => item != null && item != karakterAnimatoru).Distinct().ToList()
            : new List<Animator>();
        ilkKareEslendi = false;
        Hazirla();
    }

    public void KiyafetAnimatorlariniYenile()
    {
        karakterAnimatoru = GetComponent<Animator>();
        KiyafetAnimatorlariniAyarla(GetComponentsInChildren<Animator>(true));
    }

    private bool Hazirla()
    {
        if (karakterAnimatoru == null) karakterAnimatoru = GetComponent<Animator>();
        kiyafetAnimatorlari.RemoveAll(item => item == null || item == karakterAnimatoru);
        foreach (Animator animator in kiyafetAnimatorlari)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        return karakterAnimatoru != null && kiyafetAnimatorlari.Count > 0;
    }
}
