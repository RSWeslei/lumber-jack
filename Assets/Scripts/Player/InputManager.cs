using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vehicle;

namespace Managers
{
    public class InputManager : MonoBehaviour
    {   
        [Header("Player")]
        private PlayerInteraction playerInteraction;
        public PlayerInputs playerInputs;
        private PickupObjects pickupObjects;
        public bool grab_input;
        public bool interact_input;

        private void Awake() {
            pickupObjects = GetComponent<PickupObjects>();
            playerInteraction = GetComponentInChildren<PlayerInteraction>();
        }

        private void OnEnable() {
            HandlePlayerInputs();
        }

        private void HandlePlayerInputs (){
            playerInputs = new PlayerInputs();
            if (playerInputs != null) {
                playerInputs.PlayerActions.Grab.performed += i => {
                    grab_input = true;
                    pickupObjects.enabled = true;
                };
                playerInputs.PlayerActions.Grab.canceled += i => {
                    grab_input = false;
                    pickupObjects?.Drop();
                };
                playerInputs.PlayerActions.Interact.performed += i => {
                    interact_input = true;
                    playerInteraction.Interact();
                };
                playerInputs.PlayerActions.Interact.canceled += i => {
                    interact_input = false;
                };
            }
            playerInputs.Enable();
        }
    }
}
