using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ReportCollision : MonoBehaviour
{    
    private void OnTriggerEnter(Collider other)
    {
        (transform.parent.GetComponent<ITriggerCollider>()).onEnterDetectionZone(other, gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        (transform.parent.GetComponent<ITriggerCollider>()).onExitDetectionZone(other, gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        (transform.parent.GetComponent<ITriggerCollider>()).onStayDetectionZone(other, gameObject);
    }
}
