using System;
using ColoredShadows.Scripts;
using UnityEditor;
using UnityEngine;

public class TestDebugView : MonoBehaviour
{
    private Mesh[] numberMeshes;
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

        SetNumberMeshes();
        CustomLight[] customLights = FindObjectsByType<CustomLight>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        MeshRenderer[] meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            if (!meshRenderer.sharedMaterial.HasProperty("_ShadowID"))
                continue;
            
            Vector3 dir = sceneCamera.transform.position - meshRenderer.transform.position;
            float shadowID = meshRenderer.sharedMaterial.GetFloat("_ShadowID");
            Gizmos.color = Color.magenta;
            Quaternion lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-90, -90, 90);
            StringToMeshNumber(shadowID.ToString("0.#"), meshRenderer.transform.position, lookRotation);
        }
        
        foreach (CustomLight customLight in customLights)
        {
            Vector3 dir = sceneCamera.transform.position - customLight.transform.position;
            Gizmos.color = Color.green;
            Quaternion lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-90, -90, 90);
            StringToMeshNumber(customLight.addToShadowID.ToString("0.#"), customLight.transform.position, lookRotation);
        }
    }

    private void StringToMeshNumber(string str, Vector3 middlePos, Quaternion lookRotation)
    {
        char[] characters = str.ToCharArray();
        float charSpace = 1.5f;
        float charSize = 10;
        float meshesWidth = charSpace * (characters.Length - 1);
        if(str.Contains('.'))
            meshesWidth -= charSpace * 0.9f;
        middlePos -= lookRotation * Vector3.left * meshesWidth * 0.5f;
        foreach (char character in characters)
        {
            switch (character)
            {
                case '0':
                    Gizmos.DrawMesh(numberMeshes[0], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '1':
                    Gizmos.DrawMesh(numberMeshes[1], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '2':
                    Gizmos.DrawMesh(numberMeshes[2], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '3':
                    Gizmos.DrawMesh(numberMeshes[3], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '4':
                    Gizmos.DrawMesh(numberMeshes[4], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '5':
                    Gizmos.DrawMesh(numberMeshes[5], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '6':
                    Gizmos.DrawMesh(numberMeshes[6], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '7':
                    Gizmos.DrawMesh(numberMeshes[7], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '8':
                    Gizmos.DrawMesh(numberMeshes[8], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '9':
                    Gizmos.DrawMesh(numberMeshes[9], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '.':
                    middlePos -= lookRotation * Vector3.left * charSpace * 0.9f;
                    Gizmos.DrawMesh(numberMeshes[10], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
                case '-':
                    Gizmos.DrawMesh(numberMeshes[11], middlePos, lookRotation, Vector3.one * charSize);
                    middlePos += lookRotation * Vector3.left * charSpace;
                    break;
            }
        }
    }

    private void SetNumberMeshes()
    {
        numberMeshes = new Mesh[12];
        for (int i = 0; i < 10; i++)
        {
            numberMeshes[i] = Resources.Load<Mesh>(i.ToString());
        }
        numberMeshes[10] = Resources.Load<Mesh>("dot");
        numberMeshes[11] = Resources.Load<Mesh>("minus");
    }
}
