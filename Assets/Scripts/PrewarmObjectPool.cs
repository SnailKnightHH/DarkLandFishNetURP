using FishNet.Object;
using FishNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrewarmObjectPool : NetworkBehaviour
{
    private void Start()
    {
        /// <summary>
        /// Instantiates a number of objects and adds them to the pool.
        /// </summary>
        /// <param name="prefab">Prefab to cache.</param>
        /// <param name="count">Quantity to spawn.</param>
        /// <param name="asServer">True if storing prefabs for the server collection.</param>
        foreach (KeyValuePair<Item, GameObject> kvp in SOManager.Instance.ItemPrefabMapping)
        {
            InstanceFinder.NetworkManager.CacheObjects(kvp.Value.GetComponent<NetworkObject>(), 10, IsServer); 
        }
        foreach (EffectEnumToEffectPrefabMapping effectMapping in EffectManager.Instance.EffectMappings)
        {
            InstanceFinder.NetworkManager.CacheObjects(effectMapping.effect.GetComponent<NetworkObject>(), 30, IsServer);
        }
    }


}
