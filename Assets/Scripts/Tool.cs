using FishNet.Demo.AdditiveScenes;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    Wrench,
    Pickaxe
}

public abstract class Tool : PickupableObject
{
    public Structure structure
    {
        get; set;
    }


    public virtual ToolType ToolType { get; }

    public virtual IEnumerator UseTool(Player player)
    {
        if (structure != null)
        {
            yield return StartCoroutine(structure.UseTool(player, ToolType));
        }
    }

}
