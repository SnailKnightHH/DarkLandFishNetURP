using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities 
{
    public static string IGNORED_BY_TRIGGER_COLLIDER = "IgnoredByTriggerCollider";
    public static int SecondsToMilliseconds(float seconds)
    {
        return (int)(seconds * 1000);
    }

    public static bool GroundedCheck(Transform transform, float GroundedOffset, float GroundedRadius, LayerMask GroundLayers)
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        return Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    public static void DetermineRotationBySurfaceNormal(Transform transform, LayerMask walkableLayerMask)
    {
#if UNITY_EDITOR
        if (RotaryHeart.Lib.PhysicsExtension.Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hitInfo, Mathf.Infinity, walkableLayerMask, QueryTriggerInteraction.Ignore, RotaryHeart.Lib.PhysicsExtension.PreviewCondition.Both))
#else
        if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hitInfo, Mathf.Infinity, walkableLayerMask, QueryTriggerInteraction.Ignore))
#endif
        {
            Quaternion rotationBasedOnSurface = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            transform.SetPositionAndRotation(hitInfo.point, Quaternion.Euler(rotationBasedOnSurface.eulerAngles.x, transform.eulerAngles.y, rotationBasedOnSurface.eulerAngles.z));
        }
    }

    // Make sure to place objects/enemies in a place where there is nav mesh 
    public static Vector3 RaycastHitPointPosition(Transform transform, LayerMask walkableLayerMask)
    {
#if UNITY_EDITOR
        if (RotaryHeart.Lib.PhysicsExtension.Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hitInfo, Mathf.Infinity, walkableLayerMask, QueryTriggerInteraction.Ignore, RotaryHeart.Lib.PhysicsExtension.PreviewCondition.Both))
#else
        if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hitInfo, Mathf.Infinity, walkableLayerMask, QueryTriggerInteraction.Ignore))
#endif
        {
            return hitInfo.point;
        } else
        {
            return Vector3.zero;
        }
    }
}
