using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemySpawnPointsParent;
    [SerializeField] private Transform EnemyPrefab;

    //[SerializeField] private GameObject boatPartSpawnPointsParent;
    //[SerializeField] private Transform boatPartPrefab;

    //[SerializeField] private Transform pickupableItemPrefab;

    public override void OnStartServer()
    {
        base.OnStartServer();
        enemySpawnPointsParent.SetActive(false);
        //boatPartSpawnPointsParent.SetActive(false);
        enabled = IsServer || IsHost;
        if (!enabled)
        {
            return;
        }
        foreach (Transform child in enemySpawnPointsParent.transform)
        {
            Debug.Log("Spawned enemy at: " + child.name);
            Transform spawnedObjectTransform = Instantiate(EnemyPrefab);            
            spawnedObjectTransform.position = child.position;
            base.Spawn(spawnedObjectTransform.gameObject);
            // This is a unity "problem" where owner of object isKinetmatic is automatically set to false by network rigidbody
            // More detail: https://forum.unity.com/threads/network-rigidbody-kinematic-sets-to-unwanted-value.1385874/
            spawnedObjectTransform.GetComponent<Rigidbody>().isKinematic = true;
        }
        //foreach (Transform child in boatPartSpawnPointsParent.transform)
        //{
        //    Debug.Log("Spawned boatPart at: " + child.name);
        //    Transform spawnedObjectTransform = Instantiate(pickupableItemPrefab);
        //    spawnedObjectTransform.position = child.position;
        //    base.Spawn(spawnedObjectTransform.gameObject);
        //}
    }
}
