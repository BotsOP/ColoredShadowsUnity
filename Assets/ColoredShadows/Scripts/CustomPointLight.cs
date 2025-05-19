using System;
using UnityEngine;

public class CustomPointLight : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, new Vector3(radius * 2, radius * 2, radius * 2));
        Color faceColor = Color.gray;
        faceColor.a = 0.1f;
        Gizmos.color = faceColor;
        Gizmos.DrawCube(transform.position, new Vector3(radius * 2, radius * 2, radius * 2));
    }

    public Color color;
    public float test;
    public int pointLightIndex;
    public float radius = 5;
    public int textureSize = 1024;
}
