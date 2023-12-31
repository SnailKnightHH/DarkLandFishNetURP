using StarterAssets;
using System;
using System.Collections.Generic;
using TMPro;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using FishNet.Connection;

public class StorageAndCrafting : Interactable, ITriggerCollider
{
    public static StorageAndCrafting Instance { get; private set; }

    [SerializeField] private Transform craftingCanvas;
    [HideInInspector] public Player playerReference;
    [SerializeField] private Transform CostContainer;

    public Dictionary<ItemType, Dictionary<Item, int>> Items
    {
        get; private set;
    }

    /*
     * Documenting all places ItemChanged delegate is invoked:
     * StorageAndCrafting.cs: deposit, take out
     * ItemEntryBtn.cs: craft
     */
    public Action<Item> ItemChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            Items = new Dictionary<ItemType, Dictionary<Item, int>>();
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType)))
            {
                Items.Add(itemType, new Dictionary<Item, int>());
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var item in SOManager.Instance.AllItems)
        {
            Items[item.ItemType].Add(item, 20); // temp: modify back to 0
        }
    }

    public override void OnStartClient()
    {
        if (NetworkManager.IsServer) { return; }
        base.OnStartClient();
        SyncItemsDictServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncItemsDictServerRpc(NetworkConnection networkConnection = null)
    {
        // List guarantees order, dictionary does not (order undefined)
        List<Dictionary<string, int>> ItemsInServer = new List<Dictionary<string, int>>(Items.Keys.Count);

        foreach (var itemsDict in Items)
        {
            Dictionary<string, int> items = new Dictionary<string, int>();
            foreach (var item in itemsDict.Value)
            {
                items.Add(item.Key.ItemName, item.Value);
            }
            ItemsInServer.Add(items);
        }
        RespondToSyncItemsDictTargetRpc(networkConnection, ItemsInServer);
    }

    [TargetRpc]
    private void RespondToSyncItemsDictTargetRpc(NetworkConnection connection, List<Dictionary<string, int>> ItemsInServer)
    {
        int idx = 0;
        foreach (var item in Items)
        {
            foreach (var itemInServer in ItemsInServer[idx])
            {
                item.Value.Add(SOManager.Instance.AllItemsNameToItemMapping[itemInServer.Key], itemInServer.Value);
            }
            idx++;
        }
    }


    public bool DepositItem(Item item, int count)
    {
        if (Items[item.ItemType].ContainsKey(item)) // Ideally local and remote copies are always in sync, so the checking at the local side should be enough
        {
            //Debug.Log("In deposit item: " + IsHost + " " + IsServer + " " + IsClient);
            if (IsHost)
            {
                DepositItemClientRpc(item.ItemName, count);                
            } else if (IsServer)
            {
                Items[item.ItemType][item] += count;
                ItemChanged?.Invoke(item);
                DepositItemClientRpc(item.ItemName, count);
            } else 
            {
                DepositItemServerRpc(item.ItemName, count);
            }
            return true;
        } else
        {
            return false;
        }        
    }

    [ServerRpc(RequireOwnership = false)]
    private void DepositItemServerRpc(string itemName, int count)
    {        
        DepositItemClientRpc(itemName, count); // let server executes logic on all clients 
    }

    [ObserversRpc]
    private void DepositItemClientRpc(string itemName, int count)
    {        
        Item item = SOManager.Instance.AllItemsNameToItemMapping[itemName];
        Items[item.ItemType][item] += count;
        ItemChanged?.Invoke(item);
    }

    public bool TakeOutItem(Item item)
    {        
        if (Items[item.ItemType][item] == 0) // Ideally local and remote copies are always in sync, so the checking at the local side should be enough
        {
            return false;
        }
        else
        {
            if (IsHost)
            {
                TakeOutItemClientRpc(item.ItemName);
            } else if (IsServer)
            {
                Items[item.ItemType][item] -= 1;
                ItemChanged?.Invoke(item);
                TakeOutItemClientRpc(item.ItemName);
            } else
            {
                TakeOutItemServerRpc(item.ItemName);
            }
            return true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeOutItemServerRpc(string itemName)
    {        
        TakeOutItemClientRpc(itemName); // let server executes logic on all clients 
    }

    [ObserversRpc]
    private void TakeOutItemClientRpc(string itemName)
    {        
        Item item = SOManager.Instance.AllItemsNameToItemMapping[itemName];
        Items[item.ItemType][item] -= 1;
        ItemChanged?.Invoke(item);
    }

    private int craftItemExecutedNumberOfTimes = 0;

    public void CraftItem(Item itemCache)  // this method for some reason is getting invoked more than once per button press
    {
        craftItemExecutedNumberOfTimes++;
        Debug.Assert(itemCache != null, "No item is selected, cannot craft item.");
        if (itemCache == null) { return; }

        // Check if we can craft
        foreach (ItemCost itemCost in itemCache.Cost)
        {
            Item requiredItem = itemCost.Item;
            int requiredQuantity = itemCost.QuantityRequired;
            int quantityLeft = Items[requiredItem.ItemType][requiredItem] - requiredQuantity;
            if (quantityLeft < 0)
            {
                // Todo: UI prompt
#if UNITY_EDITOR
                Debug.Log("Insufficient " + requiredItem.ItemName + ", cannot craft.");
#endif
                return;
            }
        }

        if (IsHost)
        {
            UpdateCostItemQuantityClientRpc(itemCache.ItemName);
            UpdateCraftedItemQuantityClientRpc(itemCache.ItemName);
        }
        else
        {
            UpdateCostItemQuantityServerRpc(itemCache.ItemName);
            UpdateCraftedItemQuantityServerRpc(itemCache.ItemName);
        }
#if UNITY_EDITOR
        Debug.Log(itemCache.ItemName + " crafting complete.");
        Debug.Log("craftItemExecutedNumberOfTimes: " + craftItemExecutedNumberOfTimes);
#endif
    }

    private void UpdateCosts(string ItemName, int quantityRequired, int newQuantity)
    {
        Transform costEntry = CostContainer.Find(ItemName + "Cost");
        // Some clients may be on another page
        if (costEntry == null) { return; }
        costEntry.Find("QuantityRatio").GetComponent<TMP_Text>().text = newQuantity.ToString() + " / " + quantityRequired;
        if (newQuantity < quantityRequired)
        {
            costEntry.Find("QuantityRatio").GetComponent<TMP_Text>().color = new Color(255, 0, 0);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateCostItemQuantityServerRpc(string itemName)
    {
        UpdateCostItemQuantityClientRpc(itemName);
    }

    [ObserversRpc]
    private void UpdateCostItemQuantityClientRpc(string itemName)
    {
        Item craftedItem = SOManager.Instance.AllItemsNameToItemMapping[itemName];
        foreach (ItemCost itemCost in craftedItem.Cost)
        {
            Item requiredItem = itemCost.Item;
            int requiredQuantity = itemCost.QuantityRequired;
            Items[requiredItem.ItemType][requiredItem] -= requiredQuantity;
            StorageAndCrafting.Instance.ItemChanged?.Invoke(requiredItem);
            UpdateCosts(requiredItem.name, requiredQuantity, Items[requiredItem.ItemType][requiredItem]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateCraftedItemQuantityServerRpc(string itemName)
    {
        UpdateCraftedItemQuantityClientRpc(itemName);
    }

    [ObserversRpc]
    private void UpdateCraftedItemQuantityClientRpc(string itemName)
    {
        Item craftedItem = SOManager.Instance.AllItemsNameToItemMapping[itemName];
        Items[craftedItem.ItemType][craftedItem]++;
        StorageAndCrafting.Instance.ItemChanged?.Invoke(craftedItem);
    }




    public void onEnterDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        Player player = other.gameObject.GetComponent<Player>();
        if (player != null && player.GetComponent<NetworkObject>().OwnerId == LocalConnection.ClientId)
        {
            player.SetCraftingTablePromptState(true);
        }
        
    }

    public void onStayDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        // not needed 
    }

    public void onExitDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        Player player = other.gameObject.GetComponent<Player>();
        if (player != null)
        {
            player.SetCraftingTablePromptState(false);
        }
    }

    // Deposit
    public override void Interact2(GameObject gameobject)
    {
        Player player = gameobject.GetComponent<Player>();
        if (!player.isInventorySlotEmpty(player.CurrentlyHeldIdx)
            && DepositItem(player.playerCurrentlyHeldObject.Item1.GetComponent<PickupableObject>().objectItem, player.playerCurrentlyHeldObject.Item2))
        {
            player.dropoff(true);
        }
    }

    // Open Crafting Menu
    public override void Interact(GameObject gameobject)
    {
        Canvas canvas = craftingCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {            
            canvas.enabled = !canvas.enabled;
            if (canvas.enabled)
            {
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(canvas.transform.Find("RawMaterialsBtn").gameObject, pointer, ExecuteEvents.pointerClickHandler);
            }                
        } 
        Player player = gameobject.GetComponent<Player>();
        if (player != null)
        {
            playerReference = player;
            // Freeze movement and camera 
            player.freezePlayerAndCameraMovement = !gameobject.GetComponent<Player>().freezePlayerAndCameraMovement;
            // make cursor visible
            player._input.newState = !player._input.newState;
            player._input.SetCursorState(player._input.newState);
            player.isUsingCraftingTable = !player.isUsingCraftingTable;
            player.SetCraftingTablePromptState(false);
        }        
    }
}
