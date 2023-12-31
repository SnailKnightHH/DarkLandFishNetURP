using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnvironmentalHazardsManager : MonoBehaviour, ITriggerCollider
{
    public enum EnvironmentalHazardType
    {
        Swamp
    }

    private Dictionary<string, EnvironmentalHazardType> tagToHazardType;

    public static EnvironmentalHazardsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            tagToHazardType = new Dictionary<string, EnvironmentalHazardType>();
            tagToHazardType.Add("Swamp", EnvironmentalHazardType.Swamp);
        }
    }

    public void onEnterDetectionZone(Collider other, GameObject self)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.EnterEnvironmentalHazard(tagToHazardType[self.tag]); // Using game tags so we don't have to create many different classes to each type of enviromental hazard 
        }
    }

    public void onExitDetectionZone(Collider other, GameObject self)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.LeaveEnvironmentalHazard(tagToHazardType[self.tag]); // Using game tags so we don't have to create many different classes to each type of enviromental hazard 
        }
    }

    public void onStayDetectionZone(Collider other, GameObject self)
    {
        // Not used here
    }

}
