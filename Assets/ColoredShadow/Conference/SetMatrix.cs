using System;
using EasyButtons;
using UnityEngine;

public class SetMatrix : MonoBehaviour
{
    [SerializeField] private Camera setCamera;
    [SerializeField] private Material material;

    [Button]
    private void Set()
    {
        material.SetMatrix("_CamViewMatrix", setCamera.projectionMatrix * GetViewMatrix(setCamera.transform.position, setCamera.transform.rotation));
    }
    
    public static Matrix4x4 GetViewMatrix(Vector3 cameraPosition, Quaternion cameraRotation)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(cameraRotation));
        Matrix4x4 translationMatrix = Matrix4x4.Translate(-cameraPosition);
        Matrix4x4 viewMatrix = rotationMatrix * translationMatrix;
        viewMatrix.m20 *= -1;
        viewMatrix.m21 *= -1;
        viewMatrix.m22 *= -1;
        viewMatrix.m23 *= -1;
        
        return viewMatrix;
    }
}

