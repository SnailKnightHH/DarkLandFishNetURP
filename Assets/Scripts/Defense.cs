using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Defense : PickupableObject, ITriggerCollider
{
    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] public bool isDeployed;
    private float rotateSpeed = 10f;
    [SerializeField] private LayerMask walkableLayerMask;
    private Vector3 HitGroundLocation = Vector3.zero;
    private Vector3 LastHitGroundLocation = Vector3.zero;
    [SerializeField] private float heightOffset; // if offset too small object will fly when deployed, don't know why
    
    // These two shouldn't be necessary but for some reason it is. I don't know who is setting rotation that causes jittering
    private Vector3 deployPosition;
    private Quaternion deployRotation;

    // Materials
    // Material Idx for RPCs: 1: canDeployMaterial; 2: cannotDeployMaterial; 3: DeployedMaterial
    [SerializeField] protected Material DeployedMaterial;
    [SerializeField] protected Material canDeployMaterial;
    [SerializeField] protected Material cannotDeployMaterial;
    protected MeshRenderer[] meshRenders;
    private int numOfObjectsInMeshTriggerCollider = 0;

    public override void DisableOrEnableMesh(bool state)
    {
        UpdateShowMeshServerRpc(state);
        foreach (MeshRenderer meshRenderer in meshRenders)
        {
            meshRenderer.enabled = state;
        }
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void ChangeMaterialServerRpc(int materialIdx)
    {
        changeMaterialLocal(materialIdx);
        ChangeMaterialObserversRpc(materialIdx);
    }

    [ObserversRpc(BufferLast = true)]
    private void ChangeMaterialObserversRpc(int materialIdx)
    {
        changeMaterialLocal(materialIdx);
    }

    private void changeMaterialLocal(int materialIdx)
    {
        Material EquipMaterial = DeployedMaterial;
        switch (materialIdx)
        {
            case 1:
                EquipMaterial = canDeployMaterial;
                break;
            case 2:
                EquipMaterial = cannotDeployMaterial;
                break;
            case 3:
                EquipMaterial = DeployedMaterial;
                break;
            default:
                break;
        }
        foreach (MeshRenderer meshRenderer in meshRenders)
        {
            meshRenderer.material = EquipMaterial;
        }
    }

    /// <summary>
    /// Determine if defense can be deployed and updates mesh accordingly by default.
    /// </summary>
    /// <returns>If the defense can be deployed</returns>
    public bool IfCanDeploy(bool updateMaterial = true)
    {
        if (numOfObjectsInMeshTriggerCollider == 0)
        {
            if (updateMaterial)
            {
                if (IsServer || IsHost)
                {
                    ChangeMaterialObserversRpc(1);
                }
                else
                {
                    ChangeMaterialServerRpc(1);
                }
            }
            return true;

        }
        else
        {
            if (updateMaterial)
            {
                if (IsServer || IsHost)
                {
                    ChangeMaterialObserversRpc(2);
                }
                else
                {
                    ChangeMaterialServerRpc(2);
                }
            }
            return false;
        }
    }

    private bool CanDeploy = false;

    public override void Dropoff(int numberOfItems)
    {
        pickupableBaseClasscollider.isTrigger = false;
        UpdatePickUpStatusServerRpc(false);
        SetCarryMountTransform(null);
        SetCameraViewTransform(null);
        UpdateNumberOfItemServerRpc(numberOfItems);
        UpdateIsDeployedServerRpc(true);
        RemoveClientOwnershipServerRpc();
        //transform.position = deployPosition;
        transform.rotation = deployRotation;
    }

    public override void PickUp(Transform carryMountPoint, Transform cameraTransform, Transform defenseCarryMountPoint)
    {
        if (IsServer || IsHost)
        {
            UpdateRBGravityClientRpc(false); // Gravity disabled when picked up, otherwise transform is glichy for other clients 
            UpdateIsTriggerClientRpc(true); // player may collider with object otherwise due to syncing delay
        }
        else
        {
            UpdateRBGravityServerRpc(false);
            UpdateIsTriggerServerRpc(true);
        }
        ChangeObjectOwnershipServerRpc();    // so spawned objects can follow player 
        SetCarryMountTransform(defenseCarryMountPoint);
        UpdateIsDeployedServerRpc(false);
        CanDeploy = IfCanDeploy();
        UpdatePickUpStatusServerRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateIsDeployedServerRpc(bool isDeployed)
    {
        this.isDeployed = isDeployed;
    }

    public virtual void onEnterDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        // Since there are multiple trigger colliders, we distinguish which one by initiatingGameObject's name
        if (!isDeployed && initiatingGameObject.name == "Mesh" && other.tag != Utilities.IGNORED_BY_TRIGGER_COLLIDER) // Todo: fixed string, not robust?
        {
            numOfObjectsInMeshTriggerCollider++;
            CanDeploy = IfCanDeploy();
        }
    }

    public virtual void onExitDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        if (!isDeployed && initiatingGameObject.name == "Mesh" && other.tag != Utilities.IGNORED_BY_TRIGGER_COLLIDER) // Todo: fixed string, not robust?
        {
            numOfObjectsInMeshTriggerCollider--;
            if (numOfObjectsInMeshTriggerCollider < 0) { numOfObjectsInMeshTriggerCollider = 0; }
            CanDeploy = IfCanDeploy();
        }
    }

    public virtual void onStayDetectionZone(Collider other, GameObject initiatingGameObject)
    {
    }

    protected override void PickulableObjectFollowTransform()
    {
        if (isDeployed) { return; }
        if (HitGroundLocation == Vector3.zero)
        {
            transform.position = followCarryMountTransform.position;
        } else
        {
            transform.position = new Vector3(followCarryMountTransform.position.x, HitGroundLocation.y + heightOffset, followCarryMountTransform.position.z);
        }
        //deployPosition = transform.position;
    }    
    

    private void OnDrawGizmos()
    {
        if (HitGroundLocation != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(HitGroundLocation, 0.5f);
            Gizmos.DrawLine(HitGroundLocation, HitGroundLocation + hitInfo.normal * 10f);
        }
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, HitGroundLocation + transform.up * 10f);
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawLine(transform.position, Vector3.up * 10f);

    }

    public virtual bool Deploy()
    {
        if (!CanDeploy) { return false; }

        if (IsServer || IsHost)
        {
            ChangeMaterialObserversRpc(3);
        } else
        {
            ChangeMaterialServerRpc(3);
        }
        rb.constraints = RigidbodyConstraints.FreezeAll;    // Todo: change back to none once pickup?
        return true;
    }

    private RaycastHit hitInfo; // debugging purpose

    protected virtual void Update()
    {
        if (!isDeployed)
        {
#if UNITY_EDITOR
            if (RotaryHeart.Lib.PhysicsExtension.Physics.Raycast(transform.position, -transform.up, out RaycastHit hitInfo, Mathf.Infinity, walkableLayerMask, QueryTriggerInteraction.Ignore, RotaryHeart.Lib.PhysicsExtension.PreviewCondition.Both)) {
#else
            if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hitInfo, Mathf.Infinity, walkableLayerMask, QueryTriggerInteraction.Ignore)) {
#endif
                this.hitInfo = hitInfo;
                HitGroundLocation = LastHitGroundLocation = hitInfo.point;
                Quaternion rotationBasedOnSurface = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, hitInfo.normal), rotateSpeed * Time.deltaTime);
                transform.rotation = rotationBasedOnSurface; // Quaternion.Euler(-rotationBasedOnSurface.eulerAngles.x, transform.eulerAngles.y, -rotationBasedOnSurface.eulerAngles.z);
                deployRotation = transform.rotation;
            } else
            {
                HitGroundLocation = LastHitGroundLocation;
                transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
            }
        } 
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        meshRenders = GetComponentsInChildren<MeshRenderer>();
        if (!isDeployed)
        {
            CanDeploy = IfCanDeploy();
        } else
        {
            CanDeploy = IfCanDeploy(false);
            changeMaterialLocal(3);
        }
    }
}
