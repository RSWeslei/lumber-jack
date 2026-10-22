using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private Animator _rightHandAnimator;

    public void PlayRightHandAnimation(string animationName) 
    {
        _rightHandAnimator.Play(animationName);
    }
}
