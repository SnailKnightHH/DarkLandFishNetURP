using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ListHash<T>
{
    private Dictionary<T, int> dict;
    private List<T> list;    

    public ListHash()
    {
        dict = new Dictionary<T, int>();
        list = new List<T>();
    }

    public void Insert(T element)
    {
        if (!dict.ContainsKey(element))
        {
            dict.Add(element, list.Count);
            list.Add(element);
        }
        
    }

    public void Remove(T element)
    {
        if (dict.TryGetValue(element, out int idx))
        {
            if (idx < list.Count - 1)
            {
                list[idx] = list[list.Count - 1];
                dict[list[idx]] = idx;
            }            
            list.RemoveAt(list.Count - 1);
            dict.Remove(element);            
        }        
    }

    public T GetRandom()
    {
        int idx = Random.Range(0, list.Count);
        return list[idx];
    }

    public int Count
    {        
        get
        {
            return dict.Count;
        } 
    }
}

public class RandomizedObjectSpawner : NetworkBehaviour
{
    [SerializeField] private LayerMask walkableLayerMask;
    [SerializeField] private int numberOfHousesToSpawn = 5;
    [SerializeField] private int numberOfEnemyToSpawn = 5;
    [SerializeField] private int numberOfMineralsToSpawn = 5;

    private ListHash<Vector3> houseLocationsToSpawn;
    private ListHash<Vector3> enemyLocationsToSpawn;
    private ListHash<Vector3> mineralLocationsToSpawn;
    [SerializeField] private List<GameObject> houses;
    [SerializeField] private List<GameObject> enemies;
    [SerializeField] private List<GameObject> minerals;


    private float airborneEnemySpawnHeightOffset = 20f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (!IsServer && !IsHost) { return; }
        Transform houseLocations = transform.Find("HouseLocations");

        houseLocationsToSpawn = new ListHash<Vector3>();
        for (int i = 0; i < houseLocations.childCount; i++)
        {
            houseLocationsToSpawn.Insert(houseLocations.GetChild(i).position);
        }
        Debug.Assert(numberOfHousesToSpawn <= houseLocationsToSpawn.Count, "Cannot spawn more objects than locations.");

        for (int i = 0; i < numberOfHousesToSpawn; i++)
        {
            Vector3 location = houseLocationsToSpawn.GetRandom();
            GameObject housePrefab = houses[Random.Range(0, houses.Count)]; // houses can be duplicate for now
            GameObject houseInstance = Instantiate(housePrefab, location, housePrefab.transform.rotation);
            Utilities.DetermineRotationBySurfaceNormal(houseInstance.transform, walkableLayerMask);
            base.Spawn(houseInstance);
            houseLocationsToSpawn.Remove(location); // to ensure no duplicate locations are selected 
        }


        Transform enemyLocations = transform.Find("EnemyLocations");
        enemyLocationsToSpawn = new ListHash<Vector3>();
        for (int i = 0; i < enemyLocations.childCount; i++)
        {
            if (enemyLocations.GetChild(i).gameObject.activeSelf)
            {
                enemyLocationsToSpawn.Insert(enemyLocations.GetChild(i).position);
            }
        }
        Debug.Assert(numberOfEnemyToSpawn <= enemyLocationsToSpawn.Count, "Cannot spawn more objects than locations.");

        for (int i = 0; i < numberOfEnemyToSpawn; i++)
        {
            Vector3 location = enemyLocationsToSpawn.GetRandom();
            GameObject enemyPrefab = enemies[Random.Range(0, enemies.Count)]; 
            GameObject enemyInstance = Instantiate(enemyPrefab, location, enemyPrefab.transform.rotation);
            if (!enemyInstance.GetComponent<Enemy>().IsAirBorne)
            {
                enemyInstance.transform.position = Utilities.RaycastHitPointPosition(enemyInstance.transform, walkableLayerMask);
            } else
            {
                Vector3 hitPosition = Utilities.RaycastHitPointPosition(enemyInstance.transform, walkableLayerMask);
                enemyInstance.transform.position = new Vector3(hitPosition.x, hitPosition.y + airborneEnemySpawnHeightOffset, hitPosition.z);
            }
            base.Spawn(enemyInstance);
            enemyLocationsToSpawn.Remove(location);
        }

        Transform mineralLocations = transform.Find("MineralLocations");
        mineralLocationsToSpawn = new ListHash<Vector3>();
        for (int i = 0; i < mineralLocations.childCount; i++)
        {
            if (mineralLocations.GetChild(i).gameObject.activeSelf)
            {
                mineralLocationsToSpawn.Insert(mineralLocations.GetChild(i).position);
            }
        }
        Debug.Assert(numberOfMineralsToSpawn <= mineralLocationsToSpawn.Count, "Cannot spawn more objects than locations.");

        for (int i = 0; i < numberOfMineralsToSpawn; i++)
        {
            Vector3 location = mineralLocationsToSpawn.GetRandom();
            GameObject mineralPrefab = minerals[Random.Range(0, minerals.Count)];
            GameObject mineralInstance = Instantiate(mineralPrefab, location, mineralPrefab.transform.rotation);
            Utilities.DetermineRotationBySurfaceNormal(mineralInstance.transform, walkableLayerMask);
            base.Spawn(mineralInstance);
            mineralLocationsToSpawn.Remove(location);
        }
    }
    
}
