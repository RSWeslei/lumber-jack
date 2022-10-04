using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;

public class PlayerInteraction : MonoBehaviour
{
    private LayerMask intectableLayer;
    [SerializeField] private float raycastDistance = 2f;

    private void Start() {
        intectableLayer = LayerMask.GetMask("Interactable");
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, intectableLayer)){
            UIManager.Instance.ShowInteractableUI();
        } else {
            UIManager.Instance.HideInteractableUI();
        }
    }

    public void Interact () {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, intectableLayer)){
            hit.collider.GetComponent<IInteractable>()?.Interact();
        }
    }
}
