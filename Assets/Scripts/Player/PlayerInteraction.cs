using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;

public class PlayerInteraction : MonoBehaviour
{
    private LayerMask _intectableLayer;
    [SerializeField] private float _raycastDistance = 2f;

    private void Start() {
        _intectableLayer = LayerMask.GetMask("Interactable");
    }
    private RaycastHit oldHit;

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, _raycastDistance, _intectableLayer))
        {
            if (oldHit.collider) {
                return;
            }
            hit.collider.GetComponent<IDisplayable>()?.Display();
            oldHit = hit;
        } else {
            if (oldHit.transform != null)
             {
                oldHit.transform.GetComponent<IDisplayable>()?.Hide();
                UIDisplays.Instance.HideKeyText();
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
