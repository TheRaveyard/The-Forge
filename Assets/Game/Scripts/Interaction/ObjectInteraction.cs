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
    [SerializeField, Min(0.1f)] private float pickupDistance = 50f;
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

    // Break Distance нельзя применять, пока предмет впервые
    // физически не приблизился к точке удержания.
    private bool breakDistanceArmed;

    private bool initialized;
    private readonly RaycastHit[] raycastHits = new RaycastHit[16];

    public PickupItem HeldItem => heldItem;

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main;

        if (inputActions == null &&
            TryGetComponent(out PlayerMovement movement))
        {
            inputActions = movement.inputActions;
        }

        InputActionMap map = inputActions != null
            ? inputActions.FindActionMap("Player", false)
            : null;

        // Копии сохраняют привязки проекта,
        // но имеют независимый жизненный цикл.
        pickupAction = map?.FindAction("Interact", false)?.Clone();
        throwAction = map?.FindAction("Throw", false)?.Clone();

        if (interactionCamera == null ||
            pickupAction == null ||
            throwAction == null)
        {
            Debug.LogError(
                "[Подбор] ObjectInteraction: нужны камера и действия " +
                "Player/Interact, Player/Throw.",
                this);

            enabled = false;
            return;
        }

        playerColliders = GetComponentsInChildren<Collider>();
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        pickupAction.Enable();
        throwAction.Enable();
    }

    private void Update()
    {
        if (!Application.isFocused ||
            Time.timeScale <= 0f ||
            interactionCamera == null)
        {
            return;
        }

        if (throwAction.WasPressedThisFrame())
        {
            if (heldItem != null)
            {
                Debug.Log(
                    $"[Подбор] Бросок '{heldItem.name}'.",
                    heldItem);
            }

            Release(
                GetAimRay().direction * throwImpulse,
                "нажата кнопка броска");

            return;
        }

        if (heldItem == null &&
            pickupAction.WasPressedThisFrame())
        {
            TryPickUp();
        }
    }

    private Ray GetAimRay()
    {
        if (Cursor.lockState == CursorLockMode.Locked ||
            Mouse.current == null)
        {
            return interactionCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
        }

        return interactionCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue());
    }

    private void TryPickUp()
    {
        if (heldItem != null)
        {
            Debug.LogWarning(
                $"[Подбор] Отказ: уже удерживается '{heldItem.name}'.",
                heldItem);

            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log(
                "[Подбор] Отказ: курсор находится над интерфейсом.",
                this);

            return;
        }

        Ray ray = GetAimRay();

        // Ближайшее препятствие блокирует подбор.
        // interactionLayers должна включать стены и окружение.
        if (!TryGetWorldHit(ray, out RaycastHit hit))
        {
            Debug.Log(
                $"[Подбор] Отказ: луч ничего не нашёл " +
                $"в пределах {pickupDistance:F1} м.",
                this);

            return;
        }

        Rigidbody body = hit.rigidbody;

        if (body == null)
        {
            Debug.Log(
                $"[Подбор] Отказ: объект '{hit.collider.name}' " +
                "не имеет Rigidbody.",
                hit.collider);

            return;
        }

        if (!body.TryGetComponent(out PickupItem item))
        {
            Debug.Log(
                $"[Подбор] Отказ: Rigidbody '{body.name}' " +
                "не имеет PickupItem.",
                body);

            return;
        }

        if (!item.TryHold(this, playerColliders))
        {
            Debug.LogWarning(
                $"[Подбор] Отказ: PickupItem.TryHold() " +
                $"вернул false для '{item.name}'.",
                item);

            return;
        }

        // TryHold успешно назначил владельца.
        // Только теперь сохраняем предмет в ObjectInteraction.
        heldItem = item;

        // ВАЖНО:
        // предмет может находиться далеко от hold point,
        // поэтому Break Distance пока не действует.
        breakDistanceArmed = false;

        Vector3 target = GetAimRay().GetPoint(holdDistance);
        float initialDistance = Vector3.Distance(
            body.worldCenterOfMass,
            target);

        Debug.Log(
            $"[Подбор] Успех: взят '{item.name}'. " +
            $"Расстояние до точки удержания: {initialDistance:F2} м. " +
            $"Break Distance: {breakDistance:F2} м. " +
            "Ожидаем физического приближения предмета.",
            item);
    }

    private bool TryGetWorldHit(
        Ray ray,
        out RaycastHit closest)
    {
        closest = default;

        RaycastHit[] hits = raycastHits;

        int count = Physics.RaycastNonAlloc(
            ray,
            hits,
            pickupDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore);

        // При переполнении нельзя доверять неполному набору:
        // в нём может отсутствовать ближайшая стена.
        if (count == hits.Length)
        {
            hits = Physics.RaycastAll(
                ray,
                pickupDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore);

            count = hits.Length;
        }

        float nearest = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider.transform.IsChildOf(transform) ||
                hit.distance >= nearest)
            {
                continue;
            }

            nearest = hit.distance;
            closest = hit;
        }

        return nearest < float.PositiveInfinity;
    }

    private void FixedUpdate()
    {
        if (heldItem == null)
            return;

        PickupItem item = heldItem;

        if (interactionCamera == null)
        {
            Release(
                Vector3.zero,
                "потеряна interactionCamera");

            return;
        }

        if (!item.isActiveAndEnabled)
        {
            Release(
                Vector3.zero,
                "PickupItem отключён");

            return;
        }

        Rigidbody body = item.Body;

        if (body == null)
        {
            Release(
                Vector3.zero,
                "у PickupItem отсутствует Rigidbody");

            return;
        }

        if (body.isKinematic)
        {
            Release(
                Vector3.zero,
                "Rigidbody стал kinematic");

            return;
        }

        Ray ray = GetAimRay();
        Vector3 target = ray.GetPoint(holdDistance);

        Vector3 error =
            target - body.worldCenterOfMass;

        float distanceSqr = error.sqrMagnitude;
        float breakDistanceSqr =
            breakDistance * breakDistance;

        /*
         * Сразу после подбора предмет может быть намного дальше
         * breakDistance, особенно при pickupDistance = 50.
         *
         * Поэтому сначала Rigidbody физически притягивается.
         * Break Distance включается только после первого входа
         * в допустимую область.
         */
        if (!breakDistanceArmed)
        {
            if (distanceSqr <= breakDistanceSqr)
            {
                breakDistanceArmed = true;

                Debug.Log(
                    $"[Подбор] '{item.name}' достиг области удержания. " +
                    $"Break Distance активирован ({breakDistance:F2} м).",
                    item);
            }
        }
        else if (distanceSqr > breakDistanceSqr)
        {
            float distance = Mathf.Sqrt(distanceSqr);

            Release(
                Vector3.zero,
                $"превышен Break Distance: " +
                $"{distance:F2} м > {breakDistance:F2} м");

            return;
        }

        // Пружина двигает динамическое тело силами,
        // сохраняя Rigidbody и столкновения с окружением.
        Vector3 acceleration =
            error * positionSpring -
            body.linearVelocity * velocityDamping;

        body.AddForce(
            Vector3.ClampMagnitude(
                acceleration,
                maxAcceleration),
            ForceMode.Acceleration);
    }

    private void Release(
        Vector3 impulse,
        string reason)
    {
        PickupItem item = heldItem;

        if (item == null)
            return;

        // Очищаем до вызова PickupItem.Release().
        // Его Release() вызывает Forget(this), поэтому такой
        // порядок не создаёт повторного освобождения.
        heldItem = null;
        breakDistanceArmed = false;

        Debug.Log(
            $"[Подбор] Отпускаем '{item.name}'. " +
            $"Причина: {reason}. " +
            $"Impulse: {impulse}.",
            item);

        item.Release(this, impulse);
    }

    internal void Forget(PickupItem item)
    {
        if (heldItem != item)
            return;

        Debug.Log(
            $"[Подбор] Ссылка на '{item.name}' очищена через Forget().",
            item);

        heldItem = null;
        breakDistanceArmed = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Release(
                Vector3.zero,
                "приложение потеряло фокус");
        }
    }

    private void OnDisable()
    {
        pickupAction?.Disable();
        throwAction?.Disable();

        Release(
            Vector3.zero,
            "ObjectInteraction отключён");
    }

    private void OnDestroy()
    {
        pickupAction?.Dispose();
        throwAction?.Dispose();
    }
}