using System;
using UnityEditor;
using UnityEngine;

public class MoveInCircle : MonoBehaviour
{
    public float speed = 0.5f;
    public float radius = 1;

    private Vector3 startPos;

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Handles.DrawWireDisc(startPos, transform.forward, radius);
            Gizmos.DrawSphere(OrbitPosition(startPos, transform.forward, radius, Time.time * speed % 1), 0.1f);
            return;
        }
        Handles.DrawWireDisc(transform.position, transform.forward, radius);
        Gizmos.DrawSphere(OrbitPosition(transform.position, transform.forward, radius, Time.time * speed % 1), 0.1f);
    }

    private void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position = OrbitPosition(startPos, transform.forward, radius, Time.time * speed % 1);
    }
    
    public Vector3 OrbitPosition(Vector3 center, Vector3 direction, float radius, float t)
    {
        direction = direction.normalized;

        // Choose an up vector that is not colinear with the direction
        Vector3 up = Mathf.Abs(direction.y) < 0.999f ? Vector3.up : Vector3.right;

        // Create perpendicular basis vectors for the orbit plane
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, direction));
        Vector3 forward = Vector3.Cross(direction, right);

        // Calculate angle in radians
        float angle = t * Mathf.PI * 2f;

        // Compute orbit offset
        Vector3 offset = Mathf.Cos(angle) * right + Mathf.Sin(angle) * forward;

        return center + offset * radius;
    }
}
