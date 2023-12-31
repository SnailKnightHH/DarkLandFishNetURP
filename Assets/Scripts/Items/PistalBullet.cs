using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

// not used at the moment
public class PistalBullet : NetworkBehaviour
{
    [SerializeField] private int damage = 10;
    private Vector3 flyDirection = Vector3.zero;

    public void SetFlyDirection(Vector3 direction)
    {
        flyDirection = direction;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (flyDirection != Vector3.zero)
        {
            transform.Translate(flyDirection);
        }   
    }

    private void OnTriggerEnter(Collider other)
    {
#if UNITY_EDITOR
        Debug.Log("Pistal bullet hit: " + other.name);
#endif
        if (other.GetComponent<Enemy>() != null)
        {
            other.GetComponent<Enemy>().ReceiveDamage(damage);
            DestroyBulletServerRpc();
        }        
    }

    private void DestroyBullet()
    {
        base.Despawn();
        // Todo: if there is no collider on environment, destroy bullet after a set timer
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyBulletServerRpc()
    {
        DestroyBullet();
    }
}
