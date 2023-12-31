using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistal : PickupableObject, Iweapon
{
    [SerializeField] private Transform gunTipPoint;
    [SerializeField] private GameObject pistalBullet;
    private float shootDistance = 30f;
    [SerializeField] private int pistolDamage = 10;

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
            return PlayerTransform.GetComponent<Player>().cameraTransform;
        }
    }

    public void Attack()
    {
        AudioManager.Instance.PlayAudioDiscrete(NetworkObject, AudioManager.SoundName.pistol);
        EffectManager.Instance.PlayEffect(EffectName.MuzzleFlash, 2f, gunTipPoint.position, gunTipPoint.rotation, followCarryMountTransform.GetComponentInParent<NetworkObject>().NetworkManager.IsServer);

#if UNITY_EDITOR
        if (RotaryHeart.Lib.PhysicsExtension.Physics.Raycast(ShootTransform.position, ShootTransform.forward, out RaycastHit hitInfo, shootDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore, RotaryHeart.Lib.PhysicsExtension.PreviewCondition.Both))
#else
        if (Physics.Raycast(ShootTransform.position, ShootTransform.forward, out RaycastHit hitInfo, shootDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
#endif
        {
            //Debug.Log("pistol hits: " + hitInfo.transform.name);
            if (hitInfo.transform.GetComponentInParent<Enemy>() != null)
            {
                hitInfo.transform.GetComponentInParent<Enemy>().ReceiveDamage(gameObject, pistolDamage);
            }
        }
    }
}
