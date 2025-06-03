using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class DynamicShadowManager : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] meshRenderers;
    [SerializeField] private Camera camera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Material materialFloor;
    [SerializeField] private float speed;
    [SerializeField] private Gradient gradient;
    [SerializeField] private MeshRenderer goToMeshRenderer;

    private MeshRenderer lastMeshRenderer;

    private Color lastColor;
    private float t;
    private bool hitObject;
    private Matrix4x4 viewMatrix;

    public void HitObject(MeshRenderer meshRenderer)
    {
        lastMeshRenderer?.material.SetFloat("_ShadowID", 1000);
        goToMeshRenderer?.material.SetColor("_Color", lastColor);
        lastMeshRenderer = goToMeshRenderer;
        goToMeshRenderer = meshRenderer;
        hitObject = true;
        t = 0;
    }

    private void Start()
    {
        lastColor = gradient.Evaluate(0);
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.material = new Material(meshRenderer.material);
            if (meshRenderer == goToMeshRenderer)
            {
                meshRenderer.material.SetColor("_Color", gradient.Evaluate(1));
                meshRenderer.material.SetFloat("_ShadowID", 1);
                continue;
            }
            meshRenderer.material.SetColor("_Color", lastColor);
            meshRenderer.material.SetFloat("_ShadowID", 1000);
        }
    }

    private void Update()
    {
        viewMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
        
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit) && Input.GetMouseButtonDown(0))
        {
            if (hit.transform.gameObject.tag == "DynamicShadowObject")
            {
                MeshRenderer meshRenderer = meshRenderers[Random.Range(0, meshRenderers.Length)];
                while (meshRenderer == goToMeshRenderer)
                {
                    meshRenderer = meshRenderers[Random.Range(0, meshRenderers.Length)];
                }
                HitObject(meshRenderer);
            }
        }

        if (hitObject)
        {
            t += Time.deltaTime * speed;
            materialFloor.SetFloat("_T", EaseOutElastic(t));

            if (t > 1)
            {
                lastMeshRenderer.material.SetFloat("_ShadowID", 1000);
                // hitObject = false;
                t = 1;
                // return;
            }
            
            // Transform world positions to clip space
            Vector3 fromPos = lastMeshRenderer.transform.position;
            Vector4 fromViewSpace = viewMatrix * new Vector4(fromPos.x, fromPos.y, fromPos.z, 1);
            Vector3 fromUVPos = new Vector3(fromViewSpace.x, fromViewSpace.y, fromViewSpace.z) / fromViewSpace.w;
            fromUVPos = fromUVPos * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);

            Vector3 toPos = goToMeshRenderer.transform.position;
            Vector4 toViewSpace = viewMatrix * new Vector4(toPos.x, toPos.y, toPos.z, 1);
            Vector3 toUVPos = new Vector3(toViewSpace.x, toViewSpace.y, toViewSpace.z) / toViewSpace.w;
            toUVPos = toUVPos * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);

            // Compute direction vector in screen space
            Vector2 direction = toUVPos - fromUVPos;
            Vector2 direction2 = fromUVPos - toUVPos;

            // Compute angle from Vector2.up (0,1), signed
            float fromAngle = Mathf.Atan2(direction2.x, direction2.y) * Mathf.Rad2Deg;
            float toAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

            // Normalize to [0, 360]
            if (fromAngle < 0) fromAngle += 360f;
            fromAngle = 360 - fromAngle;
            fromAngle += 360;
        
            if (toAngle < 0) toAngle += 360f;
            toAngle = 360 - toAngle;
            toAngle += 0.3f;
        
            lastMeshRenderer.material.SetFloat("_ShadowID", fromAngle);
            goToMeshRenderer.material.SetColor("_Color", lastColor);
            goToMeshRenderer.material.SetFloat("_ShadowID", toAngle);
            goToMeshRenderer.material.SetColor("_Color", gradient.Evaluate(EaseOutElastic(t) * 0.9f));
        }
    }
    
    float EaseOutElastic(float x)
    {
        float c4 = (2 * Mathf.PI) / 3;

        if (x == 0)
            return 0;
        if (x == 1)
            return 1;

        return Mathf.Pow(2, -15 * x) * Mathf.Sin((x * 10 - 0.75f) * c4) + 1;
    }
}
