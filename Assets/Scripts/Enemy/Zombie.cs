using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie : Enemy, ITriggerCollider
{        
    private Vector3 playerLastSeenLocation;

    protected override int Damage { get => 10; }

    protected override void Start()
    {
        base.Start();
        playerLastSeenLocation = transform.position;
    }

    protected override void Update()
    {
        base.Update();        
    }

    public override void Move()
    {
        agent.destination = lockedPlayer == null ? playerLastSeenLocation : pathFindingDestination.position; 
    }

    protected override void ActionWhenRayCastHitPlayer(Player player)
    {
        Debug.DrawRay(eyeTransform.position, player.eyeTransform.position - eyeTransform.position, Color.yellow);
        pathFindingDestination = player.gameObject.transform;            
    }


    protected override void OnExitAdditionalAction(Player player)
    {
        playerLastSeenLocation = player.transform.position;
    }
}
