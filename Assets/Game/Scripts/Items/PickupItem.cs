using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PickupItem : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float heldAngularDamping = 8f;

    private struct CollisionPair
    {
        public Collider Item;
        public Collider Player;
    }

    private readonly List<CollisionPair> ignoredCollisions =
        new List<CollisionPair>();

    private Collider[] itemColliders;
    private ObjectInteraction owner;

    private bool savedGravity;
    private float savedAngularDamping;

    private Coroutine collisionRestoreRoutine;

    private static readonly WaitForFixedUpdate PhysicsStep =
        new WaitForFixedUpdate();

    public Rigidbody Body { get; private set; }

    public bool IsHeld => owner != null;


    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>();

        if (Body == null)
        {
            Debug.LogError(
                $"[PickupItem] '{name}': Rigidbody отсутствует.",
                this);

            return;
        }

        if (itemColliders == null || itemColliders.Length == 0)
        {
            Debug.LogWarning(
                $"[PickupItem] '{name}': не найдено ни одного Collider.",
                this);
        }
    }


    public bool TryHold(
        ObjectInteraction holder,
        Collider[] playerColliders)
    {
        // Проверяем все причины, по которым предмет нельзя взять.

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: '{name}' отключён.",
                this);

            return false;
        }

        if (holder == null)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: holder == null для '{name}'.",
                this);

            return false;
        }

        if (owner != null)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: '{name}' уже удерживается " +
                $"объектом '{owner.name}'.",
                this);

            return false;
        }

        if (Body == null)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: у '{name}' отсутствует Rigidbody.",
                this);

            return false;
        }

        if (Body.isKinematic)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: Rigidbody '{name}' isKinematic.",
                this);

            return false;
        }

        if (!Body.detectCollisions)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: у '{name}' " +
                "Rigidbody.detectCollisions выключен.",
                this);

            return false;
        }

        if (playerColliders == null)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: playerColliders == null " +
                $"для '{name}'.",
                this);

            return false;
        }

        // Предмет должен иметь хотя бы один рабочий физический
        // Collider, принадлежащий именно этому Rigidbody.
        bool hasCollider = false;

        foreach (Collider item in itemColliders)
        {
            if (item == null)
                continue;

            if (!item.enabled)
                continue;

            if (item.isTrigger)
                continue;

            if (item.attachedRigidbody != Body)
                continue;

            hasCollider = true;
            break;
        }

        if (!hasCollider)
        {
            Debug.LogWarning(
                $"[PickupItem] TryHold отказ: у '{name}' нет активного " +
                "не-trigger Collider, принадлежащего его Rigidbody.",
                this);

            return false;
        }

        /*
         * Если после предыдущего броска ещё работала корутина
         * восстановления столкновений, останавливаем её.
         *
         * Сами записи ignoredCollisions не очищаем:
         * они всё ещё описывают реально отключённые столкновения.
         */
        StopCollisionRestore();

        // Сохраняем владельца ДО изменения физики.
        owner = holder;

        // Сохраняем исходные настройки Rigidbody.
        savedGravity = Body.useGravity;
        savedAngularDamping = Body.angularDamping;

        // Во время удержания предмет остаётся динамическим.
        // ObjectInteraction будет двигать его через AddForce.
        Body.useGravity = false;
        Body.angularDamping = heldAngularDamping;
        Body.WakeUp();

        int ignoredCount = 0;

        // Отключаем столкновения предмета с коллайдерами игрока.
        // Это предотвращает толкание самого игрока удерживаемым
        // перед ним предметом.
        foreach (Collider item in itemColliders)
        {
            if (item == null ||
                !item.enabled ||
                item.isTrigger ||
                item.attachedRigidbody != Body)
            {
                continue;
            }

            foreach (Collider player in playerColliders)
            {
                if (player == null ||
                    !player.enabled)
                {
                    continue;
                }

                // Не меняем пары, которые уже кем-то игнорируются.
                if (Physics.GetIgnoreCollision(item, player))
                    continue;

                Physics.IgnoreCollision(
                    item,
                    player,
                    true);

                ignoredCollisions.Add(
                    new CollisionPair
                    {
                        Item = item,
                        Player = player
                    });

                ignoredCount++;
            }
        }

        Debug.Log(
            $"[PickupItem] HOLD OK: '{name}'. " +
            $"Владелец: '{holder.name}'. " +
            $"Gravity: {savedGravity} -> false. " +
            $"Angular damping: {savedAngularDamping:F2} -> " +
            $"{heldAngularDamping:F2}. " +
            $"Отключено столкновений с игроком: {ignoredCount}.",
            this);

        return true;
    }


    public void Release(
        ObjectInteraction holder,
        Vector3 impulse)
    {
        if (holder == null)
        {
            Debug.LogWarning(
                $"[PickupItem] Release проигнорирован для '{name}': " +
                "holder == null.",
                this);

            return;
        }

        if (owner != holder)
        {
            Debug.LogWarning(
                $"[PickupItem] Release проигнорирован для '{name}': " +
                $"вызвавший holder = '{holder.name}', " +
                $"текущий owner = " +
                $"'{(owner != null ? owner.name : "null")}'.",
                this);

            return;
        }

        Debug.Log(
            $"[PickupItem] RELEASE: '{name}'. " +
            $"Владелец: '{holder.name}'. " +
            $"Impulse: {impulse}.",
            this);

        // Сначала предмет перестаёт считаться удерживаемым.
        owner = null;

        /*
         * Сообщаем ObjectInteraction, что этот предмет
         * больше не должен находиться в heldItem.
         *
         * В нормальном Release() ObjectInteraction уже очищает
         * heldItem перед вызовом этого метода, поэтому Forget()
         * там просто ничего не сделает.
         *
         * Но это также защищает от Release, инициированного
         * непосредственно самим PickupItem.
         */
        holder.Forget(this);

        RestoreBody();

        // Импульс броска применяем только к рабочему
        // динамическому Rigidbody.
        if (Body != null &&
            !Body.isKinematic &&
            gameObject.activeInHierarchy)
        {
            if (impulse.sqrMagnitude > 0f)
            {
                Body.AddForce(
                    impulse,
                    ForceMode.Impulse);
            }
        }

        /*
         * Пытаемся сразу восстановить те столкновения,
         * для которых предмет уже не пересекается с игроком.
         *
         * Пересекающиеся пары пока оставляем отключёнными,
         * иначе физика может резко вытолкнуть игрока/предмет.
         */
        RestoreCollisions(false);

        if (ignoredCollisions.Count > 0 &&
            isActiveAndEnabled)
        {
            collisionRestoreRoutine =
                StartCoroutine(RestoreAfterSeparation());
        }
    }


    private void RestoreBody()
    {
        if (Body == null)
            return;

        Body.useGravity = savedGravity;
        Body.angularDamping = savedAngularDamping;
        Body.WakeUp();
    }


    private IEnumerator RestoreAfterSeparation()
    {
        /*
         * После отпускания/броска ждём физического разделения
         * предмета и игрока.
         *
         * Логов внутри цикла нет, поэтому Console не будет
         * получать сообщения каждый FixedUpdate.
         */
        while (ignoredCollisions.Count > 0)
        {
            yield return PhysicsStep;

            RestoreCollisions(false);
        }

        collisionRestoreRoutine = null;
    }


    private void StopCollisionRestore()
    {
        if (collisionRestoreRoutine == null)
            return;

        StopCoroutine(collisionRestoreRoutine);
        collisionRestoreRoutine = null;
    }


    private void RestoreCollisions(bool immediately)
    {
        for (int i = ignoredCollisions.Count - 1; i >= 0; i--)
        {
            CollisionPair pair = ignoredCollisions[i];

            /*
             * Если один из объектов уничтожен, запись можно
             * просто удалить — восстанавливать уже нечего.
             */
            if (pair.Item == null ||
                pair.Player == null)
            {
                ignoredCollisions.RemoveAt(i);
                continue;
            }

            if (!immediately)
            {
                /*
                 * Пока оба Collider включены и их bounds
                 * пересекаются, оставляем столкновение
                 * отключённым.
                 */
                if (pair.Item.enabled &&
                    pair.Player.enabled &&
                    pair.Item.bounds.Intersects(
                        pair.Player.bounds))
                {
                    continue;
                }
            }

            Physics.IgnoreCollision(
                pair.Item,
                pair.Player,
                false);

            ignoredCollisions.RemoveAt(i);
        }
    }


    private void OnDisable()
    {
        StopCollisionRestore();

        /*
         * Если предмет отключили прямо во время удержания,
         * обязательно очищаем heldItem у ObjectInteraction.
         */
        if (owner != null)
        {
            ObjectInteraction previousOwner = owner;
            owner = null;

            Debug.Log(
                $"[PickupItem] '{name}' отключён во время удержания. " +
                $"Очищаем ссылку у '{previousOwner.name}'.",
                this);

            previousOwner.Forget(this);

            RestoreBody();
        }

        // При выключении компонента ждать разделения уже нельзя,
        // поэтому восстанавливаем все пары немедленно.
        RestoreCollisions(true);
    }


    private void OnDestroy()
    {
        /*
         * OnDisable обычно уже выполнит очистку.
         * Этот код оставлен как дополнительная защита.
         */
        StopCollisionRestore();

        if (owner != null)
        {
            ObjectInteraction previousOwner = owner;
            owner = null;

            previousOwner.Forget(this);
        }

        RestoreCollisions(true);
    }
}