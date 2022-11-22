using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupObjects : MonoBehaviour
{
    [Header("Pickup Settings")]
    public bool isDraggingObject = false;
    [SerializeField] private Transform _carryPosition;
    private Rigidbody _objectRigidbody;
    private Transform _objectTransform;

    [Header("Raycast")]
    [SerializeField] private float _raycastDistance = 2f;
    [SerializeField] private LayerMask _layerMask;
    private Transform _cameraTransform;

    [Header("ObjectFollow")]
    [SerializeField] private float _maxDistance = 1.5f;
    [SerializeField] private float _currentDistance = 0f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;
    private Quaternion _originalRotation;

    private void Awake() 
    {
        _cameraTransform = GetComponentInChildren<Camera>().transform;
        this.enabled = false;
    }

    private void Update() 
    {
        if (isDraggingObject) 
        {
            CarryObject();
        }
    }

    private void OnEnable() 
    {
        PickUp();
    }

    public void PickUp() 
    { 
        RaycastHit hit;
        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out hit, _raycastDistance, _layerMask)) 
        {
            _objectRigidbody = hit.transform.GetComponent<Rigidbody>();
            if (_objectRigidbody == null) {
                return;
            }
            _objectTransform = hit.transform;
            _originalRotation = _objectTransform.rotation;
            _objectRigidbody.useGravity = false;
            _objectRigidbody.freezeRotation = true;
            isDraggingObject = true;
        }
    }
    private void CarryObject() 
    {
        _currentDistance = Vector3.Distance(_objectRigidbody.transform.position, _carryPosition.position);
        if (_currentDistance > _maxDistance) 
        {
            Drop();
            return;
        }
    }

    public void Drop() 
    {
        if (_objectRigidbody != null) 
        {
            _objectRigidbody.useGravity = true;
            _objectRigidbody.freezeRotation = false;
            _objectRigidbody.transform.SetParent(null);
        }
        isDraggingObject = false;
        _objectRigidbody = null;
        _currentDistance = 0f;
        this.enabled = false;
    }
}
