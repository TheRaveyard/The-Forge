using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] public float playerSpeed = 4f;
    
    public float playerJumpHeight = 1f;
    public float playerGravity = -20f;
    public float acceleration = 18f;

    [Header("Зависимости")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private InputActionAsset inputAction;
    [SerializeField] private Transform cameraTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if(cameraTransform == null && Camera.main != null);
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
