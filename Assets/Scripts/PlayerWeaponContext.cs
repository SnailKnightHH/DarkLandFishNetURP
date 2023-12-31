using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponContext
{
    public PlayerWeaponContext(Iweapon weapon = null)
    {
        this.weapon = weapon;
    }

    private Iweapon weapon;
    public bool HasWeapon()
    {
        return weapon != null;
    }

    public void SetWeapon(Iweapon weapon, Transform transform)
    {
        this.weapon = weapon;
        weapon.PlayerTransform = transform;
    }
    

    public void WeaponAttack()
    {
        weapon.Attack();
    }

}
