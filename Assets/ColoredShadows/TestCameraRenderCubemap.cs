using System;
using UnityEngine;
using UnityEngine.Rendering;

public class TestCameraRenderCubemap : MonoBehaviour
{
    public RenderTexture renderTexture;
    private void Update()
    {
        Camera camera = GetComponent<Camera>();
        camera.RenderToCubemap(renderTexture);
        
    }
}
