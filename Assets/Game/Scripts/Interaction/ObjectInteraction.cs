using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectInteraction : MonoBehaviour
{
    public GameObject gameObject1;
    public GameObject gameObject2;
    public GameObject gameObject3;
    private float distanceToObject = 5f;
    private Camera camera;
    private InputAction pickupAction;
    private InputAction dropAction;
    public PickupItem heldItem;
    private InputActionAsset inputActions;
    private bool initialized;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // inputActions = Movement.inputActions;
        InputActionMap map = inputActions.FindActionMap("Player", false);
        pickupAction = map.FindAction("Interact", false)?.Clone();
        dropAction = map.FindAction("Throw", false)?.Clone();
        initialized = true;
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        
    }

    void Release()
    {
        
    }

    void TryPickUp()
    {
        
    }

    void OnEnable()
    {
        
    }

    void Forget()
    {
        
    }
}
