using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fist : MonoBehaviour, Iweapon
{
    private GameObject attackableObject;
    [SerializeField] private int fistDamage = 5; // Todo: Instead of a different field, hook up with player's damage (override from character class)

    public void Attack()
    {
        if (attackableObject != null)
        {
#if UNITY_EDITOR
            Debug.Log("Fist attack hit: " + attackableObject.name);
#endif
            if (attackableObject.GetComponentInParent<Enemy>() != null)
            {
                attackableObject.GetComponentInParent<Enemy>().ReceiveDamage(gameObject, fistDamage);
            }                        
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Transform PlayerTransform
    {
        get; set;
    }

    public Transform ShootTransform
    {
        get
        {
            // not needed
            return null;
        }
    }

    private void OnTriggerStay(Collider other)
    {
#if UNITY_EDITOR
        Debug.Log("attackable object " + other.gameObject.name + " in range.");
#endif
        attackableObject = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == attackableObject)
        {
#if UNITY_EDITOR
            Debug.Log("attackable object " + other.gameObject.name + " leaves range.");
#endif
            attackableObject = null;
        }
    }
}
