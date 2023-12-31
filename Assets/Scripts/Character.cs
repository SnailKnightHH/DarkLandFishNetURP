using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : NetworkBehaviour
{
    [Tooltip("Move speed of the character in m/s")]
    [SerializeField] public float speed;
    [SerializeField] protected int initialHealth;
    protected int curHealth;

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private int healthNetworkVariable;    
    protected Rigidbody rb;
    protected Collider collider;

    protected virtual void Start()
    {
        healthNetworkVariable = initialHealth;
        curHealth = initialHealth;
    }

    public virtual void ReceiveDamage(int damage)
    {
#if UNITY_EDITOR
        Debug.Log("In receive damage");
#endif
        DealDamageToHealthServerRpc(damage);
        if (healthNetworkVariable <= 0)
        {
            Die();
        }
    }

    public virtual async void ReceiveDamage(GameObject DamageDealer, int damage)
    {
#if UNITY_EDITOR
        Debug.Log("In receive damage");
#endif
        DealDamageToHealthServerRpc(damage);
        if (healthNetworkVariable <= 0)
        {
            Die();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DealDamageToHealthServerRpc(int damage)
    {
        healthNetworkVariable -= damage;
        curHealth -= damage;
#if UNITY_EDITOR
        Debug.Log(gameObject.name + " received damage: " + damage + ", health left: " + healthNetworkVariable);
#endif
    }

    protected abstract int Damage
    {
        get; 
    }
    // [SerializeField] private Effect effect;
    // [SerializeField] private List<Ability> Abilities;

    public abstract void Move();
    protected abstract void Die();
    // public virtual void CalcSpeed();
}
