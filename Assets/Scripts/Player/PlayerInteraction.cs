using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;
using System;

public class PlayerInteraction : MonoBehaviour
{
    private LayerMask _intectableLayer;
    private RaycastHit oldHit;
    [SerializeField] private float _raycastDistance = 2f;

    // public event EventHandler OnPlayerInteract;
    // public event EventHandler OnPlayerStopInteract;

    private void Start() {
        _intectableLayer = LayerMask.GetMask("Interactable");
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, _raycastDistance, _intectableLayer))
        {
            if (oldHit.collider) {
                return;
            }
            // OnPlayerInteract?.Invoke(this, EventArgs.Empty);
            hit.collider.GetComponent<IDisplayable>()?.Display();
            oldHit = hit;
        } 
        else {
            if (oldHit.collider)
            {
                // OnPlayerStopInteract?.Invoke(this, EventArgs.Empty);
                oldHit.collider.GetComponent<IDisplayable>()?.Hide();
                UIDisplays.Instance.HideKeyInfo();
                oldHit = new RaycastHit();
            }
        }
    }

    public void Interact () 
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, _raycastDistance, _intectableLayer))
        {
            hit.collider.GetComponent<IInteractable>()?.Interact();
        }
    }
}
