using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

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
    [SerializeField] private Transform _cameraTransform;

    [Header("ObjectFollow")]
    [SerializeField] private float _maxDistance = 1.5f;
    [SerializeField] private float _currentDistance = 0f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;
    private Quaternion _originalRotation;

    private void Update() 
    {
        if (isDraggingObject) 
        {
            CarryObject();
        }
    }

    private void OnEnable() 
    {
        InputManager.Instance.OnPickupObject += PickUp;
        InputManager.Instance.OnDropObject += Drop;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnPickupObject -= PickUp;
        InputManager.Instance.OnDropObject -= Drop;
    }

    public void PickUp() 
    { 
        Debug.Log("Pickup");
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
    }
}
