using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


[DisallowMultipleComponent]
public sealed class ObjectInteraction : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private InputActionAsset inputActions;


    [Header("Подбор")]
    [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;


    [Header("Физическое удержание")]
    [SerializeField, Min(0.1f)] private float holdDistance = 1.5f;
    [SerializeField, Min(0.1f)] private float breakDistance = 3f;
    [SerializeField, Min(0.1f)] private float positionSpring = 60f;
    [SerializeField, Min(0f)] private float velocityDamping = 15f;
    [SerializeField, Min(0.1f)] private float maxAcceleration = 80f;
    [SerializeField, Min(0f)] private float throwImpulse = 6f;


    private InputAction pickupAction;
    private InputAction throwAction;
    private Collider[] playerColliders;
    private PickupItem heldItem;
    private bool initialized;
    private readonly RaycastHit[] raycastHits = new RaycastHit[16];


    public PickupItem HeldItem => heldItem;


    private void Awake()
    {
        if (interactionCamera == null) interactionCamera = Camera.main;
        if (inputActions == null && TryGetComponent(out PlayerMovement movement))
            inputActions = movement.inputActions;
        InputActionMap map = inputActions.FindActionMap("Player", false);
        // Копии сохраняют привязки проекта, но имеют независимый жизненный цикл.
        pickupAction = map?.FindAction("Interact", false)?.Clone();
        throwAction = map?.FindAction("Throw", false)?.Clone();
        if (interactionCamera == null || pickupAction == null || throwAction == null)
        {
            Debug.LogError("ObjectInteraction: нужны камера и действия Player/Interact, Player/Throw.", this);
            enabled = false;
            return;
        }
        playerColliders = GetComponentsInChildren<Collider>();
        initialized = true;
    }


    private void OnEnable()
    {
        if (!initialized) return;
        pickupAction.Enable();
        throwAction.Enable();
    }


    private void Update()
    {
        if (!Application.isFocused || Time.timeScale <= 0f || interactionCamera == null) return;
        if (throwAction.WasPressedThisFrame())
        {
            Release(GetAimRay().direction * throwImpulse);
            return;
        }
        if (heldItem == null && pickupAction.WasPressedThisFrame()) TryPickUp();
    }


    private Ray GetAimRay()
    {
        if (Cursor.lockState == CursorLockMode.Locked || Mouse.current == null)
            return interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return interactionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
    }


    private void TryPickUp()
    {
        if (Cursor.lockState != CursorLockMode.Locked && EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject()) return;
        Ray ray = GetAimRay();
        // Ближайшее препятствие блокирует подбор; маска должна включать стены и окружение.
        if (!TryGetWorldHit(ray, out RaycastHit hit)) return;
        Rigidbody body = hit.rigidbody;
        if (body == null || !body.TryGetComponent(out PickupItem item)) return;
        if (item.TryHold(this, playerColliders)) heldItem = item;
    }


    private bool TryGetWorldHit(Ray ray, out RaycastHit closest)
    {
        closest = default;
        RaycastHit[] hits = raycastHits;
        int count = Physics.RaycastNonAlloc(ray, hits, pickupDistance, interactionLayers,
            QueryTriggerInteraction.Ignore);
        // При переполнении нельзя доверять неполному набору: в нём может отсутствовать стена.
        if (count == hits.Length)
        {
            hits = Physics.RaycastAll(ray, pickupDistance, interactionLayers, QueryTriggerInteraction.Ignore);
            count = hits.Length;
        }
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            closest = hit;
        }
        return nearest < float.PositiveInfinity;
    }


    private void FixedUpdate()
    {
        if (heldItem == null) return;
        if (interactionCamera == null || !heldItem.isActiveAndEnabled || heldItem.Body == null ||
            heldItem.Body.isKinematic)
        {
            Release(Vector3.zero);
            return;
        }
        Ray ray = GetAimRay();
        Vector3 target = ray.GetPoint(holdDistance);
        Rigidbody body = heldItem.Body;
        Vector3 error = target - body.worldCenterOfMass;
        if (error.sqrMagnitude > breakDistance * breakDistance)
        {
            Release(Vector3.zero);
            return;
        }
        // Пружина двигает динамическое тело силами, сохраняя столкновения с окружением.
        Vector3 acceleration = error * positionSpring - body.linearVelocity * velocityDamping;
        body.AddForce(Vector3.ClampMagnitude(acceleration, maxAcceleration), ForceMode.Acceleration);
    }


    private void Release(Vector3 impulse)
    {
        PickupItem item = heldItem;
        heldItem = null;
        if (item != null) item.Release(this, impulse);
    }


    internal void Forget(PickupItem item)
    {
        if (heldItem == item) heldItem = null;
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) Release(Vector3.zero);
    }


    private void OnDisable()
    {
        pickupAction?.Disable();
        throwAction?.Disable();
        Release(Vector3.zero);
    }


    private void OnDestroy()
    {
        pickupAction?.Dispose();
        throwAction?.Dispose();
    }
}
