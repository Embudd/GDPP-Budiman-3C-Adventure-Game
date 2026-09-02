using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{


    [SerializeField]
    private Transform _cameraTransform;
    [SerializeField]
    private CameraManager _cameraManager;
    [SerializeField]
    private Animator _animator;



    [SerializeField]
    private InputManager _input;
    [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField]
    private float _walkSpeed = 350f;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _walkSprintTransition;


    [SerializeField]
    private float _rotationSmoothTime = 0.1f;
    private float _rotationSmoothVelocity;


    [SerializeField]
    private float _jumpForce = 10000f;


    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _detectorRadius;
    [SerializeField]
    private LayerMask _groundLayer;


    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckerDistance;
    [SerializeField]
    private float _stepForce;


    [SerializeField]
    private float _climbSpeed;
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbCheckDistance;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private Vector3 _climbOffset;


    private Vector3 _movementDirection;
    private bool _isSprintPressed;
    private float _speed;
    private bool _isJump;
    private bool _isGrounded;


    private PlayerStance _playerStance;


    private void Awake()
    {
        _playerStance = PlayerStance.Stand;

        _speed = _walkSpeed;
    }


    private void Start()
    {
        _input.OnMoveInput += InputManager_OnMove;
        _input.OnSprintInput += InputManager_OnSprint;
        _input.OnJumpInput += InputManager_OnJump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimbInput += CancelClimb;
        _cameraManager.OnChangePerspective += ChangePerspective;
    }


    private void OnDestroy()
    {
        _input.OnMoveInput -= InputManager_OnMove;
        _input.OnSprintInput -= InputManager_OnSprint;
        _input.OnJumpInput -= InputManager_OnJump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimbInput -= CancelClimb;
        _cameraManager.OnChangePerspective -= ChangePerspective;
    }

    private void Update()
    {
        CheckStep();
        CheckIsGrounded();
        Move();
    }


    private void InputManager_OnMove(Vector2 axisDirection)
    {
        _movementDirection = new Vector3(axisDirection.x, 0, axisDirection.y);
    }


    #region Movement

    private void Move()
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;

        if (isPlayerStanding)
        {

            Vector3 velocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            _animator.SetFloat("Velocity", _movementDirection.magnitude * velocity.magnitude);
            _animator.SetFloat("VelocityX", velocity.magnitude * _movementDirection.x);
            _animator.SetFloat("VelocityZ", velocity.magnitude * _movementDirection.z);

            switch (_cameraManager.CameraState)
            {
                case CameraState.ThirdPerson:
                    if (_movementDirection.magnitude >= 0.1)
                    {
                        float rotationAngle = Mathf.Atan2(_movementDirection.x, _movementDirection.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        _rigidbody.AddForce(movementDirection * Time.deltaTime * _speed);
                    }
                    break;
                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                    Vector3 verticalDirection = _movementDirection.z * transform.forward;
                    Vector3 horizontalDirection = _movementDirection.x * transform.right;
                    movementDirection = verticalDirection + horizontalDirection;
                    _rigidbody.AddForce(movementDirection * Time.deltaTime * _speed);
                    break;
                default:
                    break;
            }
        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = _movementDirection.x * transform.right;
            Vector3 vertical = _movementDirection.z * transform.up;
            movementDirection = horizontal + vertical;
            _rigidbody.AddForce(movementDirection * Time.deltaTime * _climbSpeed);
        }
    }


    private void InputManager_OnSprint(bool isSprint)
    {
        _isSprintPressed = isSprint;
    }


    private void Sprint(bool isSprint)
    {
        if (isSprint)
        {
            if (_speed < _sprintSpeed)
            {
                _speed = _speed + _walkSprintTransition * Time.deltaTime;
            }
        }
        else
        {
            if (_speed > _walkSpeed)
            {
                _speed = _speed - _walkSprintTransition * Time.deltaTime;
            }
        }
    }

    #endregion


    #region Jump

    private void InputManager_OnJump(bool isJump)
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
        }
    }


    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
    }

    #endregion  


    # region StepSlope
    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position,
                                            transform.forward,
                                            _stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position +
                                                _upperStepOffset,
                                                transform.forward,
                                                _stepCheckerDistance);
        if (isHitLowerStep && !isHitUpperStep)
        {
            _rigidbody.AddForce(0, _stepForce, 0);
        }
    }

    #endregion


    #region Climb

    private void StartClimb()
    {
        bool isInFrontOfClimbingWall = Physics.Raycast(_climbDetector.position,
                                                    transform.forward,
                                                    out RaycastHit hit,
                                                    _climbCheckDistance,
                                                    _climbableLayer);

        bool isNotClimbing = _playerStance != PlayerStance.Climb;

        if (isInFrontOfClimbingWall && _isGrounded && isNotClimbing)
        {
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(70);
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;
            _playerStance = PlayerStance.Climb;
            _rigidbody.useGravity = false;
        }
    }


    private void CancelClimb()
    {
        if (_playerStance == PlayerStance.Climb)
        {
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(40);
            _playerStance = PlayerStance.Stand;
            _rigidbody.useGravity = true;
            transform.position -= transform.forward * 1f;
        }
    }

    #endregion


    #region Perspective

    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }

    #endregion
}
