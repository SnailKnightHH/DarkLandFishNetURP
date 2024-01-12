using UnityEngine;

public class TentacleTargetFollow : MonoBehaviour
{
    public Transform creatureTransform; // Assign your creature's transform here
    public Vector3 offset; // This will hold the initial offset
    public float followSpeed = 0.1f; // Adjust the speed for the delay effect

    private void Start()
    {
        // Calculate and store the initial offset
        offset = transform.position - creatureTransform.position;
    }

    private void Update()
    {
        // Calculate the desired position of the target
        Vector3 desiredPosition = creatureTransform.position + offset;

        // Smoothly interpolate to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}
