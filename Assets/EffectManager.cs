using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FishNet.Object;

[Serializable]
public class EffectEnumToEffectPrefabMapping
{
    public EffectName effectName;
    public GameObject effect;
}

public enum EffectName
{
    MuzzleFlash
}

public class EffectManager : NetworkBehaviour
{
    public static EffectManager Instance { get; private set; }

    [SerializeField] private List<EffectEnumToEffectPrefabMapping> effectMappings = new List<EffectEnumToEffectPrefabMapping>();
    public List<EffectEnumToEffectPrefabMapping> EffectMappings
    {
        get
        {
            return effectMappings;
        }
    }

    private Dictionary<EffectName, GameObject> effectsDict = new Dictionary<EffectName, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        foreach (EffectEnumToEffectPrefabMapping kvp in effectMappings)
        {
            effectsDict.Add(kvp.effectName, kvp.effect);
        }
    }

    public void PlayEffect(EffectName effectName, float destroyDelay, Vector3 position, Quaternion rotation, bool isServer)
    {
        PlayEffectServerRpc(effectName, destroyDelay, position, rotation, isServer);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayEffectServerRpc(EffectName effectName, float destroyDelay, Vector3 position, Quaternion rotation, bool isServer)
    {
        NetworkObject spawnedNetworkObject = NetworkManager.ObjectPool.RetrieveObject(effectsDict[effectName].GetComponent<NetworkObject>().PrefabId, effectsDict[effectName].GetComponent<NetworkObject>().SpawnableCollectionId, position, rotation, IsServer);
        base.Spawn(spawnedNetworkObject);
        StartCoroutine(DestroyAfterDelay(destroyDelay, spawnedNetworkObject));
    }

    // Todo: Don't need to create a coroutine if find out an easier way to destroy after timer delay. while loop with Time.deltaTime only works in update or coroutine afaik.
    private IEnumerator DestroyAfterDelay(float timer, NetworkObject networkObject)
    {
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        base.Despawn(networkObject, DespawnType.Destroy); // can use pool if needed
    }

}
