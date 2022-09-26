using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupObjects : MonoBehaviour
{
    private PlayerManager playerManager;
    private Rigidbody objectRigidbody;
    public bool isDraggingObject=false;
    [SerializeField] private Transform carryPosition;
    [SerializeField] private float moveForce = 10f;

    // Raycast
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private LayerMask layerMask;
    private Transform cameraTransform;

    private void Awake() {
        playerManager = GetComponent<PlayerManager>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    private void Update() {
        if (isDraggingObject) {
            CarryObject();
        }
    }

    public void TryMoveObject() { 
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, raycastDistance, layerMask)) {
            objectRigidbody = hit.transform.GetComponent<Rigidbody>();
            Vector3 hitPoint = hit.point;
            isDraggingObject = true;
        }
    }

    private void CarryObject() {
        objectRigidbody.AddForce((carryPosition.position - objectRigidbody.position) * moveForce);
    }

    public void DropObject() {
        isDraggingObject = false;
    }
}
