using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupObjects : MonoBehaviour
{
    private PlayerManager playerManager;
    private Rigidbody objectRigidbody;
    public bool isDraggingObject=false;
    [SerializeField] private Transform carryPosition;
    // [SerializeField] private float moveForce = 10f;

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
            if (objectRigidbody == null) {
                return;
            }
            objectRigidbody.useGravity = false;
            objectRigidbody.freezeRotation = true;
            isDraggingObject = true;
        }
    }

    [SerializeField] private float distance;
    [SerializeField] private float maxDistance = 1.5f;
    [SerializeField] private float dragSpeed = 10f;
    private void CarryObject() {
        distance = Vector3.Distance(objectRigidbody.transform.position, carryPosition.position);
        if (distance > maxDistance) {
            DropObject();
            return;
        }
        Vector3 directionToPoint = carryPosition.position - objectRigidbody.transform.position;
        float distanceToPoint = directionToPoint.magnitude;

        objectRigidbody.velocity = directionToPoint * dragSpeed * distanceToPoint; 
    }

    public void DropObject() {
        if (objectRigidbody != null) {
            objectRigidbody.useGravity = true;
            objectRigidbody.freezeRotation = false;
        }
        isDraggingObject = false;
        objectRigidbody = null;
        distance = 0f;
    }
}
