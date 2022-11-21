using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class InputManager : MonoBehaviour
    {   
        [Header("Player")]
        public Vector2 movement_input;
        public bool grab_input;
        public bool interact_input;
        public bool jump_input;
        public PlayerInputs playerInputs;
        private PlayerInteraction _playerInteraction;
        private PickupObjects _pickupObjects;

        [Header("Player Wield")]
        public WieldManager wieldManager;

        [Header("UI")]
        public static InputManager Instance;
        [SerializeField] private Hotbar hotbar;
        private GUIInputs _guiInputs;
        private float _hotbarNumberInput;
        private Vector2 _hotbarScrollInput;

        private void Awake() 
        {
            _pickupObjects = GetComponent<PickupObjects>();
            _playerInteraction = GetComponentInChildren<PlayerInteraction>();
            Instance = this;
        }

        private void OnEnable() 
        {
            HandlePlayerInputs();
            HandleGUIInputs();
            HandleWieldInputs();
            HandlePlayerMovement();
        }

        private void HandleWieldInputs() 
        {
            if (playerInputs != null) 
            {
                playerInputs.PlayerActions.LeftMouse.performed += ctx => {
                    wieldManager.WieldRight();
                };
            }
        }

        private void HandlePlayerMovement()
        {
            if (playerInputs != null) 
            {
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

        private void HandlePlayerInputs()
        {
            playerInputs = new PlayerInputs();
            if (playerInputs != null) 
            {
                playerInputs.PlayerActions.Grab.performed += i => {
                    grab_input = true;
                    _pickupObjects.enabled = true;
                };
                playerInputs.PlayerActions.Grab.canceled += i => {
                    grab_input = false;
                    _pickupObjects?.Drop();
                };
                playerInputs.PlayerActions.Interact.performed += i => {
                    interact_input = true;
                    _playerInteraction.Interact();
                };
                playerInputs.PlayerActions.Interact.canceled += i => {
                    interact_input = false;
                };
            }
            playerInputs.Enable();
        }

        private void HandleGUIInputs() 
        {
            _guiInputs = new GUIInputs();
            if (_guiInputs != null) 
            {
                _guiInputs.PlayerUI.HotbarMouseScroll.performed += i => {
                    _hotbarScrollInput = i.ReadValue<Vector2>().normalized;
                    hotbar.SelectSlotScroll(_hotbarScrollInput.y);
                };
                _guiInputs.PlayerUI.HotbarNumbers.performed += i => {
                    _hotbarNumberInput = i.ReadValue<float>();
                    hotbar.SelectSlot((int)(_hotbarNumberInput-1));
                };
            }
            _guiInputs.Enable();
        }
    }
}
