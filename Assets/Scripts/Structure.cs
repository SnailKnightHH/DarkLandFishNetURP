using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Connection;


public abstract class Structure : Interactable
{
    [SerializeField] private Image RadialProgress;
    [SerializeField] private TMP_Text ConstructionFinishedText;

    private float timeTakeToBuildOneitem = 1f;
    protected const float ACTION_DELAY = 1.5f;

    public bool BuildButtonReleased
    {
        get; set;
    }

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private bool isBuilt;
    private bool _isBuilt;
    public bool IsBuilt
    {
        get { return _isBuilt; }
    }

    private bool structureIsBeingBuilt;

    [SerializeField] protected StructureSO structureSO;
    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private int totalNumOfItemsRequired;
    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private int totalNumOfItemsAlreadyFilled;

    // Item, ValueTuple<numFilled, numRequired>
    private Dictionary<Item, ValueTuple<int, int>> resources = new Dictionary<Item, ValueTuple<int, int>>();
    public Dictionary<Item, ValueTuple<int, int>> Resources
    {
        get { return resources; }
    }

    [SerializeField] public GenericHorizontalCostUI UI;

    public override void OnStartServer()
    {
        base.OnStartServer();
        totalNumOfItemsRequired = structureSO.totalNumOfRequiredItems;
        totalNumOfItemsAlreadyFilled = 0;
        isBuilt = false;
        _isBuilt = isBuilt;
        foreach (var cost in structureSO.Cost)
        {
            resources.Add(cost.Item, ValueTuple.Create(0, cost.QuantityRequired));
        }
        structureIsBeingBuilt = false;        
    }

    public override void OnStartClient()
    {
        if (NetworkManager.IsServer) { return; }
        base.OnStartClient();
        _isBuilt = isBuilt;
        SyncResourcesDictAndBuiltStatusAndUIServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncResourcesDictAndBuiltStatusAndUIServerRpc(NetworkConnection networkConnection = null)
    {
        string[] serverResourcesItemNames = new string[structureSO.Cost.Count];
        int[] serverResourcesFilledQuantity = new int[structureSO.Cost.Count];
        int idx = 0;
        foreach (KeyValuePair<Item, ValueTuple<int, int>> entry in resources)
        {
            serverResourcesItemNames[idx] = new string(entry.Key.ItemName);            
            serverResourcesFilledQuantity[idx] = entry.Value.Item1;
            idx++;
        }
        RespondToSyncTargetRpc(networkConnection, serverResourcesItemNames, serverResourcesFilledQuantity, structureIsBeingBuilt);
    }

    [TargetRpc]
    private void RespondToSyncTargetRpc(NetworkConnection connection, string[] serverResourcesItemNames, int[] serverResourcesFilledQuantity, bool structureIsBeingBuilt)
    {
        this.structureIsBeingBuilt = structureIsBeingBuilt;
        // Paid structures should not have an issue here since resources length is 0
        for (int i = 0; i < serverResourcesItemNames.Length; i++)
        {
            resources.Add(SOManager.Instance.AllItemsNameToItemMapping[serverResourcesItemNames[i]],
                ValueTuple.Create(serverResourcesFilledQuantity[i], FindCostInfoWithItemName(structureSO.Cost, serverResourcesItemNames[i])));
        }
        foreach (var entry in resources)
        {
            UI.UpdateCostRatio(entry);
        }        
    }

    // Todo: Use dictionary instead
    private int FindCostInfoWithItemName(List<ItemCost> costs, string itemName)
    {
        foreach (var itemCost in costs)
        {
            if(itemCost.Item.ItemName == itemName)
            {
                return itemCost.QuantityRequired;
            }
        }
        Debug.Assert(false, "Resource not found with item name: " + itemName);
        return -1;
    }

    protected virtual void Start()
    {
        ConstructionFinishedText.enabled = false;
        RadialProgress.enabled = false;
        RadialProgress.fillAmount = 0;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddToTotalNumOfItemsAlreadyFilledServerRpc(int num)
    {
        totalNumOfItemsAlreadyFilled += num;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateResourcesEntryServerRpc(string itemName, int quantity)
    {
        UpdateResourcesEntryClientRpc(itemName, quantity);
    }

    [ObserversRpc]
    private void UpdateResourcesEntryClientRpc(string itemName, int quantity) // Note this method needs to be update, and not increment or add, since the executing client already updates its local copy 
    {
        if (SOManager.Instance.AllItemsNameToItemMapping.TryGetValue(itemName, out Item item))
        {
            if (resources.ContainsKey(item))
            {
                resources[item] = ValueTuple.Create(quantity, resources[item].Item2);                
            }
        }
    }

    private IEnumerator RadialProgressLerp(float filled, float total, Action<bool> updateFinishItemStatus)
    {
        float StartAmount = RadialProgress.fillAmount;
        float endAmount = filled / total; // Cast is not redundant, compiler is lying..
        float time = 0f;
        while (time < timeTakeToBuildOneitem)
        {
            if (BuildButtonReleased) {
                yield break;
            }
            RadialProgress.fillAmount = math.lerp(StartAmount, endAmount, time);
            time += Time.deltaTime;
            yield return null;
        }
        updateFinishItemStatus(true);
    }

    // Even though isBuilt.Value should be the source of truth, it is quite flaky if used alone due to network delay
    // Therefore, _isBuilt is used as source of truth, and updated immediately with RPCs. Network variable is used to sync data
    // so players who join late can have the correct information
    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void UpdateIsBuiltServerRpc()
    {
        isBuilt = true;
        _isBuilt = true;
        UpdateIsBuiltClientRpc();
    }

    [ObserversRpc]
    public void UpdateIsBuiltClientRpc()
    {        
        _isBuilt = true;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void UpdateStructureBuildStatusServerRpc(bool status)
    {
        structureIsBeingBuilt = status;
        UpdateStructureBuildStatusClientRpc(status);
    }

    [ObserversRpc]
    private void UpdateStructureBuildStatusClientRpc(bool status)
    {
        structureIsBeingBuilt = status;
    }

    protected abstract void FinishBuildingAction(Player player);

    public IEnumerator UseTool(Player player, ToolType currentlyEquippedToolType)
    {
        if (currentlyEquippedToolType != structureSO.ToolType) { yield break; }
        if (_isBuilt)
        {
            ConstructionFinishedText.text = "Construction Complete";
            ConstructionFinishedText.enabled = true;            
            yield return new WaitForSeconds(ACTION_DELAY);
            ConstructionFinishedText.enabled = false;
            yield break;
        }

        if (structureIsBeingBuilt) // Todo: race condition: 1. client spams build button, structureIsBeingBuilt takes time to sync to all clients -> UI glitch; 2. what if two players build simultaneously?
        {
            ConstructionFinishedText.text = "Structure is being built";
            ConstructionFinishedText.enabled = true;
            yield return new WaitForSeconds(ACTION_DELAY);
            ConstructionFinishedText.enabled = false;
            yield break;
        }

        player.IsBuilding = true;
        foreach (var entry in resources)
        {
            UI.UpdateCostRatio(entry);
        }
        if (IsServer || IsHost)
        {
            UpdateStructureBuildStatusClientRpc(true);
        } else
        {
            UpdateStructureBuildStatusServerRpc(true);
        }

        // Display initial progress
        if (player.GetComponent<NetworkObject>().OwnerId == LocalConnection.ClientId)
        {
            RadialProgress.enabled = true;
            RadialProgress.fillAmount = (float)((float)totalNumOfItemsAlreadyFilled / (float)totalNumOfItemsRequired);   
        }

        int totalFilled = totalNumOfItemsAlreadyFilled;

        // Todo: Maybe potentially refactor this? Since right now paid and unpaid is completely separate. Low priority though since it is working fine 
        if (structureSO.IsPaid)
        {
            for (int j = totalFilled; j < totalNumOfItemsRequired; j++)
            {
                if (player.GetComponent<NetworkObject>().OwnerId == LocalConnection.ClientId)
                {
                    totalFilled++;
                    bool _;
                    yield return StartCoroutine(RadialProgressLerp(totalFilled, totalNumOfItemsRequired, (_) => { }));
                    if (BuildButtonReleased)
                    {
                        break;
                    }
                    AddToTotalNumOfItemsAlreadyFilledServerRpc(1);
                }
            }

            if (IsHost)
            {
                UpdateStructureBuildStatusClientRpc(false);
            }
            else
            {
                UpdateStructureBuildStatusServerRpc(false);
            }

            player.IsBuilding = false;
            if (BuildButtonReleased)
            {
                RadialProgress.enabled = false;
                yield break;
            }

            if (totalFilled == totalNumOfItemsRequired)
            {
                UpdateIsBuiltServerRpc();
                RadialProgress.enabled = false;
                FinishBuildingAction(player);
            }
            yield break;
        }

        for (int i = 0; i < player.InventoryList.Count; i++)
        {
            if (player.isInventorySlotEmpty(i))
            {
                continue;
            }
            Item currentItem = player.InventoryList[i].Item1.GetComponent<PickupableObject>().objectItem;
            // If player has a required resource 
            if (resources.ContainsKey(currentItem))
            {
                int usedMaterial = 0;
                for (int j = 0; j < player.InventoryList[i].Item2; j++)
                {
                    // If structure does not need any more of this type of resource
                    if (resources[currentItem].Item1 == resources[currentItem].Item2)
                    {
                        break;
                    }

                    // This if statement is to only update UI if it's local player 
                    if (player.GetComponent<NetworkObject>().OwnerId == LocalConnection.ClientId)
                    {                        
                        // totalFilled is a local copy of totalNumOfItemsAlreadyFilled network variable so we have instant update 
                        totalFilled++;
                        bool isThisItemFinished = false;
                        yield return StartCoroutine(RadialProgressLerp(totalFilled, totalNumOfItemsRequired, (finished) => { isThisItemFinished = finished; }));
                        if (isThisItemFinished)
                        {
                            ValueTuple<int, int> newTuple = ValueTuple.Create(resources[currentItem].Item1 + 1, resources[currentItem].Item2);
                            resources[currentItem] = newTuple;
                            usedMaterial++;
                            UI.UpdateCostRatio(KeyValuePair.Create(currentItem, newTuple));
                        }
                        if (BuildButtonReleased) 
                        {
                            break;
                        }                        
  
                    }
                }

                // Sync totalNumOfItemsAlreadyFilled network variable as well as resources entry on all machines 
                if (usedMaterial > 0)
                {
                    AddToTotalNumOfItemsAlreadyFilledServerRpc(usedMaterial);
                    if (IsHost)
                    {
                        UpdateResourcesEntryClientRpc(currentItem.ItemName, resources[currentItem].Item1);
                    } else
                    {
                        UpdateResourcesEntryServerRpc(currentItem.ItemName, resources[currentItem].Item1);                        
                    }
                }

                // Player inventory update
                if (usedMaterial > 0)
                {
                    if (player.InventoryList[i].Item2 - usedMaterial == 0)
                    {
                        player.DeleteInventorySlotItem(i);
                    }
                    else
                    {
                        player.UpdateInventorySlot(i, Tuple.Create(player.InventoryList[i].Item1, player.InventoryList[i].Item2 - usedMaterial));
                    }

                }

                if (BuildButtonReleased)
                {
                    break;
                }
            }
        }

        
        if (IsHost)
        {
            UpdateStructureBuildStatusClientRpc(false);
        }
        else
        {
            UpdateStructureBuildStatusServerRpc(false);
        }

        player.IsBuilding = false;
        if (BuildButtonReleased)
        {
            RadialProgress.enabled = false;
            yield break;
        }

        if (totalFilled == totalNumOfItemsRequired)
        {
            UpdateIsBuiltServerRpc();
            RadialProgress.enabled = false;
            ConstructionFinishedText.enabled = true;
            FinishBuildingAction(player);
            
            yield return new WaitForSeconds(ACTION_DELAY);
            ConstructionFinishedText.enabled = false;
        } else
        {
            yield return new WaitForSeconds(ACTION_DELAY);
            RadialProgress.enabled = false;
        }
    }

    void Update()
    {
        
    }

    public override void Interact(GameObject gameobject)
    {
        // not used for now
    }

    public override void Interact2(GameObject gameobject)
    {
        // not used for now
    }
}
