using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableButton : MonoBehaviour, IInteractable
{
    public virtual void Interact()
    {
        // Do animation
        // Do sound
    }
}
