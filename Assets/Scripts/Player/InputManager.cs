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

        [Header("UI")]
        private GUIInputs guiInputs;
        private float hotbarNumberInput;
        public Vector2 hotbarScrollInput;
        [SerializeField] private Hotbar hotbar;

        private void Awake() {
            pickupObjects = GetComponent<PickupObjects>();
            playerInteraction = GetComponentInChildren<PlayerInteraction>();
        }

        private void OnEnable() {
            HandlePlayerInputs();
            HandleGUIInputs();
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

        private void HandleGUIInputs() {
            guiInputs = new GUIInputs();
            if (guiInputs != null) 
            {
                guiInputs.PlayerUI.HotbarMouseScroll.performed += i => {
                    hotbarScrollInput = i.ReadValue<Vector2>().normalized;
                    hotbar.SelectSlotScroll(hotbarScrollInput.y);
                };
                guiInputs.PlayerUI.HotbarNumbers.performed += i => {
                    hotbarNumberInput = i.ReadValue<float>();
                    hotbar.SelectSlot((int)(hotbarNumberInput-1));
                };
            }
            guiInputs.Enable();
        }
    }
}
