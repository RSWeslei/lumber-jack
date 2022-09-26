using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private PlayerInputs playerInputs;
    private PickupObjects pickupObjects;
    public bool grab_input;
    public bool leftMouse_input;
    
    private void Awake() {
        pickupObjects = GetComponent<PickupObjects>();
    }

    private void OnEnable() {
        playerInputs = new PlayerInputs();
        if (playerInputs != null) {
            playerInputs.PlayerActions.Grab.performed += i => {
                grab_input = true;
                pickupObjects.TryMoveObject();
            };
            playerInputs.PlayerActions.Grab.canceled += i => {
                grab_input = false;
                pickupObjects.DropObject();
            };
        }
        playerInputs.Enable();
    }  
}
