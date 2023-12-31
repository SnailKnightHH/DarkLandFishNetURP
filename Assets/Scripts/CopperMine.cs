using FishNet.Demo.AdditiveScenes;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopperMine : Structure
{
    private MeshRenderer meshRenderer;
    private Collider collider;

    protected override void FinishBuildingAction(Player player)
    {
        player.SpawnItem(structureSO.ItemName, player.CarryMountPoint.position, player.cameraTransform.rotation, NumOfItem: structureSO.totalNumOfRequiredItems);
        StartCoroutine(DestroyAfterDelay());
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void DisableMeshAndColliderServerRpc()
    {
        meshRenderer.enabled = false;
        collider.enabled = false;
        DisableMeshAndColliderClientRpc();
    }

    [ObserversRpc]
    private void DisableMeshAndColliderClientRpc()
    {
        meshRenderer.enabled = false;
        collider.enabled = false;
    }

    private IEnumerator DestroyAfterDelay()
    {
        if (IsServer || IsHost)
        {
            DisableMeshAndColliderClientRpc();
        } else
        {
            DisableMeshAndColliderServerRpc();
        }
        yield return ACTION_DELAY;
        base.Despawn();
    }

    protected override void Start()
    {
        base.Start();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        collider = GetComponentInChildren<Collider>();
    }

    void Update()
    {
        
    }
}
