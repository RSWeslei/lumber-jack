using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class EquippedItemSway : MonoBehaviour
{
    private float _amount = 0.04f;
    private float _maxAmount = 0.2f;
    private float _smoothAmount = 3f;
    private Vector3 _defaultPos;

    private void Start()
     {
        _defaultPos = transform.localPosition;
    }

    private void Update() 
    {
        float movementX = -InputManager.Instance.movement_input.x * _amount;
        float movementY = -InputManager.Instance.movement_input.y * _amount;

        movementX = Mathf.Clamp(movementX, -_maxAmount, _maxAmount);
        movementY = Mathf.Clamp(movementY, -_maxAmount, _maxAmount);

        Vector3 finalPosition = new Vector3(movementX, movementY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition + _defaultPos, Time.deltaTime * _smoothAmount);
    }
}
