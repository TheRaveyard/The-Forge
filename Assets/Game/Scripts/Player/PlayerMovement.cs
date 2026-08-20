using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    [Header("Движение")]
    public float playerSpeed = 4f;
    public float playerJumpHeight = 1f;
    public float playerGravity = -20f;
    public float acceleration = 18f;

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
        if (!gameplayInputEnabled)
        {
            return;
        }
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
        
    }
    private void UpdateLook(Vector2 input)
    {
        
    }
}
