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
            objectRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            isDraggingObject = true;
        }
    }

    [Header("ObjectFollow")]
    [SerializeField] private float minSpeed = 0;
    [SerializeField] private float maxSpeed = 300f;
    [SerializeField] private float maxDistance = 10f;
    private float currentSpeed = 0f;
    private float currentDist = 0f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;
    Quaternion lookRot;

    private void CarryObject() {
        currentDist = Vector3.Distance(carryPosition.position, objectRigidbody.position);
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, currentDist / maxDistance);
        objectRigidbody.velocity = (carryPosition.position - objectRigidbody.position) * currentSpeed * Time.deltaTime;
        lookRot = Quaternion.LookRotation(carryPosition.position - objectRigidbody.position);
        objectRigidbody.MoveRotation(Quaternion.RotateTowards(objectRigidbody.rotation, lookRot, rotationSpeed * Time.deltaTime));
    }

    public void DropObject() {
        isDraggingObject = false;
        objectRigidbody.constraints = RigidbodyConstraints.None;
        objectRigidbody = null;
        currentDist = 0;
    }
}
