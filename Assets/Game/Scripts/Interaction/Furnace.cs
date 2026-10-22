using UnityEngine;
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]

public class Furnace : MonoBehaviour
{
    public int goldCount;
    public int copperCount;
    public int bronzeCount;
    public bool isComplete => goldCount >= requiredGold && copperCount >= requiredCopper;
    private int requiredGold = 2;
    private int requiredBronze = 0;
    private int requiredCopper = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnTriggerStay(Collider other)
    {
        TryAccept(other);
    }
    private void OnTriggerEnter(Collider other)
    {
        TryAccept(other);
    }
    private void TryAccept(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null)
        {
            return;
        }

        if (!body.TryGetComponent(out MaterialDefinition material) ||
            !material.isActiveAndEnabled ||
            !body.TryGetComponent(out PickupItem item) ||
            !item.isActiveAndEnabled || item.IsHeld)
            return;
        
        switch (material.Type)
        {
            case OreType.Gold:
                if (goldCount >= requiredGold)
                {
                    return;
                }
                goldCount++; //goldCount = goldCount + 1;
                break;
            case OreType.Bronze:
                if (bronzeCount >= requiredGold)
                {
                    return;
                }
                bronzeCount++;
                break;
            case OreType.Copper:
                if (copperCount >= requiredGold)
                {
                    return;
                }
                copperCount++;
                break;
            default:
                return;
        }

        if (isComplete == true)
        {
            return;
        }
        Destroy(other.attachedRigidbody.gameObject);
    }
}
