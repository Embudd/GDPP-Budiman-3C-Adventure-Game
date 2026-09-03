using System;
using System.Collections;
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
    private PlayerAudioManager _playerAudioManager;    
    [SerializeField]
    private CapsuleCollider _collider;


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


    [SerializeField]
    private float _crouchSpeed;


    [SerializeField]
    private float _glideSpeed;
    [SerializeField]
    private float _airDrag;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private float _minGlideRotationX;
    [SerializeField]
    private float _maxGlideRotationX;


    [SerializeField]
    private float _resetComboInterval;


    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private LayerMask _hitLayer;


    private Vector3 _movementDirection;
    private bool _isSprintPressed;
    private float _speed;
    private bool _isJump;
    private bool _isGrounded;
    private bool _isPunching;
    private int _combo = 0;
    private Coroutine _resetCombo;


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
        _input.OnCrouchInput += Crouch;
        _cameraManager.OnChangePerspective += ChangePerspective;
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlide += CancelGlide;
        _input.OnPunchInput += Punch;
    }


    private void OnDestroy()
    {
        _input.OnMoveInput -= InputManager_OnMove;
        _input.OnSprintInput -= InputManager_OnSprint;
        _input.OnJumpInput -= InputManager_OnJump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimbInput -= CancelClimb;
        _input.OnCrouchInput -= Crouch;
        _cameraManager.OnChangePerspective -= ChangePerspective;
        _input.OnGlideInput -= StartGlide;
        _input.OnCancelGlide -= CancelGlide;
        _input.OnPunchInput -= Punch;
    }

    private void Update()
    {
        CheckStep();
        CheckIsGrounded();
        Move();
        Glide();
    }


    #region Movement

    private void InputManager_OnMove(Vector2 axisDirection)
    {
        _movementDirection = new Vector3(axisDirection.x, 0, axisDirection.y);
    }


    private void Move()
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        bool isPlayerCrouching = _playerStance == PlayerStance.Crouch;
        bool isPlayerGliding = _playerStance == PlayerStance.Glide;

        if ((isPlayerStanding || isPlayerCrouching) && !_isPunching)
        {
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

            Vector3 velocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            _animator.SetFloat("Velocity", _movementDirection.magnitude * velocity.magnitude);
            _animator.SetFloat("VelocityX", velocity.magnitude * _movementDirection.x);
            _animator.SetFloat("VelocityZ", velocity.magnitude * _movementDirection.z);
        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = _movementDirection.x * transform.right;
            Vector3 vertical = _movementDirection.z * transform.up;
            movementDirection = horizontal + vertical;
            _rigidbody.AddForce(movementDirection * Time.deltaTime * _climbSpeed);

            Vector3 velocity = new Vector3(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.y, 0);
            _animator.SetFloat("ClimbVelocityX", velocity.magnitude * _movementDirection.x);
            _animator.SetFloat("ClimbVelocityY", velocity.magnitude * _movementDirection.z);
        }
        else if (isPlayerGliding)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * _movementDirection.z * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideRotationX, _maxGlideRotationX);
            rotationDegree.y += _glideRotationSpeed.y * _movementDirection.y * Time.deltaTime;
            rotationDegree.z += _glideRotationSpeed.z * _movementDirection.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);
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
            _rigidbody.AddForce(jumpDirection * _jumpForce * Time.fixedDeltaTime);

            _animator.SetTrigger("Jump");
        }
    }


    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);
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
            _rigidbody.AddForce(0, _stepForce * Time.fixedDeltaTime, 0);
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
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;
            _playerStance = PlayerStance.Climb;
            _rigidbody.useGravity = false;

            _collider.center = Vector3.up * 1.3f;

            _animator.SetBool("IsClimbing", true);

            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(70);
        }
    }


    private void CancelClimb()
    {
        if (_playerStance == PlayerStance.Climb)
        {

            _playerStance = PlayerStance.Stand;
            _rigidbody.useGravity = true;
            transform.position -= transform.forward * 1f;

            _collider.center = Vector3.up * 0.9f;

            _animator.SetBool("IsClimbing", false);
        }
    }

    #endregion


    #region Crouch

    private void Crouch()
    {
        if (_playerStance == PlayerStance.Stand)
        {
            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("IsCrouch", true);
            _speed = _crouchSpeed;

            _collider.height = 1.3f;
            _collider.center = Vector3.up * 0.66f;
        }
        else if (_playerStance == PlayerStance.Crouch)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsCrouch", false);
            _speed = _walkSpeed;

            _collider.height = 1.8f;
            _collider.center = Vector3.up * 0.9f;
        }
    }

    #endregion


    #region Perspective

    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }

    #endregion


    #region Gliding

    private void StartGlide()
    {
        if (_playerStance != PlayerStance.Glide && !_isGrounded)
        {            
            _playerStance = PlayerStance.Glide;
            _animator.SetBool("IsGliding", true);
            _playerAudioManager.PlayGlideSfx();

            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        }
    }


    private void CancelGlide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsGliding", false);
            _playerAudioManager.StopGlideSfx();

            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
        }
    }


    private void Glide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift + _airDrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            _rigidbody.AddForce(totalForce * Time.deltaTime);
        }
    }
    #endregion


    #region Punch

    private void Punch()
    {
        if (!_isPunching && _playerStance == PlayerStance.Stand)
        {
            _isPunching = true;
            _animator.SetBool("IsPunching", _isPunching);

            if (_combo < 3)
            {
                _combo += 1;
            }
            else
            {
                _combo = 1;
            }
            _animator.SetInteger("Combo", _combo);
            _animator.SetTrigger("Punch");            
        }
    }


    private void EndPunch()
    {
        _isPunching = false;
        _animator.SetBool("IsPunching", _isPunching);
        Debug.Log("EndPunch");

        if (_resetCombo != null)
        {
            StopCoroutine(_resetCombo);
        }
        _resetCombo = StartCoroutine(ResetCombo());
    }


    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(_resetComboInterval);
        _combo = 0;
    }


    private void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);
        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject != null)
            {
                Destroy(hitObjects[i].gameObject);
            }
        }
    }

    #endregion
}
