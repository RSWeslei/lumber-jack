using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupObjects : MonoBehaviour
{
    [Header("Pickup Settings")]
    private Rigidbody objectRigidbody;
    private Transform objectTransform;
    public bool isDraggingObject = false;
    [SerializeField] private Transform carryPosition;

    [Header("Raycast")]
    [SerializeField] private float raycastDistance = 2f;
    [SerializeField] private LayerMask layerMask;
    private Transform cameraTransform;

    [Header("ObjectFollow")]
    [SerializeField] private float maxDistance = 1.5f;
    // [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float currentDistance = 0f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;
    private Quaternion originalRotation;

    private void Awake() {
        cameraTransform = GetComponentInChildren<Camera>().transform;
        this.enabled = false;
    }

    private void Update() {
        if (isDraggingObject) {
            CarryObject();
        }
    }

    private void OnEnable() {
        PickUp();
    }

    public void PickUp() { 
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, raycastDistance, layerMask)) {
            objectRigidbody = hit.transform.GetComponent<Rigidbody>();
            if (objectRigidbody == null) {
                return;
            }
            objectTransform = hit.transform;
            originalRotation = objectTransform.rotation;
            objectRigidbody.useGravity = false;
            objectRigidbody.freezeRotation = true;
            isDraggingObject = true;
        }
    }
    private void CarryObject() {
        currentDistance = Vector3.Distance(objectRigidbody.transform.position, carryPosition.position);
        if (currentDistance > maxDistance) {
            Drop();
            return;
        }
    }

    public void Drop() {
        if (objectRigidbody != null) {
            objectRigidbody.useGravity = true;
            objectRigidbody.freezeRotation = false;
            objectRigidbody.transform.SetParent(null);
        }
        isDraggingObject = false;
        objectRigidbody = null;
        currentDistance = 0f;
        this.enabled = false;
    }
}
