using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WieldManager : MonoBehaviour
{
    [SerializeField] private Hand _leftHand;
    [SerializeField] private Hand _rightHand;
    [SerializeField] private AnimationManager _animationManager;
    
    public void WieldRight()
    {
        // _animationManager.PlayRightHandAnimation("Use");
        Animator animator = _rightHand.GetWieldedAnimator();
        // MUDAR PARA ANIMAR COM O PERSONAGEM, NAO COM A M�O
        if (animator != null) 
        {
            animator.Play("Use");
        }
    }
}
