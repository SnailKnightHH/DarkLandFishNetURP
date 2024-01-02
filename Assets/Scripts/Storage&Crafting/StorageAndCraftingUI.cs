using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageAndCraftingUI : MonoBehaviour
{
    public static StorageAndCraftingUI Instance { get; private set; }
    private Transform ItemListContainer;
    private Transform ItemTemplate;
    private Transform CostTemplate;
    public Transform ItemDetails;
    [SerializeField] private float templateHeight = 10;
    [SerializeField] public float templateWidth = 80;
    [SerializeField] private Button RawMaterialPageBtn;
    [SerializeField] private Button GearsPageBtn;
    [SerializeField] private Button DefensePageBtn;
    [SerializeField] private Button WeaponsPageBtn;
    [SerializeField] private Button PartsPageBtn;
    public Item CurrentlySelectedItem
    {
        get; set;
    }

    private Dictionary<ItemType, Dictionary<Item, int>> Items
    {
        get; set;
    }

    // Start is called before the first frame update
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            RawMaterialPageBtn.onClick.AddListener(() =>
            {
                RenderPage(ItemType.RawMaterial);
            });
            GearsPageBtn.onClick.AddListener(() =>
            {
                RenderPage(ItemType.Gears);
            });
            DefensePageBtn.onClick.AddListener(() =>
            {
                RenderPage(ItemType.Defense);
            });
            WeaponsPageBtn.onClick.AddListener(() =>
            {
                RenderPage(ItemType.Weapons);
            });
            PartsPageBtn.onClick.AddListener(() =>
            {
                RenderPage(ItemType.Parts);
            });
            GetComponent<Canvas>().enabled = false;
        }
    }

    private void Start()
    {
        StorageAndCrafting.Instance.ItemChanged += UpdatePage;
        RefetchItems();
    }

    private int GetItemQuantity(Item item)
    {
        return Items[item.ItemType][item];
    }

    private void UpdatePage(Item item)
    {
        Transform ItemListContainer = transform.Find("ItemListContainer");
        int childCount = ItemListContainer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            if (ItemListContainer.GetChild(i).Find("ItemName").GetComponent<TMP_Text>().text == item.ItemName)
            {
                ItemListContainer.GetChild(i).Find("Quantity").GetComponent<TMP_Text>().text = GetItemQuantity(item).ToString();
                break;
            }
        }
    }

    private void RefetchItems()
    {
        Items = StorageAndCrafting.Instance.Items;
    }

    private void RenderPage(ItemType itemType)
    {
        ItemListContainer = transform.Find("ItemListContainer");
        for (int i = 0; i < ItemListContainer.childCount; i++)
        {
            Destroy(ItemListContainer.GetChild(i).gameObject);
        }
        ItemTemplate = transform.Find("ItemTemplate");
        int idx = 0;

        /* 
         * For some reason Instantiate and Destroy does not finish immediately (at least GPU does not immediately render this on screen.
         * So later on when we try to ExecuteEvents.Execute, the child is still the old one (which should be deleted already).
         * So have to settle with this bit of ugly code for now. 
        */
        Transform firstChildEntry = null;
        Item firstChildItem = null;
        foreach (KeyValuePair<Item, int> item in Items[itemType])
        {
            Transform newEntry = Instantiate(ItemTemplate, ItemListContainer);
            RectTransform newEntryRectTransform = newEntry.GetComponent<RectTransform>();
            newEntryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * idx);
            newEntry.Find("ItemName").GetComponent<TMP_Text>().text = item.Key.ItemName;
            newEntry.Find("Quantity").GetComponent<TMP_Text>().text = item.Value.ToString();
            newEntry.Find("Icon").GetComponent<Image>().sprite = item.Key.ItemIcon;
            newEntry.gameObject.SetActive(true);
            // I know this is ugly...
            if (idx == 0)
            {
                firstChildEntry = newEntry;
                firstChildItem = item.Key;
            }
            idx++;
        }

        // By default display details for the first item on page 
        //Transform Description = ItemDetails.Find("Description");
        //Transform CostContainer = ItemDetails.Find("Description");
        //Transform CraftBtn = ItemDetails.Find("CraftBtn");
        Transform Description;
        Transform CostContainer;
        Transform CraftBtn;
        UIElementReferenceGrab(out Description, out CraftBtn, out CostContainer, out CostTemplate, firstChildItem);
        if (itemType == ItemType.RawMaterial)
        {
            CraftBtn.gameObject.SetActive(false);
        }
        else
        {
            CraftBtn.gameObject.SetActive(true);
        }
        if (firstChildItem == null) { return; }
        UpdateCostContainer(Description, CostContainer, CostTemplate, firstChildItem, templateWidth, firstChildItem.Description);

        //var pointer = new PointerEventData(EventSystem.current);
        //if (firstChildEntry != null)
        //{
        //    ExecuteEvents.Execute(firstChildEntry.Find("ItemEntryBtn").gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        //}        
    }

    public void CraftItem()  // this method for some reason is getting invoked more than once per button press
    {
        Debug.Assert(CurrentlySelectedItem != null, "Cannot craft null item");
        if (CurrentlySelectedItem == null) { return; }
        StorageAndCrafting.Instance.CraftItem(CurrentlySelectedItem);
    }

    public void UIElementReferenceGrab(out Transform ItemDescriptionField, out Transform CraftBtn, out Transform CostContainer, out Transform CostTemplate, Item item)
    {
        ItemDescriptionField = ItemDetails.transform.Find("Description");
        CraftBtn = ItemDetails.transform.Find("CraftBtn");
        // Make sure only one listener is subscribed 
        CraftBtn.GetComponent<Button>().onClick.RemoveAllListeners();
        CraftBtn.GetComponent<Button>().onClick.AddListener(CraftItem);
        CostContainer = ItemDetails.transform.Find("CostContainer");
        CostTemplate = ItemDetails.transform.parent.Find("CostTemplate");
        CurrentlySelectedItem = item;
    }

    public void UpdateCostContainer(Transform ItemDescriptionField, Transform CostContainer, Transform CostTemplate, Item item, float templateWidth, string description)
    {
        if (ItemDescriptionField == null || CostContainer == null || CostTemplate == null || item == null)
        {
            return;
        }
        ItemDescriptionField.GetComponent<TMP_Text>().text = description;
        for (int i = 0; i < CostContainer.childCount; i++)
        {
            Destroy(CostContainer.GetChild(i).gameObject);
        }

        int idx = 0;
        foreach (ItemCost itemCost in item.Cost)
        {
            Transform newEntry = Instantiate(CostTemplate, CostContainer);
            RectTransform newEntryRectTransform = newEntry.GetComponent<RectTransform>();
            newEntryRectTransform.anchoredPosition = new Vector2(templateWidth * idx, 0 * idx);
            idx++;
            //newEntry.Find("ItemIcon").GetComponent<Image>().sprite = itemCost.Item.ItemIcon; // no Icon yet
            Items.TryGetValue(itemCost.Item.ItemType, out var ItemDict);
            ItemDict.TryGetValue(itemCost.Item, out int TotalAvailable);
            newEntry.Find("QuantityRatio").GetComponent<TMP_Text>().text = TotalAvailable + " / " + itemCost.QuantityRequired;
            newEntry.name = itemCost.Item.ItemName + "Cost"; // eg. for Pistal, it could be: AmmoCost
            if (TotalAvailable < itemCost.QuantityRequired)
            {
                newEntry.Find("QuantityRatio").GetComponent<TMP_Text>().color = new Color(255, 0, 0);
            }
            newEntry.gameObject.SetActive(true);
        }
    }

}
