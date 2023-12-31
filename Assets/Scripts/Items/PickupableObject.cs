using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupableObject : Carryable, IThrowable
{
    [SerializeField] private float throwForce = 30;
    [SerializeField] private Item _item;

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private bool IsPickedUp;
    public bool isPickedUp => IsPickedUp;

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] public int NumOfItem = 1;

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    [HideInInspector] private bool showMesh = true;
    public bool ShowMesh => showMesh;

    public Item objectItem
    {
        get { return _item; }
    }
    protected Transform followCarryMountTransform;
    protected Transform followCameraViewTransform;   
    protected Rigidbody rb;

    protected MeshRenderer pickupableBaseClassmeshRenderer;
    protected Collider pickupableBaseClasscollider;

    protected AudioSource audioSource;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        pickupableBaseClassmeshRenderer = GetComponentInChildren<MeshRenderer>();
        pickupableBaseClasscollider = GetComponentInChildren<Collider>();
    }

    // Todo: what if server disconnects?
    public override void OnStartClient()
    {
        base.OnStartClient();
        DisableOrEnableMesh(showMesh);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePickUpStatusServerRpc(bool isPickedUp)
    {
        IsPickedUp = isPickedUp;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateShowMeshServerRpc(bool state)
    {
        showMesh = state;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateNumberOfItemServerRpc(int num)
    {
        NumOfItem = num;
    }

    // Parameter needs to be Transform (a reference type)
    public void SetCarryMountTransform(Transform followTransform) 
    {
        //Debug.Log("ServerRpc SetFollowTransformServerRpc called");
        this.followCarryMountTransform = followTransform;
    }

    public void SetCameraViewTransform(Transform followTransform)
    {
        this.followCameraViewTransform = followTransform;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeObjectOwnershipServerRpc(NetworkConnection sender = null)
    {
        GetComponent<NetworkObject>().GiveOwnership(sender); 
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveClientOwnershipServerRpc()
    {
        GetComponent<NetworkObject>().RemoveOwnership();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyNetworkObjectServerRpc()
    {
        base.Despawn();
    }

    //public void SetObjectPosition(Vector3 position)
    //{
    //    transform.position = position;
    //    SetObjectPositionServerRpc(position);
    //}

    //[ServerRpc(RequireOwnership = false)]
    //private void SetObjectPositionServerRpc(Vector3 position)
    //{
    //    transform.position = position;
    //}

    public virtual void DisableOrEnableMesh(bool state)
    {
        UpdateShowMeshServerRpc(state);
        pickupableBaseClassmeshRenderer.enabled = state;
    }

    [ObserversRpc(BufferLast = true)]
    protected void UpdateRBGravityClientRpc(bool ifUseGravity)
    {
        rb.useGravity = ifUseGravity;
        rb.constraints = ifUseGravity ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    protected void UpdateRBGravityServerRpc(bool ifUseGravity)
    {
        rb.useGravity = ifUseGravity;
        rb.constraints = ifUseGravity ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
        UpdateRBGravityClientRpc(ifUseGravity);
    }

    [ObserversRpc]
    protected void UpdateIsTriggerClientRpc(bool isTrigger)
    {
        pickupableBaseClasscollider.isTrigger = isTrigger;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    protected void UpdateIsTriggerServerRpc(bool isTrigger)
    {
        pickupableBaseClasscollider.isTrigger = isTrigger;
        UpdateIsTriggerClientRpc(isTrigger);
    }

    public virtual void Dropoff(int numberOfItems)
    {
        if (IsServer || IsHost)
        {
            UpdateRBGravityClientRpc(true);
            UpdateIsTriggerClientRpc(false);
        } else
        {
            UpdateRBGravityServerRpc(true);
            UpdateIsTriggerServerRpc(false);
        }
        UpdatePickUpStatusServerRpc(false);
        SetCarryMountTransform(null);
        SetCameraViewTransform(null);
        UpdateNumberOfItemServerRpc(numberOfItems);
        RemoveClientOwnershipServerRpc();
    }

    public virtual void PickUp(Transform carryMountPoint, Transform cameraTransform, Transform defenseCarryMountPoint)
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
        SetCarryMountTransform(carryMountPoint);
        SetCameraViewTransform(cameraTransform);
        UpdatePickUpStatusServerRpc(true);
    }

    private void LateUpdate()
    {
        if (followCarryMountTransform == null)
        {
            return;
        }

        PickulableObjectFollowTransform();
    }

    protected virtual void PickulableObjectFollowTransform()
    {
        print("Two transforms positions: " + transform.position + " " + followCarryMountTransform.position);
        transform.position = followCarryMountTransform.position;
        if (followCameraViewTransform != null)
        {
            transform.rotation = followCameraViewTransform.rotation;
        }        
    }

    public void Throw(Transform throwTransform)
    {
#if UNITY_EDITOR
        Debug.Log("Throw " + _item.ItemName + " reached");
#endif
        rb.AddRelativeForce(new Vector3(0, 0.5f, 1) * throwForce, ForceMode.VelocityChange); // Don't need to define item weight separately since ForceMode.Impulse takes rb.mass into consideration        
    }
}
