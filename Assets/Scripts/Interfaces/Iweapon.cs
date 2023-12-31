using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Iweapon
{
    public void Attack();
    Transform PlayerTransform
    {
        get; set;
    }

    Transform ShootTransform
    {
        get;
    }
}
