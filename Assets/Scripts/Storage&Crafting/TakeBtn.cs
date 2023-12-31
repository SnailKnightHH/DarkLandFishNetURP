using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TakeBtn : MonoBehaviour
{
    private Dictionary<string, Item> ItemNameObjectMapping
    {
        get; set;
    }

    private Dictionary<ItemType, Dictionary<Item, int>> Items
    {
        get; set;
    }

    private void RefetchItems()
    {
        ItemNameObjectMapping = SOManager.Instance.AllItemsNameToItemMapping;
        Items = StorageAndCrafting.Instance.Items;
    }

    // Start is called before the first frame update
    void Start()
    {
        RefetchItems();
        //StorageAndCrafting.Instance.ItemChanged += ButtonResponse;
        //ButtonResponse(GetItem());
        GetComponent<Button>().onClick.AddListener(() => TakeOutItem());
        changeButtonColorOrTransparency(UnityEngine.Color.grey);
    }

    private void TakeOutItem()  // Todo: potentially simplify this logic
    {
        if (GetItemQuantity(GetItem()) > 0)
        {
            Player player = StorageAndCrafting.Instance.playerReference;
            int[] pickupIdices = player.DeterminePickupIdx(GetItem());
            // if player can pick up (ie. inventory has space) and storage has enough number to take out, ie. any spawned object will be in a player's hand
            // Todo: right now player can only take out one item at a time; may need to refactor relevant inventory function logic when user can take out more than one item in one click
            if (pickupIdices[0] != -1 && StorageAndCrafting.Instance.TakeOutItem(GetItem())) // For now pickupIdices[1] will always be -1 since that scenario will never occur
            {   
                if (player.isInventorySlotEmpty(pickupIdices[0])) 
                {
                    player.SpawnItem(GetItem().ItemName, player.CarryMountPoint.position, player.cameraTransform.rotation, 1, pickupIdices);
                }
                else 
                {
                    player.AddOneToInventoryList(pickupIdices[0]);
                }
            }
        }
    }


    private void changeButtonColorOrTransparency(UnityEngine.Color color)
    {
        ColorBlock cb = GetComponent<Button>().colors;
        cb.normalColor = color;
        GetComponent<Button>().colors = cb;
    }

    private Item GetItem()
    {
        ItemNameObjectMapping.TryGetValue(transform.parent.Find("ItemName").GetComponent<TMP_Text>().text, out Item item);
        return item;
    }

    private int GetItemQuantity(Item item)
    {
        return Items[item.ItemType][item];
    }

    // Somehow this button object is destroyed? not solved error, happens when deposit
    //private void ButtonResponse(Item item)
    //{
    //    if (GetItemQuantity(item) > 0)
    //    {
    //        changeButtonColorOrTransparency(UnityEngine.Color.white);
    //    }
    //    else
    //    {
    //        changeButtonColorOrTransparency(UnityEngine.Color.grey);
    //    }
    //}

    // Update is called once per frame
    void Update()
    {
        
    }
}
