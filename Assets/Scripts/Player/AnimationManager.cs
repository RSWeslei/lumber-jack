using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private Animator rightHandAnimator;

    public void PlayRightHandAnimation(string animationName) {
        rightHandAnimator.Play(animationName);
    }
}
