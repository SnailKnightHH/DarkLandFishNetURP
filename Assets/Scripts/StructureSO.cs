using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum StructureType
//{
//    Bridge,
//    Turret,
//    Mining
//}

[CreateAssetMenu(fileName = "Structure", menuName = "ScriptableObjects/Structure")]
public class StructureSO : ScriptableObject
{
    public string StructureName;
    public Sprite StructureIcon;
    //public StructureType StructureType;
    public ToolType ToolType;
    public List<ItemCost> Cost;
    public string Description;
    // For already paid structures, this should still not be zero since it determines how long the player can mine this mineral
    public int totalNumOfRequiredItems;
    public bool IsPaid;
    public string ItemName;
}
