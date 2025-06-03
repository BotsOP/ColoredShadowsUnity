using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class MoveInCircle : MonoBehaviour
{
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float radius = 1;
    [SerializeField] private bool useCustomForwardVector;
    [SerializeField] private Vector3 customForwardVector;

    private Vector3 startPos;
    private float offset;

    private void OnDrawGizmosSelected()
    {
        customForwardVector = customForwardVector.normalized;

        if (Application.isPlaying)
        {
            if (useCustomForwardVector)
            {
                Handles.DrawWireDisc(startPos, customForwardVector.normalized, radius);
                Gizmos.DrawSphere(OrbitPosition(startPos, customForwardVector.normalized, radius, Time.time * speed % 1), 0.1f);
            }
            else
            {
                Handles.DrawWireDisc(startPos, transform.forward, radius);
                Gizmos.DrawSphere(OrbitPosition(startPos, transform.forward, radius, Time.time * speed % 1), 0.1f);
            }
            
            return;
        }
        
        if (useCustomForwardVector)
        {
            Handles.DrawWireDisc(transform.position, customForwardVector.normalized, radius);
            Gizmos.DrawSphere(OrbitPosition(transform.position, customForwardVector.normalized, radius, Time.time * speed % 1), 0.1f);
        }
        else
        {
            Handles.DrawWireDisc(transform.position, transform.forward, radius);
            Gizmos.DrawSphere(OrbitPosition(transform.position, transform.forward, radius, Time.time * speed % 1), 0.1f);
        }
        
    }

    private void Start()
    {
        offset = Random.Range(0f, 1f);
        startPos = transform.position;
    }

    void Update()
    {
        if (useCustomForwardVector)
        {
            transform.position = OrbitPosition(startPos, customForwardVector.normalized, radius, (Time.time + offset) * speed % 1);
        }
        else
        {
            transform.position = OrbitPosition(startPos, transform.forward, radius, (Time.time + offset) * speed % 1);
        }
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
