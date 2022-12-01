using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;
using System;

public class PlayerInteraction : MonoBehaviour
{
    private LayerMask interactableLayer;
    private RaycastHit oldHit;
    [SerializeField] private float raycastDistance = 2f;

    private void Awake() {
        interactableLayer = LayerMask.GetMask("Interactable");
    }

    void OnEnable()
    {
        InputManager.Instance.OnPlayerInteract += Interact;
    }

    void OnDisable()
    {
        InputManager.Instance.OnPlayerInteract -= Interact;
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.CapsuleCast(transform.position, transform.position + Vector3.up, 0.1f, transform.forward, out hit, raycastDistance, interactableLayer)) {
            if (oldHit.collider) {
                return;
            }
            hit.collider.GetComponent<IDisplayable>()?.Display();
            oldHit = hit;
        } else {
            {
                UIDisplays.Instance.HideKeyInfo();
                oldHit = new RaycastHit();
            }
        }
    }

    public void Interact () 
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, interactableLayer))
        {
            hit.collider.GetComponent<IInteractable>()?.Interact();
        }
    }
}
