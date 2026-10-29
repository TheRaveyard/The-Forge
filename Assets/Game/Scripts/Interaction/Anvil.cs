using UnityEngine;

public class Anvil : MonoBehaviour
{
    public bool Full;
    public Transform placementPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAccept(other);
    }

    private  void TryAccept(Collider other)
    {
        if (other == null || Full == true)
        {
            return;

        }

        Rigidbody body = other.attachedRigidbody;
        if (body == null)
        {
            return;
        }
        if (!body.TryGetComponent(out PickupItem item) ||
            !item.isActiveAndEnabled || item.IsHeld)
            return;
        Debug.Log(body);
        body.position = placementPoint.position;
        body.rotation = placementPoint.rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
        Full = true;
        Debug.Log("Загатовка Поставленна");
    }
}

