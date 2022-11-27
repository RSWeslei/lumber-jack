using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;
using System;

public class PlayerInteraction : MonoBehaviour
{
    private LayerMask intectableLayer;
    private RaycastHit oldHit;
    [SerializeField] private float raycastDistance = 2f;

    private void Awake() {
        intectableLayer = LayerMask.GetMask("Interactable");
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
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, intectableLayer))
        {
            if (oldHit.collider) {
                return;
            }
            hit.collider.GetComponent<IDisplayable>()?.Display();
            oldHit = hit;
        } 
        else {
            if (oldHit.collider)
            {
                oldHit.collider.GetComponent<IDisplayable>()?.Hide();
                UIDisplays.Instance.HideKeyInfo();
                oldHit = new RaycastHit();
            }
        }
    }

    public void Interact () 
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, intectableLayer))
        {
            hit.collider.GetComponent<IInteractable>()?.Interact();
        }
    }
}
