using UnityEngine;

public class TentacleTargetFollow : MonoBehaviour
{
    public Transform creatureTransform; // Assign your creature's transform here
    private Vector3 initialOffset; // This will hold the initial offset
    public float followSpeed = 0.1f; // Adjust the speed for the delay effect
    public float idleMovementAmount = 0.5f; // The amount of random movement when idle
    public float idleMovementSpeed = 1.0f; // The speed of the random movement

    private Vector3 randomOffset;
    private Vector3 noiseOffset; // Unique offset for each tentacle

    private void Awake()
    {
        // Try to find a Mesh Renderer component on this GameObject and disable it
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    private void Start()
    {
        // Detach the tentacle from any parent object
        transform.parent = null;

        // Calculate and store the initial offset in local space
        initialOffset = creatureTransform.InverseTransformPoint(transform.position);

        // Generate a unique noise offset for this tentacle
        noiseOffset = new Vector3(Random.value * 100f, Random.value * 100f, Random.value * 100f);
    }

    private void Update()
    {
        // Transform the offset by the creature's current rotation
        Vector3 worldSpaceOffset = creatureTransform.TransformPoint(initialOffset);

        // Add unique random movement to make each tentacle move a bit when the creature is idle
        randomOffset = new Vector3(
            (Mathf.PerlinNoise(Time.time * idleMovementSpeed + noiseOffset.x, noiseOffset.x) - 0.5f) * idleMovementAmount,
            (Mathf.PerlinNoise(Time.time * idleMovementSpeed + noiseOffset.y, noiseOffset.y) - 0.5f) * idleMovementAmount,
            (Mathf.PerlinNoise(Time.time * idleMovementSpeed + noiseOffset.z, noiseOffset.z) - 0.5f) * idleMovementAmount
        );

        // Smoothly interpolate to the desired position with added random movement
        transform.position = Vector3.Lerp(transform.position, worldSpaceOffset + randomOffset, followSpeed * Time.deltaTime);
    }
}
