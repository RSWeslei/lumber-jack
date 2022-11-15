using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquippedItemSway : MonoBehaviour
{
    private float amount = 0.05f;
    private float maxAmount = 0.3f;
    private float smoothAmount = 3f;

    private Vector3 defaultPos;

    private void Start() {
        defaultPos = transform.localPosition;
    }

    private void Update() {
        float movementX = -Input.GetAxis("Mouse X") * amount;
        float movementY = -Input.GetAxis("Mouse Y") * amount;

        movementX = Mathf.Clamp(movementX, -maxAmount, maxAmount);
        movementY = Mathf.Clamp(movementY, -maxAmount, maxAmount);

        Vector3 finalPosition = new Vector3(movementX, movementY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition + defaultPos, Time.deltaTime * smoothAmount);
    }
}
