using System;
using System.Collections.Generic;
using TMPro;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class GenericHorizontalCostUI : NetworkBehaviour
{
    [SerializeField] StructureSO structureSO;
    // Todo: Paid structures should not need to have these assigned in the editor, how to conditionally remove serialize field from editor?
    private RectTransform[] costPositions;
    private Transform CostTemplate;
    private Transform CostListContainer;

    // I think most cases cost count is <= 2
    private Dictionary<Item, int> elementsItemToIdxMapping = new Dictionary<Item, int>(2);
    private List<Transform> elementsList = new List<Transform>(2);
    

    private void Start()
    {
        if (!structureSO.IsPaid)
        {
            costPositions = transform.Find("CostPositions").GetComponentsInChildren<RectTransform>();
            CostTemplate = transform.Find("CostTemplate");
            CostListContainer = transform.Find("CostListContainer");
            CostTemplate.gameObject.SetActive(false);
        }
        canDisplay(false);
        PopulateDisplay();
        // Remember to call UpdateCostRatio() in the initialization of the gameobject that uses generic horizontal cost UI
    }

    public void canDisplay(bool ifDisplay)
    {
        transform.Find("StructureName").gameObject.SetActive(ifDisplay);
        if (structureSO.IsPaid) { return; }
        foreach (var transform in elementsList)
        {
            transform.gameObject.SetActive(ifDisplay);
        }
    }

    //public bool UIIsEnabled()
    //{
    //    if (elementsList.Count > 0)
    //    {
    //        return elementsList[0].gameObject.activeSelf;
    //    }
    //    return false;
    //}
    
    public void UpdateCostRatio(KeyValuePair<Item, ValueTuple<int, int>> newData)
    {
       if (IsHost)
       {
            UpdateCostRatioClientRpc(newData.Key.ItemName, newData.Value.Item1, newData.Value.Item2);
       } else
       {
            UpdateCostRatioServerRpc(newData.Key.ItemName, newData.Value.Item1, newData.Value.Item2);
       }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateCostRatioServerRpc(string itemName, int filled, int required)
    {
        UpdateCostRatioClientRpc(itemName, filled, required);
    }

    [ObserversRpc]
    private void UpdateCostRatioClientRpc(string itemName, int filled, int required)
    {
        Item item = SOManager.Instance.AllItemsNameToItemMapping[itemName];
        elementsList[elementsItemToIdxMapping[item]].Find("CostRatio").GetComponent<TMP_Text>().text
            = filled.ToString() + " / " + required.ToString();
    }

    private void PopulateDisplay()
    {
        transform.Find("StructureName").GetComponent<TMP_Text>().text = structureSO.StructureName;
        if (structureSO.IsPaid) { return; }

        int idx = 0;
        foreach (var resource in structureSO.Cost)
        {
            Transform newEntry = Instantiate(CostTemplate, CostListContainer);
            RectTransform newEntryRectTransform = newEntry.GetComponent<RectTransform>();
            //newEntryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * idx);
            newEntry.Find("CostImage").GetComponent<Image>().sprite = resource.Item.ItemIcon;
            newEntry.Find("CostRatio").GetComponent<TMP_Text>().text = 0 + " / " + resource.QuantityRequired.ToString();
            elementsItemToIdxMapping.Add(resource.Item, idx);
            elementsList.Add(newEntry);
            idx++;
        }

        switch (structureSO.Cost.Count)
        {
            case 1:
                elementsList[0].transform.position = costPositions[2].transform.position;               
                break;
            case 2:
                elementsList[0].transform.position = costPositions[1].transform.position;                
                elementsList[1].transform.position = costPositions[3].transform.position;                
                break;
            case 3:
                elementsList[0].transform.position = costPositions[1].transform.position;                
                elementsList[1].transform.position = costPositions[2].transform.position;                
                elementsList[2].transform.position = costPositions[3].transform.position;                
                break;
            case 4:
                elementsList[0].transform.position = costPositions[0].transform.position;                
                elementsList[1].transform.position = costPositions[1].transform.position;                
                elementsList[2].transform.position = costPositions[2].transform.position;                
                elementsList[3].transform.position = costPositions[3].transform.position;                          
                break;
            default:
                elementsList[0].transform.position = costPositions[0].transform.position;                
                elementsList[1].transform.position = costPositions[1].transform.position;                
                elementsList[2].transform.position = costPositions[2].transform.position;                
                elementsList[3].transform.position = costPositions[3].transform.position;                
                elementsList[4].transform.position = costPositions[4].transform.position;                
                break;
        }
    }

}
