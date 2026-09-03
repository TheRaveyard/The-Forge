using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    [Header("Движение")]
    public float playerSpeed = 4f;
    public float playerJumpHeight = 1f;
    public float playerGravity = -20f;
    public float acceleration = 18f;
    private float verticalVelocity;
    private Vector3 horizontalVelocity;

    [Header("Зависимости")]
    private CharacterController characterController;
    public InputActionAsset inputActions;
    private Transform cameraTransform;

    [Header("Ввод")]
    private InputAction jumpAction;
    private InputAction moveAction;
    private InputAction lookAction;
    private bool initialized;
    private bool gameplayInputEnabled = true;

    [Header("Камера")]
    private float cameraPitch;

    [Header("Звуки")]
    private AudioSource footstepAudio;
    private float footstepTimer = 0f;
    private float footstepInterval = 0.3f;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if(cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        initialized = TryInitializeInput();
        
        if (!initialized)
        {
            enabled = false;
            return;
        }
        cameraPitch = NormalizePitch(cameraTransform.localEulerAngles.x);
    }

    // Update is called once per frame
    private void Update()
    {
        Vector2 movementInput = Vector2.zero;
        bool jumpRequested = false;
        if (!gameplayInputEnabled)
        {
            return;
        }
        else
        {
            movementInput = moveAction.ReadValue<Vector2>();
            jumpRequested = jumpAction.WasPressedThisFrame();
        }
        Debug.log(movementInput, jumpRequested);
        ApplyMovement(movementInput, jumpRequested);
    }

    private bool TryInitializeInput()
    {
        // if (characterController == null || cameraTransform == null || inputActions == null)
        // {
        //     return false;
        // }
        InputActionMap playerMap = inputActions.FindActionMap("Player", false);
        moveAction = playerMap?.FindAction("Move", false);
        lookAction = playerMap?.FindAction("Look", false);
        jumpAction = playerMap?.FindAction("Jump", false);
        if (moveAction != null && lookAction != null && jumpAction != null)
        {
            return true;
        }
        Debug.LogError("PlayerMovement: в карте Player отсутствуют обязательные действия ввода.", this);
        return false;
    }
    private static float NormalizePitch(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
    private void ApplyMovement(Vector2 input, bool jumpRequested)
    {
        Vector3 inputDirection = new Vector3(input.x, 0f, input.y);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);
        Vector3 targetVelocity =
            transform.TransformDirection(inputDirection) * playerSpeed;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.deltaTime);
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        if (jumpRequested &&
            characterController.isGrounded &&
            playerJumpHeight > 0f)
        {
            verticalVelocity = Mathf.Sqrt(
                playerJumpHeight * -2f * playerGravity);
        }
        verticalVelocity += playerGravity * Time.deltaTime;
        Vector3 finalVelocity =
            horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(finalVelocity * Time.deltaTime);
        CheckForMovement();
    }
    private void UpdateLook(Vector2 input)
    {
        
    }
    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
    }
    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();

    }
    private void CheckForMovement()
    {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f)
        {
            return;
        }
        footstepAudio.PlayOneShot(footstepAudio.clip);
    }
}
