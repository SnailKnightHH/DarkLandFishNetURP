using UnityEngine;

interface ITriggerCollider 
{
    public void onEnterDetectionZone(Collider other, GameObject initiatingGameObject);
    public void onExitDetectionZone(Collider other, GameObject initiatingGameObject);
    public void onStayDetectionZone(Collider other, GameObject initiatingGameObject);
}
