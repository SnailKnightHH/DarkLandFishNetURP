using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Wrench : Tool // Todo: instead of inheritance, just make tool a concrete class, delete subclasses and have a scriptable object field that has tooltype -> save memory 
{
    public override ToolType ToolType { 
        get
        {
            return ToolType.Wrench;
        }
    }

}
