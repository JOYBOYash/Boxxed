using UnityEngine;

public class RotatingSpikes : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Degrees per second")]
    public float rotationSpeed = 180f;

    [Header("Space")]
    public bool useLocalSpace = true;

    void Update()
    {
        Vector3 rotation = rotationAxis.normalized * rotationSpeed * Time.deltaTime;

        if (useLocalSpace)
        {
            transform.Rotate(rotation, Space.Self);
        }
        else
        {
            transform.Rotate(rotation, Space.World);
        }
    }
}