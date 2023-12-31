using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemEntryBtn : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private GameObject ItemDetails;
    private Item itemCache;

    private Dictionary<string, Item> ItemNameObjectMapping
    {
        get; set;
    }

    private Dictionary<ItemType, Dictionary<Item, int>> Items
    {
        get; set;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ItemNameObjectMapping == null)
        {
            Initialize();
        }
        ItemNameObjectMapping.TryGetValue(transform.parent.Find("ItemName").GetComponent<TMP_Text>().text, out Item item);

        Transform ItemDescriptionField;
        Transform CostContainer;
        Transform CostTemplate;
        Transform CraftBtn;
        StorageAndCraftingUI.Instance.UIElementReferenceGrab(out ItemDescriptionField, out CraftBtn, out CostContainer, out CostTemplate, item);
        StorageAndCraftingUI.Instance.UpdateCostContainer(ItemDescriptionField, CostContainer, CostTemplate, item, StorageAndCraftingUI.Instance.templateWidth, item.Description);
    }


    private void RefetchItems()
    {
        ItemNameObjectMapping = SOManager.Instance.AllItemsNameToItemMapping;
        Items = StorageAndCrafting.Instance.Items;
    }

    private void Initialize()
    {
        RefetchItems();
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (ItemNameObjectMapping == null)
        {
            Initialize();
        }        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
