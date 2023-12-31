using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
class ItemPrefabMapping
{
    public Item item;
    public GameObject prefab;
}

public class SOManager : MonoBehaviour
{
    [SerializeField] private List<Item> _allItems;
    
    public List<Item> AllItems
    {
        get
        {
            return _allItems;
        }
    }

    [SerializeField] private List<ItemPrefabMapping> AllPrefabsMapping;
    public Dictionary<Item, GameObject> ItemPrefabMapping;

    private Dictionary<string, Item> _allItemsNameToItemMapping = new Dictionary<string, Item>();
    public Dictionary<string, Item> AllItemsNameToItemMapping
    {
        get
        {
            return _allItemsNameToItemMapping;
        }
    }

    public static SOManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;

            foreach (Item item in _allItems) {
                _allItemsNameToItemMapping.Add(item.ItemName, item);
            }

            ItemPrefabMapping = new Dictionary<Item, GameObject>();
            foreach (ItemPrefabMapping itemPrefabMapping in AllPrefabsMapping)
            {
                ItemPrefabMapping.Add(itemPrefabMapping.item, itemPrefabMapping.prefab);
            }     

        }
    }

}
