using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public Vector2 movement_input;
        public bool jump_input;

        [Header("Player Wield")]
        public WieldManager wieldManager;

        [Header("UI")]
        private GUIInputs guiInputs;
        private float hotbarNumberInput;
        private Vector2 hotbarScrollInput;
        [SerializeField] private Hotbar hotbar;
        public static InputManager Instance;

        private void Awake() {
            pickupObjects = GetComponent<PickupObjects>();
            playerInteraction = GetComponentInChildren<PlayerInteraction>();
            Instance = this;
        }

        private void OnEnable() {
            HandlePlayerInputs();
            HandleGUIInputs();
            HandleWieldInputs();
            HandlePlayerMovement();
        }

        private void HandleWieldInputs() {
            if (playerInputs != null) {
                playerInputs.PlayerActions.LeftMouse.performed += ctx => {
                    wieldManager.WieldRight();
                };
            }
        }

        private void HandlePlayerMovement() {
            if (playerInputs != null) {
                playerInputs.PlayerMovement.Movement.performed += ctx => {
                    movement_input = playerInputs.PlayerMovement.Movement.ReadValue<Vector2>();
                };
                playerInputs.PlayerMovement.Jump.performed += ctx => {
                    jump_input = true;
                };
                playerInputs.PlayerMovement.Jump.canceled += ctx => {
                    jump_input = false;
                };
            }
        }

        private void HandlePlayerInputs() {
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
