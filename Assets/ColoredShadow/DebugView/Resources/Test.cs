using System;
using UnityEditor;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        if (SceneView.lastActiveSceneView == null)
        {
            return;
        }
        Camera sceneCamera = SceneView.lastActiveSceneView.camera;
        if (sceneCamera == null)
        {
            return;
        }

        transform.rotation =  Quaternion.LookRotation(sceneCamera.transform.position - transform.position) * Quaternion.Euler(90, -90, 90);
    }
}
