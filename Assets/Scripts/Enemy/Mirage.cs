using FishNet.Object.Synchronizing;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Mirage : Enemy
{
    private MeshRenderer mesh;
    private float dashForce = 10f;
    private float afterDashDelay = 1f;
    private float rageDistance = 4f;
    private float teleportDelay = 2f;    
    private float teleportDistance = 5f;
    private float teleportAngle = 45;
    [SerializeField] GameObject SpotLightGameobject;

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private bool CanMove;

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private bool CanBeAttacked;    

    protected override int Damage { get => 20; }

    Iweapon weapon; // debug purposes    

    protected override void Awake()
    {
        base.Awake();
        CanMove = true;
        CanBeAttacked = true;
    }

    protected override void Start()
    {
        base.Start();
        mesh = GetComponentInChildren<MeshRenderer>();
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateCanMoveVariableServerRpc(bool canMove)
    {
        CanMove = canMove;        
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateCanBeAttackedServerRpc(bool canBeAttacked)
    {
        CanBeAttacked = canBeAttacked;
    }

    public override async void ReceiveDamage(GameObject damageDealer, int damage)
    {
        if (!CanBeAttacked) { return; }
        base.ReceiveDamage(damage);
        if (Vector3.Distance(transform.position, pathFindingDestination.position) <= rageDistance)
        {
            UpdateCanBeAttackedServerRpc(false);
            UpdateCanMoveVariableServerRpc(false);            
            await VanishAndClone(damageDealer.GetComponent<Iweapon>());
            Dash(damageDealer.GetComponent<Iweapon>());
            UpdateCanBeAttackedServerRpc(true);
            UpdateCanMoveVariableServerRpc(true);
        }

    }

    private async Task VanishAndClone(Iweapon weapon)
    {
        this.weapon = weapon; // debug purposes
        UpdateMeshVisibility(false);        
        Vector3 dir = (weapon.PlayerTransform.GetComponent<Player>().cameraTransform.position - transform.position).normalized;
        dir.y = 0;
        Vector2 locationOne2d = new Vector2((Quaternion.Euler(0, teleportAngle, 0) * dir).x * teleportDistance, (Quaternion.Euler(0, teleportAngle, 0) * dir).z * teleportDistance) + new Vector2(transform.position.x, transform.position.z);
        Vector2 locationTwo2d = new Vector2((Quaternion.Euler(0, teleportAngle * -1, 0) * dir).x * teleportDistance, (Quaternion.Euler(0, teleportAngle * -1, 0) * dir).z * teleportDistance) + new Vector2(transform.position.x, transform.position.z);
        Vector3 location1 = new Vector3(locationOne2d.x, transform.position.y, locationOne2d.y);
        Vector3 location2 = new Vector3(locationTwo2d.x, transform.position.y, locationTwo2d.y);
        IReadOnlyList<Vector3> potentialTeleportPositions = new List<Vector3> { transform.position, location1, location2 };
        int idx = UnityEngine.Random.Range(0, potentialTeleportPositions.Count);
        transform.position = potentialTeleportPositions[idx];

        await Task.Delay(Utilities.SecondsToMilliseconds(teleportDelay));
        UpdateMeshVisibility(true);
    }

    private void Dash(Iweapon weapon)
    {

        if (IsHost)
        {
            DashClientRpc((weapon.PlayerTransform.GetComponent<Player>().cameraTransform.position - transform.position).normalized * dashForce);
        }
        else
        {
            DashServerRpc((weapon.PlayerTransform.GetComponent<Player>().cameraTransform.position - transform.position).normalized * dashForce);
        }
    }

    private void UpdateMeshVisibility(bool ifEnable)
    {
        if (IsHost)
        {
            UpdateMeshVisibilityClientRpc(ifEnable);
        }
        else
        {
            UpdateMeshVisibilityServerRpc(ifEnable);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateMeshVisibilityServerRpc(bool ifEnable)
    {
        UpdateMeshVisibilityClientRpc(ifEnable);
    }

    [ObserversRpc]
    private void UpdateMeshVisibilityClientRpc(bool ifEnable)
    {
        mesh.enabled = ifEnable;
        SpotLightGameobject.SetActive(ifEnable);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DashServerRpc(Vector3 force)
    {
        DashClientRpc(force);
    }

    [ObserversRpc]
    private void DashClientRpc(Vector3 force)
    {
        rb.isKinematic = false;
        collider.isTrigger = true;
        // Todo: potentially use lerp instead so we don't have to worry about kinematic state and collider state        
        rb.AddForce(force, ForceMode.Impulse);
        StartCoroutine(DashDelay());    // cannot use async for RPC

    }

    private IEnumerator DashDelay()
    {
        yield return new WaitForSeconds(afterDashDelay);  // delay first then collider and rb adjustment -> otherwise too short for unity to react I guess
        collider.isTrigger = false;
        rb.isKinematic = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rageDistance);
        if (weapon == null) return;
        Vector3 dir = (weapon.PlayerTransform.GetComponent<Player>().cameraTransform.position - transform.position).normalized;
        dir.y = 0;
        Vector2 locationOne2d = new Vector2((Quaternion.Euler(0, teleportAngle, 0) * dir).x * teleportDistance, (Quaternion.Euler(0, teleportAngle, 0) * dir).z * teleportDistance) + new Vector2(transform.position.x, transform.position.z);
        Vector2 locationTwo2d = new Vector2((Quaternion.Euler(0, teleportAngle * -1, 0) * dir).x * teleportDistance, (Quaternion.Euler(0, teleportAngle * -1, 0) * dir).z * teleportDistance) + new Vector2(transform.position.x, transform.position.z);
        Vector3 location1 = new Vector3(locationOne2d.x, transform.position.y, locationOne2d.y);
        Vector3 location2 = new Vector3(locationTwo2d.x, transform.position.y, locationTwo2d.y);
        //print("teleport location 1: " + location1);
        //print("teleport location 2: " + location2);
        //print("transform position: " + transform.position);
        Gizmos.DrawLine(weapon.PlayerTransform.GetComponent<Player>().cameraTransform.position, eyeTransform.position);
        Gizmos.DrawWireSphere(location1, 1f);
        Gizmos.DrawWireSphere(location2, 1f);
        Gizmos.DrawWireSphere(transform.position, 1f);
    }


    public override void Move()
    {
        if (lockedPlayer != null && CanMove)
        {
            agent.isStopped = false;
            pathFindingDestination = lockedPlayer.gameObject.transform;
            agent.destination = pathFindingDestination.position;
        }
        else if (!CanMove)
        {
            agent.isStopped = true;            
        }

    }

    protected override void OnExitAdditionalAction(Player player)
    {
        // not used for now
    }

}
