using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flashlight : Tool
{
    private Light light;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        light = GetComponentInChildren<Light>();
        light.enabled = false;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void ToggleFlashLightServerRpc()
    {
        light.enabled = !light.enabled;
        ToggleFlashLightClientRpc();
    }

    // ExcludeOwner -> client calls server rpc, which runs locally, then other clients receive update. Exclude owner so action initiator does not execute action twice.
    // ExcludeServer -> client calls server rpc, if server is a client itself (ie. it's a host), then server rpc run locally (count = 1), then client rpc runs on host (count = 2) -> execute action twice
    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ToggleFlashLightClientRpc()
    {
        light.enabled = !light.enabled;
    }

    public override IEnumerator UseTool(Player player)
    {
        if (IsHost || IsServer)
        {
            // If is host, then as mentioned above client rpc excludes server, so have to enforce that action locally separately, then invoke client rpc for network syncing
            light.enabled = !light.enabled;
            ToggleFlashLightClientRpc();
        } else
        {
            ToggleFlashLightServerRpc();
        }
        yield return null;
    }

    public override void DisableOrEnableMesh(bool state)
    {
        UpdateShowMeshServerRpc(state);
        pickupableBaseClassmeshRenderer.enabled = state;
        light.enabled = false;
    }
}
