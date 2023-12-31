using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    RawMaterial,
    Gears,
    Defense,
    Weapons,
    Parts
}

public enum ItemRarity
{
    Common,
    Rare,
    Legendary
}

[Serializable]
public class ItemCost
{
    public Item Item;
    public int QuantityRequired;    
}

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Item")]
public class Item : ScriptableObject
{
    public string ItemName;
    public Sprite ItemIcon;
    public ItemType ItemType;
    public ItemRarity Rarity;    
    public List<ItemCost> Cost;
    public string Description;
}
