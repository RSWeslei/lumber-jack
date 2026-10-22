using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class WieldManager : MonoBehaviour
{
    [SerializeField] private Hand leftHand;
    [SerializeField] private Hand rightHand;
    
    public void WieldRight()
    {
        // animationManager.PlayRightHandAnimation("Use");
        Animator animator = rightHand.GetWieldedAnimator();
        // MUDAR PARA ANIMAR COM O PERSONAGEM, NAO COM A M�O
        if (animator != null) 
        {
            animator.Play("Use");
        }
    }

    void OnEnable()
    {
        InputManager.Instance.OnPlayerAttack += WieldRight;
    }
    void OnDisable()
    {
        InputManager.Instance.OnPlayerAttack -= WieldRight;
    }
}
