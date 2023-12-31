using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class FlyingPufferFish : Enemy
{
    private float hoistSpeed = 2f;
    private bool grabbedPlayerReachedMaxHeight = false;

    protected override int Damage { get => 20; }

    protected override void Awake()
    {
        base.Awake();
        IsAirBorne = true;
    }

    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    protected override void Attack()
    {
        if (grabbedPlayerReachedMaxHeight)
        {
            base.Attack(); // periodically cause damage
        }
        if (lockedPlayer != null 
            && Vector2.Distance(new Vector2(lockedPlayer.transform.position.x, lockedPlayer.transform.position.z), 
            new Vector2(transform.position.x, transform.position.z)) <= 3f)
        {
            Player player = lockedPlayer.GetComponent<Player>();
            if (!player.Grabbed)
            {
                StartCoroutine(Grab(player));
            }
        }            
    }

    private IEnumerator Grab(Player player)
    {
        UpdatePlayerGrabbedStatusServerRpc(true);
        Vector3 startingPosition = player.transform.position;
        Vector3 endPosition = transform.position;
        float distance = Vector3.Distance(startingPosition, endPosition);
        float remainingDistance = distance;

        while (remainingDistance > 0)
        {
            player.transform.position = Vector3.Lerp(startingPosition, endPosition, 1 - (remainingDistance / distance));
            remainingDistance -= hoistSpeed * Time.deltaTime;
            yield return null;
        }
        grabbedPlayerReachedMaxHeight = true;
    }


    public override void Move()
    {
        if (lockedPlayer != null)
        {
            Vector2 playerPos = new Vector2(lockedPlayer.transform.position.x, lockedPlayer.transform.position.z);
            Vector2 thisPos = new Vector2(transform.position.x, transform.position.z);
            if (Vector2.Distance(playerPos, thisPos) > agentStoppingDistance)
            {
                Vector2 result = Vector2.MoveTowards(new Vector2(transform.position.x, transform.position.z), new Vector2(lockedPlayer.transform.position.x, lockedPlayer.transform.position.z), speed * Time.deltaTime);
                transform.position = new Vector3(result.x, transform.position.y, result.y);
            }
            
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerGrabbedStatusServerRpc(bool isGrabbed)
    {
        lockedPlayer.GetComponent<Player>().Grabbed = isGrabbed;
    }

    protected override void Die()
    {
        if (lockedPlayer != null)
        {
            UpdatePlayerGrabbedStatusServerRpc(false);
        } 
        // Todo: Make player fall smoothly instead of appearing on the ground instantly
        base.Die();
    }

    protected override void OnExitAdditionalAction(Player player)
    {
        // not used for now
    }
}
