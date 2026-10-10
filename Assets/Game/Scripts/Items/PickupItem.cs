using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PickupItem : MonoBehaviour
{
    [SerializeField, Min(0f)] private float heldAngularDamping = 8f;


    private struct CollisionPair
    {
        public Collider Item;
        public Collider Player;
    }


    private readonly List<CollisionPair> ignoredCollisions = new List<CollisionPair>();
    private Collider[] itemColliders;
    private ObjectInteraction owner;
    private bool savedGravity;
    private float savedAngularDamping;
    private Coroutine collisionRestoreRoutine;
    private static readonly WaitForFixedUpdate PhysicsStep = new WaitForFixedUpdate();


    public Rigidbody Body { get; private set; }


    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>();
    }


    public bool TryHold(ObjectInteraction holder, Collider[] playerColliders)
    {
        if (!isActiveAndEnabled || holder == null || owner != null || Body == null ||
            Body.isKinematic || !Body.detectCollisions || playerColliders == null) return false;
        bool hasCollider = false;
        foreach (Collider item in itemColliders)
            if (item != null && item.enabled && !item.isTrigger && item.attachedRigidbody == Body)
                hasCollider = true;
        if (!hasCollider) return false;


        StopCollisionRestore();
        owner = holder;
        savedGravity = Body.useGravity;
        savedAngularDamping = Body.angularDamping;
        Body.useGravity = false;
        Body.angularDamping = heldAngularDamping;
        Body.WakeUp();
        foreach (Collider item in itemColliders)
        {
            if (item == null || !item.enabled || item.isTrigger || item.attachedRigidbody != Body) continue;
            foreach (Collider player in playerColliders)
            {
                if (player == null || !player.enabled || Physics.GetIgnoreCollision(item, player)) continue;
                Physics.IgnoreCollision(item, player, true);
                ignoredCollisions.Add(new CollisionPair { Item = item, Player = player });
            }
        }
        return true;
    }


    public void Release(ObjectInteraction holder, Vector3 impulse)
    {
        if (holder == null || owner != holder) return;
        owner = null;
        holder.Forget(this);
        RestoreBody();
        if (Body != null && !Body.isKinematic && gameObject.activeInHierarchy)
            Body.AddForce(impulse, ForceMode.Impulse);
        RestoreCollisions(false);
        if (ignoredCollisions.Count > 0 && isActiveAndEnabled)
            collisionRestoreRoutine = StartCoroutine(RestoreAfterSeparation());
    }


    private void RestoreBody()
    {
        if (Body == null) return;
        Body.useGravity = savedGravity;
        Body.angularDamping = savedAngularDamping;
    }


    private IEnumerator RestoreAfterSeparation()
    {
        // После броска ждём разделения с игроком, чтобы включение столкновений не вытолкнуло его.
        while (ignoredCollisions.Count > 0)
        {
            yield return PhysicsStep;
            RestoreCollisions(false);
        }
        collisionRestoreRoutine = null;
    }


    private void StopCollisionRestore()
    {
        if (collisionRestoreRoutine == null) return;
        StopCoroutine(collisionRestoreRoutine);
        collisionRestoreRoutine = null;
    }


    private void RestoreCollisions(bool immediately)
    {
        for (int i = ignoredCollisions.Count - 1; i >= 0; i--)
        {
            CollisionPair pair = ignoredCollisions[i];
            if (pair.Item != null && pair.Player != null)
            {
                if (!immediately && pair.Item.enabled && pair.Player.enabled &&
                    pair.Item.bounds.Intersects(pair.Player.bounds)) continue;
                Physics.IgnoreCollision(pair.Item, pair.Player, false);
            }
            ignoredCollisions.RemoveAt(i);
        }
    }


    private void OnDisable()
    {
        StopCollisionRestore();
        if (owner != null)
        {
            ObjectInteraction previousOwner = owner;
            owner = null;
            previousOwner.Forget(this);
            RestoreBody();
        }
        RestoreCollisions(true);
    }
}
