using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ITrackable
{
    Transform TrackOrigin { get; }
}

// Type T: The type of Game object that you want to track
public class GenericColliderDetector<T> : ITriggerCollider
{
    private HashSet<GameObject> GOList = new HashSet<GameObject>(1);

    public GameObject lockedTarget
    {
        get; private set; 
    }

    private Transform detectorOrigin;

    // This should be the layer name of the type of gameobject that is tracking T; eg. T: player, layer mask name: "Enemy"
    private string LayerMaskToExclude;

    private Action<T> AdditionalActionAfterLockAcquired { get; set; }
    private Action<T> OnExitAdditionalAction { get; set; }

    public GenericColliderDetector(Transform detectorOrigin, string LayerMaskToExclude, Action<T> AdditionalActionAfterLockAcquired, Action<T> OnExitAdditionalAction)
    {
        this.detectorOrigin = detectorOrigin;
        this.LayerMaskToExclude = LayerMaskToExclude;
        this.AdditionalActionAfterLockAcquired = AdditionalActionAfterLockAcquired;
        this.OnExitAdditionalAction = OnExitAdditionalAction;
    }


    /*
        Detection:
        run raycast on GO of type T in detection zone
        if line of sight is established, set target to T
     */
    public virtual void onEnterDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        if (other.gameObject.GetComponentInParent<T>() != null)
        {
#if UNITY_EDITOR
            Debug.Log("entered detection zone");
#endif
            GOList.Add(other.transform.root.gameObject);
        }
    }
    public virtual void onExitDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        if (other.gameObject.GetComponentInParent<T>() != null)
        {
#if UNITY_EDITOR
            Debug.Log("left detection zone");
#endif
            GOList.Remove(other.transform.root.gameObject);
            if (lockedTarget != null // This can be executed multiple times since player mesh, player face direction, player root etc all satisfis the outer if
                && lockedTarget.GetComponent<NetworkObject>().ObjectId == other.transform.root.GetComponent<NetworkObject>().ObjectId)
            {
                lockedTarget = null;
                OnLockedTargetChanged?.Invoke();
            }
            OnExitAdditionalAction(other.gameObject.GetComponentInParent<T>());
        }
    }

    public void onStayDetectionZone(Collider other, GameObject initiatingGameObject)
    {
        // not used for now
    }

    public void raycastTargets()
    {
        foreach (GameObject GO in GOList)
        {
            T target = GO.GetComponent<T>();
#if UNITY_EDITOR
            if (RotaryHeart.Lib.PhysicsExtension.Physics.Raycast(detectorOrigin.position, GO.GetComponent<ITrackable>().TrackOrigin.position - detectorOrigin.position, out RaycastHit hit, Mathf.Infinity, ~LayerMask.NameToLayer(LayerMaskToExclude), QueryTriggerInteraction.Ignore, RotaryHeart.Lib.PhysicsExtension.PreviewCondition.Both))
#else
            if (Physics.Raycast(detectorOrigin.position, GO.GetComponent<ITrackable>().TrackOrigin.position - detectorOrigin.position, out RaycastHit hit, Mathf.Infinity, ~LayerMask.NameToLayer(LayerMaskToExclude), QueryTriggerInteraction.Ignore))
#endif
                {
                    if (hit.collider.gameObject.GetComponentInParent<T>() != null
                    && hit.collider.gameObject.GetComponentInParent<NetworkObject>().ObjectId == GO.GetComponent<NetworkObject>().ObjectId)
                {
                    lockedTarget = GO;
                    OnLockedTargetChanged?.Invoke();
                    AdditionalActionAfterLockAcquired(target);
                    return;
                }
            }
        }
        lockedTarget = null;
    }

    public Action OnLockedTargetChanged;

}
