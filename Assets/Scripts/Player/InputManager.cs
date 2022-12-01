using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class InputManager : MonoBehaviour
{   
    [Header("Player")]
    public Vector2 movement_input;
    public Vector2 look_input;
    public bool grab_input;
    public bool interact_input;
    public bool jump_input;
    public PlayerInputs playerInputs;

    [Header("UI")]
    public static InputManager Instance;
    private GUIInputs _guiInputs;
    private float _hotbarNumberInput;
    private float _hotbarScrollInput;

    [Header("Keys")]
    public string interactKey;
    public string grabKey;
    public string jumpKey;
    public string sprintKey;

    public event Action<int> OnHotbarNumberInput;
    public event Action<float> OnHotbarScrollInput;
    public event Action OnPickupObject;
    public event Action OnDropObject;
    public event Action OnPlayerInteract;
    public event Action OnPlayerAttack;

    private void Awake() 
    {
        if (Instance == null){
            Instance = this;
        }
        else{
            Destroy(gameObject);
        }
    }

    private void OnEnable() 
    {
        HandlePlayerInputs();
        HandleGUIInputs();
        HandleWieldInputs();
        HandlePlayerMovement();
        Keys();
    }

    private void Keys()
    {
        interactKey = playerInputs.PlayerActions.Interact.bindings[0].effectivePath;
        grabKey = playerInputs.PlayerActions.Grab.bindings[0].effectivePath;
        jumpKey = playerInputs.PlayerMovement.Jump.bindings[0].effectivePath;
        sprintKey = playerInputs.PlayerMovement.Sprint.bindings[0].effectivePath;
        Debug.Log(interactKey);
    }

    private void HandleWieldInputs() 
    {
        if (playerInputs != null) 
        {
            playerInputs.PlayerActions.LeftMouse.performed += ctx => {
                OnPlayerAttack?.Invoke();
            };
        }
    }

    private void HandlePlayerMovement()
    {
        if (playerInputs != null) 
        {
            playerInputs.PlayerMovement.Move.performed += ctx => {
                movement_input = playerInputs.PlayerMovement.Move.ReadValue<Vector2>();
            };
            playerInputs.PlayerMovement.Jump.performed += ctx => {
                jump_input = true;
            };
            playerInputs.PlayerMovement.Jump.canceled += ctx => {
                jump_input = false;
            };
            playerInputs.PlayerMovement.Look.performed += ctx => {
                look_input = playerInputs.PlayerMovement.Look.ReadValue<Vector2>();
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
                OnPickupObject?.Invoke();
            };
            playerInputs.PlayerActions.Grab.canceled += i => {
                grab_input = false;
                OnDropObject?.Invoke();
            };
            playerInputs.PlayerActions.Interact.performed += i => {
                interact_input = true;
                OnPlayerInteract?.Invoke();
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
                _hotbarScrollInput = i.ReadValue<Vector2>().normalized.y;
                OnHotbarScrollInput?.Invoke(_hotbarScrollInput);
            };
            _guiInputs.PlayerUI.HotbarNumbers.performed += i => {
                _hotbarNumberInput = i.ReadValue<float>();
                OnHotbarNumberInput?.Invoke((int)_hotbarNumberInput-1);
            };
        }
        _guiInputs.Enable();
    }
}
