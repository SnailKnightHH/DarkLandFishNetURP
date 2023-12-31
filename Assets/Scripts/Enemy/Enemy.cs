using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Character, ITriggerCollider, ITrackable
{
    public bool IsAirBorne { get; protected set; }
    [SerializeField] protected Transform eyeTransform;
    protected Transform pathFindingDestination;
    protected Transform initialTransform;
    protected NavMeshAgent agent;
    protected GameObject lockedPlayer;
    protected float agentStoppingDistance = 2f;
    GenericColliderDetector<Player> colliderDetector;

    [field: SerializeField] protected virtual float BaseAttackInterval
    {
        get; set;
    }
    [field: SerializeField] protected virtual string EnemyType
    {
        get; set;
    }

    public Transform TrackOrigin => eyeTransform;

    private bool isAttacking = false;

    protected virtual void Awake()
    {
        IsAirBorne = false;
    }

    protected override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        collider = GetComponentInChildren<Collider>();
        colliderDetector = new GenericColliderDetector<Player>(eyeTransform, "Enemy", ActionWhenRayCastHitPlayer, OnExitAdditionalAction);
        colliderDetector.OnLockedTargetChanged += () => { lockedPlayer = colliderDetector.lockedTarget; };
        initialTransform = transform;
        pathFindingDestination = initialTransform;
        // Weird fix for agent mesh not
        agent.stoppingDistance = agentStoppingDistance;
        agent.enabled = false;
        agent.enabled = true;
    }

    protected virtual void Update()
    {
        if (lockedPlayer == null)
        {
            colliderDetector.raycastTargets();
        }
        Move();
        Attack();
    }

    protected virtual void Attack()
    {
        if (lockedPlayer != null && !isAttacking && Vector3.Distance(lockedPlayer.transform.position, transform.position) <= agentStoppingDistance)
        {
            Player player = lockedPlayer.GetComponent<Player>();
            StartCoroutine(baseAttack(player));
        }
    }

    private IEnumerator baseAttack(Player player)
    {
#if UNITY_EDITOR
        Debug.Log(EnemyType + " attacked");
#endif
        if (player != null)
        {
            isAttacking = true;
            player.ReceiveDamage(Damage);
            yield return new WaitForSeconds(BaseAttackInterval);
            isAttacking = false;
        }
    }

    // potential for template method
    protected override void Die()
    {
#if UNITY_EDITOR
        Debug.Log("Enemy killed");
#endif
        // Todo: For now use this to force ontriggerexit. Need a better way.
        transform.position = new Vector3(1000000, 1000000, 1000000);
        base.Despawn();
    }

    public virtual void onEnterDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        colliderDetector.onEnterDetectionZone(other, initiatingGameObject);
    }
    public virtual void onExitDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        colliderDetector.onExitDetectionZone(other, initiatingGameObject);
    }

    protected abstract void OnExitAdditionalAction(Player player);

    public void onStayDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        // Not needed for enemy
    }

    private void OnDrawGizmos()
    {
        if (lockedPlayer != null)
        {
            Gizmos.DrawLine(eyeTransform.position, lockedPlayer.transform.position);
        }
        
    }


    // template method 
    protected virtual void ActionWhenRayCastHitPlayer(Player player) // not abstract since implementation is not a requirement
    {

    }
    protected virtual void ActionWhenRayCastDoesNotHitPlayer(Player player)
    {

    }
}
