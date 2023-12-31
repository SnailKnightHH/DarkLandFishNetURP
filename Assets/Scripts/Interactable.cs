using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public abstract class Interactable : NetworkBehaviour
{
    public abstract void Interact(GameObject gameobject);

    public abstract void Interact2(GameObject gameobject);
}
