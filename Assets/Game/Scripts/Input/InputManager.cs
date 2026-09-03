using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static InputActions;


public class InputManager : MonoBehaviour, IPlayerActions
{


    private InputActions _inputActions;


    public event Action<Vector2> OnMoveInput;
    public event Action<bool> OnSprintInput;
    public event Action<bool> OnJumpInput;
    public event Action OnClimbInput;
    public event Action OnCancelClimbInput;
    public event Action OnCrouchInput;
    public event Action OnGlideInput;
    public event Action OnCancelGlide;
    public event Action OnPunchInput;


    public event Action OnChangePOV;


    private void Awake()
    {
        _inputActions = new InputActions();

        _inputActions.Enable();
        _inputActions.Player.SetCallbacks(this);
    }


    private void OnDisable()
    {
        _inputActions.Disable();
        _inputActions.Player.SetCallbacks(null);
    }


    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnMoveInput?.Invoke(context.ReadValue<Vector2>());
        }
        else
        {
            OnMoveInput?.Invoke(Vector2.zero);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnSprintInput?.Invoke(true);
        }
        else
        {
            OnSprintInput?.Invoke(false);
        }
    }


    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnJumpInput?.Invoke(true);
        }
    }

    public void OnClimb(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnClimbInput?.Invoke();
        }
    }

    public void OnCancelClimb(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnCancelClimbInput?.Invoke();
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Handle by CinemachineInputAxis 
    }

    void IPlayerActions.OnChangePOV(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnChangePOV?.Invoke();
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnCrouchInput?.Invoke();
        }
    }

    public void OnGlide(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnGlideInput?.Invoke();
        }
    }

    void IPlayerActions.OnCancelGlide(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnCancelGlide?.Invoke();
        }
    }

    public void OnPunch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnPunchInput?.Invoke();
        }
    }
}
