using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bridge : Structure
{
    // I could have used tags for distinction, but for now the norm is: colliders[0]: collider before completion; colliders[1]: collider after completion.
    private Collider[] colliders;

    [ObserversRpc]
    private void UpdateColliderClientRpc(bool isCompleted)
    {
        UpdateCollider(isCompleted);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void UpdateColliderServerRpc(bool isCompleted)
    {
        UpdateCollider(isCompleted);
        UpdateColliderClientRpc(isCompleted);
    }

    private void UpdateCollider(bool isCompleted)
    {
        int colliderIdx = isCompleted ? 1 : 0;  // Explicit bool to int conversion
        colliders[colliderIdx].enabled = true;
        colliders[1 - colliderIdx].enabled = false;
    }

                
    protected override void FinishBuildingAction(Player player)
    {
        if (IsServer || IsHost)
        {
            UpdateColliderClientRpc(true);
        } else
        {
            UpdateColliderServerRpc(true);
        }
    }

    protected override void Start()
    {
        base.Start();
        colliders = GetComponentsInChildren<Collider>();
        colliders[0].enabled = true;
        colliders[1].enabled = false;

    }

}
