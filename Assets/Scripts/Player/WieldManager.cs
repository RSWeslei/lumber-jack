using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WieldManager : MonoBehaviour
{
    [SerializeField] private Hand leftHand;
    [SerializeField] private Hand rightHand;

    [SerializeField] private AnimationManager animationManager;
    
    public void WieldRight() {
        // animationManager.PlayRightHandAnimation("Use");
        Animator animator = rightHand.GetWieldedAnimator();
        // MUDAR PARA ANIMAR COM O PERSONAGEM, NAO COM A MÃO
        if (animator != null) {
            animator.Play("Use");
        }
    }
}
