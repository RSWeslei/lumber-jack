using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Core
{
    /// <summary>
    /// Ponto central do input do jogador (a pé e hotbar). Valores contínuos (mover, olhar)
    /// são lidos na hora; ações pontuais viram eventos.
    /// </summary>
    // Roda antes dos outros scripts para o Instance e as ações já existirem no OnEnable deles
    [DefaultExecutionOrder(-100)]
    public class InputManager : MonoBehaviour
    {
        private GUIInputs _guiInputs;

        public static InputManager Instance { get; private set; }

        public PlayerInputs PlayerInputs { get; private set; }

        public Vector2 MoveInput => PlayerInputs.PlayerMovement.Move.ReadValue<Vector2>();
        public Vector2 LookInput => PlayerInputs.PlayerMovement.Look.ReadValue<Vector2>();
        public bool IsJumpHeld => PlayerInputs.PlayerMovement.Jump.IsPressed();
        public bool IsGrabHeld => PlayerInputs.PlayerActions.Grab.IsPressed();

        // Nome da tecla para mostrar na UI ("E", "Left Shift"...)
        public string InteractKeyLabel { get; private set; }
        public string GrabKeyLabel { get; private set; }
        public string JumpKeyLabel { get; private set; }
        public string SprintKeyLabel { get; private set; }

        public event Action AttackPressed;
        public event Action InteractPressed;
        public event Action GrabPressed;
        public event Action GrabReleased;
        /// <summary>Slot escolhido pelas teclas numéricas (começa em 0).</summary>
        public event Action<int> HotbarSlotSelected;
        /// <summary>+1 rolou para cima, -1 para baixo.</summary>
        public event Action<float> HotbarScrolled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Remove só o componente duplicado, não o objeto (que é o próprio player)
                Destroy(this);
                return;
            }

            Instance = this;
            PlayerInputs = new PlayerInputs();
            _guiInputs = new GUIInputs();

            InteractKeyLabel = GetKeyLabel(PlayerInputs.PlayerActions.Interact);
            GrabKeyLabel = GetKeyLabel(PlayerInputs.PlayerActions.Grab);
            JumpKeyLabel = GetKeyLabel(PlayerInputs.PlayerMovement.Jump);
            SprintKeyLabel = GetKeyLabel(PlayerInputs.PlayerMovement.Sprint);
        }

        private void OnEnable()
        {
            if (PlayerInputs == null) return;

            PlayerInputs.PlayerActions.LeftMouse.performed += HandleAttackPerformed;
            PlayerInputs.PlayerActions.Interact.performed += HandleInteractPerformed;
            PlayerInputs.PlayerActions.Grab.performed += HandleGrabPerformed;
            PlayerInputs.PlayerActions.Grab.canceled += HandleGrabCanceled;
            _guiInputs.PlayerUI.HotbarNumbers.performed += HandleHotbarNumberPerformed;
            _guiInputs.PlayerUI.HotbarMouseScroll.performed += HandleHotbarScrollPerformed;

            PlayerInputs.Enable();
            _guiInputs.Enable();
        }

        private void OnDisable()
        {
            if (PlayerInputs == null) return;

            PlayerInputs.PlayerActions.LeftMouse.performed -= HandleAttackPerformed;
            PlayerInputs.PlayerActions.Interact.performed -= HandleInteractPerformed;
            PlayerInputs.PlayerActions.Grab.performed -= HandleGrabPerformed;
            PlayerInputs.PlayerActions.Grab.canceled -= HandleGrabCanceled;
            _guiInputs.PlayerUI.HotbarNumbers.performed -= HandleHotbarNumberPerformed;
            _guiInputs.PlayerUI.HotbarMouseScroll.performed -= HandleHotbarScrollPerformed;

            PlayerInputs.Disable();
            _guiInputs.Disable();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            PlayerInputs?.Dispose();
            _guiInputs?.Dispose();
            // Instance não é zerado: o OnDisable dos outros ainda se desinscreve daqui ao sair do Play
        }

        /// <summary>Nome da tecla que seleciona o slot da hotbar (começa em 0); vazio se não houver.</summary>
        public string GetHotbarKeyLabel(int slotIndex)
        {
            // Cada binding de HotbarNumbers é um slot, na ordem
            InputAction action = _guiInputs.PlayerUI.HotbarNumbers;
            return slotIndex < action.bindings.Count ? GetKeyLabel(action, slotIndex) : string.Empty;
        }

        private void HandleAttackPerformed(InputAction.CallbackContext context) => AttackPressed?.Invoke();

        private void HandleInteractPerformed(InputAction.CallbackContext context) => InteractPressed?.Invoke();

        private void HandleGrabPerformed(InputAction.CallbackContext context) => GrabPressed?.Invoke();

        private void HandleGrabCanceled(InputAction.CallbackContext context) => GrabReleased?.Invoke();

        // As teclas 1-9 estão configuradas para devolver o próprio número
        private void HandleHotbarNumberPerformed(InputAction.CallbackContext context)
        {
            HotbarSlotSelected?.Invoke(Mathf.RoundToInt(context.ReadValue<float>()) - 1);
        }

        private void HandleHotbarScrollPerformed(InputAction.CallbackContext context)
        {
            float scroll = context.ReadValue<Vector2>().y;
            if (scroll != 0f) HotbarScrolled?.Invoke(Mathf.Sign(scroll));
        }

        private static string GetKeyLabel(InputAction action, int bindingIndex = 0)
        {
            if (bindingIndex >= action.bindings.Count) return string.Empty;
            return InputControlPath.ToHumanReadableString(action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }
}
