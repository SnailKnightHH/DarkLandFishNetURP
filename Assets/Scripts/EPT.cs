using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Energy Pulse Turret
public class EPT : Defense
{
    private int maxAmmo = 50;
    private int curAmmo = 10;
    private float ammoRegenTime = 1f;
    private int damage = 5;
    private float attackInterval = 1f;
    private GameObject lockedEnemy;    
    [SerializeField] private Transform MeshTransform;
    [SerializeField] private Transform[] MuzzleFlashTransforms;
    

    private GenericColliderDetector<Enemy> colliderDetector;
    public override void onEnterDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        base.onEnterDetectionZone(other, initiatingGameObject);
        if (isDeployed)
        {
            colliderDetector.onEnterDetectionZone(other, initiatingGameObject);
        }         
    }

    public override void onExitDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        base.onExitDetectionZone(other, initiatingGameObject);
        if (isDeployed)
        {
            colliderDetector.onExitDetectionZone(other, initiatingGameObject);
        }
    }

    public override void onStayDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        base.onStayDetectionZone(other, initiatingGameObject);
        if (isDeployed)
        {
            colliderDetector.onStayDetectionZone(other, initiatingGameObject);
        }
    }

    bool regeningAmmo = false;
    bool linedUp = false;

    private void AmmoRegen()
    {
        if (((lockedEnemy == null && curAmmo < maxAmmo) || curAmmo == 0) && !regeningAmmo)
        {
            regeningAmmo = true;
            StartCoroutine(EPTAmmoRegen());            
        }
    }

    private IEnumerator EPTAmmoRegen()
    {
        yield return new WaitForSeconds(ammoRegenTime);
        curAmmo++;
        regeningAmmo = false;
    }

    bool attacking = false;
    private void Attack()
    {
        if (lockedEnemy != null && curAmmo > 0 && !attacking && linedUp)
        {
            attacking = true;
            foreach (Transform transform in MuzzleFlashTransforms)
            {
                EffectManager.Instance.PlayEffect(EffectName.MuzzleFlash, 2f, transform.position, transform.rotation, NetworkObject.NetworkManager.IsServer);
            }
            StartCoroutine(EPTAttack());
        }
    }

    private IEnumerator EPTAttack()
    {
        lockedEnemy.GetComponent<Enemy>().ReceiveDamage(damage);
        curAmmo--;
        yield return new WaitForSeconds(attackInterval);
        attacking = false;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        colliderDetector = new GenericColliderDetector<Enemy>(MeshTransform, "Defense", (enemy) => { }, (enemy) => { });
        colliderDetector.OnLockedTargetChanged += () => { lockedEnemy = colliderDetector.lockedTarget; };
    }

    private void Aim()
    {
        if (lockedEnemy != null)
        {
            //Vector3 targetPositionFlat = new Vector3(lockedEnemy.transform.position.x, MeshTransform.position.y, lockedEnemy.transform.position.z);
            Vector3 targetPositionFlat = lockedEnemy.transform.position;

            // Calculate the direction from this GameObject to the modified target position
            Vector3 direction = (targetPositionFlat - MeshTransform.position).normalized;

            // If there is a valid direction, look in that direction
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                //// Apply a 90-degree offset around the Y axis
                //Quaternion offsetRotation = Quaternion.Euler(0, -90, 0);
                Quaternion finalRotation = lookRotation;

                MeshTransform.rotation = Quaternion.RotateTowards(MeshTransform.rotation, finalRotation, Time.deltaTime * 20f);

                if (MeshTransform.rotation == finalRotation)
                {
                    linedUp = true;
                }
                else
                {
                    linedUp = false;
                }
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!isDeployed) { return; }
        if (lockedEnemy == null)
        {
            colliderDetector.raycastTargets();

        }
        AmmoRegen();
        Aim();
        Attack();        
    }

}
